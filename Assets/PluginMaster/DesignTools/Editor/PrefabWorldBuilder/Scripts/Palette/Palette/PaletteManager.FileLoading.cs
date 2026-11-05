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
    public partial class PaletteManager : ISerializationCallbackReceiver
    {

        private static bool _loadPaletteFilesPending = false;
        static System.DateTime _lastLoadTime = System.DateTime.MinValue;

        public static void SetLoadPaletteFilesPending()
        {
            _loadPaletteFilesPending = true;
        }

        public static bool loadPaletteFilesPending => _loadPaletteFilesPending;

        public void LoadPaletteFiles(bool deleteUnusedThumbnails)
        {
            const double throttleSeconds = 3.0;
            var now = System.DateTime.Now;
            if ((now - _lastLoadTime).TotalSeconds < throttleSeconds)
                return;
            _lastLoadTime = now;
            _loadPaletteFilesPending = false;
            var palDir = PWBData.palettesDirectory;
            if (!System.IO.Directory.Exists(palDir)) return;
            var txtPaths = System.IO.Directory.GetFiles(palDir, "*.txt");
            if (txtPaths.Length == 0)
            {
                if (_nonPinnedPaletteDataList.Count == 0) CreateEmptyPalette();
                _nonPinnedPaletteDataList[0].filePath = _nonPinnedPaletteDataList[0].Save();
            }
            var allPalettesList = allPalettes;

            System.Collections.Generic.List<PaletteData> toRemove = new System.Collections.Generic.List<PaletteData>();
            System.Collections.Generic.HashSet<long> loadedIds = new System.Collections.Generic.HashSet<long>();
            foreach (var path in txtPaths)
            {
                var fileText = System.IO.File.ReadAllText(path);
                if (string.IsNullOrEmpty(fileText)) continue;
                try
                {
                    var basicPaletteDataFromFile = JsonUtility.FromJson<BasicPaletteData>(fileText);
                    if (allPalettesList.Count > 0)
                    {
                        var sameHash = allPalettesList.Where(
                            p => p.GetHashCode() == basicPaletteDataFromFile.hashCode);
                        if (sameHash.Count() > 0)
                        {
                            toRemove.AddRange(sameHash);
                            continue;
                        }
                        var sameId = allPalettesList.Where(p => p.id == basicPaletteDataFromFile.id);
                        if (sameId.Count() > 0)
                        {
                            var currentData = sameId.First();
                            currentData.Save();
                            toRemove.AddRange(sameId);
                            continue;
                        }
                    }
                    var paletteData = JsonUtility.FromJson<PaletteData>(fileText);
                    if (paletteData == null) continue;
                    var loadedId = paletteData.id;
                    if (loadedIds.Contains(loadedId))
                    {
                        Debug.LogWarning($"PWB found a duplicated palette id in file: {path}, " +
                            $"name: {paletteData.name}, id: {PaletteData.GetFileNameFromData(paletteData, false)}");
                        DeletePaletteFile(path, paletteData.thumbnailsFolderPath);
                        continue;
                    }
                    loadedIds.Add(loadedId);
                    paletteData.filePath = path;
                    AddPalette(paletteData, save: false);
                    if (paletteData.RepairItemParentIds())
                    {
                        Debug.Log($"[PWB] Repaired corrupted parent references in palette: '{paletteData.name}'");
                        paletteData.Save();
                    }
                }
                catch
                {
                    Debug.LogWarning($"PWB found a corrupted palette file at: {path}");
                }
                for (int i = 0; i < toRemove.Count; i++) RemovePalette(toRemove[i]);
                toRemove.Clear();
            }
            if (deleteUnusedThumbnails) ThumbnailUtils.DeleteUnusedThumbnails();
        }
    }
}
#pragma warning restore UDR0001
