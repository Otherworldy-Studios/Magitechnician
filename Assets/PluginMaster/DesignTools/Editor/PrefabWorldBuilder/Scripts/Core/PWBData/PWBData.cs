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
    [System.Serializable]
    public partial class PWBData
    {
        #region PATHS & CONSTANTS

        public const string DATA_DIR = "Data";
        public const string FILE_NAME = "PWBData";
        public const string FULL_FILE_NAME = FILE_NAME + ".txt";
        public const string RELATIVE_TOOL_DIR = "PluginMaster/DesignTools/Editor/PrefabWorldBuilder";
        public const string RELATIVE_RESOURCES_DIR = RELATIVE_TOOL_DIR + "/Resources";
        public const string RELATIVE_DATA_DIR = RELATIVE_RESOURCES_DIR + "/" + DATA_DIR;
        public const string PALETTES_DIR = "Palettes";
        public const string VERSION = "4.12.3";

        [SerializeField] private string _version = VERSION;
        [SerializeField] private string _rootDirectory = null;

        public string version => _version;

        private string rootDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(_rootDirectory)) UpdateRootDirectory();
                else
                {
                    var fullPath = PWBCore.GetFullPath(_rootDirectory);
                    if (!System.IO.Directory.Exists(fullPath)) UpdateRootDirectory();
                }
                return _rootDirectory;
            }
        }

        public static string palettesDirectory
        {
            get
            {
                var dir = PWBSettings.fullDataDir + "/" + PALETTES_DIR;
#if !PWB_DO_NOT_INITIALIZE_ON_LOAD
                if (!System.IO.Directory.Exists(dir) && System.IO.File.Exists(dataPath))
                    System.IO.Directory.CreateDirectory(dir);
#endif
                return dir;
            }
        }

        public static string dataPath => PWBSettings.fullDataDir + "/" + FULL_FILE_NAME;

        public string documentationPath => rootDirectory + "/Documentation/Prefab World Builder Documentation.pdf";

        public void UpdateRootDirectory()
        {
            var directories = System.IO.Directory.GetDirectories(Application.dataPath, "PrefabWorldBuilder",
               System.IO.SearchOption.AllDirectories).Where(d => d.Replace("\\", "/").Contains(RELATIVE_TOOL_DIR)).ToArray();
            if (directories.Length == 0)
            {
                _rootDirectory = Application.dataPath + "/" + RELATIVE_TOOL_DIR;
                _rootDirectory = _rootDirectory.Replace("\\", "/");
                System.IO.Directory.CreateDirectory(_rootDirectory);
            }
            else _rootDirectory = directories[0];
            _rootDirectory = PWBCore.GetRelativePath(_rootDirectory);
        }

        #endregion

        #region AUTO SAVE & UNDO

        [SerializeField] private int _autoSavePeriodMinutes = 1;
        [SerializeField] private bool _undoPalette = true;

        public int autoSavePeriodMinutes
        {
            get => _autoSavePeriodMinutes;
            set
            {
                value = Mathf.Clamp(value, 1, 10);
                if (_autoSavePeriodMinutes == value) return;
                _autoSavePeriodMinutes = value;
                SaveAndUpdateVersion();
            }
        }

        public bool undoPalette
        {
            get => _undoPalette;
            set
            {
                if (_undoPalette == value) return;
                _undoPalette = value;
                SaveAndUpdateVersion();
            }
        }

        #endregion

        #region CONTROL POINTS & VISUALIZATION

        [SerializeField] private int _controlPointSize = 2;
        [SerializeField] private Color _selectedContolPointColor = Color.cyan;
        [SerializeField] private bool _selectedControlPointBlink = false;

        public int controPointSize
        {
            get => _controlPointSize;
            set
            {
                if (_controlPointSize == value) return;
                _controlPointSize = value;
                SaveAndUpdateVersion();
            }
        }

        public Color selectedContolPointColor
        {
            get => _selectedContolPointColor;
            set
            {
                if (_selectedContolPointColor == value) return;
                _selectedContolPointColor = value;
                SaveAndUpdateVersion();
            }
        }

        public bool selectedControlPointBlink
        {
            get => _selectedControlPointBlink;
            set
            {
                if (_selectedControlPointBlink == value) return;
                _selectedControlPointBlink = value;
                SaveAndUpdateVersion();
            }
        }

        #endregion

        #region WINDOW BEHAVIOR

        [SerializeField] private bool _closeAllWindowsWhenClosingTheToolbar = false;
        [SerializeField] private bool _openToolPropertiesWhenAToolIsSelected = false;
        [SerializeField] private bool _openBrushPropertiesWhenABrushIsSelected = false;
        [SerializeField] private bool _selectTheNextPaletteInAlphabeticalOrder = true;

        public bool closeAllWindowsWhenClosingTheToolbar
        {
            get => _closeAllWindowsWhenClosingTheToolbar;
            set
            {
                if (_closeAllWindowsWhenClosingTheToolbar == value) return;
                _closeAllWindowsWhenClosingTheToolbar = value;
                SaveAndUpdateVersion();
            }
        }

        public bool openToolPropertiesWhenAToolIsSelected
        {
            get => _openToolPropertiesWhenAToolIsSelected;
            set
            {
                if (_openToolPropertiesWhenAToolIsSelected == value) return;
                _openToolPropertiesWhenAToolIsSelected = value;
                SaveAndUpdateVersion();
            }
        }

        public bool openBrushPropertiesWhenABrushIsSelected
        {
            get => _openBrushPropertiesWhenABrushIsSelected;
            set
            {
                if (_openBrushPropertiesWhenABrushIsSelected == value) return;
                _openBrushPropertiesWhenABrushIsSelected = value;
                SaveAndUpdateVersion();
            }
        }

        public bool selectTheNextPaletteInAlphabeticalOrder
        {
            get => _selectTheNextPaletteInAlphabeticalOrder;
            set
            {
                if (_selectTheNextPaletteInAlphabeticalOrder == value) return;
                _selectTheNextPaletteInAlphabeticalOrder = value;
                SaveAndUpdateVersion();
            }
        }

        #endregion

        #region THUMBNAILS & ASSETS

        [SerializeField] private int _thumbnailLayer = 7;
        [SerializeField] private bool _createThumbnailsFolder = false;
        [SerializeField] private bool _useAssetPreview = false;

        public int thumbnailLayer
        {
            get => _thumbnailLayer;
            set
            {
                value = Mathf.Clamp(value, 0, 31);
                if (_thumbnailLayer == value) return;
                _thumbnailLayer = value;
                SaveAndUpdateVersion();
            }
        }

        public bool createThumbnailsFolder
        {
            get => _createThumbnailsFolder;
            set
            {
                if (_createThumbnailsFolder == value) return;
                _createThumbnailsFolder = value;
                var thumbnailsFolderName = "Thumbnails";
                if (palettesDirectory.Contains(Application.dataPath))
                {
                    var palettesFolder = palettesDirectory.Substring(Application.dataPath.Length - 6);
                    var thumbnailsFolderPath = palettesFolder + "/" + thumbnailsFolderName;
                    if (_createThumbnailsFolder)
                    {
                        if (!UnityEditor.AssetDatabase.IsValidFolder(thumbnailsFolderPath))
                            UnityEditor.AssetDatabase.CreateFolder(palettesFolder, thumbnailsFolderName);
                        var palettesFolders = UnityEditor.AssetDatabase.GetSubFolders(palettesFolder);
                        foreach (var paletteFolder in palettesFolders)
                        {
                            var folderName = System.IO.Path.GetFileName(paletteFolder);
                            if (folderName == thumbnailsFolderName) continue;
                            var destinationPath = thumbnailsFolderPath + "/" + folderName;
                            UnityEditor.AssetDatabase.MoveAsset(paletteFolder, destinationPath);
                        }
                    }
                    else if (UnityEditor.AssetDatabase.IsValidFolder(thumbnailsFolderPath))
                    {
                        var thumbnailsFolders = UnityEditor.AssetDatabase.GetSubFolders(thumbnailsFolderPath);
                        foreach (var thumbnailFolder in thumbnailsFolders)
                        {
                            var folderName = System.IO.Path.GetFileName(thumbnailFolder);
                            var destinationPath = palettesFolder + "/" + folderName;
                            UnityEditor.AssetDatabase.MoveAsset(thumbnailFolder, destinationPath);
                        }
                        UnityEditor.AssetDatabase.DeleteAsset(thumbnailsFolderPath);
                    }
                }
                else
                {
                    var thumbnailsFolderPath = palettesDirectory + "/" + thumbnailsFolderName;
                    if (_createThumbnailsFolder)
                    {
                        if (!System.IO.Directory.Exists(thumbnailsFolderPath))
                            System.IO.Directory.CreateDirectory(thumbnailsFolderPath);
                        var palettesFolders = System.IO.Directory.GetDirectories(palettesDirectory);
                        foreach (var paletteFolder in palettesFolders)
                        {
                            var folderName = System.IO.Path.GetFileName(paletteFolder);
                            if (folderName == thumbnailsFolderName) continue;
                            var destinationPath = thumbnailsFolderPath + "/" + folderName;
                            System.IO.Directory.Move(paletteFolder, destinationPath);
                        }
                    }
                    else if (System.IO.Directory.Exists(thumbnailsFolderPath))
                    {
                        var thumbnailsFolders = System.IO.Directory.GetDirectories(thumbnailsFolderPath);
                        foreach (var thumbnailFolder in thumbnailsFolders)
                        {
                            var folderName = System.IO.Path.GetFileName(thumbnailFolder);
                            var destinationPath = palettesDirectory + "/" + folderName;
                            System.IO.Directory.Move(thumbnailFolder, destinationPath);
                        }
                        System.IO.Directory.Delete(thumbnailsFolderPath);
                    }
                }
                SaveAndUpdateVersion();
            }
        }

        public bool useAssetPreview
        {
            get => _useAssetPreview;
            set
            {
                if (_useAssetPreview == value) return;
                _useAssetPreview = value;
                PaletteManager.UpdateAllThumbnails();
                SaveAndUpdateVersion();
            }
        }

        #endregion

        #region UI TEXT & INFO

        [SerializeField] private bool _showInfoText = true;

        public bool showInfoText
        {
            get => _showInfoText;
            set
            {
                if (_showInfoText == value) return;
                _showInfoText = value;
                SaveAndUpdateVersion();
            }
        }

        public void ToggleInfoText() => showInfoText = !showInfoText;

        #endregion

        #region NAMING & ENUMERATION

        [SerializeField] private bool _renameItemParent = false;
        [SerializeField] private bool _addEnumerationToName = true;

        public bool ranameItemParent
        {
            get => _renameItemParent;
            set
            {
                if (_renameItemParent == value) return;
                _renameItemParent = value;
                SaveAndUpdateVersion();
            }
        }

        public bool addEnumerationToName
        {
            get => _addEnumerationToName;
            set
            {
                if (_addEnumerationToName == value) return;
                _addEnumerationToName = value;
                SaveAndUpdateVersion();
            }
        }

        #endregion

        #region PREVIEW SETTINGS

        [SerializeField] private int _maxPreviewCountInEditMode = 200;

        public int maxPreviewCountInEditMode
        {
            get => _maxPreviewCountInEditMode;
            set
            {
                value = Mathf.Max(value, 0);
                if (_maxPreviewCountInEditMode == value) return;
                _maxPreviewCountInEditMode = value;
                SaveAndUpdateVersion();
            }
        }

        #endregion

        #region UNSAVED CHANGES HANDLING

        public enum UnsavedChangesAction { ASK, SAVE, DISCARD }

        [SerializeField] private UnsavedChangesAction _unsavedChangesAction = UnsavedChangesAction.ASK;

        public UnsavedChangesAction unsavedChangesAction
        {
            get => _unsavedChangesAction;
            set
            {
                if (_unsavedChangesAction == value) return;
                _unsavedChangesAction = value;
                SaveAndUpdateVersion();
            }
        }

        #endregion

        #region PREFAB TRACKING

        [SerializeField]
        private SerializableDictionary<string, int> _prefabCountDict
            = new SerializableDictionary<string, int>();

        public string GetPrefabCount(string prefabGuid)
        {
            int count = 1;
            if (_prefabCountDict.ContainsKey(prefabGuid))
            {
                count = _prefabCountDict[prefabGuid] + 1;
                _prefabCountDict[prefabGuid] = count;
            }
            else _prefabCountDict.Add(prefabGuid, 1);
            PWBCore.SetSavePending();
            return count.ToString();
        }

        #endregion

        #region MANAGERS

        [SerializeField] private PaletteManager _paletteManager = PaletteManager.instance;

        [SerializeField] private FloorManager _floorManager = FloorManager.instance;
        [SerializeField] private WallManager _wallManager = WallManager.instance;
        [SerializeField] private BlockManager _blockManager = BlockManager.instance;
        [SerializeField] private PinManager _pinManager = PinManager.instance;
        [SerializeField] private BrushManager _brushManager = BrushManager.instance;
        [SerializeField] private GravityToolController _gravityToolController = GravityToolController.instance;
        [SerializeField] private LineManager _lineManager = LineManager.instance;
        [SerializeField] private ShapeManager _shapeManager = ShapeManager.instance;
        [SerializeField] private TilingManager _tilingManager = TilingManager.instance;
        [SerializeField] private ReplacerManager _replacerManager = ReplacerManager.instance;
        [SerializeField] private EraserManager _eraserManager = EraserManager.instance;

        [SerializeField]
        private SelectionToolController _selectionToolController = SelectionToolController.instance;
        [SerializeField]
        private CircleSelectManager _circleSelectToolController = CircleSelectManager.instance;
        [SerializeField] private ExtrudeManager _extrudeSettings = ExtrudeManager.instance;
        [SerializeField] private MirrorManager _mirrorManager = MirrorManager.instance;
        [SerializeField] private GridManager _snapManager = new GridManager();
        #endregion

        #region GLOBAL SETTINGS
        [SerializeField] private ToolParentingSettings _globalParentingSettings = new ToolParentingSettings();
        public ToolParentingSettings globalParentingSettings => _globalParentingSettings;
        #endregion

        #region SAVE STATE

        private bool _savePending = false;
        private bool _saving = false;

        public bool saving => _saving;

        public void SetSavePending() { _savePending = true; }

        public void SaveIfPending() { if (_savePending) SaveAndUpdateVersion(); }

        #endregion

        #region FILE OPERATIONS
        public void Save() => Save(false);
        public void SaveAndUpdateVersion() => Save(true);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Domain reload", "UDR0004:Domain Reload Analyzer")]
        public void Save(bool updateVersion)
        {
            if (UnityEditor.EditorApplication.isCompiling
                || UnityEditor.EditorApplication.isUpdating
                || UnityEditor.BuildPipeline.isBuildingPlayer)
            {
                _savePending = true;
                UnityEditor.EditorApplication.delayCall += () => Save(updateVersion);
                return;
            }
            _saving = true;
#if !PWB_DO_NOT_INITIALIZE_ON_LOAD
            if (updateVersion) VersionUpdate();
            _version = VERSION;
            var jsonString = JsonUtility.ToJson(this, true);
            var fileExist = System.IO.File.Exists(dataPath);
            if (!System.IO.Directory.Exists(PWBSettings.fullDataDir))
                System.IO.Directory.CreateDirectory(PWBSettings.fullDataDir);
            UnityEditor.AssetDatabase.StartAssetEditing();
            try
            {
                System.IO.File.WriteAllText(dataPath, jsonString);
            }
            finally
            {
                UnityEditor.AssetDatabase.StopAssetEditing();
                _saving = false;
                _savePending = false;
            }
            if (!fileExist) PWBCore.refreshDatabase = true;
#else
    _savePending = false;
    _saving = false;
#endif
        }

        public static string ReadDataText()
        {
            var fullFilePath = dataPath;
            if (!System.IO.File.Exists(fullFilePath))
            {
                PWBCore.staticData.Save(false);
                if (!System.IO.File.Exists(fullFilePath)) return null;
            }
            return System.IO.File.ReadAllText(fullFilePath);
        }

        public static void DeleteFile()
        {
            var fullFilePath = dataPath;
            if (System.IO.File.Exists(fullFilePath)) System.IO.File.Delete(fullFilePath);
            var metaPath = fullFilePath += ".meta";
            if (System.IO.File.Exists(metaPath)) System.IO.File.Delete(metaPath);
        }
        #endregion
    }
}
#pragma warning restore UDR0001
