/*
Copyright(c) Omar Duarte
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
using System.Collections.Generic;
using UnityEngine;

namespace PluginMaster
{
    public static partial class PWBIO
    {
        #region FLOOR MIRROR REFLECTION HELPERS
        private static Quaternion GetFloorMirrorReflectionRotation(bool isMirroredX, bool isMirroredZ)
        {
            if (!ModularToolModes.reflectRotation) return Quaternion.identity;

            var euler = new Vector3(0, 180f, 0);


            if (isMirroredX && !isMirroredZ)
            {
                euler.y = -90f;
            }
            else if (isMirroredZ && !isMirroredX)
            {
                euler.y = 90f;
            }
            return Quaternion.Euler(euler);
        }

        private static Vector3 GetFloorMirrorReflectionScale(bool isMirroredX, bool isMirroredZ)
        {
            if (!ModularToolModes.reflectScale) return Vector3.one;
            return new Vector3(isMirroredX ? -1f : 1f, 1f, isMirroredZ ? -1f : 1f);
        }
        #endregion

        #region FLOOR MIRRORED TRANSFORMS
        private static List<MirroredTransform> GetFloorMirroredTransforms(
            Vector3 baseCellCenter, Vector3 brushOffset)
        {
            bool mx = ModularToolModes.mirrorX;
            bool mz = ModularToolModes.mirrorZ;

            var result = new List<MirroredTransform>();
            if (!mx && !mz) return result;

            var origin = ModularToolModes.symmetryOrigin;
            var rotation = GridManager.settings.rotation;
            var invRot = Quaternion.Inverse(rotation);
            var local = invRot * (baseCellCenter - origin);

            var toolSettings = FloorManager.settings;
            var localBrushOffset = brushOffset;

            if (FloorManager.quarterTurns > 0)
                localBrushOffset = Quaternion.AngleAxis(
                    FloorManager.quarterTurns * 90,
                    toolSettings.upwardAxis) * localBrushOffset;

            if (mx)
            {
                var rotOffset = GetFloorMirrorReflectionRotation(isMirroredX: true, isMirroredZ: false);

                var mirroredOffset = toolSettings.subtractBrushOffset
                    ? (rotOffset) * (localBrushOffset * 0.5f)
                    : Vector3.zero;
                if (ModularToolModes.reflectScale && localBrushOffset.x != 0)
                {
                    mirroredOffset.x = -mirroredOffset.x;
                }

                var mirroredCenter = new Vector3(-local.x, local.y, local.z);
                result.Add(new MirroredTransform(
                    rotation * (mirroredCenter + mirroredOffset) + origin,
                    rotation * rotOffset * invRot,
                    GetFloorMirrorReflectionScale(true, false)));
            }
            if (mz)
            {
                var rotOffset = GetFloorMirrorReflectionRotation(isMirroredX: false, isMirroredZ: true);
                var mirroredOffset = toolSettings.subtractBrushOffset
                    ? rotOffset * (localBrushOffset * 0.5f)
                    : Vector3.zero;
                if (ModularToolModes.reflectScale && localBrushOffset.z != 0)
                {
                    mirroredOffset.z = -mirroredOffset.z;
                }
                var mirroredCenter = new Vector3(local.x, local.y, -local.z);
                result.Add(new MirroredTransform(
                    rotation * (mirroredCenter + mirroredOffset) + origin,
                    rotation * rotOffset * invRot,
                    GetFloorMirrorReflectionScale(false, true)));
            }
            if (mx && mz)
            {
                var rotOffset = GetFloorMirrorReflectionRotation(isMirroredX: true, isMirroredZ: true);
                var mirroredOffset = toolSettings.subtractBrushOffset
                    ? rotOffset * (localBrushOffset * 0.5f)
                    : Vector3.zero;
                if (ModularToolModes.reflectScale)
                {
                    if (localBrushOffset.x != 0) mirroredOffset.x = -mirroredOffset.x;
                    if (localBrushOffset.z != 0) mirroredOffset.z = -mirroredOffset.z;
                }

                var mirroredCenter = new Vector3(-local.x, local.y, -local.z);
                result.Add(new MirroredTransform(
                    rotation * (mirroredCenter + mirroredOffset) + origin,
                    rotation * rotOffset * invRot,
                    GetFloorMirrorReflectionScale(true, true)));
            }

            return result;
        }
        #endregion

        #region FLOOR CELL HELPERS
        private static bool IsFloorCellOccupied(Vector3 cellCenter, Vector3 halfCellSize,
            Quaternion itemRotation, double halfStep)
        {
            var nearby = new List<GameObject>();
            boundsOctree.GetColliding(cellCenter, halfCellSize,
                GridManager.settings.rotation, itemRotation, nearby);

            foreach (var obj in nearby)
            {
                if (obj == null || !obj.activeInHierarchy) continue;
                var objCenter = BoundsUtils.GetBoundsRecursive(obj.transform).center;
                if ((objCenter - cellCenter).magnitude > halfStep) continue;
                if (PaletteManager.selectedPalette.ContainsSceneObject(obj)) return true;
            }
            return false;
        }

        private static void PreviewFloorMirroredTile(Camera camera,
            MirroredTransform mt,
            BrushstrokeItem strokeItem,
            Quaternion itemRotation,
            GameObject prefab,
            FloorSettings toolSettings,
            BrushSettings brush,
            double halfStep)
        {
            var mirroredItemRotation = mt.rotationOffset * Quaternion.Inverse(itemRotation);
            var cellCenter = toolSettings.subtractBrushOffset
                ? mt.position
                : mt.position + mirroredItemRotation * brush.localPositionOffset;
            var halfCellSize = toolSettings.moduleSize / 2;

            if (IsFloorCellOccupied(cellCenter, halfCellSize, mirroredItemRotation, halfStep)) return;

            var mirroredScaleMult = Vector3.Scale(strokeItem.scaleMultiplier, mt.scaleMultiplier);
            var itemPosition = GetFloorItemPosition(prefab, mirroredScaleMult, mirroredItemRotation,
                cellCenter, toolSettings.moduleSize);
            var previewRotation = mirroredItemRotation * Quaternion.Inverse(prefab.transform.rotation);
            var translateMatrix = Matrix4x4.Translate(-prefab.transform.position);
            var rootToWorld = Matrix4x4.TRS(itemPosition, previewRotation, mirroredScaleMult) * translateMatrix;
            var reverseTriangles = (mt.scaleMultiplier.x * mt.scaleMultiplier.y * mt.scaleMultiplier.z) < 0;
            var layer = toolSettings.overwritePrefabLayer ? toolSettings.layer : prefab.layer;

            PreviewBrushItem(prefab, rootToWorld, layer, camera,
                redMaterial: false, reverseTriangles: reverseTriangles, flipX: false, flipY: false);

            var itemScale = Vector3.Scale(prefab.transform.localScale, mirroredScaleMult);
            _paintStroke.Add(new PaintStrokeItem(prefab, strokeItem.settings.guid, itemPosition,
                mirroredItemRotation * prefab.transform.rotation, itemScale, layer, toolSettings.parent,
                surface: null, flipX: false, flipY: false));
        }
        #endregion
    }
}
#pragma warning restore UDR0001
