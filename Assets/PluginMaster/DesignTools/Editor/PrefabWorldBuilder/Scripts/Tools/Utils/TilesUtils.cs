/*
Copyright (c) Omar Duarte
Unauthorized copying of this file, via any medium is strictly prohibited.
Writen by Omar Duarte.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#pragma warning disable UDR0001
using UnityEngine;

namespace PluginMaster
{
    public static class TilesUtils
    {
        public enum SizeType
        {
            SMALLEST_OBJECT,
            BIGGEST_OBJECT,
            CUSTOM
        }

        public static Vector3 GetCellSize(SizeType cellSizeType, MultibrushSettings multibrush,
            Vector3 DefaultValue, bool subtractBrushOffset)
        {
            if (cellSizeType == SizeType.CUSTOM) return DefaultValue;

            void SubtractBrushOffset(ref Vector3 size, MultibrushItemSettings brush)
            {
                var dv = size - brush.localPositionOffset;
#if UNITY_2021_1_OR_NEWER
                dv.x = System.MathF.Round(dv.x, digits: 5);
                dv.y = System.MathF.Round(dv.y, digits: 5);
                dv.z = System.MathF.Round(dv.z, digits: 5);
#else
                dv.x = (float)System.Math.Round(dv.x, digits: 5);
                dv.y = (float)System.Math.Round(dv.y, digits: 5);
                dv.z = (float)System.Math.Round(dv.z, digits: 5);
#endif
                if (dv.x <= 0) dv.x = size.x;
                if (dv.y <= 0) dv.y = size.y;
                if (dv.z <= 0) dv.z = size.z;
                size = dv;
            }

            var cellSize = Vector3.one * (cellSizeType == SizeType.SMALLEST_OBJECT
                   ? float.MaxValue : float.MinValue);
            foreach (var item in multibrush.items)
            {
                var prefab = item.prefab;
                if (prefab == null) continue;
                var scaleMultiplier = cellSizeType == SizeType.SMALLEST_OBJECT
                    ? item.minScaleMultiplier : item.maxScaleMultiplier;
                var bounds = BoundsUtils.GetBoundsRecursive(prefab.transform,
                    prefab.transform.rotation * Quaternion.Euler(multibrush.eulerOffset), ignoreDissabled: true,
                    BoundsUtils.ObjectProperty.BOUNDING_BOX, recursive: true, useDictionary: false);
                var localSize = bounds.size;
                if (subtractBrushOffset)
                    SubtractBrushOffset(ref localSize, item);
                var itemSize = Vector3.Scale(localSize, scaleMultiplier);
                cellSize = cellSizeType == SizeType.SMALLEST_OBJECT
                    ? Vector3.Min(cellSize, itemSize) : Vector3.Max(cellSize, itemSize);
            }
            return cellSize;
        }

        public static Vector3 GetCellSize(SizeType cellSizeType, BrushSettings brush,
            AxesUtils.SignedAxis upwardAxis, AxesUtils.SignedAxis forwardAxis,
            Vector3 defaultValue, bool tangentSpace, int quarterTurns, bool subtractBrushOffset)
        {
            if (brush == null) return defaultValue;

            if (cellSizeType == SizeType.CUSTOM)
            {
                return defaultValue;
            }

            var cellSize = Vector3.one * (cellSizeType == SizeType.SMALLEST_OBJECT
                    ? float.MaxValue : float.MinValue);
            if (ToolController.current == ToolController.Tool.TILING && ToolController.editMode
                && PWBIO.selectedPersistentTilingData != null)
            {
                var prefabs = new System.Collections.Generic.HashSet<GameObject>();
                var objSet = PWBIO.selectedPersistentTilingData.objectSet;
                var scaleMultiplier = cellSizeType == SizeType.SMALLEST_OBJECT
                        ? brush.minScaleMultiplier : brush.maxScaleMultiplier;

                foreach (var obj in objSet)
                {
                    if (obj == null) continue;
                    var objSize = BoundsUtils.GetBoundsRecursive(obj.transform,
                        obj.transform.rotation * Quaternion.Euler(brush.eulerOffset)).size;
                    cellSize = cellSizeType == SizeType.SMALLEST_OBJECT
                        ? Vector3.Min(cellSize, objSize) : Vector3.Max(cellSize, objSize);
                    var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(obj);
                    if (prefab == null) continue;
                    if (prefabs.Contains(prefab)) continue;
                    prefabs.Add(prefab);
                    var prefabSize = Vector3.Scale(BoundsUtils.GetBoundsRecursive(prefab.transform,
                       prefab.transform.rotation * Quaternion.Euler(brush.eulerOffset)).size, scaleMultiplier);
                    cellSize = cellSizeType == SizeType.SMALLEST_OBJECT
                        ? Vector3.Min(cellSize, prefabSize) : Vector3.Max(cellSize, prefabSize);
                }
            }
            else if (brush is MultibrushSettings)
            {
                var multibrush = brush as MultibrushSettings;
                cellSize = GetCellSize(cellSizeType, multibrush, defaultValue, subtractBrushOffset);
            }

            if (cellSize == Vector3.one * float.MaxValue || cellSize == Vector3.one * float.MinValue) return defaultValue;
            var rotation = Quaternion.Euler(AxesUtils.SignedAxis.GetEulerAnglesFromAxes(forwardAxis, upwardAxis));

            if (tangentSpace)
            {
                if (upwardAxis.axis == AxesUtils.Axis.Y) cellSize.y = cellSize.z;
                else if (upwardAxis.axis == AxesUtils.Axis.X)
                {
                    cellSize.x = cellSize.y;
                    cellSize.y = cellSize.z;
                }
            }
            else
            {
                cellSize = rotation * cellSize;
                if (quarterTurns > 0) cellSize = Quaternion.AngleAxis(quarterTurns * 90, upwardAxis) * cellSize;
                cellSize.x = Mathf.Abs(cellSize.x);
                cellSize.y = Mathf.Abs(cellSize.y);
                cellSize.z = Mathf.Abs(cellSize.z);
            }
            if (Mathf.Approximately(cellSize.x, 0)) cellSize.x = 0.5f;
            if (Mathf.Approximately(cellSize.y, 0)) cellSize.y = 0.5f;
            if (Mathf.Approximately(cellSize.z, 0)) cellSize.z = 0.5f;
            return cellSize;
        }
    }
}
#pragma warning restore UDR0001
