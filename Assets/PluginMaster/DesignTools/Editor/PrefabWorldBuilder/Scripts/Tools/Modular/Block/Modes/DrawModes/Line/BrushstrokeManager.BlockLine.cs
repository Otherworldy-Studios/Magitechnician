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
        public static void UpdateBlockLineBrushstroke(bool setNextIdx)
        {
            ResetBlockCellCount();
            if (PaletteManager.selectedBrush == null) return;

            if (BlockToolModes.lineState == BlockToolModes.LineState.FIRST_POINT)
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

            var cellPositions = GetLineCellPositions(BlockToolModes.lineFirstPoint, BlockToolModes.lineSecondPoint,
                cellSize, cellRotation, BlockToolModes.projectionAxis);

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

        public static void UpdateDeleteBlockLineBrushstroke()
        {
            ResetBlockCellCount();

            var toolSettings = BlockManager.settings;
            var cellSize = toolSettings.moduleSize;

            _brushstroke.Clear();

            var cellPositions = GetLineCellPositions(BlockToolModes.lineFirstPoint, BlockToolModes.lineSecondPoint,
                cellSize, GridManager.settings.rotation, BlockToolModes.projectionAxis);
            foreach (var cellCenter in cellPositions)
            {
                var strokeItem = new BrushstrokeItem(cellCenter);
                _brushstroke.Add(strokeItem);
            }
        }
        private static System.Collections.Generic.List<Vector3> GetLineCellPositions(Vector3 startPoint,
            Vector3 endPoint, Vector3 cellSize, Quaternion cellRotation,
            BlockToolModes.ProjectionAxis projectionAxis)
        {
            var positions = new System.Collections.Generic.List<Vector3>();

            var projectedEnd = GetProjectedEndPoint(startPoint, endPoint, projectionAxis, cellRotation);
            var inverseRotation = Quaternion.Inverse(cellRotation);
            var localStart = inverseRotation * startPoint;
            var localEnd = inverseRotation * projectedEnd;
            var localDirection = localEnd - localStart;

            var cellStepsX = cellSize.x > 0 ? Mathf.RoundToInt(localDirection.x / cellSize.x) : 0;
            var cellStepsY = cellSize.y > 0 ? Mathf.RoundToInt(localDirection.y / cellSize.y) : 0;
            var cellStepsZ = cellSize.z > 0 ? Mathf.RoundToInt(localDirection.z / cellSize.z) : 0;

            var absCellStepsX = Mathf.Abs(cellStepsX);
            var absCellStepsY = Mathf.Abs(cellStepsY);
            var absCellStepsZ = Mathf.Abs(cellStepsZ);

            _blockCellsCountX = Mathf.Max(1, absCellStepsX + 1);
            _blockCellsCountY = Mathf.Max(1, absCellStepsY + 1);
            _blockCellsCountZ = Mathf.Max(1, absCellStepsZ + 1);

            var stepCount = Mathf.Max(absCellStepsX, absCellStepsY, absCellStepsZ);
            if (stepCount == 0)
            {
                positions.Add(startPoint);
                return positions;
            }

            var tangentX = cellRotation * Vector3.right;
            var tangentY = cellRotation * Vector3.up;
            var tangentZ = cellRotation * Vector3.forward;

            var visitedCells = new System.Collections.Generic.HashSet<Vector3Int>();

            for (int i = 0; i <= stepCount; i++)
            {
                float t = (float)i / stepCount;
                var cellX = Mathf.RoundToInt(t * cellStepsX);
                var cellY = Mathf.RoundToInt(t * cellStepsY);
                var cellZ = Mathf.RoundToInt(t * cellStepsZ);

                var cellCoord = new Vector3Int(cellX, cellY, cellZ);
                if (visitedCells.Contains(cellCoord)) continue;
                visitedCells.Add(cellCoord);

                var cellPos = startPoint
                    + tangentX * (cellX * cellSize.x)
                    + tangentY * (cellY * cellSize.y)
                    + tangentZ * (cellZ * cellSize.z);
                positions.Add(cellPos);
            }

            return positions;
        }

        private static Vector3 GetProjectedEndPoint(Vector3 startPoint, Vector3 endPoint,
            BlockToolModes.ProjectionAxis projectionAxis, Quaternion cellRotation)
        {
            if (projectionAxis == BlockToolModes.ProjectionAxis.NONE) return endPoint;

            var planeNormal = GetProjectionPlaneNormal(projectionAxis, cellRotation);
            var direction = endPoint - startPoint;
            var projectedDirection = Vector3.ProjectOnPlane(direction, planeNormal);
            return startPoint + projectedDirection;
        }

        private static Vector3 GetProjectionPlaneNormal(BlockToolModes.ProjectionAxis projectionAxis,
            Quaternion cellRotation)
        {
            switch (projectionAxis)
            {
                case BlockToolModes.ProjectionAxis.CAMERA:
                    var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                    return sceneView != null ? sceneView.camera.transform.forward : Vector3.forward;
                case BlockToolModes.ProjectionAxis.DOWN:
                    return cellRotation * Vector3.down;
                case BlockToolModes.ProjectionAxis.UP:
                    return cellRotation * Vector3.up;
                case BlockToolModes.ProjectionAxis.BACK:
                    return cellRotation * Vector3.back;
                case BlockToolModes.ProjectionAxis.FORWARD:
                    return cellRotation * Vector3.forward;
                case BlockToolModes.ProjectionAxis.LEFT:
                    return cellRotation * Vector3.left;
                case BlockToolModes.ProjectionAxis.RIGHT:
                    return cellRotation * Vector3.right;
                default:
                    return Vector3.up;
            }
        }
    }
}
#pragma warning restore UDR0001
