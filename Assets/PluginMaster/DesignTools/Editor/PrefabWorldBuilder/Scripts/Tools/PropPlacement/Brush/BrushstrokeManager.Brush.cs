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
    public static partial class BrushstrokeManager
    {
        private static bool TangentPositionIsOverlapedByBrushstrokeItems(float x, float y, float minSpacing)
        {
            var minSpacingSqr = minSpacing * minSpacing;
            for (int i = 0; i < _brushstroke.Count; ++i)
            {
                var item = _brushstroke[i];
                if (item.tangentPosition == Vector3.zero) continue;
                var dx = item.tangentPosition.x - x;
                var dy = item.tangentPosition.y - y;
                var distanceSqr = dx * dx + dy * dy;
                if (distanceSqr < minSpacingSqr) return true;
            }
            return false;
        }
        private static void UpdateBrushBaseStroke(BrushToolBase ToolSettings)
        {
            if (ToolSettings.spacingType == BrushToolBase.SpacingType.AUTO)
            {
                var maxSize = 0.1f;
                foreach (var item in PaletteManager.selectedBrush.items)
                {
                    if (item.prefab == null) continue;
                    var itemSize = BoundsUtils.GetBoundsRecursive(item.prefab.transform).size;
                    itemSize = Vector3.Scale(itemSize,
                        item.randomScaleMultiplier ? item.maxScaleMultiplier : item.scaleMultiplier);
                    maxSize = Mathf.Max(itemSize.x, itemSize.z, maxSize);
                }
                ToolSettings.minSpacing = maxSize;
                ToolProperties.RepainWindow();
            }

            if (ToolSettings.brushShape == BrushToolSettings.BrushShape.POINT)
            {
                var nextIdx = PaletteManager.selectedBrush.nextItemIndex;
                if (nextIdx == -1) return;
                if (PaletteManager.selectedBrush.frequencyMode == PluginMaster.MultibrushSettings.FrequencyMode.PATTERN
                    && nextIdx == -2) return;
                _brushstroke.Clear();

                AddBrushstrokeItem(nextIdx, PaletteManager.selectedBrush.GetPatternTokenIndex(),
                    tangentPosition: Vector3.zero, angle: Vector3.zero,
                    scale: ScaleMultiplier(nextIdx, ToolSettings), ToolSettings);
                _currentPinIdx = Mathf.Clamp(nextIdx, 0, PaletteManager.selectedBrush.itemCount - 1);
            }
            else
            {
                var radius = ToolSettings.radius;
                var radiusSqr = radius * radius;

                var minSpacing = ToolSettings.minSpacing * 100f / Mathf.Max(ToolSettings.density, 1);

                var delta = Mathf.Max(minSpacing, 0.01f);
                var maxRandomOffset = delta * ToolSettings.randomness;

                int halfSize = (int)Mathf.Ceil(radius / delta) + 1;
                const int MAX_SIZE = 32;
                if (halfSize > MAX_SIZE)
                {
                    halfSize = MAX_SIZE;
                    delta = radius / MAX_SIZE;
                    minSpacing = delta;
                    maxRandomOffset = delta * ToolSettings.randomness;
                }
                int size = halfSize * 2;
                float col0x = -delta * halfSize;
                float row0y = -delta * halfSize;


                var availableCells = new System.Collections.Generic.HashSet<(int row, int col)>();
                for (int row = 0; row < size; ++row)
                {
                    for (int col = 0; col < size; ++col)
                    {
                        var x = col0x + col * delta;
                        var y = row0y + row * delta;
                        if (ToolSettings.brushShape == BrushToolBase.BrushShape.CIRCLE)
                        {
                            var distanceSqr = x * x + y * y;
                            if (distanceSqr >= radiusSqr) continue;
                        }
                        availableCells.Add((row, col));
                    }
                }

                while (availableCells.Count > 0)
                {
                    var randomIdx = Random.Range(0, availableCells.Count);
                    var cell = availableCells.ElementAt(randomIdx);
                    var col = cell.col;
                    var row = cell.row;
                    var x = col0x + col * delta;
                    var y = row0y + row * delta;

                    if (ToolSettings.randomizePositions)
                    {
                        x += Random.Range(-maxRandomOffset, maxRandomOffset);
                        y += Random.Range(-maxRandomOffset, maxRandomOffset);
                        col = Mathf.Clamp(Mathf.RoundToInt((x - col0x) / delta), 0, size - 1);
                        row = Mathf.Clamp(Mathf.RoundToInt((y - row0y) / delta), 0, size - 1);
                        cell = (row, col);
                        if (availableCells.Contains(cell)) availableCells.Remove(cell);
                        else continue;
                    }
                    else availableCells.Remove(cell);
                    if (ToolSettings is BrushToolSettings)
                    {
                        var btoolSettings = ToolSettings as BrushToolSettings;
                        if (btoolSettings.avoidOverlapping != BrushToolSettings.AvoidOverlappingType.DISABLED
                            && TangentPositionIsOverlapedByBrushstrokeItems(x, y, minSpacing))
                            continue;
                    }
                    if (ToolSettings.brushShape == BrushToolBase.BrushShape.CIRCLE)
                    {
                        var distanceSqr = x * x + y * y;
                        if (distanceSqr >= radiusSqr) continue;
                    }
                    else if (ToolSettings.brushShape == BrushToolBase.BrushShape.SQUARE)
                    {
                        if (Mathf.Abs(x) > radius || Mathf.Abs(y) > radius) continue;
                    }
                    var nextItemIdx = PaletteManager.selectedBrush.nextItemIndex;
                    var position = new Vector3(x, y, 0f);
                    if ((PaletteManager.selectedBrush.frequencyMode
                        == MultibrushSettings.FrequencyMode.RANDOM && nextItemIdx == -1)
                        || (PaletteManager.selectedBrush.frequencyMode
                        == MultibrushSettings.FrequencyMode.PATTERN && nextItemIdx == -2)) continue;
                    var item = PaletteManager.selectedBrush.items[nextItemIdx];

                    AddBrushstrokeItem(nextItemIdx, PaletteManager.selectedBrush.GetPatternTokenIndex(),
                        tangentPosition: position, angle: Vector3.zero,
                        ScaleMultiplier(nextItemIdx, ToolSettings), ToolSettings);
                }
            }
        }

        public static void UpdateSingleBrushstroke(IPaintToolSettings settings)
        {
            _brushstroke.Clear();
            if (PaletteManager.selectedBrush == null) return;
            var nextIdx = PaletteManager.selectedBrush.nextItemIndex;
            if (nextIdx == -1) return;
            if (PaletteManager.selectedBrush.frequencyMode == PluginMaster.MultibrushSettings.FrequencyMode.PATTERN
                && nextIdx == -2)
            {
                if (PaletteManager.selectedBrush.patternMachine != null) PaletteManager.selectedBrush.patternMachine.Reset();
                else return;
            }


            AddBrushstrokeItem(nextIdx, PaletteManager.selectedBrush.GetPatternTokenIndex(),
                tangentPosition: Vector3.zero, angle: Vector3.zero,
                scale: ScaleMultiplier(nextIdx, settings), settings);

            const int maxTries = 10;
            int tryCount = 0;
            while (_brushstroke.Count == 0 && ++tryCount < maxTries)
            {
                nextIdx = PaletteManager.selectedBrush.nextItemIndex;
                if (nextIdx >= 0)
                {
                    AddBrushstrokeItem(nextIdx, PaletteManager.selectedBrush.GetPatternTokenIndex(),
                        tangentPosition: Vector3.zero, angle: Vector3.zero,
                        scale: ScaleMultiplier(nextIdx, settings), settings);
                    break;
                }
            }
            _currentPinIdx = Mathf.Clamp(nextIdx, 0, PaletteManager.selectedBrush.itemCount - 1);
        }
    }
}
#pragma warning restore UDR0001
