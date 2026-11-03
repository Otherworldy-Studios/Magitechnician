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
        public static void UpdateBlockReplaceBrushstroke(
            System.Collections.Generic.IEnumerable<Vector3> cellCenters)
        {
            ResetBlockCellCount();
            _brushstroke.Clear();
            if (PaletteManager.selectedBrush == null) return;

            var toolSettings = BlockManager.settings;
            var cellSize = toolSettings.moduleSize;

            var localRotation = Quaternion.FromToRotation(Vector3.up, toolSettings.upwardAxis);
            var rotation = GridManager.settings.rotation * localRotation;
            var angle = rotation.eulerAngles;

            var forwardAxis = toolSettings.forwardAxis;
            if (BlockManager.quarterTurns > 0)
                forwardAxis = Quaternion.AngleAxis(-90 * BlockManager.quarterTurns, toolSettings.upwardAxis) * forwardAxis;
            angle = AxesUtils.SignedAxis.GetEulerAnglesFromAxes(forwardAxis, toolSettings.upwardAxis);
            angle = (Quaternion.Euler(angle) * GridManager.settings.rotation).eulerAngles;

            if (PaletteManager.selectedBrush.restartPatternForEachStroke)
                PaletteManager.selectedBrush.ResetCurrentItemIndex();

            var blockItemsCount = 0;
            foreach (var cellCenter in cellCenters)
            {
                var idx = PaletteManager.selectedBrush.currentItemIndex;
                if (idx == -1) break;
                if (PaletteManager.selectedBrush.frequencyMode == MultibrushSettings.FrequencyMode.PATTERN && idx == -2)
                {
                    if (PaletteManager.selectedBrush.patternMachine != null)
                        PaletteManager.selectedBrush.patternMachine.Reset();
                    else break;
                }

                var adjustedCenter = cellCenter;
                if (toolSettings.subtractBrushOffset)
                {
                    BrushSettings brush = PaletteManager.selectedBrush.GetItemAt(idx);
                    if (toolSettings.overwriteBrushProperties) brush = toolSettings.brushSettings;
                    var r = GridManager.settings.rotation;
                    if (BlockManager.quarterTurns > 0)
                        r *= Quaternion.AngleAxis(BlockManager.quarterTurns * 90, toolSettings.upwardAxis);
                    adjustedCenter += r * (brush.localPositionOffset * 0.5f);
                }

                var scale = localRotation * ScaleMultiplier(idx, toolSettings);
                scale.x = Mathf.Abs(scale.x);
                scale.y = Mathf.Abs(scale.y);
                scale.z = Mathf.Abs(scale.z);
                AddBrushstrokeItem(idx, PaletteManager.selectedBrush.GetPatternTokenIndex(),
                    adjustedCenter, angle, scale, toolSettings);
                PaletteManager.selectedBrush.SetNextItemIndex();

                ++blockItemsCount;
            }

            _blockCellsCountX = Mathf.Max(1, blockItemsCount);
            _blockCellsCountY = 1;
            _blockCellsCountZ = 1;
        }
    }
}
#pragma warning restore UDR0001
