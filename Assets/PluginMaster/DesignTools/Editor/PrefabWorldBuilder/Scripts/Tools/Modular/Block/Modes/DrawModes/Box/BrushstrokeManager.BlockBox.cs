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
        private const int MAX_BLOCK_BOX_CELLS = 1000;
        public static void UpdateBlockBoxBrushstroke()
        {
            ResetBlockCellCount();
            if (PaletteManager.selectedBrush == null) return;

            var toolSettings = BlockManager.settings;
            var cellSize = toolSettings.moduleSize;
            var cellRotation = GridManager.settings.rotation;
            if (BlockManager.quarterTurns > 0)
                cellRotation *= Quaternion.AngleAxis(BlockManager.quarterTurns * 90, toolSettings.upwardAxis);

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

            var cellPositions = GetBoxCellPositions(BlockToolModes.boxFirstPoint, BlockToolModes.boxSecondPoint,
                cellSize, cellRotation);

            var blockItemsCount = 0;
            foreach (var cellCenter in cellPositions)
            {
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
                AddBrushstrokeItem(idx, tokenIdx, cellCenter, angle, scale, toolSettings);
                if (!restoringPrevItem) PaletteManager.selectedBrush.SetNextItemIndex();

                ++blockItemsCount;
            }
        }

        private static System.Collections.Generic.List<Vector3> GetBoxCellPositions(Vector3 firstPoint,
            Vector3 secondPoint, Vector3 cellSize, Quaternion cellRotation)
        {
            var positions = new System.Collections.Generic.List<Vector3>();

            if (cellSize.x <= 0f || cellSize.y <= 0f || cellSize.z <= 0f)
            {
                ResetBlockCellCount();
                return positions;
            }

            firstPoint = PWBIO.SnapPositionToBlockCellCenter(firstPoint);
            secondPoint = PWBIO.SnapPositionToBlockCellCenter(secondPoint);

            var inverseRotation = Quaternion.Inverse(cellRotation);
            var localDelta = inverseRotation * (secondPoint - firstPoint);

            var cellStepsX = Mathf.Abs(Mathf.RoundToInt(localDelta.x / cellSize.x));
            var cellStepsY = Mathf.Abs(Mathf.RoundToInt(localDelta.y / cellSize.y));
            var cellStepsZ = Mathf.Abs(Mathf.RoundToInt(localDelta.z / cellSize.z));

            _blockCellsCountX = Mathf.Max(1, cellStepsX + 1);
            _blockCellsCountY = Mathf.Max(1, cellStepsY + 1);
            _blockCellsCountZ = Mathf.Max(1, cellStepsZ + 1);

            var totalCells = (long)_blockCellsCountX * _blockCellsCountY * _blockCellsCountZ;
            if (totalCells > MAX_BLOCK_BOX_CELLS)
            {
                Debug.LogWarning($"Too many cells in block box: {totalCells}. Max allowed is {MAX_BLOCK_BOX_CELLS}. " +
                    $"Please decrease the distance between the first and second points.");
                positions.Clear();
                return positions;
            }

            var tangentX = cellRotation * Vector3.right;
            var tangentY = cellRotation * Vector3.up;
            var tangentZ = cellRotation * Vector3.forward;

            var directionX = localDelta.x >= 0f ? 1 : -1;
            var directionY = localDelta.y >= 0f ? 1 : -1;
            var directionZ = localDelta.z >= 0f ? 1 : -1;

            for (int x = 0; x <= cellStepsX; x++)
            {
                for (int y = 0; y <= cellStepsY; y++)
                {
                    for (int z = 0; z <= cellStepsZ; z++)
                    {
                        var cellPos = firstPoint
                            + tangentX * (x * cellSize.x * directionX)
                            + tangentY * (y * cellSize.y * directionY)
                            + tangentZ * (z * cellSize.z * directionZ);

                        positions.Add(PWBIO.SnapPositionToBlockCellCenter(cellPos));
                    }
                }
            }

            return positions;
        }

        public static void UpdateDeleteBlockBoxBrushstroke()
        {
            ResetBlockCellCount();

            var toolSettings = BlockManager.settings;
            var cellSize = toolSettings.moduleSize;
            var cellRotation = GridManager.settings.rotation;
            if (BlockManager.quarterTurns > 0)
                cellRotation *= Quaternion.AngleAxis(BlockManager.quarterTurns * 90, toolSettings.upwardAxis);

            var cellPositions = GetBoxCellPositions(BlockToolModes.boxFirstPoint, BlockToolModes.boxSecondPoint,
                cellSize, cellRotation);

            _brushstroke.Clear();
            foreach (var cellCenter in cellPositions)
            {
                var strokeItem = new BrushstrokeItem(cellCenter);
                _brushstroke.Add(strokeItem);
            }
        }
    }
}
#pragma warning restore UDR0001
