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

        private static System.Collections.Generic.List<GameObject> _regionSelectTargets
            = new System.Collections.Generic.List<GameObject>();
        private static BlockToolModes.MoveNormalDirection _regionNormalDirection
            = BlockToolModes.MoveNormalDirection.UP;


        private static void PreviewBlockRegionSelect(Camera camera,
            out Vector3 mousePos3D, out Vector3 localMousePos3D)
        {
            mousePos3D = Vector3.zero;
            localMousePos3D = Vector3.zero;
            _regionSelectTargets.Clear();

            var mousePos2D = Event.current.mousePosition;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePos2D);

            if (!PWBToolRaycast(mouseRay, out RaycastHit raycastHit, out _,
                maxDistance: float.MaxValue, layerMask: -1, paintOnPalettePrefabs: true,
                castOnMeshesWithoutCollider: true, createTempColliders: true,
                exceptions: null, ignoreSceneColliders: true))
            {
                return;
            }

            var toolSettings = BlockManager.settings;
            var cellSize = toolSettings.moduleSize;
            var cellRotation = GridManager.settings.rotation;
            if (BlockManager.quarterTurns > 0)
                cellRotation *= Quaternion.AngleAxis(BlockManager.quarterTurns * 90, toolSettings.upwardAxis);

            var hitPointFaceCenter = SnapPositionToCellFaceCenter(raycastHit.point);
            _regionNormalDirection = GetMoveNormalDirectionFromHitNormal(raycastHit.normal);
            var hitCellCenter = GetCellCenterFromFaceCenterAndNormalDirection(hitPointFaceCenter,
                _regionNormalDirection);
            mousePos3D = hitCellCenter;

            var step = GridManager.settings.step;
            if (!BlockManager.IsCellOccupied(hitCellCenter, step)) return;

            var visited = new System.Collections.Generic.HashSet<Vector3Int>();
            var queue = new System.Collections.Generic.Queue<Vector3Int>();
            visited.Add(Vector3Int.zero);
            queue.Enqueue(Vector3Int.zero);

            var halfStep = Mathf.Min(cellSize.x, cellSize.y, cellSize.z) * 0.4999f;

            while (queue.Count > 0)
            {
                var coord = queue.Dequeue();
                var worldPos = hitCellCenter + cellRotation * Vector3.Scale(coord, step);

                if (BlockManager.IsCellOccupied(worldPos, step, out GameObject[] objects, out _))
                {
                    foreach (var obj in objects)
                    {
                        var root = UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(obj);
                        if (root != null && !_regionSelectTargets.Contains(root))
                            _regionSelectTargets.Add(root);
                        else if (root == null && !_regionSelectTargets.Contains(obj))
                            _regionSelectTargets.Add(obj);
                    }
                }

                var neighbors = GetRegionSelectNeighborOffsets();
                foreach (var offset in neighbors)
                {
                    var nextCoord = coord + offset;
                    if (visited.Contains(nextCoord)) continue;
                    visited.Add(nextCoord);
                    var nextWorldPos = hitCellCenter + cellRotation * Vector3.Scale(nextCoord, step);
                    if (IsCellConnectedForRegionSelect(hitCellCenter, nextWorldPos, step, cellRotation))
                        queue.Enqueue(nextCoord);
                }
            }

            if (Event.current.type == EventType.Repaint)
            {
                foreach (var obj in _regionSelectTargets)
                {
                    if (obj == null) continue;
                    var objCenter = BoundsUtils.GetBoundsRecursive(obj.transform).center;
                    var snapped = SnapPositionToBlockCellCenter(objCenter);
                    var TRS = Matrix4x4.TRS(snapped, cellRotation, cellSize);
                    Graphics.DrawMesh(cubeMesh, TRS, snapBoxMaterial, layer: 0, camera);
                }
            }
        }

        private static System.Collections.Generic.List<Vector3Int> GetRegionSelectNeighborOffsets()
        {
            var offsets = new System.Collections.Generic.List<Vector3Int>();

            Vector3Int t1, t2;
            if (_regionNormalDirection == BlockToolModes.MoveNormalDirection.UP
                || _regionNormalDirection == BlockToolModes.MoveNormalDirection.DOWN)
            { t1 = Vector3Int.right; t2 = Vector3Int.forward; }
            else if (_regionNormalDirection == BlockToolModes.MoveNormalDirection.LEFT
                || _regionNormalDirection == BlockToolModes.MoveNormalDirection.RIGHT)
            { t1 = Vector3Int.forward; t2 = Vector3Int.up; }
            else { t1 = Vector3Int.right; t2 = Vector3Int.up; }

            offsets.Add(t1); offsets.Add(-t1); offsets.Add(t2); offsets.Add(-t2);
            if (BlockToolModes.regionSelectNeighborSearchingDirections
                == BlockToolModes.RegionSelectNeighborSearchingDirections.EIGHT_DIRECTIONS)
            {
                offsets.Add(t1 + t2); offsets.Add(t1 - t2); offsets.Add(-t1 + t2); offsets.Add(-t1 - t2);
            }
            return offsets;
        }

        private static bool IsCellConnectedForRegionSelect(Vector3 startCenter, Vector3 nextCenter,
            Vector3 cellSize, Quaternion cellRotation)
        {
            if (!BlockManager.IsCellOccupied(nextCenter, cellSize, out var nextObjects, out _)) return false;
            if (BlockToolModes.regionConectivity == BlockToolModes.RegionConectivity.GEOMETRY) return true;

            BlockManager.IsCellOccupied(startCenter, cellSize, out var startObjects, out _);

            var startPrefabs = new System.Collections.Generic.HashSet<UnityEngine.Object>(
                startObjects.Select(o => UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(o)));
            var nextPrefabs = new System.Collections.Generic.HashSet<UnityEngine.Object>(
                nextObjects.Select(o => UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(o)));

            startPrefabs.IntersectWith(nextPrefabs);
            return startPrefabs.Count > 0;
        }

        private static void BlockRegionSelectInput(Vector3 mousePos3D)
        {
            if (_regionSelectTargets == null || _regionSelectTargets.Count == 0)
            {
                if (Event.current.button == 0 && Event.current.type == EventType.MouseDown
                    && !Event.current.shift && !Event.current.control)
                {
                    UnityEditor.Selection.objects = new Object[0];
                }
                return;
            }

            if (Event.current.button == 0 && Event.current.type == EventType.MouseDown)
            {
                if (Event.current.shift)
                {
                    var current = UnityEditor.Selection.objects.ToList();
                    foreach (var obj in _regionSelectTargets)
                    {
                        if (!current.Contains(obj))
                            current.Add(obj);
                    }
                    UnityEditor.Selection.objects = current.ToArray();
                }
                else if (Event.current.control)
                {
                    var current = UnityEditor.Selection.objects.ToList();
                    foreach (var obj in _regionSelectTargets)
                    {
                        current.Remove(obj);
                    }
                    UnityEditor.Selection.objects = current.ToArray();
                }
                else
                {
                    UnityEditor.Selection.objects = _regionSelectTargets.ToArray();
                }
            }
        }
    }

}
#pragma warning restore UDR0001
