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
    public static partial class PWBIO
    {
        #region BLOCK BY BLOCK DELETE

        private static Vector3 _lastDeletePreviewCellCenter;
        private static bool _hasLastDeletePreviewPosition;
        private static Vector3 _lastDeletePreviewHitNormal = Vector3.up;
        private static System.Collections.Generic.List<Vector3> _lastDeletePreviewMirroredCenters
            = new System.Collections.Generic.List<Vector3>();


        private static void BlockByBlockDeleteToggle()
        {
            if ((Event.current.type == EventType.KeyUp || Event.current.type == EventType.KeyDown))
            {
                if (Event.current.control && !Event.current.alt && !Event.current.shift)
                    _modularDeleteMode = true;
                else if (_modularDeleteMode && (!Event.current.control || Event.current.alt || Event.current.shift))
                {
                    _modularDeleteMode = false;
                    _hasLastDeletePreviewPosition = false;
                    _lastDeletePreviewMirroredCenters.Clear();
                    return;
                }
            }
        }

        private static bool PreviewBlockByBlockDelete(Camera camera, 
            out Vector3 localMousePos3D)
        {
            localMousePos3D = Vector3.zero;
            _blockDeleteTargets = new System.Collections.Generic.List<GameObject>();

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
                _lastDeletePreviewCellCenter = cellCenter;
                _hasLastDeletePreviewPosition = true;
                _lastDeletePreviewHitNormal = hitNormal;
            }
            else if (_hasLastDeletePreviewPosition)
            {
                if (Event.current.type == EventType.MouseMove)
                {
                    _hasLastDeletePreviewPosition = false;
                    _lastDeletePreviewMirroredCenters.Clear();
                    return false;
                }
                cellCenter = _lastDeletePreviewCellCenter;
                hitNormal = _lastDeletePreviewHitNormal;
            }
            else
            {
                return false;
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

            _lastDeletePreviewMirroredCenters = allCellPositions;

            foreach (var cellPos in allCellPositions)
            {
                var obj = FindObjectAtCellCenter(cellPos, halfStep);
                if (obj != null)
                {
                    var root = UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(obj);
                    if (root != null) obj = root;
                    if (!_blockDeleteTargets.Contains(obj))
                        _blockDeleteTargets.Add(obj);
                }
            }

            if (_blockDeleteTargets.Count == 0 && (Event.current.type != EventType.Repaint || !_hasLastDeletePreviewPosition))
                return false;

            if (Event.current.type != EventType.Repaint) return _blockDeleteTargets.Count > 0;

            foreach (var cellPos in allCellPositions)
            {
                var TRS = Matrix4x4.TRS(cellPos, GridManager.settings.rotation, toolSettings.moduleSize);
                Graphics.DrawMesh(cubeMesh, TRS, transparentRedMaterial2, layer: 0, camera);
            }

            return _blockDeleteTargets.Count > 0;
        }

        private static GameObject FindObjectAtCellCenter(Vector3 cellCenter, float halfStep)
        {
            var halfCellSize = BlockManager.settings.moduleSize / 2;
            var nearbyObjects = new System.Collections.Generic.List<GameObject>();
            var cellRotation = GridManager.settings.rotation;
            if (BlockManager.quarterTurns > 0)
                cellRotation *= Quaternion.AngleAxis(BlockManager.quarterTurns * 90, BlockManager.settings.upwardAxis);

            boundsOctree.GetColliding(cellCenter, halfCellSize, GridManager.settings.rotation,
                cellRotation, nearbyObjects);

            foreach (var obj in nearbyObjects)
            {
                if (obj == null) continue;
                if (!obj.activeInHierarchy) continue;
                if (!PaletteManager.selectedPalette.ContainsSceneObject(obj)) continue;

                var objCenter = BoundsUtils.GetBoundsRecursive(obj.transform).center;
                var centerDistance = (objCenter - cellCenter).magnitude;
                if (centerDistance <= halfStep) return obj;
            }
            return null;
        }

        private static void BlockByBlockDeleteInput()
        {
            if (Event.current.button != 0) return;
            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
            {
                if (_blockDeleteTargets == null || _blockDeleteTargets.Count == 0) return;
                foreach (var target in _blockDeleteTargets)
                {
                    EraseBlockObject(target);
                }
            }
            if (Event.current.type == EventType.MouseUp)
            {
                _hasLastDeletePreviewPosition = false;
                _lastDeletePreviewMirroredCenters.Clear();
            }
        }

        private static void EraseBlockObject(GameObject obj)
        {
            if (obj == null) return;
            var root = UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(obj);
            if (root != null) obj = root;

            var objCenter = BoundsUtils.GetBoundsRecursive(obj.transform).center;
            var cellCenter = SnapPositionToBlockCellCenter(objCenter);
            var toolSettings = BlockManager.settings;
            var cellSize = toolSettings.moduleSize;
            var cellRotation = GridManager.settings.rotation;
            if (BlockManager.quarterTurns > 0)
                cellRotation *= Quaternion.AngleAxis(BlockManager.quarterTurns * 90, toolSettings.upwardAxis);

            BlockManager.RemoveOccupiedCell(cellCenter, cellSize, cellRotation, out _);
#if UNITY_6000_3_OR_NEWER
            PWBCore.DestroyTempCollider(obj.GetEntityId());
#else
            PWBCore.DestroyTempCollider(obj.GetInstanceID());
#endif
            _boundsOctree?.Remove(obj);
            UnityEditor.Undo.DestroyObjectImmediate(obj);
        }
#endregion
    }
}
#pragma warning restore UDR0001
