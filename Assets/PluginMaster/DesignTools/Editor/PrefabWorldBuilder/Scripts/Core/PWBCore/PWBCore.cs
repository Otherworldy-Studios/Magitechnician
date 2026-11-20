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
using System.Linq;

namespace PluginMaster
{
    #region CORE
    public static partial class PWBCore
    {

        #region REFRESH ASSET DATABASE
        public static bool refreshDatabase { get; set; }
        private static double _lastRefreshTime = -1;
        private static bool _deferredRefreshPending = false;
        private const double REFRESH_COOLDOWN_SECONDS = 3.0;

        public static void AssetDatabaseRefresh()
        {
            if (!ApplicationEventHandler.importingPackage)
            {
                if (!DataReimportHandler.importingAssets && !ApplicationEventHandler.sceneOpening)
                {
                    var now = UnityEditor.EditorApplication.timeSinceStartup;
                    if (_lastRefreshTime >= 0
                        && now - _lastRefreshTime < REFRESH_COOLDOWN_SECONDS)
                    {
                        if (!_deferredRefreshPending)
                        {
                            _deferredRefreshPending = true;
                            UnityEditor.EditorApplication.delayCall += DeferredRefresh;
                        }
                        return;
                    }
                    _lastRefreshTime = now;
                    UnityEditor.AssetDatabase.Refresh();
                }
            }
            else ApplicationEventHandler.RefreshOnImportingCancelled();
            refreshDatabase = false;
        }

        private static void DeferredRefresh()
        {
            _deferredRefreshPending = false;
            var now = UnityEditor.EditorApplication.timeSinceStartup;
            if (now - _lastRefreshTime < REFRESH_COOLDOWN_SECONDS)
            {
                _deferredRefreshPending = true;
                UnityEditor.EditorApplication.delayCall += DeferredRefresh;
                return;
            }
            AssetDatabaseRefresh();
        }
        #endregion

        #region DOCUMENTATION

        private static UnityEngine.Object _documentationPdf = null;
        [UnityEditor.MenuItem("Tools/Plugin Master/Prefab World Builder/Documentation...", false, 1300)]
        public static void OpenDocFile()
        {
            if (_documentationPdf == null)
                _documentationPdf = UnityEditor.AssetDatabase.LoadMainAssetAtPath(PWBCore.staticData.documentationPath);
            if (_documentationPdf == null) Debug.LogWarning("Missing Documentation File");
            else UnityEditor.AssetDatabase.OpenAsset(_documentationPdf);
        }
        #endregion
    }
    #endregion

    #region SETTINGS
    [System.Serializable]
    public class PWBSettings
    {
        #region COMMON
        private static string _settingsPath = null;
        private static PWBSettings _instance = null;
        private PWBSettings() { }

        private static PWBSettings instance
        {
            get
            {
                if (_instance == null) _instance = new PWBSettings();
                return _instance;
            }
        }
        private static string settingsPath
        {
            get
            {
                if (_settingsPath == null)
                    _settingsPath = System.IO.Directory.GetParent(Application.dataPath) + "/ProjectSettings/PWBSettings.txt";
                return _settingsPath;
            }
        }
        private void LoadFromFile()
        {
            if (!System.IO.File.Exists(settingsPath))
            {
                var files = System.IO.Directory.GetFiles(Application.dataPath,
                        PWBData.FULL_FILE_NAME, System.IO.SearchOption.AllDirectories);
                if (files.Length > 0) _dataDir = System.IO.Path.GetDirectoryName(files[0]);
                else
                {
                    _dataDir = Application.dataPath + "/" + PWBData.RELATIVE_DATA_DIR;
                    System.IO.Directory.CreateDirectory(_dataDir);
                    _dataDir = PWBCore.GetRelativePath(_dataDir);
                }
                Save();
            }
            else
            {
                var settings = JsonUtility.FromJson<PWBSettings>(System.IO.File.ReadAllText(settingsPath));
                _dataDir = settings._dataDir;
                if (PWBCore.IsFullPath(_dataDir)) _dataDir = PWBCore.GetRelativePath(_dataDir);

                foreach (var loadedProfile in settings._shortcutProfiles)
                {
                    var profiles = _shortcutProfiles.Where(p => p.profileName == loadedProfile.profileName);
                    if (profiles.Count() <= 0) continue;
                    var profile = profiles.First();
                    profile.Copy(loadedProfile);
                }
                _selectedProfileIdx = settings._selectedProfileIdx;
            }
        }

        private void Save()
        {
            var jsonString = JsonUtility.ToJson(this, true);
            System.IO.File.WriteAllText(settingsPath, jsonString);
        }
        #endregion

