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
        #region PIN
        [SerializeField]
        private PWBKeyShortcut _pinMoveHandlesUp = new PWBKeyShortcut("Move handles up",
           PWBShortcut.Group.PIN, KeyCode.U, EventModifiers.Control | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _pinMoveHandlesDown = new PWBKeyShortcut("Move handles down",
           PWBShortcut.Group.PIN, KeyCode.J, EventModifiers.Control | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _pinSelectNextHandle = new PWBKeyShortcut("Select the next handle as active",
           PWBShortcut.Group.PIN, KeyCode.Y, EventModifiers.Control | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _pinSelectPrevHandle = new PWBKeyShortcut("Select the previous handle as active",
           PWBShortcut.Group.PIN, KeyCode.H, EventModifiers.Control | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _pinSelectPivotHandle = new PWBKeyShortcut("Set the pivot as the active handle",
           PWBShortcut.Group.PIN, KeyCode.T, EventModifiers.Control | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _pinToggleRepeatItem = new PWBKeyShortcut("Toggle repeat item option",
           PWBShortcut.Group.PIN, KeyCode.T, EventModifiers.Control);
        [SerializeField]
        private PWBKeyShortcut _pinResetScale = new PWBKeyShortcut("Reset scale",
          PWBShortcut.Group.PIN, KeyCode.Period, EventModifiers.Control | EventModifiers.Shift);

        [SerializeField]
        private PWBKeyShortcut _pinRotate90YCW = new PWBKeyShortcut("Rotate 90� around Y",
          PWBShortcut.Group.PIN, KeyCode.Q, EventModifiers.Control);
        [SerializeField]
        private PWBKeyShortcut _pinRotate90YCCW = new PWBKeyShortcut("Rotate -90� around Y",
          PWBShortcut.Group.PIN, KeyCode.W, EventModifiers.Control);
        [SerializeField]
        private PWBKeyShortcut _pinRotateAStepYCW = new PWBKeyShortcut("Rotate in steps around Y",
        PWBShortcut.Group.PIN, KeyCode.Q, EventModifiers.Control | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _pinRotateAStepYCCW
            = new PWBKeyShortcut("Rotate in negative steps around Y",
        PWBShortcut.Group.PIN, KeyCode.W, EventModifiers.Control | EventModifiers.Shift);

        [SerializeField]
        private PWBKeyShortcut _pinRotate90XCW = new PWBKeyShortcut("Rotate 90� around X",
          PWBShortcut.Group.PIN, KeyCode.K, EventModifiers.Control | EventModifiers.Alt);
        [SerializeField]
        private PWBKeyShortcut _pinRotate90XCCW = new PWBKeyShortcut("Rotate -90� around X",
          PWBShortcut.Group.PIN, KeyCode.L, EventModifiers.Control | EventModifiers.Alt);
        [SerializeField]
        private PWBKeyShortcut _pinRotateAStepXCW = new PWBKeyShortcut("Rotate in steps around X",
        PWBShortcut.Group.PIN, KeyCode.K, EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _pinRotateAStepXCCW
            = new PWBKeyShortcut("Rotate in negative steps around X",
        PWBShortcut.Group.PIN, KeyCode.L, EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift);

        [SerializeField]
        private PWBKeyShortcut _pinRotate90ZCW = new PWBKeyShortcut("Rotate 90� around Z",
          PWBShortcut.Group.PIN, KeyCode.Period, EventModifiers.Control | EventModifiers.Alt);
        [SerializeField]
        private PWBKeyShortcut _pinRotate90ZCCW = new PWBKeyShortcut("Rotate -90� around Z",
          PWBShortcut.Group.PIN, KeyCode.Comma, EventModifiers.Control | EventModifiers.Alt);
        [SerializeField]
        private PWBKeyShortcut _pinRotateAStepZCW = new PWBKeyShortcut("Rotate in steps around Z",
        PWBShortcut.Group.PIN, KeyCode.Period, EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _pinRotateAStepZCCW
            = new PWBKeyShortcut("Rotate in negative steps around Z",
        PWBShortcut.Group.PIN, KeyCode.Comma, EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift);

        [SerializeField]
        private PWBKeyShortcut _pinResetRotation = new PWBKeyShortcut("Reset rotation to zero",
         PWBShortcut.Group.PIN, KeyCode.M, EventModifiers.Control | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _pinSnapRotationToGrid = new PWBKeyShortcut("Snap rotation to grid",
         PWBShortcut.Group.PIN, KeyCode.G, EventModifiers.Shift);


        [SerializeField]
        private PWBKeyShortcut _pinAdd1UnitToSurfDist = new PWBKeyShortcut("Surface distance +1",
          PWBShortcut.Group.PIN, KeyCode.U, EventModifiers.Control | EventModifiers.Alt);
        [SerializeField]
        private PWBKeyShortcut _pinSubtract1UnitFromSurfDist
            = new PWBKeyShortcut("Surface distance -1",
          PWBShortcut.Group.PIN, KeyCode.J, EventModifiers.Control | EventModifiers.Alt);
        [SerializeField]
        private PWBKeyShortcut _pinAdd01UnitToSurfDist
            = new PWBKeyShortcut("Surface distance +0.1",
         PWBShortcut.Group.PIN, KeyCode.U, EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _pinSubtract01UnitFromSurfDist
            = new PWBKeyShortcut("Surface distance -0.1",
          PWBShortcut.Group.PIN, KeyCode.J,
          EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift);

        [SerializeField]
        private PWBKeyShortcut _pinResetSurfDist = new PWBKeyShortcut("Surface distance = 0.0",
         PWBShortcut.Group.PIN, KeyCode.B, EventModifiers.Shift);

        [SerializeField]
        private PWBKeyShortcut _pinSelectPreviousItem = new PWBKeyShortcut("Select previous item in the multi-brush",
          PWBShortcut.Group.PIN, KeyCode.O, EventModifiers.Control | EventModifiers.Alt);
        [SerializeField]
        private PWBKeyShortcut _pinSelectNextItem = new PWBKeyShortcut("Select next item in the multi-brush",
          PWBShortcut.Group.PIN, KeyCode.N, EventModifiers.Control | EventModifiers.Alt);

        [SerializeField]
        private PWBKeyShortcut _pinFlipX = new PWBKeyShortcut("Flip sprite horizontally",
            PWBShortcut.Group.PIN, KeyCode.U, EventModifiers.Shift);

        public PWBKeyShortcut pinMoveHandlesUp => _pinMoveHandlesUp;
        public PWBKeyShortcut pinMoveHandlesDown => _pinMoveHandlesDown;
        public PWBKeyShortcut pinSelectNextHandle => _pinSelectNextHandle;
        public PWBKeyShortcut pinSelectPrevHandle => _pinSelectPrevHandle;
        public PWBKeyShortcut pinSelectPivotHandle => _pinSelectPivotHandle;
        public PWBKeyShortcut pinToggleRepeatItem => _pinToggleRepeatItem;
        public PWBKeyShortcut pinResetScale => _pinResetScale;

        public PWBKeyShortcut pinRotate90YCW => _pinRotate90YCW;
        public PWBKeyShortcut pinRotate90YCCW => _pinRotate90YCCW;
        public PWBKeyShortcut pinRotateAStepYCW => _pinRotateAStepYCW;
        public PWBKeyShortcut pinRotateAStepYCCW => _pinRotateAStepYCCW;

        public PWBKeyShortcut pinRotate90XCW => _pinRotate90XCW;
        public PWBKeyShortcut pinRotate90XCCW => _pinRotate90XCCW;
        public PWBKeyShortcut pinRotateAStepXCW => _pinRotateAStepXCW;
        public PWBKeyShortcut pinRotateAStepXCCW => _pinRotateAStepXCCW;

        public PWBKeyShortcut pinRotate90ZCW => _pinRotate90ZCW;
        public PWBKeyShortcut pinRotate90ZCCW => _pinRotate90ZCCW;
        public PWBKeyShortcut pinRotateAStepZCW => _pinRotateAStepZCW;
        public PWBKeyShortcut pinRotateAStepZCCW => _pinRotateAStepZCCW;

        public PWBKeyShortcut pinResetRotation => _pinResetRotation;
        public PWBKeyShortcut pinSnapRotationToGrid => _pinSnapRotationToGrid;

        public PWBKeyShortcut pinAdd1UnitToSurfDist => _pinAdd1UnitToSurfDist;
        public PWBKeyShortcut pinSubtract1UnitFromSurfDist => _pinSubtract1UnitFromSurfDist;
        public PWBKeyShortcut pinAdd01UnitToSurfDist => _pinAdd01UnitToSurfDist;
        public PWBKeyShortcut pinSubtract01UnitFromSurfDist => _pinSubtract01UnitFromSurfDist;

        public PWBKeyShortcut pinResetSurfDist => _pinResetSurfDist;

        public PWBKeyShortcut pinSelectPreviousItem => _pinSelectPreviousItem;
        public PWBKeyShortcut pinSelectNextItem => _pinSelectNextItem;

        public PWBKeyShortcut pinFlipX => _pinFlipX;


        #endregion

        #region BRUSH & GRAVITY
        [SerializeField]
        private PWBKeyShortcut _brushUpdatebrushstroke = new PWBKeyShortcut("Update brushstroke",
          PWBShortcut.Group.BRUSH | PWBShortcut.Group.GRAVITY, KeyCode.Period, EventModifiers.Control | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _brushResetRotation = new PWBKeyShortcut("Reset brush rotation",
          PWBShortcut.Group.BRUSH | PWBShortcut.Group.GRAVITY, KeyCode.M, EventModifiers.Control);
        public PWBKeyShortcut brushUpdatebrushstroke => _brushUpdatebrushstroke;
        public PWBKeyShortcut brushResetRotation => _brushResetRotation;
        #endregion

        #region GRAVITY
        [SerializeField]
        private PWBKeyShortcut _gravityAdd1UnitToSurfDist
            = new PWBKeyShortcut("Surface distance +1",
          PWBShortcut.Group.GRAVITY, KeyCode.U, EventModifiers.Control | EventModifiers.Alt);
        [SerializeField]
        private PWBKeyShortcut _gravitySubtract1UnitFromSurfDist
            = new PWBKeyShortcut("Surface distance -1",
          PWBShortcut.Group.GRAVITY, KeyCode.J, EventModifiers.Control | EventModifiers.Alt);
        [SerializeField]
        private PWBKeyShortcut _gravityAdd01UnitToSurfDist
            = new PWBKeyShortcut("Surface distance +0.1",
         PWBShortcut.Group.GRAVITY, KeyCode.U,
         EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _gravitySubtract01UnitFromSurfDist
            = new PWBKeyShortcut("Surface distance -0.1",
          PWBShortcut.Group.GRAVITY, KeyCode.J,
          EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift);

        public PWBKeyShortcut gravityAdd1UnitToSurfDist => _gravityAdd1UnitToSurfDist;
        public PWBKeyShortcut gravitySubtract1UnitFromSurfDist => _gravitySubtract1UnitFromSurfDist;
        public PWBKeyShortcut gravityAdd01UnitToSurfDist => _gravityAdd01UnitToSurfDist;
        public PWBKeyShortcut gravitySubtract01UnitFromSurfDist => _gravitySubtract01UnitFromSurfDist;
        #endregion

        #region EDIT MODE
        [SerializeField]
        private PWBKeyShortcut _editModeDeleteItemAndItsChildren
            = new PWBKeyShortcut("Delete selected item and its children",
           PWBShortcut.Group.LINE | PWBShortcut.Group.SHAPE | PWBShortcut.Group.TILING,
           KeyCode.Delete, EventModifiers.Alt);
        [SerializeField]
        private PWBKeyShortcut _editModeDeleteItemButNotItsChildren
            = new PWBKeyShortcut("Delete selected item but not its children",
           PWBShortcut.Group.LINE | PWBShortcut.Group.SHAPE | PWBShortcut.Group.TILING,
           KeyCode.Delete, EventModifiers.Alt | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _editModeSelectParent = new PWBKeyShortcut("Select parent object",
           PWBShortcut.Group.LINE | PWBShortcut.Group.SHAPE | PWBShortcut.Group.TILING,
           KeyCode.T, EventModifiers.Control | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _editModeToggle = new PWBKeyShortcut("Toggle edit mode",
          PWBShortcut.Group.LINE | PWBShortcut.Group.SHAPE | PWBShortcut.Group.TILING,
          KeyCode.Period, EventModifiers.Alt | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _editModeDuplicate = new PWBKeyShortcut("Duplicate",
          PWBShortcut.Group.LINE | PWBShortcut.Group.SHAPE | PWBShortcut.Group.TILING,
          KeyCode.D, EventModifiers.Control | EventModifiers.Shift);
        public PWBKeyShortcut editModeDeleteItemAndItsChildren => _editModeDeleteItemAndItsChildren;
        public PWBKeyShortcut editModeDeleteItemButNotItsChildren => _editModeDeleteItemButNotItsChildren;
        public PWBKeyShortcut editModeSelectParent => _editModeSelectParent;
        public PWBKeyShortcut editModeToggle => _editModeToggle;
        public PWBKeyShortcut editModeDuplicate => _editModeDuplicate;
        #endregion

        #region LINE
        [SerializeField]
        private PWBKeyShortcut _lineSelectAllPoints = new PWBKeyShortcut("Select all points",
          PWBShortcut.Group.LINE, KeyCode.A, EventModifiers.Control | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _lineDeselectAllPoints = new PWBKeyShortcut("Deselect all points",
          PWBShortcut.Group.LINE, KeyCode.D, EventModifiers.Control | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _lineToggleCurve = new PWBKeyShortcut("Set previous segment as Curved or Straight",
          PWBShortcut.Group.LINE, KeyCode.Y, EventModifiers.Control | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _lineToggleClosed = new PWBKeyShortcut("Close or open the line",
          PWBShortcut.Group.LINE, KeyCode.O, EventModifiers.Control | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _lineEditModeTypeToggle = new PWBKeyShortcut("Toggle edit mode type",
         PWBShortcut.Group.LINE, KeyCode.Comma, EventModifiers.Alt | EventModifiers.Shift);

        public PWBKeyShortcut lineSelectAllPoints => _lineSelectAllPoints;
        public PWBKeyShortcut lineDeselectAllPoints => _lineDeselectAllPoints;
        public PWBKeyShortcut lineToggleCurve => _lineToggleCurve;
        public PWBKeyShortcut lineToggleClosed => _lineToggleClosed;
        public PWBKeyShortcut lineEditModeTypeToggle => _lineEditModeTypeToggle;
        #endregion

        #region TILING & SELECTION
        [SerializeField]
        private PWBKeyShortcut _selectionRotate90XCW = new PWBKeyShortcut("Rotate 90� around X",
          PWBShortcut.Group.TILING | PWBShortcut.Group.SELECTION, KeyCode.U, EventModifiers.Control | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _selectionRotate90XCCW = new PWBKeyShortcut("Rotate -90� around X",
          PWBShortcut.Group.TILING | PWBShortcut.Group.SELECTION, KeyCode.J, EventModifiers.Control | EventModifiers.Shift);
        [SerializeField]
        private PWBKeyShortcut _selectionRotate90YCW = new PWBKeyShortcut("Rotate 90� around Y",
          PWBShortcut.Group.TILING | PWBShortcut.Group.SELECTION, KeyCode.K, EventModifiers.Control | EventModifiers.Alt);
        [SerializeField]
        private PWBKeyShortcut _selectionRotate90YCCW = new PWBKeyShortcut("Rotate -90� around Y",
          PWBShortcut.Group.TILING | PWBShortcut.Group.SELECTION, KeyCode.L, EventModifiers.Control | EventModifiers.Alt);
        [SerializeField]
        private PWBKeyShortcut _selectionRotate90ZCW = new PWBKeyShortcut("Rotate 90� around Z",
          PWBShortcut.Group.TILING | PWBShortcut.Group.SELECTION, KeyCode.U, EventModifiers.Control | EventModifiers.Alt);
        [SerializeField]
        private PWBKeyShortcut _selectionRotate90ZCCW = new PWBKeyShortcut("Rotate -90� around Z",
          PWBShortcut.Group.TILING | PWBShortcut.Group.SELECTION, KeyCode.J, EventModifiers.Control | EventModifiers.Alt);
        public PWBKeyShortcut selectionRotate90XCW => _selectionRotate90XCW;
        public PWBKeyShortcut selectionRotate90XCCW => _selectionRotate90XCCW;
        public PWBKeyShortcut selectionRotate90YCW => _selectionRotate90YCW;
        public PWBKeyShortcut selectionRotate90YCCW => _selectionRotate90YCCW;
        public PWBKeyShortcut selectionRotate90ZCW => _selectionRotate90ZCW;
        public PWBKeyShortcut selectionRotate90ZCCW => _selectionRotate90ZCCW;
        #endregion

        #region SELECTION
        [SerializeField]
        private PWBKeyShortcut _selectionTogglePositionHandle = new PWBKeyShortcut("Toggle position handle",
          PWBShortcut.Group.SELECTION, KeyCode.W);
        [SerializeField]
        private PWBKeyShortcut _selectionToggleRotationHandle = new PWBKeyShortcut("Toggle rotation handle",
          PWBShortcut.Group.SELECTION, KeyCode.E);
        [SerializeField]
        private PWBKeyShortcut _selectionToggleScaleHandle = new PWBKeyShortcut("Toggle scale handle",
          PWBShortcut.Group.SELECTION, KeyCode.R);
        [SerializeField]
        private PWBKeyShortcut _selectionEditCustomHandle = new PWBKeyShortcut("Edit custom handle",
          PWBShortcut.Group.SELECTION, KeyCode.U);
        [SerializeField]
        private PWBKeyShortcut _selectionToggleSpace = new PWBKeyShortcut("Toggle Space Global/Local",
          PWBShortcut.Group.SELECTION, KeyCode.X, EventModifiers.Shift);
        [SerializeField]
        private PWBHoldKeysAndMouseMoveShortcut _selectionMoveToMousePosition
            = new PWBHoldKeysAndMouseMoveShortcut("Move to mouse position", PWBShortcut.Group.SELECTION,
          KeyCode.W, EventModifiers.Shift);
        public PWBKeyShortcut selectionTogglePositionHandle => _selectionTogglePositionHandle;
        public PWBKeyShortcut selectionToggleRotationHandle => _selectionToggleRotationHandle;
        public PWBKeyShortcut selectionToggleScaleHandle => _selectionToggleScaleHandle;
        public PWBKeyShortcut selectionEditCustomHandle => _selectionEditCustomHandle;
        public PWBKeyShortcut selectionToggleSpace => _selectionToggleSpace;
        public PWBHoldKeysAndMouseMoveShortcut selectionMoveToMousePosition => _selectionMoveToMousePosition;
        #endregion

        #region FLOOR
        [SerializeField]
        private PWBKeyShortcut _floorRotate90YCW = new PWBKeyShortcut("Rotate 90� around Y",
          PWBShortcut.Group.FLOOR, KeyCode.S, EventModifiers.Shift);
        public PWBKeyShortcut floorRotate90YCW => _floorRotate90YCW;
        #endregion

        #region WALL
        [SerializeField]
        private PWBKeyShortcut _wallHalfTurn = new PWBKeyShortcut("Rotate 180� around Y",
          PWBShortcut.Group.WALL, KeyCode.S, EventModifiers.Shift);
        public PWBKeyShortcut wallHalfTurn => _wallHalfTurn;
        #endregion

        #region BLOCK
        [SerializeField]
        private PWBKeyShortcut _blockPreviewRotate90YCW = new PWBKeyShortcut("Rotate preview 90� around Y",
          PWBShortcut.Group.BLOCK, KeyCode.S, EventModifiers.Shift);
        public PWBKeyShortcut blockPreviewRotate90YCW => _blockPreviewRotate90YCW;

        [SerializeField]
        private PWBHoldKeysAndClickShortcut _blockRotate90YCW
           = new PWBHoldKeysAndClickShortcut("Rotate 90� around Y",
         PWBShortcut.Group.BLOCK, KeyCode.A);
        public PWBKeyShortcut blockRotate90YCW => _blockRotate90YCW;

        [SerializeField]
        private PWBHoldKeysAndClickShortcut _blockRotate90XCW
           = new PWBHoldKeysAndClickShortcut("Rotate 90� around X",
         PWBShortcut.Group.BLOCK, KeyCode.G);
        public PWBKeyShortcut blockRotate90XCW => _blockRotate90XCW;

        [SerializeField]
        private PWBHoldKeysAndClickShortcut _blockRotate90ZCW
           = new PWBHoldKeysAndClickShortcut("Rotate 90� around Z",
         PWBShortcut.Group.BLOCK, KeyCode.B);
        public PWBKeyShortcut blockRotate90ZCW => _blockRotate90ZCW;

        [SerializeField]
        private PWBHoldKeysAndClickShortcut _blockRotate90YCCW
           = new PWBHoldKeysAndClickShortcut("Rotate -90� around Y",
         PWBShortcut.Group.BLOCK, KeyCode.A, EventModifiers.Shift);
        public PWBKeyShortcut blockRotate90YCCW => _blockRotate90YCCW;

        [SerializeField]
        private PWBHoldKeysAndClickShortcut _blockRotate90XCCW
           = new PWBHoldKeysAndClickShortcut("Rotate -90� around X",
         PWBShortcut.Group.BLOCK, KeyCode.G, EventModifiers.Shift);
        public PWBKeyShortcut blockRotate90XCCW => _blockRotate90XCCW;

        [SerializeField]
        private PWBHoldKeysAndClickShortcut _blockRotate90ZCCW
           = new PWBHoldKeysAndClickShortcut("Rotate -90� around Z",
         PWBShortcut.Group.BLOCK, KeyCode.B, EventModifiers.Shift);
        public PWBKeyShortcut blockRotate90ZCCW => _blockRotate90ZCCW;
        #endregion
        
        #region TOOL_MODES
        [SerializeField]
        private PWBHoldKeysAndClickShortcut _toolModesMoveSymmetryOriginToMousePos
            = new PWBHoldKeysAndClickShortcut("Move the symmetry origin to mouse position",
          PWBShortcut.Group.TOOL_MODES, KeyCode.S, EventModifiers.Shift);
        public PWBHoldKeysAndClickShortcut toolModesMoveSymmetryOriginToMousePos => _toolModesMoveSymmetryOriginToMousePos;
        #endregion
    }
}
#pragma warning restore UDR0001
