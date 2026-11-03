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
    public partial class PaletteManager : ISerializationCallbackReceiver
    {
        #region BRUSH QUERIES
        public static MultibrushSettings GetBrushById(long id)
        {
            foreach (var palette in allPalettes)
                foreach (var brush in palette.brushes)
                    if (brush.id == id) return brush;
            return null;
        }

        public static MultibrushSettings GetBrushByThumbnail(string thumbnailPath)
        {
            foreach (var palette in allPalettes)
                foreach (var brush in palette.brushes)
                    if (brush.thumbnailPath == thumbnailPath) return brush;
            return null;
        }

        public static MultibrushSettings GetBrushByItemId(long id)
        {
            foreach (var palette in allPalettes)
                foreach (var brush in palette.brushes)
                    foreach (var item in brush.items)
                        if (item.id == id) return brush;
            return null;
        }

        public static bool BrushExist(long id) => allPalettes.Exists(b => b.id == id);

        public static int GetBrushIdx(long id)
        {
            var palette = selectedPalette;
            var brushes = palette.brushes;
            for (int i = 0; i < brushes.Length; ++i)
                if (brushes[i].id == id) return i;
            return -1;
        }
        #endregion
        #region PALETTE QUERIES
        public static PaletteData GetPalette(MultibrushSettings brush)
        {
            foreach (var palette in allPalettes)
                if (palette.ContainsBrush(brush)) return palette;
            return null;
        }

        public static PaletteData GetPalette(long id)
        {
            foreach (var palette in allPalettes)
                if (palette.id == id) return palette;
            return null;
        }
        #endregion
        #region PALETTE INFO PROPERTIES
        public static string[] paletteNames
        {
            get
            {
                var nonPinnedPalettesNames = new string[instance.nonPinnedPaletteDataList.Count];
                for (int i = 0; i < instance.nonPinnedPaletteDataList.Count; i++)
                    nonPinnedPalettesNames[i] = instance.nonPinnedPaletteDataList[i].name;
                return nonPinnedPalettesNames;
            }
        }

        public static long[] paletteIds
        {
            get
            {
                var nonPinnedPalettesIds = new long[instance.nonPinnedPaletteDataList.Count];
                for (int i = 0; i < instance.nonPinnedPaletteDataList.Count; i++)
                    nonPinnedPalettesIds[i] = instance.nonPinnedPaletteDataList[i].id;
                return nonPinnedPalettesIds;
            }
        }
        #endregion
    }
}
#pragma warning restore UDR0001
