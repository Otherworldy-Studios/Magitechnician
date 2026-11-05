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
using UnityEngine;

namespace PluginMaster
{
    public static partial class BrushstrokeManager
    {

        private static int _cellsCountX = 0;
        private static int _cellsCountZ = 0;

        public static int cellsCountX => _cellsCountX;
        public static int cellsCountZ => _cellsCountZ;
        public static void ResetCellCount()
        {
            _cellsCountX = 1;
            _cellsCountZ = 1;
        }
        public static void UpdateFloorBrushstroke(bool setNextIdx, bool deleteBox = false)
        {
            ResetCellCount();
            if (FloorManager.state == FloorManager.ToolState.FIRST_CORNER)
            {
                UpdateFirstModularBrushstroke(FloorManager.settings, setNextIdx);
                return;
            }
            var toolSettings = FloorManager.settings;
            var diagonal = FloorManager.secondCorner - FloorManager.firstCorner;
            var localDiagonal = Quaternion.Inverse(GridManager.settings.rotation) * diagonal;

            _cellsCountX = Mathf.RoundToInt(Mathf.Abs(localDiagonal.x / toolSettings.moduleSize.x)) + 1;
            var signX = localDiagonal.x >= 0 ? 1 : -1;
            var dirX = GridManager.settings.rotation * (Vector3.right * signX);
            var deltaX = dirX * toolSettings.moduleSize.x;

            _cellsCountZ = Mathf.RoundToInt(Mathf.Abs(localDiagonal.z / toolSettings.moduleSize.z)) + 1;
            var signZ = localDiagonal.z >= 0 ? 1 : -1;
            var dirZ = GridManager.settings.rotation * (Vector3.forward * signZ);
            var deltaZ = dirZ * toolSettings.moduleSize.z;

            var localRotation = Quaternion.FromToRotation(Vector3.up, toolSettings.upwardAxis);
            var rotation = GridManager.settings.rotation * localRotation;
            var angle = rotation.eulerAngles;

            var prevBrushstroke = _brushstroke.ToArray();
            _brushstroke.Clear();

            if (PaletteManager.selectedBrush.restartPatternForEachStroke)
                PaletteManager.selectedBrush.ResetCurrentItemIndex();
            var floorItemsCount = 0;
            for (int xIdx = 0; xIdx < _cellsCountX; ++xIdx)
            {
                var tangent = deltaX * xIdx;
                for (int zIdx = 0; zIdx < _cellsCountZ; ++zIdx)
                {
                    var bitangent = deltaZ * zIdx;
                    var cellCenter = FloorManager.firstCorner + tangent + bitangent;
                    var idx = PaletteManager.selectedBrush.currentItemIndex;
                    BrushSettings brush = PaletteManager.selectedBrush.GetItemAt(idx);
                    if (toolSettings.overwriteBrushProperties) brush = toolSettings.brushSettings;
                    if (toolSettings.subtractBrushOffset)
                    {
                        var r = GridManager.settings.rotation;
                        if (FloorManager.quarterTurns > 0)
                            r *= Quaternion.AngleAxis(FloorManager.quarterTurns * 90, toolSettings.upwardAxis);
                        cellCenter += r * brush.localPositionOffset;
                    }
                    else cellCenter += rotation * brush.localPositionOffset;

                    if (deleteBox)
                    {
                        var additionalAngle = (Quaternion.Euler(angle)
                            * Quaternion.Euler(PaletteManager.selectedBrush.eulerOffset)).eulerAngles;
                        var strokeItem = new BrushstrokeItem(index: 0, tokenIndex: 0, brush as MultibrushItemSettings,
                            cellCenter, additionalAngle, scaleMultiplier: FloorManager.settings.moduleSize,
                            flipX: false, flipY: false, surfaceDistance: 0);
                        _brushstroke.Add(strokeItem);
                    }
                    else
                    {
                        var tokenIdx = PaletteManager.selectedBrush.GetPatternTokenIndex();
                        if (!PaletteManager.selectedBrush.restartPatternForEachStroke
                            && prevBrushstroke.Length > floorItemsCount)
                        {
                            idx = prevBrushstroke[floorItemsCount].index;
                            tokenIdx = prevBrushstroke[floorItemsCount].tokenIndex;
                            PaletteManager.selectedBrush.SetPatternTokenIndex(tokenIdx);
                        }
                        var scale = localRotation * ScaleMultiplier(idx, toolSettings);
                        scale.x = Mathf.Abs(scale.x);
                        scale.y = Mathf.Abs(scale.y);
                        scale.z = Mathf.Abs(scale.z);
                        AddBrushstrokeItem(idx, tokenIdx, cellCenter, angle, scale, toolSettings);
                        if (setNextIdx) PaletteManager.selectedBrush.SetNextItemIndex();
                    }
                    ++floorItemsCount;
                }
            }
        }
    }
}
#pragma warning restore UDR0001
