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
        private static void UpdateFirstModularBrushstroke(ModularToolBase settings, bool setNextIdx)
        {
            if (PaletteManager.selectedBrush == null) return;

            var prevBrushstroke = _brushstroke.ToArray();
            _brushstroke.Clear();

            if (!PaletteManager.selectedBrush.restartPatternForEachStroke && prevBrushstroke.Length > 0)
            {
                var prev = prevBrushstroke[0];
                PaletteManager.selectedBrush.SetPatternTokenIndex(prev.tokenIndex);
            }

            int nextIdx;
            if (PaletteManager.selectedBrush.restartPatternForEachStroke)
            {
                PaletteManager.selectedBrush.ResetCurrentItemIndex();
                nextIdx = PaletteManager.selectedBrush.currentItemIndex;
            }
            else if (setNextIdx)
            {
                nextIdx = PaletteManager.selectedBrush.nextItemIndex;
            }
            else
            {
                nextIdx = PaletteManager.selectedBrush.currentItemIndex;
            }

            if (!PaletteManager.selectedBrush.restartPatternForEachStroke
                && !setNextIdx
                && prevBrushstroke.Length > 0)
            {
                nextIdx = prevBrushstroke[0].index;
            }

            if (nextIdx == -1) return;
            if (PaletteManager.selectedBrush.frequencyMode == MultibrushSettings.FrequencyMode.PATTERN
                && nextIdx == -2)
            {
                if (PaletteManager.selectedBrush.patternMachine != null) PaletteManager.selectedBrush.patternMachine.Reset();
                else return;
            }

            var forwardAxis = settings.forwardAxis;
            if (settings is FloorSettings)
            {
                var floorSettings = (FloorSettings)settings;
                var quarterTurns = FloorManager.quarterTurns;
                if (floorSettings.swapXZ) ++quarterTurns;
                forwardAxis = Quaternion.AngleAxis(-90 * quarterTurns, settings.upwardAxis) * forwardAxis;
            }
            else if (settings is BlockSettings)
            {
                var blockSettings = (BlockSettings)settings;
                var quarterTurns = BlockManager.quarterTurns;
                forwardAxis = Quaternion.AngleAxis(-90 * quarterTurns, settings.upwardAxis) * forwardAxis;
            }
            else if (settings is WallSettings && WallManager.halfTurn)
                forwardAxis = Quaternion.AngleAxis(180, settings.upwardAxis) * forwardAxis;

            var angle = AxesUtils.SignedAxis.GetEulerAnglesFromAxes(forwardAxis, settings.upwardAxis);
            angle = (Quaternion.Euler(angle) * GridManager.settings.rotation).eulerAngles;
            var scale = ScaleMultiplier(nextIdx, settings);
            scale.x = Mathf.Abs(scale.x);
            scale.y = Mathf.Abs(scale.y);
            scale.z = Mathf.Abs(scale.z);
            AddBrushstrokeItem(nextIdx, PaletteManager.selectedBrush.GetPatternTokenIndex(),
                tangentPosition: Vector3.zero, angle, scale, settings);

            const int maxTries = 10;
            int tryCount = 0;
            while (_brushstroke.Count == 0 && ++tryCount < maxTries)
            {
                nextIdx = PaletteManager.selectedBrush.nextItemIndex;
                if (nextIdx >= 0)
                {
                    AddBrushstrokeItem(nextIdx, PaletteManager.selectedBrush.GetPatternTokenIndex(),
                        tangentPosition: Vector3.zero, angle, scale, settings);
                    break;
                }
            }
        }
    }
}
#pragma warning restore UDR0001
