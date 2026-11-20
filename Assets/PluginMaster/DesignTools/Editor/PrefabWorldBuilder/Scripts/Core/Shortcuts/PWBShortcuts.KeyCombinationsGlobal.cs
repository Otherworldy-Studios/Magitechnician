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
    public partial class PWBShortcuts
    {
        #region GRID
        [SerializeField]
        private PWBKeyShortcut _gridEnableShortcuts = new PWBKeyShortcut("First step to enable grid shortcuts",
           PWBShortcut.Group.GLOBAL | PWBShortcut.Group.GRID, KeyCode.G, EventModifiers.Control);
        [SerializeField]
        private PWBTwoStepKeyShortcut _gridToggle = new PWBTwoStepKeyShortcut("Toggle grid",
            PWBShortcut.Group.GRID, KeyCode.G, EventModifiers.Control);
        [SerializeField]
        private PWBTwoStepKeyShortcut _gridToggleSnapping = new PWBTwoStepKeyShortcut("Toggle snapping",
            PWBShortcut.Group.GRID, KeyCode.H, EventModifiers.Control);
        [SerializeField]
        private PWBTwoStepKeyShortcut _gridToggleLock = new PWBTwoStepKeyShortcut("Toggle grid lock",
            PWBShortcut.Group.GRID, KeyCode.L, EventModifiers.Control);
        [SerializeField]
        private PWBTwoStepKeyShortcut _gridSetOriginPosition
            = new PWBTwoStepKeyShortcut("Set the origin to the active gameobject position",
            PWBShortcut.Group.GRID, KeyCode.W, EventModifiers.Control);
        [SerializeField]
        private PWBTwoStepKeyShortcut _gridSetOriginRotation
            = new PWBTwoStepKeyShortcut("Set the grid rotation to the active gameobject rotation",
            PWBShortcut.Group.GRID, KeyCode.E, EventModifiers.Control);
        [SerializeField]
        private PWBTwoStepKeyShortcut _gridSetSize
            = new PWBTwoStepKeyShortcut("Set the snap value to the size of the active gameobject",
            PWBShortcut.Group.GRID, KeyCode.R, EventModifiers.Control);
        [SerializeField]
        private PWBTwoStepKeyShortcut _gridFrameOrigin = new PWBTwoStepKeyShortcut("Frame grid origin",
            PWBShortcut.Group.GRID, KeyCode.Q, EventModifiers.Control);
        [SerializeField]
        private PWBTwoStepKeyShortcut _gridTogglePositionHandle = new PWBTwoStepKeyShortcut("Toggle Postion Handle",
            PWBShortcut.Group.GRID, KeyCode.W, EventModifiers.Control | EventModifiers.Alt);
        [SerializeField]
        private PWBTwoStepKeyShortcut _gridToggleRotationHandle = new PWBTwoStepKeyShortcut("Toggle Rotation Handle",
            PWBShortcut.Group.GRID, KeyCode.E, EventModifiers.Control | EventModifiers.Alt);
        [SerializeField]
        private PWBTwoStepKeyShortcut _gridToggleSpacingHandle = new PWBTwoStepKeyShortcut("Toggle Spacing Handle",
            PWBShortcut.Group.GRID, KeyCode.R, EventModifiers.Control | EventModifiers.Alt);
        [SerializeField]
        private PWBTwoStepKeyShortcut _gridMoveOriginUp = new PWBTwoStepKeyShortcut("Move the origin one step up",
            PWBShortcut.Group.GRID, KeyCode.J, EventModifiers.Control | EventModifiers.Alt);
        [SerializeField]
        private PWBTwoStepKeyShortcut _gridMoveOriginDown = new PWBTwoStepKeyShortcut("Move the origin one step down",
           PWBShortcut.Group.GRID, KeyCode.M, EventModifiers.Control | EventModifiers.Alt);
        [SerializeField]
        private PWBTwoStepKeyShortcut _gridNextOrigin = new PWBTwoStepKeyShortcut("Set next origin",
           PWBShortcut.Group.GRID, KeyCode.Alpha9, EventModifiers.Control | EventModifiers.Alt);
        [SerializeField]
        private PWBHoldKeysAndClickShortcut _gridMoveOriginToMousePos
            = new PWBHoldKeysAndClickShortcut("Move the origin to mouse position",
          PWBShortcut.Group.GRID, KeyCode.A, EventModifiers.Shift);
        public PWBKeyShortcut gridEnableShortcuts => _gridEnableShortcuts;
        public PWBTwoStepKeyShortcut gridToggle => _gridToggle;
        public PWBTwoStepKeyShortcut gridToggleSnaping => _gridToggleSnapping;
        public PWBTwoStepKeyShortcut gridToggleLock => _gridToggleLock;
        public PWBTwoStepKeyShortcut gridSetOriginPosition => _gridSetOriginPosition;
        public PWBTwoStepKeyShortcut gridSetOriginRotation => _gridSetOriginRotation;
        public PWBTwoStepKeyShortcut gridSetSize => _gridSetSize;
        public PWBTwoStepKeyShortcut gridFrameOrigin => _gridFrameOrigin;
        public PWBTwoStepKeyShortcut gridTogglePositionHandle => _gridTogglePositionHandle;
        public PWBTwoStepKeyShortcut gridToggleRotationHandle => _gridToggleRotationHandle;
        public PWBTwoStepKeyShortcut gridToggleSpacingHandle => _gridToggleSpacingHandle;
        public PWBTwoStepKeyShortcut gridMoveOriginUp => _gridMoveOriginUp;
        public PWBTwoStepKeyShortcut gridMoveOriginDown => _gridMoveOriginDown;
        public PWBTwoStepKeyShortcut gridNextOrigin => _gridNextOrigin;
        public PWBHoldKeysAndClickShortcut gridMoveOriginToMousePos => _gridMoveOriginToMousePos;
        #endregion

        #region SNAP
        [SerializeField]
        private PWBKeyShortcut _snapToggleBoundsSnapping = new PWBKeyShortcut("Toggle bounds snapping",
            PWBShortcut.Group.GLOBAL, KeyCode.K, EventModifiers.Control | EventModifiers.Shift);
        public PWBKeyShortcut snapToggleBoundsSnapping => _snapToggleBoundsSnapping;
        #endregion

        #region GIZMOS
        [SerializeField]
        private PWBKeyShortcut _gizmosToggleInfotext = new PWBKeyShortcut("Toggle InfoText",
          PWBShortcut.Group.GLOBAL, KeyCode.I, EventModifiers.Control | EventModifiers.Alt);
        public PWBKeyShortcut gizmosToggleInfotext => _gizmosToggleInfotext;
        #endregion

        #region TOOLBAR
        private PWBKeyShortcut _toolbarFloorToggle = new PWBKeyShortcut("Toggle Floor Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM(Shortcuts.PWB_TOGGLE_FLOOR_SHORTCUT_ID));
        public PWBKeyShortcut toolbarFloorToggle => _toolbarFloorToggle;
        private PWBKeyShortcut _toolbarWallToggle = new PWBKeyShortcut("Toggle Wall Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM(Shortcuts.PWB_TOGGLE_WALL_SHORTCUT_ID));
        public PWBKeyShortcut toolbarWallToggle => _toolbarWallToggle;
        private PWBKeyShortcut _toolbarBlockToggle = new PWBKeyShortcut("Toggle Block Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM(Shortcuts.PWB_TOGGLE_BLOCK_SHORTCUT_ID));
        public PWBKeyShortcut toolbarBlockToggle => _toolbarBlockToggle;
        private PWBKeyShortcut _toolbarPinToggle = new PWBKeyShortcut("Toggle Pin Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM(Shortcuts.PWB_TOGGLE_PIN_SHORTCUT_ID));
        public PWBKeyShortcut toolbarPinToggle => _toolbarPinToggle;

        private PWBKeyShortcut _toolbarBrushToggle = new PWBKeyShortcut("Toggle Brush Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM(Shortcuts.PWB_TOGGLE_BRUSH_SHORTCUT_ID));
        public PWBKeyShortcut toolbarBrushToggle => _toolbarBrushToggle;

        private PWBKeyShortcut _toolbarGravityToggle = new PWBKeyShortcut("Toggle Gravity Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM(Shortcuts.PWB_TOGGLE_GRAVITY_SHORTCUT_ID));
        public PWBKeyShortcut toolbarGravityToggle => _toolbarGravityToggle;

        private PWBKeyShortcut _toolbarLineToggle = new PWBKeyShortcut("Toggle Line Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM(Shortcuts.PWB_TOGGLE_LINE_SHORTCUT_ID));
        public PWBKeyShortcut toolbarLineToggle => _toolbarLineToggle;

        private PWBKeyShortcut _toolbarShapeToggle = new PWBKeyShortcut("Toggle Shape Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM(Shortcuts.PWB_TOGGLE_SHAPE_SHORTCUT_ID));
        public PWBKeyShortcut toolbarShapeToggle => _toolbarShapeToggle;

        private PWBKeyShortcut _toolbarTilingToggle = new PWBKeyShortcut("Toggle Tiling Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM(Shortcuts.PWB_TOGGLE_TILING_SHORTCUT_ID));
        public PWBKeyShortcut toolbarTilingToggle => _toolbarTilingToggle;

        private PWBKeyShortcut _toolbarReplacerToggle = new PWBKeyShortcut("Toggle Replacer Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM(Shortcuts.PWB_TOGGLE_REPLACER_SHORTCUT_ID));
        public PWBKeyShortcut toolbarReplacerToggle => _toolbarReplacerToggle;

        private PWBKeyShortcut _toolbarEraserToggle = new PWBKeyShortcut("Toggle Eraser Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM(Shortcuts.PWB_TOGGLE_ERASER_SHORTCUT_ID));
        public PWBKeyShortcut toolbarEraserToggle => _toolbarEraserToggle;

        private PWBKeyShortcut _toolbarSelectionToggle = new PWBKeyShortcut("Toggle Selection Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM(Shortcuts.PWB_TOGGLE_SELECTION_SHORTCUT_ID));
        public PWBKeyShortcut toolbarSelectionToggle => _toolbarSelectionToggle;

        private PWBKeyShortcut _toolbarCircleSelectToggle = new PWBKeyShortcut("Toggle Circle Selection Tool",
           PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM(Shortcuts.PWB_TOGGLE_CIRCLE_SELECT_SHORTCUT_ID));
        public PWBKeyShortcut toolbarCircleSelectToggle => _toolbarCircleSelectToggle;

        private PWBKeyShortcut _toolbarExtrudeToggle = new PWBKeyShortcut("Toggle Extrude Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM(Shortcuts.PWB_TOGGLE_EXTRUDE_SHORTCUT_ID));
        public PWBKeyShortcut toolbarExtrudeToggle => _toolbarExtrudeToggle;

        private PWBKeyShortcut _toolbarMirrorToggle = new PWBKeyShortcut("Toggle Mirror Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM(Shortcuts.PWB_TOGGLE_MIRROR_SHORTCUT_ID));
        public PWBKeyShortcut toolbarMirrorToggle => _toolbarMirrorToggle;
        #endregion

        #region WINDOWS & OVERLAYS
        private PWBKeyShortcut _closeAllWindows = new PWBKeyShortcut("Close All Windows",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM(Shortcuts.PWB_CLOSE_ALL_WINDOWS_ID));
        public PWBKeyShortcut closeAllWindows => _closeAllWindows;

        private PWBKeyShortcut _toggleOverlays = new PWBKeyShortcut("Toggle Overlays",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM(Shortcuts.PWB_TOGGLE_OVERLAYS_ID));
        public PWBKeyShortcut toggleOverlays => _toggleOverlays;
        #endregion

        #region PALETTE
        [SerializeField]
        private PWBKeyShortcut _paletteDeleteBrush = new PWBKeyShortcut("Delete selected brushes",
           PWBShortcut.Group.PALETTE | PWBShortcut.Group.GLOBAL,
           KeyCode.Delete, EventModifiers.Control | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _palettePreviousBrush = new PWBKeyShortcut("Select previous brush",
          PWBShortcut.Group.PALETTE | PWBShortcut.Group.GLOBAL,
          KeyCode.Z, EventModifiers.Control | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _paletteNextBrush = new PWBKeyShortcut("Select next brush",
          PWBShortcut.Group.PALETTE | PWBShortcut.Group.GLOBAL,
          KeyCode.X, EventModifiers.Control | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _palettePreviousPalette = new PWBKeyShortcut("Select previous palette",
          PWBShortcut.Group.PALETTE | PWBShortcut.Group.GLOBAL,
          KeyCode.Z, EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _paletteNextPalette = new PWBKeyShortcut("Select next palette",
          PWBShortcut.Group.PALETTE | PWBShortcut.Group.GLOBAL,
          KeyCode.X, EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift);
        [SerializeField]
        private PWBHoldKeysAndClickShortcut _palettePickBrush = new PWBHoldKeysAndClickShortcut("Pick or add a brush",
          PWBShortcut.Group.PALETTE | PWBShortcut.Group.GLOBAL,
          KeyCode.Alpha1, EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _paletteReplaceSceneSelection = new PWBKeyShortcut("Replace selected objects in scene",
          PWBShortcut.Group.PALETTE | PWBShortcut.Group.GLOBAL,
          KeyCode.I, EventModifiers.Control | EventModifiers.Shift);

        public PWBKeyShortcut paletteDeleteBrush => _paletteDeleteBrush;
        public PWBKeyShortcut palettePreviousBrush => _palettePreviousBrush;
        public PWBKeyShortcut paletteNextBrush => _paletteNextBrush;
        public PWBKeyShortcut palettePreviousPalette => _palettePreviousPalette;
        public PWBKeyShortcut paletteNextPalette => _paletteNextPalette;
        public PWBHoldKeysAndClickShortcut palettePickBrush => _palettePickBrush;
        public PWBKeyShortcut paletteReplaceSceneSelection => _paletteReplaceSceneSelection;
        #endregion
    }
}
#pragma warning restore UDR0001
