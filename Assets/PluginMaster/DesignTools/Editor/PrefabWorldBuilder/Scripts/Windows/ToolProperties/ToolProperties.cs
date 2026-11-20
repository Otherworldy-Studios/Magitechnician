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
    public partial class ToolProperties : UnityEditor.EditorWindow
    {

        #region COMMON
        private const string UNDO_MSG = "Tool properties";
        private Vector2 _mainScrollPosition = Vector2.zero;
        private GUIContent _updateButtonContent = null;
        private GUISkin _skin = null;
        private GUIStyle _reloadBtnStyle = null;

        private GUISkin skin
        {
            get
            {
                if (_skin != null) return _skin;
                _skin = Resources.Load<GUISkin>("PWBSkin");
                return _skin;
            }
        }
        private GUIStyle reloadBtnStyle
        {
            get
            {
                if (_reloadBtnStyle != null) return _reloadBtnStyle;
                if (skin == null) return GUIStyle.none;
                _reloadBtnStyle = skin.GetStyle("EyeButton");
                return _reloadBtnStyle;
            }
        }
        private static ToolProperties _instance = null;


        [UnityEditor.MenuItem("Tools/Plugin Master/Prefab World Builder/Tool Properties...", false, 1130)]
        public static void ShowWindow() => _instance = GetWindow<ToolProperties>("Tool Properties");

        private static UnityEditor.EditorApplication.CallbackFunction _repaintDelayedCall = null;

        public static void RepainWindow()
        {
            if (_instance == null) return;

            if (_repaintDelayedCall == null)
                _instance.Repaint();
            else
                UnityEditor.EditorApplication.delayCall -= _repaintDelayedCall;

            _repaintDelayedCall = () =>
            {
                _repaintDelayedCall = null;
                if (_instance != null) _instance.Repaint();
            };

            UnityEditor.EditorApplication.delayCall += _repaintDelayedCall;
        }

        public static void CloseWindow()
        {
            if (_instance != null) _instance.Close();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Domain reload", "UDR0004:Domain Reload Analyzer")]
        private void OnEnable()
        {
            _updateButtonContent
                = new GUIContent(Resources.Load<Texture2D>("Sprites/Update"), "Update Temp Colliders");
            UnityEditor.Undo.undoRedoPerformed -= Repaint;
            UnityEditor.Undo.undoRedoPerformed += Repaint;
            PWBCore.Initialize();
        }

        private void OnDisable()
        {
            PWBCore.DestroyTempColliders();
            UnityEditor.Undo.undoRedoPerformed -= Repaint;
        }

        private void OnGUI()
        {
            if (_instance == null) _instance = this;
            using (var scrollView = new UnityEditor.EditorGUILayout.ScrollViewScope(_mainScrollPosition,
                false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUIStyle.none))
            {
                _mainScrollPosition = scrollView.scrollPosition;
#if UNITY_2021_2_OR_NEWER
#else
                if (PWBToolbar.instance == null) PWBToolbar.ShowWindow();
#endif
                if (ToolController.current == ToolController.Tool.PIN) PinGroup();
                else if (ToolController.current == ToolController.Tool.BRUSH) BrushGroup();
                else if (ToolController.current == ToolController.Tool.ERASER) EraserGroup();
                else if (ToolController.current == ToolController.Tool.GRAVITY) GravityGroup();
                else if (ToolController.current == ToolController.Tool.EXTRUDE) ExtrudeGroup();
                else if (ToolController.current == ToolController.Tool.LINE) LineGroup();
                else if (ToolController.current == ToolController.Tool.SHAPE) ShapeGroup();
                else if (ToolController.current == ToolController.Tool.TILING) TilingGroup();
                else if (ToolController.current == ToolController.Tool.SELECTION) SelectionGroup();
                else if (ToolController.current == ToolController.Tool.CIRCLE_SELECT) CircleSelectGroup();
                else if (ToolController.current == ToolController.Tool.MIRROR) MirrorGroup();
                else if (ToolController.current == ToolController.Tool.REPLACER) ReplacerGroup();
                else if (ToolController.current == ToolController.Tool.FLOOR) FloorGroup();
                else if (ToolController.current == ToolController.Tool.WALL) WallGroup();
                else if (ToolController.current == ToolController.Tool.BLOCK) BlockGroup();
            }
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                GUI.FocusControl(null);
                Repaint();
            }
        }
        public static void ClearUndo()
        {
            if (_instance == null) return;
            UnityEditor.Undo.ClearUndo(_instance);
        }
        #endregion

        #region UNDO
        [SerializeField] private LineData _lineData = LineData.instance;
        [SerializeField] private TilingData _tilingData = TilingData.instance;
        [SerializeField] private MirrorSettings _mirrorSettings = MirrorManager.settings;
        [SerializeField] private ShapeData _shapeData = ShapeData.instance;
        [SerializeField] private TilingManager _tilingManager = TilingManager.instance as TilingManager;
        [SerializeField] private ShapeManager _shapeManager = ShapeManager.instance as ShapeManager;
        [SerializeField] private LineManager _lineManager = LineManager.instance as LineManager;
        public static void RegisterUndo(string commandName)
        {
            if (_instance != null) UnityEditor.Undo.RegisterCompleteObjectUndo(_instance, commandName);
        }
        #endregion

        #region SELECTION BRUSH AND MODIFIER SETTINGS
        private static readonly string[] _modifierCommandOptions = { "All", "Palette Prefabs", "Brush Prefabs" };
        private void SelectionBrushGroup(ISelectionBrushTool settings, string actionLabel)
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                UnityEditor.EditorGUIUtility.labelWidth = 60;
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var command = (ISelectionBrushTool.Command)UnityEditor.EditorGUILayout.Popup(actionLabel,
                        (int)settings.command, _modifierCommandOptions);
                    if (check.changed)
                    {
                        settings.command = command;
                        PWBIO.UpdateOctree();
                    }
                }
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var onlyTheClosest = UnityEditor.EditorGUILayout.ToggleLeft(actionLabel + " only the closest",
                        settings.onlyTheClosest);
                    if (check.changed)
                    {
                        settings.onlyTheClosest = onlyTheClosest;
                    }
                }
            }
        }

        private void ModifierGroup(IModifierTool settings, string actionLabel)
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                UnityEditor.EditorGUIUtility.labelWidth = 60;
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var allButSelected = UnityEditor.EditorGUILayout.ToggleLeft(actionLabel + " all but selected",
                        settings.modifyAllButSelected);
                    if (check.changed)
                    {
                        settings.modifyAllButSelected = allButSelected;
                        PWBIO.UpdateOctree();
                    }
                }
            }
        }
        #endregion

    }
}
#pragma warning restore UDR0001
