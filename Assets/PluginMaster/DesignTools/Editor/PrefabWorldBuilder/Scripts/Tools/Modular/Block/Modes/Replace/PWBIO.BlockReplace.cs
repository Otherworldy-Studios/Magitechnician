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

        private static System.Collections.Generic.List<(GameObject obj, Vector3 cellCenter)> _replaceTargets
            = new System.Collections.Generic.List<(GameObject, Vector3)>();
        private static BlockToolModes.MoveNormalDirection _replaceNormalDirection
            = BlockToolModes.MoveNormalDirection.UP;
        private static System.Collections.Generic.List<Renderer> _blockReplaceRenderers
            = new System.Collections.Generic.List<Renderer>();


        private static void PreviewBlockReplace(Camera camera,
            out Vector3 mousePos3D, out Vector3 localMousePos3D)
        {
            mousePos3D = Vector3.zero;
            localMousePos3D = Vector3.zero;
            _replaceTargets.Clear();
            _paintStroke.Clear();

            foreach (var renderer in _blockReplaceRenderers)
            {
                if (renderer == null) continue;
                renderer.enabled = true;
            }
            _blockReplaceRenderers.Clear();

            if (PaletteManager.selectedBrush == null) return;

            var mousePos2D = Event.current.mousePosition;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePos2D);

            if (!PWBToolRaycast(mouseRay, out RaycastHit raycastHit, out _,
                maxDistance: float.MaxValue, layerMask: -1, paintOnPalettePrefabs: true,
                castOnMeshesWithoutCollider: true, createTempColliders: true,
                exceptions: null, ignoreSceneColliders: true))
            {
                BrushstrokeManager.ClearBrushstroke();
                return;
            }

            var toolSettings = BlockManager.settings;
            var cellSize = toolSettings.moduleSize;
            var cellRotation = GridManager.settings.rotation;
            if (BlockManager.quarterTurns > 0)
                cellRotation *= Quaternion.AngleAxis(BlockManager.quarterTurns * 90, toolSettings.upwardAxis);

            var hitPointFaceCenter = SnapPositionToCellFaceCenter(raycastHit.point);
            _replaceNormalDirection = GetMoveNormalDirectionFromHitNormal(raycastHit.normal);
            var hitCellCenter = GetCellCenterFromFaceCenterAndNormalDirection(hitPointFaceCenter,
                _replaceNormalDirection);
            mousePos3D = hitCellCenter;

            var step = GridManager.settings.step;
            var halfStep = Mathf.Min(cellSize.x, cellSize.y, cellSize.z) * 0.4999f;

            if (BlockToolModes.replaceMode == BlockToolModes.ReplaceMode.SINGLE)
            {
                if (BlockManager.IsCellOccupied(hitCellCenter, step, out GameObject[] objects, out _))
                {
                    foreach (var obj in objects)
                    {
                        var root = UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(obj);
                        var target = root != null ? root : obj;
                        if (!_replaceTargets.Any(t => t.obj == target))
                            _replaceTargets.Add((target, hitCellCenter));
                    }
                }
            }
            else // REGION
            {
                if (!BlockManager.IsCellOccupied(hitCellCenter, step)) return;

                var visited = new System.Collections.Generic.HashSet<Vector3Int>();
                var queue = new System.Collections.Generic.Queue<Vector3Int>();
                visited.Add(Vector3Int.zero);
                queue.Enqueue(Vector3Int.zero);

                while (queue.Count > 0)
                {
                    var coord = queue.Dequeue();
                    var worldPos = hitCellCenter + cellRotation * Vector3.Scale(coord, step);

                    if (BlockManager.IsCellOccupied(worldPos, step, out GameObject[] objects, out _))
                    {
                        foreach (var obj in objects)
                        {
                            var root = UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(obj);
                            var target = root != null ? root : obj;
                            if (!_replaceTargets.Any(t => t.obj == target))
                                _replaceTargets.Add((target, worldPos));
                        }
                    }

                    var neighbors = GetReplaceRegionNeighborOffsets();
                    foreach (var offset in neighbors)
                    {
                        var nextCoord = coord + offset;
                        if (visited.Contains(nextCoord)) continue;
                        visited.Add(nextCoord);
                        var nextWorldPos = hitCellCenter + cellRotation * Vector3.Scale(nextCoord, step);
                        if (IsCellConnectedForReplace(hitCellCenter, nextWorldPos, step, cellRotation))
                            queue.Enqueue(nextCoord);
                    }
                }
            }

            if (_replaceTargets.Count == 0) return;

            foreach (var (obj, _) in _replaceTargets)
            {
                if (obj == null) continue;
                var renderers = obj.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer != null && renderer.enabled)
                    {
                        _blockReplaceRenderers.Add(renderer);
                    }
                }
            }

            var cellCenters = _replaceTargets.Select(t => t.cellCenter);
            BrushstrokeManager.UpdateBlockReplaceBrushstroke(cellCenters);

            var brushstroke = BrushstrokeManager.brushstroke;

            for (int i = 0; i < brushstroke.Length && i < _replaceTargets.Count; ++i)
            {
                var strokeItem = brushstroke[i];
                if (strokeItem.settings == null) continue;
                var prefab = strokeItem.settings.prefab;
                if (prefab == null) continue;

                var cellCenter = strokeItem.tangentPosition;

                var itemRotation = Quaternion.Euler(strokeItem.additionalAngle);
                var previewRotation = itemRotation * Quaternion.Inverse(prefab.transform.rotation);
                var scaleMult = strokeItem.scaleMultiplier;
                var centerToPivot = GetCenterToPivot(prefab, scaleMult, itemRotation);

                var itemPosition = cellCenter + centerToPivot;
                var translateMatrix = Matrix4x4.Translate(-prefab.transform.position);
                var rootToWorld = Matrix4x4.TRS(itemPosition, previewRotation, scaleMult) * translateMatrix;
                var layer = toolSettings.overwritePrefabLayer ? toolSettings.layer : prefab.layer;

                PreviewBrushItem(prefab, rootToWorld, layer, camera,
                    redMaterial: false, reverseTriangles: false, flipX: false, flipY: false);

                var itemScale = Vector3.Scale(prefab.transform.localScale, scaleMult);
                Transform parentTransform = toolSettings.parent;
                var paintItem = new PaintStrokeItem(prefab, strokeItem.settings.guid, itemPosition,
                    itemRotation * prefab.transform.rotation, itemScale, layer, parentTransform,
                    surface: null, flipX: false, flipY: false);
                _paintStroke.Add(paintItem);
            }

            foreach (var renderer in _blockReplaceRenderers)
                renderer.enabled = false;
        }

        private static System.Collections.Generic.List<Vector3Int> GetReplaceRegionNeighborOffsets()
        {
            var offsets = new System.Collections.Generic.List<Vector3Int>();

            var selMode = BlockToolModes.replaceSelectionMode;
            if (selMode == BlockToolModes.ReplaceSelectionMode.BOX)
            {
                offsets.Add(Vector3Int.right); offsets.Add(Vector3Int.left);
                offsets.Add(Vector3Int.up); offsets.Add(Vector3Int.down);
                offsets.Add(Vector3Int.forward); offsets.Add(Vector3Int.back);
                if (BlockToolModes.replaceRegionSelectNeighborSearchingDirections
                    == BlockToolModes.ReplaceRegionSelectNeighborSearchingDirections.EIGHT_DIRECTIONS)
                {
                    offsets.Add(Vector3Int.right + Vector3Int.up);
                    offsets.Add(Vector3Int.right + Vector3Int.down);
                    offsets.Add(Vector3Int.left + Vector3Int.up);
                    offsets.Add(Vector3Int.left + Vector3Int.down);
                    offsets.Add(Vector3Int.right + Vector3Int.forward);
                    offsets.Add(Vector3Int.right + Vector3Int.back);
                    offsets.Add(Vector3Int.left + Vector3Int.forward);
                    offsets.Add(Vector3Int.left + Vector3Int.back);
                    offsets.Add(Vector3Int.up + Vector3Int.forward);
                    offsets.Add(Vector3Int.up + Vector3Int.back);
                    offsets.Add(Vector3Int.down + Vector3Int.forward);
                    offsets.Add(Vector3Int.down + Vector3Int.back);
                }
            }
            else // FACE
            {
                Vector3Int t1, t2;
                if (_replaceNormalDirection == BlockToolModes.MoveNormalDirection.UP
                    || _replaceNormalDirection == BlockToolModes.MoveNormalDirection.DOWN)
                { t1 = Vector3Int.right; t2 = Vector3Int.forward; }
                else if (_replaceNormalDirection == BlockToolModes.MoveNormalDirection.LEFT
                    || _replaceNormalDirection == BlockToolModes.MoveNormalDirection.RIGHT)
                { t1 = Vector3Int.forward; t2 = Vector3Int.up; }
                else { t1 = Vector3Int.right; t2 = Vector3Int.up; }

                offsets.Add(t1); offsets.Add(-t1); offsets.Add(t2); offsets.Add(-t2);
                if (BlockToolModes.replaceRegionSelectNeighborSearchingDirections
                    == BlockToolModes.ReplaceRegionSelectNeighborSearchingDirections.EIGHT_DIRECTIONS)
                {
                    offsets.Add(t1 + t2); offsets.Add(t1 - t2); offsets.Add(-t1 + t2); offsets.Add(-t1 - t2);
                }
            }
            return offsets;
        }

        private static bool IsCellConnectedForReplace(Vector3 startCenter, Vector3 nextCenter,
            Vector3 cellSize, Quaternion cellRotation)
        {
            if (!BlockManager.IsCellOccupied(nextCenter, cellSize, out var nextObjects, out _)) return false;
            if (BlockToolModes.replaceRegionConectivity == BlockToolModes.ReplaceRegionConectivity.GEOMETRY) return true;

            BlockManager.IsCellOccupied(startCenter, cellSize, out var startObjects, out _);

            var startPrefabs = new System.Collections.Generic.HashSet<UnityEngine.Object>(
                startObjects.Select(o => UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(o)));
            var nextPrefabs = new System.Collections.Generic.HashSet<UnityEngine.Object>(
                nextObjects.Select(o => UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(o)));

            startPrefabs.IntersectWith(nextPrefabs);
            return startPrefabs.Count > 0;
        }

        private static void BlockReplaceInput(Vector3 mousePos3D)
        {
            if (PaletteManager.selectedBrush == null) return;
            if (_replaceTargets == null || _replaceTargets.Count == 0) return;

            if (Event.current.button == 0 && Event.current.type == EventType.MouseDown)
            {
                foreach (var renderer in _blockReplaceRenderers)
                {
                    if (renderer != null) renderer.enabled = true;
                }
                _blockReplaceRenderers.Clear();

                var toolSettings = BlockManager.settings;
                var cellSize = toolSettings.moduleSize;
                var cellRotation = GridManager.settings.rotation;
                if (BlockManager.quarterTurns > 0)
                    cellRotation *= Quaternion.AngleAxis(BlockManager.quarterTurns * 90, toolSettings.upwardAxis);

                var brush = PaletteManager.selectedBrush;
                var brushId = brush?.id ?? -1;

                foreach (var (obj, cellCenter) in _replaceTargets)
                    EraseBlockObject(obj);

                var paintedObjects = Paint(BlockManager.settings);

                foreach (var pair in paintedObjects)
                {
                    foreach (var objAndIndex in pair.Value)
                    {
                        if (objAndIndex.Item1 != null)
                        {
                            var objCenter = BoundsUtils.GetBoundsRecursive(objAndIndex.Item1.transform).center;
                            var snappedCenter = SnapPositionToBlockCellCenter(objCenter);
                            BlockManager.AddOccupiedCell(snappedCenter, cellSize, cellRotation, brushId);
                        }
                    }
                }
            }
        }
    }
}
#pragma warning restore UDR0001
