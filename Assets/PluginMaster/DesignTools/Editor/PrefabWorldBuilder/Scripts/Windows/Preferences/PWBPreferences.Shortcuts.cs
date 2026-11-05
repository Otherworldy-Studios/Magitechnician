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
    public partial class PWBPreferences : UnityEditor.EditorWindow
    {
        #region DATA & STATE
        private bool _floorCategory = false;
        private bool _wallCategory = false;
        private bool _blockCategory = false;
        private bool _pinCategory = false;
        private bool _brushCategory = false;
        private bool _gravityCategory = false;
        private bool _lineCategory = false;
        private bool _shapeCategory = false;
        private bool _tilingCategory = false;
        private bool _eraserCategory = false;
        private bool _replacerCategory = false;
        private bool _selectionCategory = false;
        private bool _circleSelectCategory = false;
        private bool _gridCategory = false;
        private bool _snapCategory = false;
        private bool _paletteCategory = false;
        private bool _gizmosCategory = false;
        private bool _toolbarCategory = true;
        private bool _uiCategory = false;
        private bool _toolModesCategory = false;

        private PWBKeyShortcut _selectedShortcut = null;

        
        private static Texture2D _warningTexture = null;
        private static Texture2D warningTexture
        {
            get
            {
                if (_warningTexture == null) _warningTexture = Resources.Load<Texture2D>("Sprites/Warning");
                return _warningTexture;
            }
        }

        private UnityEditor.IMGUI.Controls.MultiColumnHeaderState _multiColumnHeaderState;
        private UnityEditor.IMGUI.Controls.MultiColumnHeader _multiColumnHeader;
        private UnityEditor.IMGUI.Controls.MultiColumnHeaderState.Column[] _columns;

        private static readonly Color _lighterColor = Color.white * 0.3f;
        private static readonly Color _darkerColor = Color.white * 0.1f;

        private static readonly EventModifiers[] _modifierOptions = new EventModifiers[]
        {
            EventModifiers.None,
            EventModifiers.Control,
            EventModifiers.Alt,
            EventModifiers.Shift,
            EventModifiers.Control | EventModifiers.Alt,
            EventModifiers.Control | EventModifiers.Shift,
            EventModifiers.Alt | EventModifiers.Shift,
            EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift,
        };

        private static readonly string[] _modifierDisplayedOptions = new string[]
        {
            "Disabled",
            "Ctrl",
            "Alt",
            "Shift",
            "Ctrl+Alt",
            "Ctrl+Shift",
            "Alt+Shift",
            "Ctrl+Alt+Shift"
        };

        private static readonly string[] _mouseEventsDisplayedOptions = new string[]
        {
            "Mouse scroll wheel",
            "R Btn horizontal drag",
            "R Btn vertical drag",
            "Mid Btn horizontal drag",
            "Mid Btn vertical drag"
        };
        #endregion

        #region INITIALIZATION
        private void InitializeMultiColumn()
        {
            _columns = new UnityEditor.IMGUI.Controls.MultiColumnHeaderState.Column[]
            {
                new UnityEditor.IMGUI.Controls.MultiColumnHeaderState.Column()
                {
                    allowToggleVisibility = false,
                    autoResize = true,
                    minWidth = 320,
                    width = 330,
                    canSort = false,
                    headerContent = new GUIContent("Command"),
                    headerTextAlignment = TextAlignment.Left,
                },
                new UnityEditor.IMGUI.Controls.MultiColumnHeaderState.Column()
                {
                    allowToggleVisibility = false,
                    autoResize = true,
                    minWidth = 266,
                    maxWidth = 266,
                    width = 266,
                    canSort = false,
                    headerContent = new GUIContent("Shortcut"),
                    headerTextAlignment = TextAlignment.Left,
                }
            };
            _multiColumnHeaderState = new UnityEditor.IMGUI.Controls.MultiColumnHeaderState(columns: _columns);
            _multiColumnHeader = new UnityEditor.IMGUI.Controls.MultiColumnHeader(state: _multiColumnHeaderState);
            _multiColumnHeader.visibleColumnsChanged += (multiColumnHeader) => multiColumnHeader.ResizeToFit();
            _multiColumnHeader.ResizeToFit();
        }
        #endregion

        #region CATEGORY SELECTION
        private void SelectProfileItem(object value)
        {
            PWBSettings.selectedProfileIdx = (int)value;
            Repaint();
        }

        private void SelectCategory(ref bool category)
        {
            _gridCategory = false;
            _snapCategory = false;
            _pinCategory = false;
            _brushCategory = false;
            _gravityCategory = false;
            _lineCategory = false;
            _shapeCategory = false;
            _tilingCategory = false;
            _eraserCategory = false;
            _replacerCategory = false;
            _selectionCategory = false;
            _circleSelectCategory = false;
            _paletteCategory = false;
            _toolbarCategory = false;
            _floorCategory = false;
            _wallCategory = false;
            _blockCategory = false;
            _gizmosCategory = false;
            _uiCategory = false;
            _toolModesCategory = false;
            category = true;
        }

        public static void SelectToolCategory(ToolController.Tool tool)
        {
            if (_instance == null) return;
            switch (tool)
            {
                case ToolController.Tool.PIN:
                    _instance.SelectCategory(ref _instance._pinCategory);
                    break;
                case ToolController.Tool.BRUSH:
                    _instance.SelectCategory(ref _instance._brushCategory);
                    break;
                case ToolController.Tool.GRAVITY:
                    _instance.SelectCategory(ref _instance._gravityCategory);
                    break;
                case ToolController.Tool.LINE:
                    _instance.SelectCategory(ref _instance._lineCategory);
                    break;
                case ToolController.Tool.SHAPE:
                    _instance.SelectCategory(ref _instance._shapeCategory);
                    break;
                case ToolController.Tool.TILING:
                    _instance.SelectCategory(ref _instance._tilingCategory);
                    break;
                case ToolController.Tool.ERASER:
                    _instance.SelectCategory(ref _instance._eraserCategory);
                    break;
                case ToolController.Tool.REPLACER:
                    _instance.SelectCategory(ref _instance._replacerCategory);
                    break;
                case ToolController.Tool.SELECTION:
                    _instance.SelectCategory(ref _instance._selectionCategory);
                    break;
                case ToolController.Tool.CIRCLE_SELECT:
                    _instance.SelectCategory(ref _instance._circleSelectCategory);
                    break;
                case ToolController.Tool.BLOCK:
                    _instance.SelectCategory(ref _instance._blockCategory);
                    break;
                default: return;
            }
            _instance.Repaint();
        }
        #endregion

        #region INPUT HANDLING
        private void UpdateCombination()
        {
            if (_selectedShortcut == null) return;
            if (Event.current == null) return;
            if (Event.current.type != EventType.KeyDown) return;
            if (Event.current.keyCode == KeyCode.Escape)
            {
                Repaint();
                _selectedShortcut = null;
                return;
            }
            if (Event.current.keyCode < KeyCode.Space || Event.current.keyCode > KeyCode.F15) return;
            var combi = new PWBKeyCombination(Event.current.keyCode, Event.current.modifiers);
            Event.current.Use();
            void SetCombination()
            {
                _selectedShortcut.combination.Set(Event.current.keyCode, Event.current.modifiers);
                if (_selectedShortcut.combination is PWBKeyCombinationUSM)
                    (_selectedShortcut.combination as PWBKeyCombinationUSM).Rebind(Event.current.keyCode,
                        Event.current.modifiers);
                PWBSettings.UpdateShrotcutsConflictsAndSaveFile();
            }
            if (PWBSettings.shortcuts.CheckConflicts(combi, _selectedShortcut, out string conflicts))
            {
                if (BindingConflictDialog(combi.ToString(), conflicts)) SetCombination();
            }
            else SetCombination();
            _selectedShortcut = null;
            Repaint();
        }
        #endregion
    }
}
#pragma warning restore UDR0001
