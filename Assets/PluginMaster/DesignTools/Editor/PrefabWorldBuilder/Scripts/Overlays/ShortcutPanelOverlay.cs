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
#if UNITY_2022_2_OR_NEWER
using UnityEngine;
namespace PluginMaster
{
    [UnityEditor.Overlays.Overlay(typeof(UnityEditor.SceneView), displayName: "PWB/Shortcuts",
    defaultDisplay: false, defaultDockZone = UnityEditor.Overlays.DockZone.Floating)]

    public class PWBShortcutPanel : UnityEditor.Overlays.Overlay
    {
        private static PWBShortcutPanel _instance = null;
        private System.Collections.Generic.List<(string Command, string Shortcut)> _shortcutTable
            = new System.Collections.Generic.List<(string Command, string Shortcut)>();
        UnityEngine.UIElements.MultiColumnListView _multiColumnListView = null;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Domain reload", "UDR0004:Domain Reload Analyzer")]
        public PWBShortcutPanel()
        {
            _instance = this;
            PWBSettings.OnShrotcutProfileChanged -= UpdatePanel;
            PWBSettings.OnShrotcutProfileChanged += UpdatePanel;
            collapsedIcon = Resources.Load<Texture2D>($"{ToggleManager.iconPath}Shortcuts");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Domain reload", "UDR0004:Domain Reload Analyzer")]
        public override UnityEngine.UIElements.VisualElement CreatePanelContent()
        {
            _multiColumnListView = new UnityEngine.UIElements.MultiColumnListView
            {
                showBoundCollectionSize = false,
                virtualizationMethod = UnityEngine.UIElements.CollectionVirtualizationMethod.DynamicHeight,
                selectionType = UnityEngine.UIElements.SelectionType.None,
            };

            _multiColumnListView.columns.Add(new UnityEngine.UIElements.Column
            {
                title = "Shortcut",
                stretchable = true,
                minWidth = 220,
                makeCell = () => new UnityEngine.UIElements.Label(),
                bindCell = (UnityEngine.UIElements.VisualElement element, int index) =>
                    (element as UnityEngine.UIElements.Label).text = _shortcutTable[index].Shortcut
            });
            _multiColumnListView.columns.Add(new UnityEngine.UIElements.Column
            {
                title = "Command",
                stretchable = true,
                minWidth = 270,
                makeCell = () => new UnityEngine.UIElements.Label(),
                bindCell = (UnityEngine.UIElements.VisualElement element, int index) =>
                    (element as UnityEngine.UIElements.Label).text = _shortcutTable[index].Command
            });
            _multiColumnListView.itemsSource = _shortcutTable;
            UnityEditor.SceneView.duringSceneGui -= DuringSceneGUI;
            UnityEditor.SceneView.duringSceneGui += DuringSceneGUI;
            ToolController.OnToolChange -= OnToolChange;
            ToolController.OnToolChange += OnToolChange;
            return _multiColumnListView;
        }

        private void UpdatePanel()
        {
            _shortcutTable.Clear();
            if (ToolController.current == ToolController.Tool.NONE)
            {
                _shortcutTable.AddRange(PWBShortcuts.GetAllShortcuts(PWBShortcut.Group.GLOBAL, PWBShortcut.Group.GRID));
                _shortcutTable.AddRange(PWBShortcuts.GetAllShortcuts(PWBShortcut.Group.GRID, PWBShortcut.Group.NONE));
            }
            else _shortcutTable.AddRange(PWBShortcuts.GetAllShortcuts(GetGroup(), PWBShortcut.Group.NONE));
            if(_multiColumnListView == null) CreatePanelContent();
            _multiColumnListView.Rebuild();
        }

        private void OnToolChange(ToolController.Tool prevTool) => UpdatePanel();

        private static PWBShortcut.Group GetGroup()
        {
            switch (ToolController.current)
            {
                case ToolController.Tool.PIN: return PWBShortcut.Group.PIN;
                case ToolController.Tool.BRUSH: return PWBShortcut.Group.BRUSH;
                case ToolController.Tool.GRAVITY: return PWBShortcut.Group.GRAVITY;
                case ToolController.Tool.LINE: return PWBShortcut.Group.LINE;
                case ToolController.Tool.SHAPE: return PWBShortcut.Group.SHAPE;
                case ToolController.Tool.TILING: return PWBShortcut.Group.TILING;
                case ToolController.Tool.REPLACER: return PWBShortcut.Group.REPLACER;
                case ToolController.Tool.ERASER: return PWBShortcut.Group.ERASER;
                case ToolController.Tool.SELECTION: return PWBShortcut.Group.SELECTION;
                case ToolController.Tool.CIRCLE_SELECT: return PWBShortcut.Group.CIRCLE_SELECT;
                default: return PWBShortcut.Group.NONE;
            }
        }

        private void DuringSceneGUI(UnityEditor.SceneView sceneView)
        {
            if (_shortcutTable.Count == 0) OnToolChange(ToolController.Tool.NONE);
        }
        public static bool hasInstance  => _instance != null;
        public static bool isDisplayed => _instance != null && _instance.displayed;

        public static void SetDisplayed(bool value)
        {
            if (_instance != null)
                _instance.displayed = value;
        }

        public static void ShowWindow()
        {
            if (_instance != null)
                _instance.displayed = true;
        }
    }
}
#endif
#pragma warning restore UDR0001