        #region DATA DIR
        [SerializeField] private string _dataDir = null;
        private static bool _movingDir = false;
        public static bool movingDir => _movingDir;
        private static void CheckDataDir()
        {
            if (instance._dataDir == null) instance.LoadFromFile();
            if (PWBCore.IsFullPath(instance._dataDir)) instance._dataDir = PWBCore.GetRelativePath(instance._dataDir);

        }

        public static string relativeDataDir
        {
            get
            {
                CheckDataDir();
                var currentDir = PWBCore.GetFullPath(instance._dataDir);
                if (!System.IO.Directory.Exists(currentDir))
                {
                    if (currentDir.Replace("\\", "/").Contains(PWBData.RELATIVE_DATA_DIR))
                    {
                        var directories = System.IO.Directory.GetDirectories(Application.dataPath, PWBData.DATA_DIR,
                            System.IO.SearchOption.AllDirectories)
                            .Where(d => d.Replace("\\", "/").Contains(PWBData.RELATIVE_DATA_DIR)).ToArray();
                        if (directories.Length > 0)
                        {
                            instance._dataDir = PWBCore.GetRelativePath(directories[0].Replace("\\", "/"));
                            instance.Save();
                            PaletteManager.instance.LoadPaletteFiles(false);
                            PrefabPalette.UpdateTabBar();
                        }
                    }
                }
                return instance._dataDir;
            }
        }

        public static void SetDataDir(string fullPath)
        {
            var newDirRelative = PWBCore.GetRelativePath(fullPath);
            if (instance._dataDir == newDirRelative) return;
            var currentFullDir = PWBCore.GetFullPath(instance._dataDir);
            void DeleteMeta(string path)
            {
                var metapath = path + ".meta";
                if (System.IO.File.Exists(metapath)) System.IO.File.Delete(metapath);
            }

            bool DeleteIfEmpty(string dirPath)
            {
                if (System.IO.Directory.GetFiles(dirPath).Length != 0) return false;
                System.IO.Directory.Delete(dirPath);
                DeleteMeta(dirPath);
                return true;
            }
            if (System.IO.Directory.Exists(currentFullDir))
            {
                _movingDir = true;
                var currentDataPath = currentFullDir + "/" + PWBData.FULL_FILE_NAME;
                if (System.IO.File.Exists(currentDataPath))
                {
                    var newDataPath = fullPath + "/" + PWBData.FULL_FILE_NAME;
                    if (System.IO.File.Exists(newDataPath)) System.IO.File.Delete(newDataPath);
                    DeleteMeta(currentDataPath);
                    System.IO.File.Move(currentDataPath, newDataPath);

                    var currentPalettesDir = currentFullDir + "/" + PWBData.PALETTES_DIR;
                    if (System.IO.Directory.Exists(currentPalettesDir))
                    {
                        var newPalettesDir = fullPath + "/" + PWBData.PALETTES_DIR;
                        if (!System.IO.Directory.Exists(newPalettesDir))
                            System.IO.Directory.CreateDirectory(newPalettesDir);
                        var palettesPaths = System.IO.Directory.GetFiles(currentPalettesDir, "*.txt");
                        foreach (var currentPalettePath in palettesPaths)
                        {
                            var fileName = System.IO.Path.GetFileName(currentPalettePath);
                            var newPalettePath = newPalettesDir + "/" + fileName;
                            if (System.IO.File.Exists(newPalettePath)) System.IO.File.Delete(newPalettePath);
                            DeleteMeta(currentPalettePath);

                            var paletteText = System.IO.File.ReadAllText(currentPalettePath);
                            var palette = JsonUtility.FromJson<PaletteData>(paletteText);
                            palette.filePath = newPalettePath;

                            System.IO.File.Move(currentPalettePath, newPalettePath);
                            System.IO.File.Delete(currentPalettePath);

                            var currentThumbnailsPath = currentPalettePath.Substring(0, currentPalettePath.Length - 4);
                            if (!System.IO.Directory.Exists(currentThumbnailsPath)) continue;
                            var thumbnailsDirName = fileName.Substring(0, fileName.Length - 4);
                            var newThumbnailPath = newPalettesDir + "/" + thumbnailsDirName;
                            if (System.IO.Directory.Exists(newThumbnailPath)) System.IO.Directory.Delete(newThumbnailPath);
                            DeleteMeta(currentThumbnailsPath);
                            System.IO.Directory.Move(currentThumbnailsPath, newThumbnailPath);
                        }
                    }
                    if (DeleteIfEmpty(currentPalettesDir)) DeleteIfEmpty(currentFullDir);
                    PWBCore.refreshDatabase = true;
                }
                _movingDir = false;
            }
            instance._dataDir = PWBCore.GetRelativePath(fullPath);
            instance.Save();
            PaletteManager.instance.LoadPaletteFiles(true);
            PrefabPalette.UpdateTabBar();
        }
        public static string fullDataDir => PWBCore.GetFullPath(relativeDataDir);
        #endregion

