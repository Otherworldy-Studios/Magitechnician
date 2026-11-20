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
    public static partial class BrushstrokeManager
    {
        private struct BlockFaceCellData
        {
            public Vector3 center;
            public Vector3 size;
            public System.Collections.Generic.HashSet<GameObject> prefabs;
            public long brushId;
            public BlockFaceCellData(Vector3 center, Vector3 size)
            {
                this.center = center;
                this.size = size;
                prefabs = new System.Collections.Generic.HashSet<GameObject>();
                brushId = -1;
                if (BlockManager.IsCellOccupied(PWBIO.SnapPositionToBlockCellCenter(center), size,
                    out GameObject[] objects, out brushId))
                {
                    foreach(var obj in objects)
                    {
                        var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(obj);
                        if (prefab != null) prefabs.Add(prefab);
                    }
                }
            }
        }
        public static void UpdateBlockFaceBrushstroke()
        {
            ResetBlockCellCount();
            if (PaletteManager.selectedBrush == null) return;

            var toolSettings = BlockManager.settings;
            var cellSize = toolSettings.moduleSize;
            var cellRotation = GridManager.settings.rotation;

            var faceNormal = GetFaceNormalVector(BlockToolModes.faceNormalDirection, cellRotation);
            var targetCenter = BlockToolModes.faceTargetCellCenter;
            var targetBlock = new BlockFaceCellData(targetCenter, cellSize);
            var connectedCells = GetConnectedFaceCells(targetBlock, targetCenter, faceNormal, cellSize, cellRotation,
                BlockToolModes.faceNeighborSearchingDirections, BlockToolModes.faceConectivity);
            if (connectedCells.Count == 0)
            {
                _brushstroke.Clear();
                return;
            }

            var localRotation = Quaternion.FromToRotation(Vector3.up, toolSettings.upwardAxis);
            var rotation = GridManager.settings.rotation * localRotation;
            var angle = rotation.eulerAngles;

            var forwardAxis = toolSettings.forwardAxis;
            if (BlockManager.quarterTurns > 0)
                forwardAxis = Quaternion.AngleAxis(-90 * BlockManager.quarterTurns, toolSettings.upwardAxis) * forwardAxis;
            angle = AxesUtils.SignedAxis.GetEulerAnglesFromAxes(forwardAxis, toolSettings.upwardAxis);
            angle = (Quaternion.Euler(angle) * GridManager.settings.rotation).eulerAngles;

            var prevBrushstroke = _brushstroke.ToArray();
            _brushstroke.Clear();

            if (PaletteManager.selectedBrush.restartPatternForEachStroke)
                PaletteManager.selectedBrush.ResetCurrentItemIndex();

            var normalOffset = Vector3.Scale(faceNormal, cellSize);

            var blockItemsCount = 0;
            foreach (var cellCenter in connectedCells)
            {
                var newCellCenter = cellCenter + normalOffset;

                if (BlockManager.IsCellOccupied(PWBIO.SnapPositionToBlockCellCenter(newCellCenter), cellSize)) continue;

                var idx = PaletteManager.selectedBrush.currentItemIndex;
                if (idx == -1) break;
                if (PaletteManager.selectedBrush.frequencyMode == MultibrushSettings.FrequencyMode.PATTERN && idx == -2)
                {
                    if (PaletteManager.selectedBrush.patternMachine != null)
                        PaletteManager.selectedBrush.patternMachine.Reset();
                    else break;
                }

                var tokenIdx = PaletteManager.selectedBrush.GetPatternTokenIndex();
                var restoringPrevItem = !PaletteManager.selectedBrush.restartPatternForEachStroke
                    && prevBrushstroke.Length > blockItemsCount;
                if (restoringPrevItem)
                {
                    idx = prevBrushstroke[blockItemsCount].index;
                    tokenIdx = prevBrushstroke[blockItemsCount].tokenIndex;
                    PaletteManager.selectedBrush.SetPatternTokenIndex(tokenIdx);
                }
                var scale = localRotation * ScaleMultiplier(idx, toolSettings);
                scale.x = Mathf.Abs(scale.x);
                scale.y = Mathf.Abs(scale.y);
                scale.z = Mathf.Abs(scale.z);
                AddBrushstrokeItem(idx, tokenIdx, newCellCenter, angle, scale, toolSettings);
                if (!restoringPrevItem) PaletteManager.selectedBrush.SetNextItemIndex();

                ++blockItemsCount;
            }
            _blockCellsCountX = Mathf.Max(1, blockItemsCount);
            _blockCellsCountY = 1;
            _blockCellsCountZ = 1;
        }
        public static void UpdateDeleteBlockFaceBrushstroke()
        {
            ResetBlockCellCount();

            var toolSettings = BlockManager.settings;
            var cellSize = toolSettings.moduleSize;
            var cellRotation = GridManager.settings.rotation;

            var faceNormal = GetFaceNormalVector(BlockToolModes.faceNormalDirection, cellRotation);
            var targetCenter = BlockToolModes.faceTargetCellCenter;
            var targetBlock = new BlockFaceCellData(targetCenter, cellSize);
            var connectedCells = GetConnectedFaceCells(targetBlock, targetCenter, faceNormal, cellSize, cellRotation,
                BlockToolModes.faceNeighborSearchingDirections, BlockToolModes.faceConectivity);

            _brushstroke.Clear();
            foreach (var cellCenter in connectedCells)
            {
                var strokeItem = new BrushstrokeItem(cellCenter);
                _brushstroke.Add(strokeItem);
            }
        }
        private static Vector3 GetFaceNormalVector(BlockToolModes.FaceNormalDirection direction, Quaternion cellRotation)
        {
            switch (direction)
            {
                case BlockToolModes.FaceNormalDirection.UP:
                    return cellRotation * Vector3.up;
                case BlockToolModes.FaceNormalDirection.DOWN:
                    return cellRotation * Vector3.down;
                case BlockToolModes.FaceNormalDirection.LEFT:
                    return cellRotation * Vector3.left;
                case BlockToolModes.FaceNormalDirection.RIGHT:
                    return cellRotation * Vector3.right;
                case BlockToolModes.FaceNormalDirection.FORWARD:
                    return cellRotation * Vector3.forward;
                case BlockToolModes.FaceNormalDirection.BACK:
                    return cellRotation * Vector3.back;
                default:
                    return cellRotation * Vector3.up;
            }
        }

        private static System.Collections.Generic.HashSet<Vector3> GetConnectedFaceCells(BlockFaceCellData targetBlock,
            Vector3 startCenter, Vector3 faceNormal, Vector3 cellSize, Quaternion cellRotation,
            BlockToolModes.FaceNeighborSearchingDirections searchDirections,
            BlockToolModes.FaceConectivity conectivity)
        {
            var connectedCells = new System.Collections.Generic.HashSet<Vector3>();
            
            if (!BlockManager.IsCellOccupied(PWBIO.SnapPositionToBlockCellCenter(startCenter), cellSize))
                    return connectedCells;

            var visited = new System.Collections.Generic.HashSet<Vector3Int>();
            var queue = new System.Collections.Generic.Queue<Vector3Int>();

            var neighborOffsets = GetNeighborOffsets();

            var startCoord = Vector3Int.zero;
            queue.Enqueue(startCoord);
            visited.Add(startCoord);

            const int MAX_CELLS = 10000;
            var cellCoords = new System.Collections.Generic.List<Vector3Int> { startCoord };

            while (queue.Count > 0 && cellCoords.Count < MAX_CELLS)
            {
                var currentCoord = queue.Dequeue();

                foreach (var offset in neighborOffsets)
                {
                    var neighborCoord = currentCoord + offset;

                    if (visited.Contains(neighborCoord)) continue;
                    visited.Add(neighborCoord);

                    var neighborCenter = CellCoordToWorld(neighborCoord, cellSize, cellRotation, startCenter);
                    var snappedNeighborCenter = PWBIO.SnapPositionToBlockCellCenter(neighborCenter);
                    if (!IsCellConnected(targetBlock, snappedNeighborCenter, cellSize, cellRotation, conectivity)) continue;

                    cellCoords.Add(neighborCoord);
                    queue.Enqueue(neighborCoord);
                }
            }

            foreach (var coord in cellCoords)
            {
                connectedCells.Add(CellCoordToWorld(coord, cellSize, cellRotation, startCenter));
            }

            return connectedCells;
        }

        private static Vector3 CellCoordToWorld(Vector3Int coord, Vector3 cellSize, Quaternion cellRotation, Vector3 origin)
        {
            var localPos = new Vector3(coord.x * cellSize.x, coord.y * cellSize.y, coord.z * cellSize.z);
            return origin + cellRotation * localPos;
        }
        private static System.Collections.Generic.HashSet<Vector3Int> GetNeighborOffsets()
        {
            var offsets = new System.Collections.Generic.HashSet<Vector3Int>();

            Vector3Int tangent1, tangent2;

            switch (BlockToolModes.faceNormalDirection)
            {
                case BlockToolModes.FaceNormalDirection.UP:
                case BlockToolModes.FaceNormalDirection.DOWN:
                    tangent1 = Vector3Int.right;
                    tangent2 = Vector3Int.forward;
                    break;
                case BlockToolModes.FaceNormalDirection.LEFT:
                case BlockToolModes.FaceNormalDirection.RIGHT:
                    tangent1 = Vector3Int.forward;
                    tangent2 = Vector3Int.up;
                    break;
                case BlockToolModes.FaceNormalDirection.FORWARD:
                case BlockToolModes.FaceNormalDirection.BACK:
                default:
                    tangent1 = Vector3Int.right;
                    tangent2 = Vector3Int.up;
                    break;
            }

            offsets.Add(tangent1);
            offsets.Add(-tangent1);
            offsets.Add(tangent2);
            offsets.Add(-tangent2);

            if (BlockToolModes.faceNeighborSearchingDirections
                == BlockToolModes.FaceNeighborSearchingDirections.EIGHT_DIRECTIONS)
            {
                offsets.Add(tangent1 + tangent2);
                offsets.Add(tangent1 - tangent2);
                offsets.Add(-tangent1 + tangent2);
                offsets.Add(-tangent1 - tangent2);
            }
            return offsets;
        }

        private static bool IsCellConnected(BlockFaceCellData targetBlock, Vector3 cellCenter,
            Vector3 cellSize, Quaternion cellRotation, BlockToolModes.FaceConectivity conectivity)
        {
           
            switch (conectivity)
            {
                case BlockToolModes.FaceConectivity.PREFAB:
                    if(BlockManager.IsCellOccupied(PWBIO.SnapPositionToBlockCellCenter(cellCenter), cellSize,
                        out GameObject[] objects, out _))
                    {
                        var conectedPrefabs = new System.Collections.Generic.HashSet<GameObject>();
                        foreach (var obj in objects)
                        {
                            var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(obj);
                            if (prefab != null) conectedPrefabs.Add(prefab);
                        }
                        var intersection = new System.Collections.Generic.HashSet<GameObject>(targetBlock.prefabs);
                        intersection.IntersectWith(conectedPrefabs);
                        return intersection.Count > 0;
                    }
                    return false;
                case BlockToolModes.FaceConectivity.GEOMETRY:
                default:
                    return BlockManager.IsCellOccupied(cellCenter, cellSize);
            }
        }
    }
}
#pragma warning restore UDR0001
