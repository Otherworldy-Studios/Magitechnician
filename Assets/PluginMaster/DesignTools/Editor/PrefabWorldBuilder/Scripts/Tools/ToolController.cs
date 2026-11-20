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
    [UnityEditor.InitializeOnLoad]
    public static class ToolController
    {
        public enum Tool
        {
            NONE,
            PIN,
            BRUSH,
            GRAVITY,
            LINE,
            SHAPE,
            TILING,
            REPLACER,
            ERASER,
            SELECTION,
            CIRCLE_SELECT,
            EXTRUDE,
            MIRROR,
            FLOOR,
            WALL,
            BLOCK
        }

        private static Tool _current = ToolController.Tool.NONE;
        public enum ToolState { NONE, PREVIEW, EDIT, PERSISTENT }

        private static bool _editMode = false;
        public static System.Action<Tool> OnToolChange;
        public static System.Action OnToolModeChanged;
        public static bool _triggerToolChangeEvent = true;
        private static bool _editorInitialized = false;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Domain reload", "UDR0004:Domain Reload Analyzer")]
        static ToolController()
        {
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            UnityEditor.SceneManagement.EditorSceneManager.sceneClosing -= OnSceneClosing;
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosing += OnSceneClosing;

            PaletteManager.OnBrushSelectionChanged -= TilingManager.settings.UpdateCellSize;
            PaletteManager.OnBrushSelectionChanged += TilingManager.settings.UpdateCellSize;

            UnityEditor.EditorApplication.update -= OnFirstUpdate;
            UnityEditor.EditorApplication.update += OnFirstUpdate;
        }

        public static bool editMode
        {
            get => _editMode;
            set
            {
                if (_editMode == value) return;
                _editMode = value;
                if (OnToolModeChanged != null) OnToolModeChanged();
            }
        }
        public static ToolController.Tool current
        {
            get => _current;
            set
            {
                if (_current == value) return;
                var prevTool = _current;
                _current = value;
                if (_current != prevTool)
                {
                    BoundsUtils.ClearBoundsDictionaries();
                    if (_triggerToolChangeEvent && OnToolChange != null) OnToolChange(prevTool);
                    _editMode = false;
                    _triggerToolChangeEvent = true;
                    PWBPreferences.SelectToolCategory(_current);
                    PWBItemsWindow.RepainWindow();
                }

                switch (_current)
                {
                    case Tool.PIN:
                        PWBIO.UpdateOctree();
                        PWBIO.ResetPinValues();
                        if (PinManager.settings.ignoreSceneColliders) PWBIO.UpdateSceneColliderSet();
                        break;
                    case Tool.BRUSH:
                        PWBIO.UpdateOctree();
                        if (BrushManager.settings.ignoreSceneColliders) PWBIO.UpdateSceneColliderSet();
                        break;
                    case Tool.GRAVITY:
                        PWBIO.InitializeGravityTool();
                        break;
                    case Tool.ERASER:
                        PWBIO.UpdateOctree();
                        break;
                    case Tool.REPLACER:
                        PWBIO.UpdateOctree();
                        PWBIO.ResetReplacer();
                        break;
                    case Tool.EXTRUDE:
                        SelectionManager.UpdateSelection();
                        PWBIO.ResetUnityCurrentTool();
                        PWBIO.ResetExtrudeState(false);
                        break;
                    case Tool.LINE:
                        PWBIO.InitializeLineTool();
                        break;
                    case Tool.SHAPE:
                        PWBIO.InitializeShapeTool();
                        break;
                    case Tool.TILING:
                        PWBIO.InitializeTilingTool();
                        break;
                    case Tool.SELECTION:
                        PWBIO.InitializeSelectionTool();
                        break;
                    case Tool.CIRCLE_SELECT:
                        PWBIO.UpdateOctree();
                        break;
                    case Tool.MIRROR:
                        SelectionManager.UpdateSelection();
                        PWBIO.InitializeMirrorPose();
                        break;
                    case Tool.FLOOR:
                        PWBIO.OnFloorEnabled();
                        break;
                    case Tool.WALL:
                        PWBIO.OnWallEnabled();
                        break;
                    case Tool.BLOCK:
                        PWBIO.OnBlockEnabled();
                        break;
                    case Tool.NONE:
                        PWBIO.ResetUnityCurrentTool();
                        PWBIO.ResetReplacer();
                        PWBIO.BeginInspectorRecovery();
                        PWBCore.DestroyTempColliders();
                        ApplicationEventHandler.hierarchyChangedWhileUsingTools = false;
                        break;
                    default: break;
                }

                if (_current != Tool.NONE)
                {
                    PWBIO.SaveUnityCurrentTool();
                    if (PWBCore.staticData.openToolPropertiesWhenAToolIsSelected) ToolProperties.ShowWindow();
                    PaletteManager.pickingBrushes = false;
                    ToolModes.RepainWindow();
                }

                if (_current == Tool.BRUSH || _current == Tool.PIN || _current == Tool.GRAVITY
                    || _current == Tool.REPLACER || _current == Tool.ERASER || _current == Tool.LINE
                    || _current == Tool.SHAPE || _current == Tool.TILING || _current == Tool.FLOOR
                    || _current == Tool.WALL || _current == Tool.BLOCK)
                {
                    PrefabPalette.ShowWindow();
                    SelectionManager.UpdateSelection();
                    if (_current == Tool.BRUSH || _current == Tool.PIN || _current == Tool.GRAVITY
                        || _current == Tool.REPLACER || _current == Tool.FLOOR || _current == Tool.WALL
                        || _current == Tool.BLOCK)
                        BrushstrokeManager.UpdateBrushstroke();
                    PWBIO.ResetAutoParent();
                }

                if (_current == Tool.LINE || _current == Tool.SHAPE
                    || _current == Tool.TILING || _current == Tool.NONE)
                    PWBItemsWindow.RepainWindow();

                ToolProperties.RepainWindow();
                if (BrushProperties.instance != null) BrushProperties.instance.Repaint();
                if (_current != Tool.NONE && UnityEditor.SceneView.sceneViews.Count > 0)
                    ((UnityEditor.SceneView)UnityEditor.SceneView.sceneViews[0]).Focus();
            }
        }

        public static void DeselectTool(bool triggerToolChangeEvent = true)
        {
            _triggerToolChangeEvent = triggerToolChangeEvent;
            if (current == Tool.REPLACER) PWBIO.ResetReplacer();
            current = Tool.NONE;
            PWBIO.ResetUnityCurrentTool();
            PWBToolbar.RepaintWindow();
        }
        private static void OnFirstUpdate()
        {
            _editorInitialized = true;
            UnityEditor.EditorApplication.update -= OnFirstUpdate;
        }
        private static void OnSceneClosing(UnityEngine.SceneManagement.Scene scene, bool removingScene)
        {
            if (!_editorInitialized) return;
            PWBCore.staticData.SaveAndUpdateVersion();
            DeselectTool();
        }

        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (!_editorInitialized) return;
            DeselectTool();
            PWBCore.DestroyTempColliders();
        }

        public static void OnPaletteClosed()
        {
            if (current != Tool.ERASER && current != Tool.EXTRUDE)
                current = Tool.NONE;
        }

        public static Tool GetToolFromSettings(IToolSettings settings)
        {
            if (settings is PinSettings) return Tool.PIN;
            if (settings is GravityToolSettings) return Tool.GRAVITY;
            if (settings is BrushToolSettings) return Tool.BRUSH;
            if (settings is ShapeSettings) return Tool.SHAPE;
            if (settings is LineSettings) return Tool.LINE;
            if (settings is TilingSettings) return Tool.TILING;
            if (settings is ReplacerSettings) return Tool.REPLACER;
            if (settings is EraserSettings) return Tool.ERASER;
            if (settings is SelectionToolSettings) return Tool.SELECTION;
            if (settings is CircleSelectSettings) return Tool.CIRCLE_SELECT;
            if (settings is ExtrudeSettings) return Tool.EXTRUDE;
            if (settings is MirrorSettings) return Tool.MIRROR;
            if (settings is FloorSettings) return Tool.FLOOR;
            if (settings is WallSettings) return Tool.WALL;
            if (settings is BlockSettings) return Tool.BLOCK;
            return Tool.NONE;
        }
        public static Tool GetToolFromSettings(IPaintToolSettings settings)
        {
            if (settings is PinSettings) return Tool.PIN;
            if (settings is GravityToolSettings) return Tool.GRAVITY;
            if (settings is BrushToolSettings) return Tool.BRUSH;
            if (settings is ShapeSettings) return Tool.SHAPE;
            if (settings is LineSettings) return Tool.LINE;
            if (settings is TilingSettings) return Tool.TILING;
            if (settings is ReplacerSettings) return Tool.REPLACER;
            if (settings is EraserSettings) return Tool.ERASER;
            if (settings is SelectionToolSettings) return Tool.SELECTION;
            if (settings is CircleSelectSettings) return Tool.CIRCLE_SELECT;
            if (settings is ExtrudeSettings) return Tool.EXTRUDE;
            if (settings is MirrorSettings) return Tool.MIRROR;
            if (settings is FloorSettings) return Tool.FLOOR;
            if (settings is WallSettings) return Tool.WALL;
            if (settings is BlockSettings) return Tool.BLOCK;
            return Tool.NONE;
        }

        public static IToolSettings GetSettingsFromTool(Tool tool)
        {
            switch (tool)
            {
                case Tool.PIN: return PinManager.settings;
                case Tool.BRUSH: return BrushManager.settings;
                case Tool.GRAVITY: return GravityToolController.settings;
                case Tool.REPLACER: return ReplacerManager.settings;
                case Tool.ERASER: return EraserManager.settings;
                case Tool.EXTRUDE: return ExtrudeManager.settings;
                case Tool.LINE: return LineManager.settings;
                case Tool.SHAPE: return ShapeManager.settings;
                case Tool.TILING: return TilingManager.settings;
                case Tool.SELECTION: return SelectionToolController.settings;
                case Tool.CIRCLE_SELECT: return CircleSelectManager.settings;
                case Tool.MIRROR: return MirrorManager.settings;
                case Tool.FLOOR: return FloorManager.settings;
                case Tool.WALL: return WallManager.settings;
                case Tool.BLOCK: return BlockManager.settings;
                default: return null;
            }
        }

        public static IPersistentToolController GetCurrentPersistentToolController()
        {
            switch (current)
            {
                case Tool.LINE: return LineManager.instance;
                case Tool.SHAPE: return ShapeManager.instance;
                case Tool.TILING: return TilingManager.instance;
                default: return null;
            }
        }

        public static IPersistentData[] GetCurrentPersistentToolData()
        {
            var manager = GetCurrentPersistentToolController();
            if (manager == null) return null;
            return manager.GetItems();
        }

        public static bool IsCurrentToolPersistent()
            => current == Tool.LINE || current == Tool.SHAPE || current == Tool.TILING;
    }
}
#pragma warning restore UDR0001
