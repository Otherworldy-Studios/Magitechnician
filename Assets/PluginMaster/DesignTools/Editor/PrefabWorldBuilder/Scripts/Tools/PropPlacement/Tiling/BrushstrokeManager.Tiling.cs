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
        public static void UpdateTilingBrushstroke(Vector3[] cellCenters)
        {
            _brushstroke.Clear();
            if (PaletteManager.selectedBrush == null) return;
            for (int i = 0; i < cellCenters.Length; ++i)
            {
                var nextIdx = PaletteManager.selectedBrush.nextItemIndex;
                AddBrushstrokeItem(nextIdx, PaletteManager.selectedBrush.GetPatternTokenIndex(),
                    cellCenters[i], angle: Vector3.zero, scale: Vector3.one,
                    TilingManager.settings);
            }
            ToolProperties.RepainWindow();
        }

        public static void UpdatePersistentTilingBrushstroke(Vector3[] cellCenters, TilingSettings settings,
            System.Collections.Generic.List<GameObject> tilingObjects,
            out Vector3[] objPositions, out Vector3[] strokePositions)
        {
            _brushstroke.Clear();
            var objPositionsList = new System.Collections.Generic.List<Vector3>();
            var strokePositionsList = new System.Collections.Generic.List<Vector3>();

            for (int i = 0; i < cellCenters.Length; ++i)
            {
                var objectExist = i < tilingObjects.Count;
                var position = cellCenters[i];
                if (objectExist) objPositionsList.Add(position);
                else
                {
                    if (PaletteManager.selectedBrush == null) break;
                    var nextIdx = PaletteManager.selectedBrush.nextItemIndex;
                    AddBrushstrokeItem(nextIdx, PaletteManager.selectedBrush == null ? 0
                        : PaletteManager.selectedBrush.GetPatternTokenIndex(), position,
                        angle: Vector3.zero, scale: Vector3.one,
                        settings);
                    strokePositionsList.Add(position);
                }
            }
            objPositions = objPositionsList.ToArray();
            strokePositions = strokePositionsList.ToArray();
        }
    }
}
#pragma warning restore UDR0001
