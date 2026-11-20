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
        public static void UpdateWallBrushstroke(AxesUtils.Axis segmentAxis, int cellsCount,
            bool setNextIdx, bool deleteMode)
        {
            if (WallManager.state == WallManager.ToolState.FIRST_WALL_PREVIEW)
            {
                UpdateFirstModularBrushstroke(WallManager.settings, setNextIdx);
                return;
            }
            var toolSettings = WallManager.settings;
            var diagonal = WallManager.endPointSnapped - WallManager.startPointSnapped;
            var localDiagonal = Quaternion.Inverse(GridManager.settings.rotation) * diagonal;
            Vector3 delta;
            if (segmentAxis == AxesUtils.Axis.X)
            {
                var sign = localDiagonal.x >= 0 ? 1 : -1;
                var dir = GridManager.settings.rotation * (Vector3.right * sign);
                delta = dir * GridManager.settings.step.x;
            }
            else
            {
                var sign = localDiagonal.z >= 0 ? 1 : -1;
                var dir = GridManager.settings.rotation * (Vector3.forward * sign);
                delta = dir * GridManager.settings.step.z;
            }

            var firstPoint = WallManager.endPointSnapped - (delta * (cellsCount - 1));
            var localRotation = Quaternion.Euler(AxesUtils.SignedAxis.GetEulerAnglesFromAxes(toolSettings.forwardAxis,
                toolSettings.upwardAxis));
            var rotation = GridManager.settings.rotation * localRotation;
            var angle = rotation.eulerAngles;


            var prevBrushstroke = _brushstroke.ToArray();
            _brushstroke.Clear();
            if (PaletteManager.selectedBrush.restartPatternForEachStroke)
                PaletteManager.selectedBrush.ResetCurrentItemIndex();
            var wallItemsCount = 0;
            void AddItem(Vector3 position, AxesUtils.Axis segmentAxis)
            {
                int nextIdx = 0;
                var brushItem = PaletteManager.selectedBrush.GetItemAt(PaletteManager.selectedBrush.currentItemIndex);
                if (!deleteMode)
                {
                    nextIdx = PaletteManager.selectedBrush.currentItemIndex;
                }
                if (deleteMode)
                {
                    var additionalAngle = (Quaternion.Euler(angle)
                            * Quaternion.Euler(PaletteManager.selectedBrush.eulerOffset)).eulerAngles;
                    var strokeItem = new BrushstrokeItem(index: 0, tokenIndex: 0, brushItem,
                        position, additionalAngle, scaleMultiplier: WallManager.settings.moduleSize,
                        flipX: false, flipY: false, surfaceDistance: 0);
                    _brushstroke.Add(strokeItem);
                }
                else
                {
                    var tokenIdx = PaletteManager.selectedBrush.GetPatternTokenIndex();
                    if (!PaletteManager.selectedBrush.restartPatternForEachStroke && prevBrushstroke.Length > wallItemsCount)
                    {
                        nextIdx = prevBrushstroke[wallItemsCount].index;
                        tokenIdx = prevBrushstroke[wallItemsCount].tokenIndex;
                        PaletteManager.selectedBrush.SetPatternTokenIndex(tokenIdx);
                    }
                    var scale = localRotation * ScaleMultiplier(nextIdx, toolSettings);
                    scale.x = Mathf.Abs(scale.x);
                    scale.y = Mathf.Abs(scale.y);
                    scale.z = Mathf.Abs(scale.z);
                    AddBrushstrokeItem(nextIdx, tokenIdx, position, angle, scale, toolSettings);
                    if (setNextIdx) PaletteManager.selectedBrush.SetNextItemIndex();
                }
                ++wallItemsCount;
            }
            for (int idx = 0; idx < cellsCount; ++idx)
            {
                var tangent = delta * idx;
                var position = firstPoint + tangent;
                AddItem(position, segmentAxis);
            }
        }
    }
}
#pragma warning restore UDR0001
