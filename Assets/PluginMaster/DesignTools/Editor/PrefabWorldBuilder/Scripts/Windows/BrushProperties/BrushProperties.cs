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
    public partial class BrushProperties : UnityEditor.EditorWindow, ISerializationCallbackReceiver
    {
        [SerializeField] PWBData _data = null;

        private GUISkin _skin = null;
        private GUIStyle _itemStyle = null;
        private GUIStyle _cursorStyle = null;
        private GUIStyle _thumbnailToggleStyle = null;
        private Vector2 _mainScrollPosition = Vector2.zero;

        private bool _repaint = false;
        private bool _updateBrushStroke = false;

        
        private static BrushProperties _instance = null;
        public static BrushProperties instance => _instance;
        [UnityEditor.MenuItem("Tools/Plugin Master/Prefab World Builder/Brush Properties...", false, 1120)]
        public static void ShowWindow() => _instance = GetWindow<BrushProperties>("Brush Properties");
        public static void RepaintWindow()
        {
            if (_instance == null) return;
            _instance.Repaint();
            _instance._repaint = true;
        }

        public static void CloseWindow()
        {
            if (_instance != null) _instance.Close();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Domain reload", "UDR0004:Domain Reload Analyzer")]
        private void OnEnable()
        {
            _instance = this;
            _data = PWBCore.staticData;
            PaletteManager.OnBrushSelectionChanged -= OnBrushChanged;
            PaletteManager.OnBrushSelectionChanged += OnBrushChanged;
            _skin = Resources.Load<GUISkin>("PWBSkin");
            if (_skin == null) return;
            _itemStyle = _skin.GetStyle("PaletteToggle");
            _cursorStyle = _skin.GetStyle("Cursor");
            _thumbnailToggleStyle = _skin.GetStyle("ThumbnailToggle");
            wantsMouseMove = true;
            wantsMouseEnterLeaveWindow = true;
            PaletteManager.OnSelectionChanged -= UpdateBrushSelectionSettings;
            PaletteManager.OnSelectionChanged += UpdateBrushSelectionSettings;

            _sameStateIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/Same"),
                "All selected brushes define the same value for this element");
            _mixedStateIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/Mixed"),
                "The Selection contains different values for this element");
            _changedStateIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/Edited"),
                "This value has changed");
            UnityEditor.Undo.undoRedoPerformed -= Repaint;
            UnityEditor.Undo.undoRedoPerformed += Repaint;
        }

        private void OnDisable()
        {
            PaletteManager.OnBrushSelectionChanged -= OnBrushChanged;
            PaletteManager.OnSelectionChanged -= UpdateBrushSelectionSettings;
            UnityEditor.Undo.undoRedoPerformed -= Repaint;
        }

        public static void ClearUndo()
        {
            if (_instance == null) return;
            UnityEditor.Undo.ClearUndo(_instance);
        }

        private void OnGUI()
        {
            if (UnityEditor.Lightmapping.isRunning) return;
            if (_skin == null)
            {
                Close();
                return;
            }
            OpenObjectPickerIfRequested();
            if (_itemAdded)
            {
                PaletteManager.selectedBrush.InsertItemAt(_newItem, _newItemIdx);
                _newItem = null;
                _selectedItemIdx = _newItemIdx;
                _itemAdded = false;
                OnMultiBrushChanged();
                return;
            }
            BrushInputData toggleData = null;
            using (var scrollView = new UnityEditor.EditorGUILayout.ScrollViewScope(_mainScrollPosition,
                false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUIStyle.none))
            {
                _mainScrollPosition = scrollView.scrollPosition;
                if (PaletteManager.selectionCount > 1)
                {
                    BrushSelectionFields(ref _brushPosGroupOpen, ref _brushRotGroupOpen,
                        ref _brushScaleGroupOpen, ref _brushFlipGroupOpen, BRUSH_SETTINGS_UNDO_MSG, false, true,
                        PaletteManager.selectedPalette.brushes, PaletteManager.idxSelection,
                        _brushSelectionSettings, _brushSelectionState);
                    return;
                }
                if (PaletteManager.selectedBrushIdx == -1) return;
                bool showBrushGroup = PaletteManager.selectedBrush != null;
                if (showBrushGroup)
                {
                    if (PaletteManager.selectedBrush.items.Length == 0)
                    {
                        showBrushGroup = false;
                        PaletteManager.selectedPalette.RemoveBrushAt(PaletteManager.selectedBrushIdx);
                    }
                }
                if (showBrushGroup)
                {
#if UNITY_2019_1_OR_NEWER
                    _brushGroupOpen = UnityEditor.EditorGUILayout.BeginFoldoutHeaderGroup(_brushGroupOpen, "Brush Settings");
#else
                    _brushGroupOpen = EditorGUILayout.Foldout(_brushGroupOpen, "Brush Settings");
#endif
                    if (_brushGroupOpen) BrushGroup();
#if UNITY_2019_1_OR_NEWER
                    UnityEditor.EditorGUILayout.EndFoldoutHeaderGroup();
#endif
#if UNITY_2019_1_OR_NEWER
                    _multiBrushGroupOpen = UnityEditor.EditorGUILayout.BeginFoldoutHeaderGroup(_multiBrushGroupOpen,
                        "Multi Brush Settings");
#else
                    _multiBrushGroupOpen = EditorGUILayout.Foldout(_multiBrushGroupOpen, "Multi Brush Settings");
#endif
                    if (_multiBrushGroupOpen) MultiBrushGroup(ref toggleData);
#if UNITY_2019_1_OR_NEWER
                    UnityEditor.EditorGUILayout.EndFoldoutHeaderGroup();
#endif
                }

            }
            OnObjectSelectorClosed();
            ItemMouseEventHandler(toggleData);
            var eventType = Event.current.rawType;
            if (eventType == EventType.MouseMove || eventType == EventType.MouseUp)
            {
                _moveItem.to = -1;
                draggingItem = false;
                _showCursor = false;
            }
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                GUI.FocusControl(null);
                Repaint();
            }
        }

        private void Update()
        {
            if (mouseOverWindow != this)
            {
                _moveItem.to = -1;
                _showCursor = false;
            }
            else if (draggingItem) _showCursor = true;
            if (_repaint)
            {
                _repaint = false;
                Repaint();
            }
            if (_updateBrushStroke)
            {
                _updateBrushStroke = false;
                BrushstrokeManager.UpdateBrushstroke();
            }
        }

        private void OnBrushChanged()
        {
            _selectedItemIdx = 0;
            _repaint = true;
        }

        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize()
        {
            _repaint = true;
            _updateBrushStroke = true;
        }
    }
}
#pragma warning restore UDR0001