        #region SHORTCUTS
        [SerializeField]
        private System.Collections.Generic.List<PWBShortcuts> _shortcutProfiles
           = new System.Collections.Generic.List<PWBShortcuts>()
           {
                PWBShortcuts.GetDefault(0),
                PWBShortcuts.GetDefault(1),
                PWBShortcuts.GetDefault(2)
           };
        [SerializeField] private int _selectedProfileIdx = 2;
        public static System.Action OnShrotcutProfileChanged;
        private PWBShortcuts selectedProfile
        {
            get
            {
                if (_selectedProfileIdx < 0 || _selectedProfileIdx > _shortcutProfiles.Count) _selectedProfileIdx = 0;
                return _shortcutProfiles[_selectedProfileIdx];
            }
        }

        public static PWBShortcuts shortcuts
        {
            get
            {
                CheckDataDir();
                return instance.selectedProfile;
            }
        }

        public static string[] shotcutProfileNames
        {
            get
            {
                CheckDataDir();
                return instance._shortcutProfiles.Select(p => p.profileName).ToArray();
            }
        }

        public static int selectedProfileIdx
        {
            get
            {
                CheckDataDir();
                return instance._selectedProfileIdx;
            }
            set
            {
                CheckDataDir();
                if (value == instance._selectedProfileIdx) return;
                instance._selectedProfileIdx = value;
                instance.Save();
                if (OnShrotcutProfileChanged != null) OnShrotcutProfileChanged();
            }
        }

        public static void UpdateShrotcutsConflictsAndSaveFile()
        {
            CheckDataDir();
            shortcuts.UpdateConficts();
            shortcuts.UpdateMouseConficts();
            instance.Save();
        }

        public static bool GetShortcutConflict(PWBKeyShortcut shortcut1, out PWBKeyShortcut shortcut2)
        {
            return shortcuts.GetConflictShortcuts(shortcut1, out shortcut2);
        }

        public static void ResetSelectedProfile()
        {
            CheckDataDir();
            instance._shortcutProfiles[selectedProfileIdx].Copy(PWBShortcuts.GetDefault(selectedProfileIdx));
        }

        public static void ResetShortcutToDefault(PWBKeyShortcut shortcut)
        {
            var defaultProfile = selectedProfileIdx == 1 ? PWBShortcuts.GetDefault(1) : PWBShortcuts.GetDefault(0);
            foreach (var ds in defaultProfile.keyShortcuts)
            {
                if (ds.group == shortcut.group && ds.name == shortcut.name)
                {
                    shortcut.combination.Set(ds.combination.keyCode, ds.combination.modifiers);
                    return;
                }
            }
        }

        public static void ResetShortcutToDefault(PWBMouseShortcut shortcut)
        {
            var defaultProfile = selectedProfileIdx == 1 ? PWBShortcuts.GetDefault(1) : PWBShortcuts.GetDefault(0);
            foreach (var ds in defaultProfile.mouseShortcuts)
            {
                if (ds.group == shortcut.group && ds.name == shortcut.name)
                {
                    shortcut.combination.Set(ds.combination.modifiers, ds.combination.mouseEvent);
                    return;
                }
            }
        }

        public static void ResetShortcutToDefault(PWBShortcut shortcut)
        {
            if (shortcut is PWBKeyShortcut) ResetShortcutToDefault(shortcut as PWBKeyShortcut);
            else if (shortcut is PWBMouseShortcut) ResetShortcutToDefault(shortcut as PWBMouseShortcut);
        }

        #endregion
    }
    #endregion

    #region AUTOSAVE
    [UnityEditor.InitializeOnLoad]
    public static class AutoSave
    {
        private static int _quickSaveCount = 3;

        static AutoSave()
        {
            PWBCore.staticData.UpdateRootDirectory();
            PeriodicSave();
            PeriodicQuickSave();
        }
        private async static void PeriodicSave()
        {
            if (PWBCore.staticDataWasInitialized)
            {
                await System.Threading.Tasks.Task.Delay(PWBCore.staticData.autoSavePeriodMinutes * 60000);
                PWBCore.staticData.SaveIfPending();
            }
            else await System.Threading.Tasks.Task.Delay(60000);
            PeriodicSave();
        }

        private async static void PeriodicQuickSave()
        {
            await System.Threading.Tasks.Task.Delay(300);
            ++_quickSaveCount;
            if (_quickSaveCount == 3 && PWBCore.staticDataWasInitialized)
                PWBCore.staticData.SaveIfPending();
            PeriodicQuickSave();
        }

        public static void QuickSave() => _quickSaveCount = 0;
    }
    #endregion
}
#pragma warning restore UDR0001
