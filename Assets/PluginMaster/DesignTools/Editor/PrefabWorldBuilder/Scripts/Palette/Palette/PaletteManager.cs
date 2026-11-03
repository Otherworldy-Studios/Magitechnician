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
    #region DATA STRUCTURES
    [System.Serializable]
    public struct BasicPaletteData
    {
        [SerializeField] private int _hashCode;
        [SerializeField] private string _name;
        [SerializeField] private long _id;
        public int hashCode => _hashCode;
        public string name => _name;
        public long id => _id;
    }
    #endregion

    #region PALETTE MANAGER
    [System.Serializable]
    public partial class PaletteManager : ISerializationCallbackReceiver
    {

        #region SINGLETON
        private static PaletteManager _instance = null;
        private PaletteManager() { }
        public static PaletteManager instance
        {
            get
            {
                if (_instance == null) _instance = new PaletteManager();
                return _instance;
            }
        }
        #endregion

        #region PALETTE LISTS
        private System.Collections.Generic.List<PaletteData> _nonPinnedPaletteDataList
            = new System.Collections.Generic.List<PaletteData>();
        private System.Collections.Generic.List<PaletteData> _pinnedPaletteDataList
            = new System.Collections.Generic.List<PaletteData>();
        public static System.Collections.Generic.List<PaletteData> nonPinnedPalettes => instance._nonPinnedPaletteDataList;
        public static System.Collections.Generic.List<PaletteData> pinnedPalettes => instance._pinnedPaletteDataList;
        public static System.Collections.Generic.List<PaletteData> allPalettes
        {
            get
            {
                var result = new System.Collections.Generic.List<PaletteData>(instance._pinnedPaletteDataList);
                result.AddRange(instance._nonPinnedPaletteDataList);
                return result;
            }
        }
        #endregion

        #region EVENTS
        public static System.Action OnBrushSelectionChanged = null;
        public static System.Action OnSelectionChanged = null;
        public static System.Action OnPaletteChanged = null;
        #endregion

        #region CLEAR & RESET
        public static void Clear()
        {
            ClearPaletteList();
            instance.CreateEmptyPalette();
            instance._selectedBrushIdx = -1;
            instance._idxSelection.Clear();
            instance._pickingBrushes = false;
        }

        public static void ClearPaletteList() => instance._nonPinnedPaletteDataList.Clear();
        #endregion

        #region DISPLAY SETTINGS
        [SerializeField] private bool _showBrushName = false;
        [SerializeField] private bool _viewList = false;
        [SerializeField] private bool _showTabsInMultipleRows = false;
        [SerializeField] private int _iconSize = PrefabPalette.DEFAULT_ICON_SIZE;
        public static bool showBrushName
        {
            get => instance._showBrushName;
            set
            {
                if (instance._showBrushName == value) return;
                instance._showBrushName = value;
                PWBCore.staticData.SaveAndUpdateVersion();
            }
        }

        public static bool viewList
        {
            get => instance._viewList;
            set
            {
                if (instance._viewList == value) return;
                instance._viewList = value;
                PWBCore.staticData.SaveAndUpdateVersion();
            }
        }

        public static bool showTabsInMultipleRows
        {
            get => instance._showTabsInMultipleRows;
            set
            {
                if (instance._showTabsInMultipleRows == value) return;
                instance._showTabsInMultipleRows = value;
                PWBCore.staticData.SaveAndUpdateVersion();
            }
        }

        public static int iconSize
        {
            get => instance._iconSize;
            set
            {
                if (instance._iconSize == value) return;
                instance._iconSize = value;
                PWBCore.staticData.SaveAndUpdateVersion();
            }
        }
        #endregion

        #region PALETTE MANAGEMENT
        private PaletteData _selectedPalette = null;
        [SerializeField] private long _selectedPaletteId = -1;
        public static bool addingPalettes { get; set; }
        private PaletteData CreateEmptyPalette()
        {
            var p = new PaletteData("Palette", System.DateTime.Now.ToBinary());
            AddPalette(p, save: true);
            _selectedPalette = p;
            return p;
        }

        private System.Collections.Generic.List<PaletteData> nonPinnedPaletteDataList
        {
            get
            {
                if (_nonPinnedPaletteDataList.Count == 0 && _pinnedPaletteDataList.Count == 0)
                {
                    CreateEmptyPalette();
                    _selectedBrushIdx = -1;
                }
                return _nonPinnedPaletteDataList;
            }
        }
        public static void AddPalette(PaletteData palette, bool save)
        {
            addingPalettes = true;
            if (allPalettes.Exists(p => p.id == palette.id)) return;
            if (palette.isPinned) instance._pinnedPaletteDataList.Add(palette);
            else instance._nonPinnedPaletteDataList.Add(palette);
            if (save)
            {
                palette.filePath = PWBData.palettesDirectory + "/"
                    + PaletteData.GetFileNameFromData(palette, includeExtension: true);
                palette.Save();
            }
        }

        public static void DuplicatePalette(PaletteData original)
        {
            if (original == null) return;
            var cloneId = System.DateTime.Now.ToBinary();
            var cloneName = original.name + " Copy";
            var clone = new PaletteData(cloneName, cloneId);
            clone.Copy(original);
            clone.name = cloneName;
            AddPalette(clone, save: true);
            clone.UpdateAllThumbnails();
        }

        public static void RemovePalette(PaletteData palette)
        {
            if (palette == null) return;
            if (!instance._nonPinnedPaletteDataList.Contains(palette) && !instance._pinnedPaletteDataList.Contains(palette))
                return;
            var filePath = palette.filePath;
            var thumbnailFolderPath = palette.thumbnailsFolderPath;
            if (!instance._nonPinnedPaletteDataList.Remove(palette))
                instance._pinnedPaletteDataList.Remove(palette);
            DeletePaletteFile(filePath, thumbnailFolderPath);
            if (instance._selectedPalette == palette) instance._selectedPalette = null;

            if (allPalettesCount == 0) instance.CreateEmptyPalette();
            else
            {
                var isSelected = instance._selectedPalette == palette;
                if (isSelected) PaletteManager.SelectPreviousPalette();
            }
            PaletteManager.selectedBrushIdx = -1;
            PWBCore.refreshDatabase = true;
        }

        private static void DeletePaletteFile(string filePath, string thumbnailFolderPath)
        {
            var metapath = filePath + ".meta";
            if (System.IO.File.Exists(metapath)) System.IO.File.Delete(metapath);
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
            metapath = thumbnailFolderPath + ".meta";
            if (System.IO.File.Exists(metapath)) System.IO.File.Delete(metapath);
            if (System.IO.Directory.Exists(thumbnailFolderPath))
                System.IO.Directory.Delete(thumbnailFolderPath, true);
        }
        public static void Cleanup()
        {
            foreach (var palette in allPalettes.ToArray()) palette.Cleanup();
        }
        public static void RepairItemParentIds()
        {
            foreach (var palette in allPalettes.ToArray()) palette.RepairItemParentIds();
        }
        #endregion

        #region PALETTE NAVIGATION
        public static void SelectNextPalette()
        {
            if (PrefabPalette.instance == null) return;
            if (allPalettesCount <= 1) return;
            instance._idxSelection.Clear();

            var paletteList = new System.Collections.Generic.List<PaletteData>(instance._pinnedPaletteDataList);
            if (PWBCore.staticData.selectTheNextPaletteInAlphabeticalOrder)
                paletteList.AddRange(instance.nonPinnedPaletteDataList.OrderBy(item => item.name).ToList());
            else paletteList.AddRange(instance.nonPinnedPaletteDataList);

            var nextPaletteIdx = paletteList.IndexOf(selectedPalette) + 1;
            if (nextPaletteIdx >= paletteList.Count) nextPaletteIdx = 0;
            var NextPalette = paletteList[nextPaletteIdx];

            PrefabPalette.instance.SelectPalette(NextPalette);
            if (!showTabsInMultipleRows) ShowSelectedFirst();
            selectedBrushIdx = 0;
            AddToSelection(selectedBrushIdx);
            PrefabPalette.instance.FrameSelectedBrush();
            PrefabPalette.RepaintWindow();
        }

        public static void SelectPreviousPalette()
        {
            if (PrefabPalette.instance == null) return;
            if (allPalettesCount == 0) return;
            if (allPalettesCount == 1 || selectedPalette == null)
            {
                var palette = allPalettes[0];
                PrefabPalette.instance.SelectPalette(palette);
                selectedBrushIdx = -1;
                return;
            }
            instance._idxSelection.Clear();

            var paletteList = new System.Collections.Generic.List<PaletteData>(instance._pinnedPaletteDataList);
            if (PWBCore.staticData.selectTheNextPaletteInAlphabeticalOrder)
                paletteList.AddRange(instance.nonPinnedPaletteDataList.OrderBy(item => item.name).ToList());
            else paletteList.AddRange(instance.nonPinnedPaletteDataList);

            var nextPaletteIdx = paletteList.IndexOf(selectedPalette) - 1;
            if (nextPaletteIdx < 0) nextPaletteIdx = paletteList.Count - 1;
            var NextPalette = paletteList[nextPaletteIdx];

            PrefabPalette.instance.SelectPalette(NextPalette);
            if (!showTabsInMultipleRows) ShowSelectedFirst();
            selectedBrushIdx = 0;
            AddToSelection(selectedBrushIdx);
            PrefabPalette.instance.FrameSelectedBrush();
            PrefabPalette.RepaintWindow();
        }
        #endregion

        #region PALETTE PINNING
        public static int pinnedCount => instance._pinnedPaletteDataList.Count;
        public static int nonPinnedCount => instance._nonPinnedPaletteDataList.Count;
        public static int allPalettesCount => instance._nonPinnedPaletteDataList.Count + instance._pinnedPaletteDataList.Count;
        public static string[] pinnedPaletteNames
        {
            get
            {
                var pinnedPalettesNames = new string[instance._pinnedPaletteDataList.Count];
                for (int i = 0; i < instance._pinnedPaletteDataList.Count; i++)
                    pinnedPalettesNames[i] = instance._pinnedPaletteDataList[i].name;
                return pinnedPalettesNames;
            }
        }

        public static long[] pinnedPaletteIds
        {
            get
            {
                var pinnedPalettesIds = new long[instance._pinnedPaletteDataList.Count];
                for (int i = 0; i < instance._pinnedPaletteDataList.Count; i++)
                    pinnedPalettesIds[i] = instance._pinnedPaletteDataList[i].id;
                return pinnedPalettesIds;
            }
        }
        public static void TogglePinnedPalette(PaletteData palette)
        {
            if (palette == null) return;

            if (palette.isPinned)
            {
                palette.isPinned = false;
                if (!instance.nonPinnedPaletteDataList.Contains(palette)) instance.nonPinnedPaletteDataList.Add(palette);
                instance._pinnedPaletteDataList.RemoveAll(p => p.id == palette.id);
            }
            else
            {
                palette.isPinned = true;
                instance.nonPinnedPaletteDataList.RemoveAll(p => p.id == palette.id);
                if (!instance._pinnedPaletteDataList.Contains(palette)) instance._pinnedPaletteDataList.Add(palette);
            }
        }
        #endregion

        #region THUMBNAIL OPERATIONS
        public static void UpdateSelectedThumbnails()
        {
            foreach (var idx in instance._idxSelection)
                selectedPalette.GetBrush(idx).UpdateThumbnail(updateItemThumbnails: true, savePng: true);
        }

        public static void UpdateAllThumbnails()
        {
            var palettes = allPalettes.ToArray();
            foreach (var palette in palettes) palette.UpdateAllThumbnails();
        }

        public static string[] GetPaletteThumbnailFolderPaths()
        {
            var paths = new string[allPalettes.Count];
            for (int i = 0; i < paths.Length; ++i) paths[i] = allPalettes[i].thumbnailsFolderPath;
            return paths;
        }
        #endregion

        #region SERIALIZATION
        public void OnBeforeSerialize()
        {
            _selectedPaletteId = _selectedPalette == null ? -1 : _selectedPalette.id;
        }

        public void OnAfterDeserialize()
        {
        }
        #endregion

        #region SAVE OPERATIONS
        private static bool _savePending = false;
        public static bool savePending => _savePending;

        public static void SetSavePending()
        {
            _savePending = true;
        }

        private static void SavePalettes()
        {
            foreach (var palette in allPalettes) palette.Save();
        }

        public static void SaveIfPending()
        {
            if (_savePending) SavePalettes();
            _savePending = false;
        }
        #endregion

        #region CLIPBOARD
        private static BrushSettings _clipboardSettings = null;
        private static ThumbnailSettings _clipboardThumbnailSettings = null;
        public enum Trit { FALSE, TRUE, SAME }
        private static Trit _clipboardOverwriteThumbnailSettings = Trit.FALSE;
        public static BrushSettings clipboardSetting { get => _clipboardSettings; set => _clipboardSettings = value; }
        public static ThumbnailSettings clipboardThumbnailSettings
        { get => _clipboardThumbnailSettings; set => _clipboardThumbnailSettings = value; }
        public static Trit clipboardOverwriteThumbnailSettings
        { get => _clipboardOverwriteThumbnailSettings; set => _clipboardOverwriteThumbnailSettings = value; }
        #endregion


    }
    #endregion
}
#pragma warning restore UDR0001
