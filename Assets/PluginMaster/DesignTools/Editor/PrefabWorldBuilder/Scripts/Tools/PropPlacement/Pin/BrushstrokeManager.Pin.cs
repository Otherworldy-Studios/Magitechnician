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
        
        private static int _currentPinIdx = 0;
        public static void SetNextPinBrushstroke(int delta)
        {
            _currentPinIdx = _currentPinIdx + delta;
            var mod = _currentPinIdx % PaletteManager.selectedBrush.itemCount;
            _currentPinIdx = mod < 0 ? PaletteManager.selectedBrush.itemCount + mod : mod;
            _brushstroke.Clear();
            AddBrushstrokeItem(_currentPinIdx, PaletteManager.selectedBrush.GetPatternTokenIndex(),
                tangentPosition: Vector3.zero, angle: Vector3.zero,
                ScaleMultiplier(_currentPinIdx, PinManager.settings), PinManager.settings);
        }
    }
}
#pragma warning restore UDR0001
