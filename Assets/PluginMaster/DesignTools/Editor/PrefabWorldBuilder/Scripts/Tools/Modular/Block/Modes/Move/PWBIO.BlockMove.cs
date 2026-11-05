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

        #region MOVE SELECTION
        private class MoveSelectionItem : System.IEquatable<MoveSelectionItem>
        {
            public GameObject obj = null;
            public Vector3 center = Vector3.zero;
            public Bounds bounds = new Bounds();
            private Vector3 _originalCenter = Vector3.zero;
            private Bounds _originalBounds = new Bounds();
            public MoveSelectionItem(GameObject obj, Vector3 center)
            {
                this.obj = obj;
                this.center = center;
                bounds = BoundsUtils.GetBoundsRecursive(obj.transform);
                _originalCenter = center;
                _originalBounds = bounds;
            }
            public void SaveOriginalState()
            {
                _originalCenter = center;
                _originalBounds = bounds;
            }
            public void Move(Vector3 delta)
            {
                UnityEditor.Undo.RecordObject(obj.transform, "Move Block Object");
                obj.transform.position += delta;
                center += delta;
                bounds = new Bounds(bounds.center + delta, bounds.size);
            }
            public void FinalizeMove()
            {
                boundsOctree.Update(obj, _originalBounds, bounds);
                _originalBounds = bounds;
                _originalCenter = center;
            }
            public (Vector3 oldCenter, Vector3 newCenter) GetMoveData() => (_originalCenter, center);
            public bool Equals(MoveSelectionItem other) => obj == other.obj || center == other.center;
            public override bool Equals(object obj) => obj is MoveSelectionItem other && Equals(other);
            public override int GetHashCode() => center.GetHashCode();
            public static bool operator ==(MoveSelectionItem left, MoveSelectionItem right)
            {
                if (ReferenceEquals(left, right)) return true;
                if (ReferenceEquals(left, null) || ReferenceEquals(right, null)) return false;
                return left.Equals(right);
            }
            public static bool operator !=(MoveSelectionItem left, MoveSelectionItem right) => !(left == right);
        }
        private static System.Collections.Generic.HashSet<MoveSelectionItem>
            _moveSelection = new System.Collections.Generic.HashSet<MoveSelectionItem>();
        #endregion

        public static void OnBlockSelectionChanged()
        {
            if (BlockToolModes.moveSelectionMode == BlockToolModes.MoveSelectionMode.CURRENT)
                _moveSelection.Clear();
        }

        private static bool _moveIsDragging = false;
        private static Vector3 _moveStartHitPoint = Vector3.zero;

        private static BlockToolModes.MoveNormalDirection _moveNormalDirection = BlockToolModes.MoveNormalDirection.UP;
        private static Vector3 _moveNormal = Vector3.up;
        private static Vector3 _moveHitCellCenter = Vector3.zero;
        private static GameObject _moveHitCollider = null;
        private static bool _moveHasHit = false;


        private static void PreviewBlockMove(Camera camera,
            out Vector3 mousePos3D, out Vector3 localMousePos3D)
        {
            var mousePos2D = Event.current.mousePosition;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePos2D);
            mousePos3D = Vector3.zero;
            localMousePos3D = Vector3.zero;

            var toolSettings = BlockManager.settings;
            var cellSize = toolSettings.moduleSize;
            var cellRotation = GridManager.settings.rotation;
            var gridStep = GridManager.settings.step;
            var workWithCurrentSelection = BlockToolModes.moveSelectionMode == BlockToolModes.MoveSelectionMode.CURRENT;
            if (_moveIsDragging)
            {
                var plane = new Plane(_moveNormal, _moveStartHitPoint);
                var endPoint = _moveStartHitPoint;
                if (plane.Raycast(mouseRay, out float enter))
                {
                    var hitPoint = mouseRay.GetPoint(enter);
                    endPoint = SnapPositionToBlockCellCenter(hitPoint);
                }
                var delta = endPoint - _moveStartHitPoint;
                mousePos3D = endPoint;
                if (delta.magnitude > 0.001f)
                {
                    foreach (var item in _moveSelection)
                        item.Move(delta);
                    _moveStartHitPoint = endPoint;
                }
            }
            else
            {
                _moveHasHit = false;
                if (PWBToolRaycast(mouseRay, out RaycastHit raycastHit, out GameObject collider, maxDistance: float.MaxValue,
                layerMask: -1, paintOnPalettePrefabs: true, castOnMeshesWithoutCollider: true,
                createTempColliders: true, ignoreSceneColliders: true))
                {
                    _moveHasHit = true;
                    _moveHitCollider = collider;
                    var leftMouseDown = Event.current.type == EventType.MouseDown && Event.current.button == 0;
                    var calculateHitCellCenter = !workWithCurrentSelection || leftMouseDown;

                    if (calculateHitCellCenter)
                    {
                        var hitPointFaceCenter = SnapPositionToCellFaceCenter(raycastHit.point);
                        _moveNormalDirection = GetMoveNormalDirectionFromHitNormal(raycastHit.normal);
                        _moveHitCellCenter = GetCellCenterFromFaceCenterAndNormalDirection(hitPointFaceCenter,
                            _moveNormalDirection);
                        _moveNormal = GetMoveNormalVector(_moveNormalDirection);
                    }

                    if (!workWithCurrentSelection)
                    {
                        UpdateMoveSelection(_moveHitCellCenter);
                    }
                }
                else if (!workWithCurrentSelection)
                {
                    _moveSelection.Clear();
                }
            }

            if (workWithCurrentSelection && SelectionManager.topLevelSelection.Length > 0 && _moveSelection.Count == 0)
            {
                UpdateMoveCurrentSelection();
            }
            DrawMovePreview(camera, cellSize, cellRotation);
        }

        private static BlockToolModes.MoveNormalDirection GetMoveNormalDirectionFromHitNormal(Vector3 hitNormal)
        {
            var invRotation = Quaternion.Inverse(GridManager.settings.rotation);
            var localNormal = invRotation * hitNormal;

            var absX = Mathf.Abs(localNormal.x);
            var absY = Mathf.Abs(localNormal.y);
            var absZ = Mathf.Abs(localNormal.z);

            if (absY >= absX && absY >= absZ)
                return localNormal.y >= 0
                    ? BlockToolModes.MoveNormalDirection.UP
                    : BlockToolModes.MoveNormalDirection.DOWN;

            if (absX >= absY && absX >= absZ)
                return localNormal.x >= 0
                    ? BlockToolModes.MoveNormalDirection.RIGHT
                    : BlockToolModes.MoveNormalDirection.LEFT;

            return localNormal.z >= 0
                ? BlockToolModes.MoveNormalDirection.FORWARD
                : BlockToolModes.MoveNormalDirection.BACK;
        }

        private static Vector3 GetMoveNormalVector(BlockToolModes.MoveNormalDirection direction)
        {
            var rotation = GridManager.settings.rotation;
            switch (direction)
            {
                case BlockToolModes.MoveNormalDirection.UP:
                    return rotation * Vector3.up;
                case BlockToolModes.MoveNormalDirection.DOWN:
                    return rotation * Vector3.down;
                case BlockToolModes.MoveNormalDirection.LEFT:
                    return rotation * Vector3.left;
                case BlockToolModes.MoveNormalDirection.RIGHT:
                    return rotation * Vector3.right;
                case BlockToolModes.MoveNormalDirection.FORWARD:
                    return rotation * Vector3.forward;
                case BlockToolModes.MoveNormalDirection.BACK:
                    return rotation * Vector3.back;
                default:
                    return rotation * Vector3.up;
            }
        }

        private static Vector3 GetCellCenterFromFaceCenterAndNormalDirection(Vector3 faceCenter,
            BlockToolModes.MoveNormalDirection direction)
        {
            var rotation = GridManager.settings.rotation;
            var cellSize = GridManager.settings.step;
            switch (direction)
            {
                case BlockToolModes.MoveNormalDirection.UP:
                    return faceCenter + rotation * Vector3.down * (cellSize.y * 0.5f);
                case BlockToolModes.MoveNormalDirection.DOWN:
                    return faceCenter + rotation * Vector3.up * (cellSize.y * 0.5f);
                case BlockToolModes.MoveNormalDirection.LEFT:
                    return faceCenter + rotation * Vector3.right * (cellSize.x * 0.5f);
                case BlockToolModes.MoveNormalDirection.RIGHT:
                    return faceCenter + rotation * Vector3.left * (cellSize.x * 0.5f);
                case BlockToolModes.MoveNormalDirection.FORWARD:
                    return faceCenter + rotation * Vector3.back * (cellSize.z * 0.5f);
                case BlockToolModes.MoveNormalDirection.BACK:
                    return faceCenter + rotation * Vector3.forward * (cellSize.z * 0.5f);
                default:
                    return Vector3.zero;
            }
        }

        private static void UpdateMoveCurrentSelection()
        {
            var selection = SelectionManager.topLevelSelection;
            foreach (var obj in selection)
            {
                var center = BoundsUtils.GetBoundsRecursive(obj.transform).center;
                var snapped = SnapPositionToBlockCellCenter(center);
                if (Vector3.Distance(center, snapped) < 0.01f)
                {
                    _moveSelection.Add(new MoveSelectionItem(obj, snapped));
                }
            }
        }
        private static void UpdateMoveSelection(Vector3 hitCellCenter)
        {
            _moveSelection.Clear();
            if (BlockToolModes.moveSelectionMode == BlockToolModes.MoveSelectionMode.CURRENT) return;

            var cellSize = GridManager.settings.step;
            var cellRotation = GridManager.settings.rotation;
            if (!BlockManager.IsCellOccupied(hitCellCenter, cellSize)) return;
            var boxMode = BlockToolModes.moveSelectionMode == BlockToolModes.MoveSelectionMode.BOX;

            var visited = new System.Collections.Generic.HashSet<Vector3Int>();
            var queue = new System.Collections.Generic.Queue<Vector3Int>();
            visited.Add(Vector3Int.zero);
            queue.Enqueue(Vector3Int.zero);
            while (queue.Count > 0)
            {
                var coord = queue.Dequeue();
                var worldPos = hitCellCenter + cellRotation * Vector3.Scale(coord, cellSize);
                if (BlockManager.IsCellOccupied(worldPos, cellSize, out GameObject[] objects, out _))
                {
                    foreach (var obj in objects)
                    {
                        _moveSelection.Add(new MoveSelectionItem(obj, worldPos));
                    }
                }

                var neighbors = GetMoveNeighborOffsets(boxMode);
                foreach (var offset in neighbors)
                {
                    var nextCoord = coord + offset;
                    if (visited.Contains(nextCoord)) continue;
                    visited.Add(nextCoord);
                    var nextWorldPos = hitCellCenter + cellRotation * Vector3.Scale(nextCoord, cellSize);
                    if (IsCellConnectedForMove(hitCellCenter, nextWorldPos, cellSize, cellRotation))
                        queue.Enqueue(nextCoord);
                }
            }
        }

        private static System.Collections.Generic.List<Vector3Int> GetMoveNeighborOffsets(bool boxMode)
        {
            var offsets = new System.Collections.Generic.List<Vector3Int>();

            if (boxMode)
            {
                offsets.Add(Vector3Int.right); offsets.Add(Vector3Int.left);
                offsets.Add(Vector3Int.up); offsets.Add(Vector3Int.down);
                offsets.Add(Vector3Int.forward); offsets.Add(Vector3Int.back);
                if (BlockToolModes.moveNeighborSearchingDirections
                    == BlockToolModes.MoveNeighborSearchingDirections.EIGHT_DIRECTIONS)
                {
                    // XY plane diagonals
                    offsets.Add(Vector3Int.right + Vector3Int.up);
                    offsets.Add(Vector3Int.right + Vector3Int.down);
                    offsets.Add(Vector3Int.left + Vector3Int.up);
                    offsets.Add(Vector3Int.left + Vector3Int.down);
                    // XZ plane diagonals
                    offsets.Add(Vector3Int.right + Vector3Int.forward);
                    offsets.Add(Vector3Int.right + Vector3Int.back);
                    offsets.Add(Vector3Int.left + Vector3Int.forward);
                    offsets.Add(Vector3Int.left + Vector3Int.back);
                    // YZ plane diagonals
                    offsets.Add(Vector3Int.up + Vector3Int.forward);
                    offsets.Add(Vector3Int.up + Vector3Int.back);
                    offsets.Add(Vector3Int.down + Vector3Int.forward);
                    offsets.Add(Vector3Int.down + Vector3Int.back);
                }
            }
            else
            {
                Vector3Int t1, t2;
                if (_moveNormalDirection == BlockToolModes.MoveNormalDirection.UP
                    || _moveNormalDirection == BlockToolModes.MoveNormalDirection.DOWN)
                { t1 = Vector3Int.right; t2 = Vector3Int.forward; }
                else if (_moveNormalDirection == BlockToolModes.MoveNormalDirection.LEFT
                    || _moveNormalDirection == BlockToolModes.MoveNormalDirection.RIGHT)
                { t1 = Vector3Int.forward; t2 = Vector3Int.up; }
                else { t1 = Vector3Int.right; t2 = Vector3Int.up; }

                offsets.Add(t1); offsets.Add(-t1); offsets.Add(t2); offsets.Add(-t2);
                if (BlockToolModes.moveNeighborSearchingDirections
                    == BlockToolModes.MoveNeighborSearchingDirections.EIGHT_DIRECTIONS)
                {
                    offsets.Add(t1 + t2); offsets.Add(t1 - t2); offsets.Add(-t1 + t2); offsets.Add(-t1 - t2);
                }
            }
            return offsets;
        }

        private static bool IsCellConnectedForMove(Vector3 startCenter, Vector3 nextCenter,
            Vector3 cellSize, Quaternion cellRotation)
        {
            if (!BlockManager.IsCellOccupied(nextCenter, cellSize, out var nextObjects, out _)) return false;
            if (BlockToolModes.moveConectivity == BlockToolModes.MoveConectivity.GEOMETRY) return true;

            BlockManager.IsCellOccupied(startCenter, cellSize, out var startObjects, out _);
            var startPrefabs = new System.Collections.Generic.HashSet<UnityEngine.Object>(
                startObjects.Select(o => UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(o)));
            var nextPrefabs = new System.Collections.Generic.HashSet<UnityEngine.Object>(
                nextObjects.Select(o => UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(o)));
            startPrefabs.IntersectWith(nextPrefabs);
            return startPrefabs.Count > 0;
        }

        private static void DrawMovePreview(Camera camera, Vector3 cellSize, Quaternion cellRotation)
        {
            foreach (var item in _moveSelection)
            {
                var TRS = Matrix4x4.TRS(item.center, cellRotation, cellSize);
                Graphics.DrawMesh(cubeMesh, TRS, snapBoxMaterial, 0, camera);
            }
        }

        private static void BlockMoveInput(Vector3 mousePos3D)
        {
            var workWithCurrentSelection = BlockToolModes.moveSelectionMode == BlockToolModes.MoveSelectionMode.CURRENT;
            if (_moveIsDragging)
            {
                if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
                {
                    _moveIsDragging = false;
                    Event.current.Use();
                    var moves = _moveSelection.Select(item => item.GetMoveData());
                    BlockManager.MoveOccupiedCells(moves);
                    foreach (var item in _moveSelection)
                        item.FinalizeMove();
                    PWBCore.UpdateTempCollidersTransforms(_moveSelection.Select(item => item.obj).ToArray());
                    BoundsUtils.ClearBoundsDictionaries();
                }
            }
            else if (_moveHasHit)
            {
                if (workWithCurrentSelection)
                {
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                    {
                        SelectionManager.ToggleSelection(_moveHitCollider);
                        return;
                    }
                }

                var leftMouseDown = Event.current.type == EventType.MouseDown && Event.current.button == 0;
                if (leftMouseDown && _moveSelection.Count > 0)
                {
                    var isMouseOverSelection = _moveSelection.Any(
                        item => Vector3.Distance(item.center, _moveHitCellCenter) < 0.01f);
                    if (isMouseOverSelection)
                    {
                        _moveIsDragging = true;
                        _moveStartHitPoint = _moveHitCellCenter;
                        foreach (var item in _moveSelection)
                            item.SaveOriginalState();
                        Event.current.Use();
                    }
                }
            }
        }

    }
}
#pragma warning restore UDR0001
