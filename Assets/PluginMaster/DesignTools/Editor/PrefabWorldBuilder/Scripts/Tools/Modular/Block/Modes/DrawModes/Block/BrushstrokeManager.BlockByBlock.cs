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

        private static int _blockCellsCountX = 0;
        private static int _blockCellsCountY = 0;
        private static int _blockCellsCountZ = 0;
        public static int blockCellsCountX => _blockCellsCountX;
        public static int blockCellsCountY => _blockCellsCountY;
        public static int blockCellsCountZ => _blockCellsCountZ;

        private static Vector3 _blockHitNormal = Vector3.up;
        private static Vector3 _blockCenterPosition = Vector3.zero;


        public static void ResetBlockCellCount()
        {
            _blockCellsCountX = 1;
            _blockCellsCountY = 1;
            _blockCellsCountZ = 1;
        }

        public static void SetBlockBrushParameters(Vector3 centerPosition, Vector3 hitNormal)
        {
            _blockCenterPosition = centerPosition;
            _blockHitNormal = hitNormal;
        }

        public static void UpdateBlockByBlockBrushstroke(bool setNextIdx, bool deleteBox = false)
        {
            ResetBlockCellCount();
            if (PaletteManager.selectedBrush == null) return;

            var brushSize = BlockToolModes.brushSize;
            var brushShape = BlockToolModes.selectedBrushShape;

            if (brushSize == 1)
            {
                UpdateFirstModularBrushstroke(BlockManager.settings, setNextIdx);
                return;
            }

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
            else if (setNextIdx) PaletteManager.selectedBrush.SetNextItemIndex();

            var cellPositions = GetBlockByBlockCellPositions(_blockCenterPosition, _blockHitNormal,
                cellSize, cellRotation, brushSize, brushShape);

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

                BrushSettings brush = PaletteManager.selectedBrush.GetItemAt(idx);
                if (toolSettings.overwriteBrushProperties) brush = toolSettings.brushSettings;

                if (deleteBox)
                {
                    var additionalAngle = (Quaternion.Euler(angle)
                        * Quaternion.Euler(PaletteManager.selectedBrush.eulerOffset)).eulerAngles;
                    var strokeItem = new BrushstrokeItem(index: 0, tokenIndex: 0, brush as MultibrushItemSettings,
                        cellCenter, additionalAngle, scaleMultiplier: toolSettings.moduleSize,
                        flipX: false, flipY: false, surfaceDistance: 0);
                    _brushstroke.Add(strokeItem);
                }
                else
                {
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
                }
                ++blockItemsCount;
            }
        }

        public static System.Collections.Generic.List<Vector3> GetBlockByBlockCellPositions(Vector3 centerPos, Vector3 normal,
            Vector3 cellSize, Quaternion cellRotation, int brushSize, BlockToolModes.BrushShape brushShape)
        {
            var positions = new System.Collections.Generic.List<Vector3>();

            switch (brushShape)
            {
                case BlockToolModes.BrushShape.SQUARE:
                    GetSquareBrushPositions(centerPos, normal, cellSize, cellRotation, brushSize, positions);
                    break;
                case BlockToolModes.BrushShape.CIRCLE:
                    GetCircleBrushPositions(centerPos, normal, cellSize, cellRotation, brushSize, positions);
                    break;
                case BlockToolModes.BrushShape.CUBE:
                    GetCubeBrushPositions(centerPos, cellSize, cellRotation, brushSize, positions);
                    break;
                case BlockToolModes.BrushShape.SPHERE:
                    GetSphereBrushPositions(centerPos, cellSize, cellRotation, brushSize, positions);
                    break;
            }

            return positions;
        }

        private static void GetTangentAxesFromNormal(Vector3 normal, Quaternion cellRotation, Vector3 cellSize,
            out Vector3 tangent1, out Vector3 tangent2, out float step1, out float step2,
            out int countAxis1, out int countAxis2)
        {
            var absNormal = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));

            if (absNormal.y >= absNormal.x && absNormal.y >= absNormal.z)
            {
                tangent1 = cellRotation * Vector3.right;
                tangent2 = cellRotation * Vector3.forward;
                step1 = cellSize.x;
                step2 = cellSize.z;
                countAxis1 = 0;
                countAxis2 = 2;
            }
            else if (absNormal.x >= absNormal.y && absNormal.x >= absNormal.z)
            {
                tangent1 = cellRotation * Vector3.forward;
                tangent2 = cellRotation * Vector3.up;
                step1 = cellSize.z;
                step2 = cellSize.y;
                countAxis1 = 2;
                countAxis2 = 1;
            }
            else
            {
                tangent1 = cellRotation * Vector3.right;
                tangent2 = cellRotation * Vector3.up;
                step1 = cellSize.x;
                step2 = cellSize.y;
                countAxis1 = 0;
                countAxis2 = 1;
            }
        }

        private static void GetSquareBrushPositions(Vector3 centerPos, Vector3 normal,
            Vector3 cellSize, Quaternion cellRotation, int brushSize,
            System.Collections.Generic.List<Vector3> positions)
        {
            GetTangentAxesFromNormal(normal, cellRotation, cellSize,
                out Vector3 tangent1, out Vector3 tangent2, out float step1, out float step2,
                out int countAxis1, out int countAxis2);

            int halfSize = brushSize / 2;
            int startOffset = -halfSize;
            int endOffset = brushSize - halfSize - 1;

            var counts = new int[3] { 1, 1, 1 };
            counts[countAxis1] = brushSize;
            counts[countAxis2] = brushSize;
            _blockCellsCountX = counts[0];
            _blockCellsCountY = counts[1];
            _blockCellsCountZ = counts[2];

            for (int i = startOffset; i <= endOffset; i++)
            {
                for (int j = startOffset; j <= endOffset; j++)
                {
                    var cellPos = centerPos + tangent1 * (i * step1) + tangent2 * (j * step2);
                    positions.Add(cellPos);
                }
            }
        }

        private static void GetCircleBrushPositions(Vector3 centerPos, Vector3 normal,
            Vector3 cellSize, Quaternion cellRotation, int brushSize,
            System.Collections.Generic.List<Vector3> positions)
        {
            GetTangentAxesFromNormal(normal, cellRotation, cellSize,
                out Vector3 tangent1, out Vector3 tangent2, out float step1, out float step2,
                out int countAxis1, out int countAxis2);

            int halfSize = brushSize / 2;
            int startOffset = -halfSize;
            int endOffset = brushSize - halfSize - 1;
            float radiusSqr = (brushSize / 2f) * (brushSize / 2f);

            var counts = new int[3] { 1, 1, 1 };
            counts[countAxis1] = brushSize;
            counts[countAxis2] = brushSize;
            _blockCellsCountX = counts[0];
            _blockCellsCountY = counts[1];
            _blockCellsCountZ = counts[2];

            float centerOffset = (brushSize % 2 == 0) ? 0.5f : 0f;

            for (int i = startOffset; i <= endOffset; i++)
            {
                for (int j = startOffset; j <= endOffset; j++)
                {
                    float di = i + centerOffset;
                    float dj = j + centerOffset;
                    float distSqr = di * di + dj * dj;
                    if (distSqr > radiusSqr) continue;
                    var cellPos = centerPos + tangent1 * (i * step1) + tangent2 * (j * step2);
                    positions.Add(cellPos);
                }
            }
        }

        private static void GetCubeBrushPositions(Vector3 centerPos, Vector3 cellSize,
            Quaternion cellRotation, int brushSize,
            System.Collections.Generic.List<Vector3> positions)
        {
            _blockCellsCountX = brushSize;
            _blockCellsCountY = brushSize;
            _blockCellsCountZ = brushSize;

            int halfSize = brushSize / 2;
            int startOffset = -halfSize;
            int endOffset = brushSize - halfSize - 1;

            var tangentX = cellRotation * Vector3.right;
            var tangentY = cellRotation * Vector3.up;
            var tangentZ = cellRotation * Vector3.forward;

            for (int x = startOffset; x <= endOffset; x++)
            {
                for (int y = startOffset; y <= endOffset; y++)
                {
                    for (int z = startOffset; z <= endOffset; z++)
                    {
                        var cellPos = centerPos
                            + tangentX * (x * cellSize.x)
                            + tangentY * (y * cellSize.y)
                            + tangentZ * (z * cellSize.z);
                        positions.Add(cellPos);
                    }
                }
            }
        }

        private static void GetSphereBrushPositions(Vector3 centerPos, Vector3 cellSize,
            Quaternion cellRotation, int brushSize,
            System.Collections.Generic.List<Vector3> positions)
        {
            _blockCellsCountX = brushSize;
            _blockCellsCountY = brushSize;
            _blockCellsCountZ = brushSize;

            int halfSize = brushSize / 2;
            int startOffset = -halfSize;
            int endOffset = brushSize - halfSize - 1;
            float radiusSqr = (brushSize / 2f) * (brushSize / 2f);

            var tangentX = cellRotation * Vector3.right;
            var tangentY = cellRotation * Vector3.up;
            var tangentZ = cellRotation * Vector3.forward;

            float centerOffset = (brushSize % 2 == 0) ? 0.5f : 0f;

            for (int x = startOffset; x <= endOffset; x++)
            {
                for (int y = startOffset; y <= endOffset; y++)
                {
                    for (int z = startOffset; z <= endOffset; z++)
                    {
                        float dx = x + centerOffset;
                        float dy = y + centerOffset;
                        float dz = z + centerOffset;
                        float distSqr = dx * dx + dy * dy + dz * dz;
                        if (distSqr > radiusSqr) continue;
                        var cellPos = centerPos
                            + tangentX * (x * cellSize.x)
                            + tangentY * (y * cellSize.y)
                            + tangentZ * (z * cellSize.z);
                        positions.Add(cellPos);
                    }
                }
            }
        }
    }
}
#pragma warning restore UDR0001
