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
    public partial class PrefabPalette : UnityEditor.EditorWindow, ISerializationCallbackReceiver
    {
        #region COMMON
        private GUISkin _skin = null;
        private int _initFrameCount = -1;
        private int _onGUILayoutCount = 0;

        [SerializeField] private PaletteManager _paletteManager = null;

        
        private static PrefabPalette _instance = null;
        public static PrefabPalette instance => _instance;
        [UnityEditor.MenuItem("Tools/Plugin Master/Prefab World Builder/Palette...", false, 1110)]
        public static void ShowWindow() => _instance = GetWindow<PrefabPalette>("Palette");

        
        private static bool _repaint = false;
        public static void RepaintWindow()
        {
            if (_instance != null) _instance.Repaint();
            _repaint = true;
        }

        public static void OnChangeRepaint()
        {
            if (_instance != null)
            {
                _instance.OnPaletteChange();
                RepaintWindow();
            }
        }
        public static void CloseWindow()
        {
            if (_instance != null) _instance.Close();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Domain reload", "UDR0004:Domain Reload Analyzer")]
        private void OnEnable()
        {
            _instance = this;
            _initFrameCount = -1;
            _paletteManager = PaletteManager.instance;
            _skin = Resources.Load<GUISkin>("PWBSkin");
            if (_skin == null) return;
            _toggleStyle = _skin.GetStyle("PaletteToggle");
            _loadingIcon = Resources.Load<Texture2D>("Sprites/Loading");
            _toggleStyle.margin = new RectOffset(4, 4, 4, 4);
            _dropdownIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/DropdownArrow"));
            _labelIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/Label"), "Filter by label");
            _selectionFilterIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/SelectionFilter"),
                "Filter by selection");
            _folderFilterIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/FolderFilter"), "Filter by folder");
            _newBrushIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/New"), "New Brush");
            _deleteBrushIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/Delete"), "Delete Brush");
            _pickerIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/Picker"), "Brush Picker");
            _clearFilterIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/Clear"));
            _settingsIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/Settings"));
            _pinTabIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/PinTab"), "Toggle pin status");
            _pinnedTabIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/PinnedTab"), "Toggle pin status");
            _pinTabLightIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/LightTheme/PinTab"), "Toggle pin status");
            _pinnedTabLightIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/LightTheme/PinnedTab"),
                "Toggle pin status");
            _cursorStyle = _skin.GetStyle("Cursor");
            autoRepaintOnSceneChange = true;
            if (PaletteManager.allPalettesCount == 0)
            {
                PWBCore.Initialize();
                PaletteManager.instance.LoadPaletteFiles(true);
                PaletteManager.InitializeSelectedPalette();
            }
            UpdateLabelFilter();
            UpdateFilteredList(false);
            PaletteManager.ClearSelection(false);
            UnityEditor.Undo.undoRedoPerformed -= OnPaletteChange;
            UnityEditor.Undo.undoRedoPerformed += OnPaletteChange;
        }

        private void OnDisable() => UnityEditor.Undo.undoRedoPerformed -= OnPaletteChange;

        private void OnDestroy() => ToolController.OnPaletteClosed();
        public static void ClearUndo()
        {
            if (_instance == null) return;
            UnityEditor.Undo.ClearUndo(_instance);
        }
        

        private void OnGUI()
        {
            if (Event.current.type == EventType.Layout)
                _onGUILayoutCount++;

            if (_initFrameCount < 0)
            {
                _initFrameCount = _onGUILayoutCount;
                Repaint();
                return;
            }

            if (_onGUILayoutCount == _initFrameCount)
            {
                Repaint();
                return;
            }

            bool initializing = _onGUILayoutCount <= _initFrameCount + 2;
            if (initializing && Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout)
            {
                Repaint();
                return;
            }

            if (UnityEditor.Lightmapping.isRunning) return;
            if (_skin == null)
            {
                Close();
                return;
            }
            if (PaletteManager.loadPaletteFilesPending)
            {
                PaletteManager.instance.LoadPaletteFiles(true);
                Reload(false);
                UpdateTabBar();
            }
            if (_contextBrushAdded && Event.current.type == EventType.Layout)
            {
                RegisterUndo("Add Brush");
                if (_newContextBrush != null)
                {
                    PaletteManager.selectedPalette.AddBrush(_newContextBrush);
                    _newContextBrush = null;
                    PaletteManager.selectedBrushIdx = PaletteManager.selectedPalette.brushes.Length - 1;
                    _contextBrushAdded = false;
                    OnPaletteChange();
                }
            }
            try
            {
                TabBar();
                SearchBar();
                Palette();
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (System.Exception e)
            {
                if (!initializing)
                    Debug.LogWarning($"[PWB Palette] GUI Exception caught: {e.Message}\n{e.StackTrace}");
                RepaintWindow();
                GUIUtility.ExitGUI();
            }
            var eventType = Event.current.rawType;
            if (eventType == EventType.MouseMove || eventType == EventType.MouseUp)
            {
                _moveBrush.to = -1;
                draggingBrush = false;
                _showCursor = false;
            }
            else if (PWBSettings.shortcuts.paletteDeleteBrush.Check()) OnDelete();
            if (PWBSettings.shortcuts.paletteReplaceSceneSelection.Check()) PWBIO.ReplaceSelected();
        }

        private void Update()
        {
            if (mouseOverWindow != this)
            {
                _moveBrush.to = -1;
                _showCursor = false;
            }
            else if (draggingBrush) _showCursor = true;
            if (_repaint)
            {
                _repaint = false;
                Repaint();
            }
            if (_frameSelectedBrush && _newSelectedPositionSet) DoFrameSelectedBrush();
            if (PaletteManager.savePending) PaletteManager.SaveIfPending();
        }
        private void RegisterUndo(string name)
        {
            if (PWBCore.staticData.undoPalette) UnityEditor.Undo.RegisterCompleteObjectUndo(this, name);
        }

        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize()
        {
            _repaint = true;
            _initFrameCount = -1;
            PaletteManager.ClearSelection(false);
        }

        public void UpdateAllThumbnails() => PaletteManager.UpdateAllThumbnails();
        public void ResetInitialization()
        {
            _initFrameCount = -1;
        }

        #endregion

        #region PALETTE
        private Vector2 _scrollPosition;
        private Rect _scrollViewRect;
        private Vector2 _prevSize;
        private int _columnCount = 1;
        private GUIStyle _toggleStyle = null;
        private const int MIN_ICON_SIZE = 24;
        private const int MAX_ICON_SIZE = 256;
        public const int DEFAULT_ICON_SIZE = 64;
        private int _prevIconSize = DEFAULT_ICON_SIZE;

        private GUIContent _dropdownIcon = null;
        private bool _draggingBrush = false;
        private bool _showCursor = false;
        private Rect _cursorRect;
        private GUIStyle _cursorStyle = null;
        private (int from, int to, bool perform) _moveBrush = (0, 0, false);

        private bool draggingBrush
        {
            get => _draggingBrush;
            set
            {
                _draggingBrush = value;
                wantsMouseMove = value;
                wantsMouseEnterLeaveWindow = value;
            }
        }

        private void Palette()
        {
            UpdateColumnCount();

            _prevIconSize = PaletteManager.iconSize;

            if (_moveBrush.perform)
            {
                RegisterUndo("Change Brush Order");
                var selection = PaletteManager.idxSelection;
                PaletteManager.selectedPalette.Swap(_moveBrush.from, _moveBrush.to, ref selection);
                PaletteManager.idxSelection = selection;
                if (selection.Length == 1) PaletteManager.selectedBrushIdx = selection[0];
                _moveBrush.perform = false;
                UpdateFilteredList(false);
            }
            BrushInputData toggleData = null;

            using (var scrollView = new UnityEditor.EditorGUILayout.ScrollViewScope(_scrollPosition, false, false,
                GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, _skin.box))
            {
                _scrollPosition = scrollView.scrollPosition;
                Brushes(ref toggleData);
                if (_showCursor) GUI.Box(_cursorRect, string.Empty, _cursorStyle);
            }
            _scrollViewRect = GUILayoutUtility.GetLastRect();
            if (PaletteManager.selectedPalette.brushCount == 0) DropBox();

            Bottom();

            BrushMouseEventHandler(toggleData);
            PaletteContext();
            DropPrefab();
        }

        private void UpdateColumnCount()
        {
            if (PaletteManager.allPalettesCount == 0) return;
            var paletteData = PaletteManager.selectedPalette;
            var brushes = paletteData.brushes;
            if (_scrollViewRect.width > MIN_ICON_SIZE)
            {
                if (_prevSize != position.size || _prevIconSize != PaletteManager.iconSize || _repaint)
                {
                    var iconW = (float)((PaletteManager.iconSize + 4) * brushes.Length + 6) / brushes.Length;
                    _columnCount = Mathf.Max((int)(_scrollViewRect.width / iconW), 1);
                    var rowCount = Mathf.CeilToInt((float)brushes.Length / _columnCount);
                    var h = rowCount * (PaletteManager.iconSize + 4) + 42;

                    if (h > _scrollViewRect.height)
                    {
                        iconW = (float)((PaletteManager.iconSize + 4) * brushes.Length + 17) / brushes.Length;
                        _columnCount = Mathf.Max((int)(_scrollViewRect.width / iconW), 1);
                    }
                }
                _prevSize = position.size;
            }
        }

        public void OnPaletteChange()
        {
            UpdateLabelFilter();
            UpdateFilteredList(false);
            _repaint = true;
            UpdateColumnCount();
            Repaint();
        }
        #endregion
    }
}
#pragma warning restore UDR0001
