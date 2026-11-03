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

namespace PluginMaster
{
    public class PaletteAssetPostprocessor : UnityEditor.AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool repaintPalette = false;
            var selectedPalette = PaletteManager.selectedPalette;
            foreach (var path in importedAssets)
            {
                if (selectedPalette != null && selectedPalette.ContainsPrefabPath(path))
                {
                    repaintPalette = true;
                    break;
                }
            }
            foreach (var path in deletedAssets)
            {
                if (selectedPalette != null && selectedPalette.ContainsPrefabPath(path))
                {
                    PaletteManager.Cleanup();
                    PaletteManager.ClearSelection();
                    repaintPalette = true;
                    break;
                }
            }
            if (repaintPalette) PrefabPalette.OnChangeRepaint();
        }
    }
}
#pragma warning restore UDR0001
