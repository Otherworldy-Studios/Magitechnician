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
using System.Linq;
using UnityEngine;

namespace PluginMaster
{
    public static partial class PWBIO
    {

        private static Vector3 _lastBrushSelectPreviewCellCenter;
        private static bool _hasLastBrushSelectPreviewPosition;
        private static Vector3 _lastBrushSelectHitNormal = Vector3.up;
        private static System.Collections.Generic.HashSet<GameObject> _brushSelectStrokeProcessed
            = new System.Collections.Generic.HashSet<GameObject>();
        private static bool _brushSelectStrokeIsDeselecting;

        private static void PreviewBlockBrushSelectBlockByBlock(Camera camera,
            out Vector3 mousePos3D, out Vector3 localMousePos3D)
        {
            mousePos3D = Vector3.zero;
            localMousePos3D = Vector3.zero;

            var mousePos2D = Event.current.mousePosition;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePos2D);

            var nearbyObjects = new System.Collections.Generic.List<GameObject>();
            boundsOctree.GetColliding(nearbyObjects, mouseRay, float.MaxValue);

            GameObject targetObj = null;
            float minDistance = float.MaxValue;

            foreach (var obj in nearbyObjects)
            {
                if (obj == null) continue;
                if (!obj.activeInHierarchy) continue;
                if (!PaletteManager.selectedPalette.ContainsSceneObject(obj)) continue;

                var objBounds = BoundsUtils.GetBoundsRecursive(obj.transform);
                float distance;
                if (objBounds.IntersectRay(mouseRay, out distance))
                {
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        targetObj = obj;
                    }
                }
            }

            Vector3 hitNormal = Vector3.up;
            if (targetObj != null)
            {
                var targetBounds = BoundsUtils.GetBoundsRecursive(targetObj.transform);
                var hitPoint = mouseRay.GetPoint(minDistance);
                var localHit = hitPoint - targetBounds.center;
                var halfExtents = targetBounds.extents;
                float minFaceDist = float.MaxValue;
                var normals = new Vector3[]
                {
                    Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back
                };
                var extents = new float[]
                {
                    halfExtents.x, halfExtents.x, halfExtents.y, halfExtents.y, halfExtents.z, halfExtents.z
                };
                var components = new float[]
                {
                    localHit.x, -localHit.x, localHit.y, -localHit.y, localHit.z, -localHit.z
                };
                for (int i = 0; i < 6; i++)
                {
                    var faceDist = Mathf.Abs(extents[i] - components[i]);
                    if (faceDist < minFaceDist)
                    {
                        minFaceDist = faceDist;
                        hitNormal = normals[i];
                    }
                }
            }

            var toolSettings = BlockManager.settings;
            Vector3 cellCenter;

            if (targetObj != null)
            {
                var targetBounds = BoundsUtils.GetBoundsRecursive(targetObj.transform);
                var objCenter = targetBounds.center;
                cellCenter = SnapPositionToBlockCellCenter(objCenter, out localMousePos3D);
                mousePos3D = cellCenter;
                _lastBrushSelectPreviewCellCenter = cellCenter;
                _hasLastBrushSelectPreviewPosition = true;
                _lastBrushSelectHitNormal = hitNormal;
            }
            else if (_hasLastBrushSelectPreviewPosition)
            {
                if (Event.current.type == EventType.MouseMove)
                {
                    _hasLastBrushSelectPreviewPosition = false;
                    return;
                }
                cellCenter = _lastBrushSelectPreviewCellCenter;
                hitNormal = _lastBrushSelectHitNormal;
            }
            else
            {
                return;
            }

            var cellSize = toolSettings.moduleSize;
            var halfStep = Mathf.Min(cellSize.x, cellSize.y, cellSize.z) * 0.4999f;
            var cellRotation = GridManager.settings.rotation;
            if (BlockManager.quarterTurns > 0)
                cellRotation *= Quaternion.AngleAxis(BlockManager.quarterTurns * 90, toolSettings.upwardAxis);

            var brushSize = BlockToolModes.brushSize;
            var brushShape = BlockToolModes.selectedBrushShape;

            var cellPositions = BrushstrokeManager.GetBlockByBlockCellPositions(
                cellCenter, hitNormal, cellSize, cellRotation, brushSize, brushShape);

            var allCellPositions = new System.Collections.Generic.List<Vector3>(cellPositions);
            var processedPositions = new System.Collections.Generic.HashSet<Vector3>();
            foreach (var pos in cellPositions)
                processedPositions.Add(pos);

            foreach (var cellPos in cellPositions)
            {
                var mirroredPositions = GetMirrorAndAxisModesPositions(cellPos);
                foreach (var mirroredPos in mirroredPositions)
                {
                    if (processedPositions.Add(mirroredPos))
                        allCellPositions.Add(mirroredPos);
                }
            }

            var targetObjects = new System.Collections.Generic.List<GameObject>();
            foreach (var cellPos in allCellPositions)
            {
                var obj = FindObjectAtCellCenter(cellPos, halfStep);
                if (obj != null)
                {
                    var root = UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(obj);
                    if (root != null) obj = root;
                    if (!targetObjects.Contains(obj))
                        targetObjects.Add(obj);
                }
            }

            if (targetObjects.Count == 0
                && (Event.current.type != EventType.Repaint || !_hasLastBrushSelectPreviewPosition))
                return;

            if (Event.current.button == 0)
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    _brushSelectStrokeProcessed.Clear();

                    if (targetObjects.Count > 0)
                    {
                        var current = UnityEditor.Selection.objects.ToList();
                        _brushSelectStrokeIsDeselecting = current.Contains(targetObjects[0]);

                        foreach (var obj in targetObjects)
                        {
                            _brushSelectStrokeProcessed.Add(obj);
                            if (_brushSelectStrokeIsDeselecting)
                            {
                                current.Remove(obj);
                            }
                            else if (!current.Contains(obj))
                            {
                                current.Add(obj);
                            }
                        }

                        if (!_brushSelectStrokeIsDeselecting && !Event.current.shift)
                            UnityEditor.Selection.objects = targetObjects.ToArray();
                        else
                            UnityEditor.Selection.objects = current.ToArray();
                    }
                    else if (!Event.current.shift)
                    {
                        _brushSelectStrokeIsDeselecting = false;
                        UnityEditor.Selection.objects = new Object[0];
                    }
                }
                else if (Event.current.type == EventType.MouseDrag)
                {
                    if (targetObjects.Count > 0)
                    {
                        var current = UnityEditor.Selection.objects.ToList();
                        var changed = false;
                        foreach (var obj in targetObjects)
                        {
                            if (_brushSelectStrokeProcessed.Contains(obj)) continue;
                            _brushSelectStrokeProcessed.Add(obj);
                            changed = true;

                            if (_brushSelectStrokeIsDeselecting)
                                current.Remove(obj);
                            else if (!current.Contains(obj))
                                current.Add(obj);
                        }
                        if (changed)
                            UnityEditor.Selection.objects = current.ToArray();
                    }
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    _brushSelectStrokeProcessed.Clear();
                }
            }

            if (Event.current.type != EventType.Repaint) return;

            foreach (var cellPos in allCellPositions)
            {
                var TRS = Matrix4x4.TRS(cellPos, GridManager.settings.rotation, toolSettings.moduleSize);
                Graphics.DrawMesh(cubeMesh, TRS, snapBoxMaterial, layer: 0, camera);
            }
        }
    }
}
#pragma warning restore UDR0001
