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
    [System.Serializable]
    public partial class PWBShortcuts
    {
        #region PROFILE
        [SerializeField] private string _profileName = string.Empty;
        public string profileName { get => _profileName; set => _profileName = value; }
        public PWBShortcuts(string name) => _profileName = name;
        public void Copy(PWBShortcuts other)
        {
            //keyShortcuts
            /*/// GIZMOS ///*/
            _gizmosToggleInfotext.Copy(other._gizmosToggleInfotext);
            /*/// GRID ///*/
            _gridEnableShortcuts.Copy(other._gridEnableShortcuts);
            _gridToggle.Copy(other._gridToggle);
            _gridToggleSnapping.Copy(other._gridToggleSnapping);
            _gridToggleLock.Copy(other._gridToggleLock);
            _gridSetOriginPosition.Copy(other._gridSetOriginPosition);
            _gridSetOriginRotation.Copy(other._gridSetOriginRotation);
            _gridSetSize.Copy(other._gridSetSize);
            _gridFrameOrigin.Copy(other._gridFrameOrigin);
            _gridTogglePositionHandle.Copy(other._gridTogglePositionHandle);
            _gridToggleRotationHandle.Copy(other._gridToggleRotationHandle);
            _gridToggleSpacingHandle.Copy(other._gridToggleSpacingHandle);
            _gridMoveOriginUp.Copy(other._gridMoveOriginUp);
            _gridMoveOriginDown.Copy(other._gridMoveOriginDown);
            _gridNextOrigin.Copy(other._gridNextOrigin);
            /*/// SNAP ///*/
            _snapToggleBoundsSnapping.Copy(other._snapToggleBoundsSnapping);
            /*/// PIN ///*/
            _pinMoveHandlesUp.Copy(other._pinMoveHandlesUp);
            _pinMoveHandlesDown.Copy(other._pinMoveHandlesDown);
            _pinSelectNextHandle.Copy(other._pinSelectNextHandle);
            _pinSelectPivotHandle.Copy(other._pinSelectPivotHandle);
            _pinToggleRepeatItem.Copy(other._pinToggleRepeatItem);
            _pinResetScale = other._pinResetScale;

            _pinRotate90YCW.Copy(other._pinRotate90YCW);
            _pinRotate90YCCW.Copy(other._pinRotate90YCCW);
            _pinRotateAStepYCW.Copy(other._pinRotateAStepYCW);
            _pinRotateAStepYCCW.Copy(other._pinRotateAStepYCCW);

            _pinRotate90XCW.Copy(other._pinRotate90XCW);
            _pinRotate90XCCW.Copy(other._pinRotate90XCCW);
            _pinRotateAStepXCW.Copy(other._pinRotateAStepXCW);
            _pinRotateAStepXCCW.Copy(other._pinRotateAStepXCCW);

            _pinRotate90ZCW.Copy(other._pinRotate90ZCW);
            _pinRotate90ZCCW.Copy(other._pinRotate90ZCCW);
            _pinRotateAStepZCW.Copy(other._pinRotateAStepZCW);
            _pinRotateAStepZCCW.Copy(other._pinRotateAStepZCCW);

            _pinResetRotation.Copy(other._pinResetRotation);
            _pinSnapRotationToGrid.Copy(other._pinSnapRotationToGrid);

            _pinAdd1UnitToSurfDist.Copy(other._pinAdd1UnitToSurfDist);
            _pinSubtract1UnitFromSurfDist.Copy(other._pinSubtract1UnitFromSurfDist);
            _pinAdd01UnitToSurfDist.Copy(other._pinAdd01UnitToSurfDist);
            _pinSubtract01UnitFromSurfDist.Copy(other._pinSubtract01UnitFromSurfDist);

            _pinResetSurfDist.Copy(other._pinResetSurfDist);

            _pinSelectPreviousItem.Copy(other._pinSelectPreviousItem);
            _pinSelectNextItem.Copy(other._pinSelectNextItem);

            _pinFlipX.Copy(other._pinFlipX);
            /*/// BRUSH & GRAVITY ///*/
            _brushUpdatebrushstroke.Copy(other._brushUpdatebrushstroke);
            _brushResetRotation.Copy(other._brushResetRotation);
            /*/// GRAVITY ///*/
            _gravityAdd1UnitToSurfDist.Copy(other._gravityAdd1UnitToSurfDist);
            _gravitySubtract1UnitFromSurfDist.Copy(other._gravitySubtract1UnitFromSurfDist);
            _gravityAdd01UnitToSurfDist.Copy(other._gravityAdd01UnitToSurfDist);
            _gravitySubtract01UnitFromSurfDist.Copy(other._gravitySubtract01UnitFromSurfDist);
            /*/// EDIT MODE ///*/
            _editModeDeleteItemAndItsChildren.Copy(other._editModeDeleteItemAndItsChildren);
            _editModeDeleteItemButNotItsChildren.Copy(other._editModeDeleteItemButNotItsChildren);
            _editModeSelectParent.Copy(other._editModeSelectParent);
            _editModeToggle.Copy(other._editModeToggle);
            _editModeDuplicate.Copy(other._editModeDuplicate);
            /*/// LINE ///*/
            _lineSelectAllPoints.Copy(other._lineSelectAllPoints);
            _lineDeselectAllPoints.Copy(other._lineDeselectAllPoints);
            _lineToggleCurve.Copy(other._lineToggleCurve);
            _lineToggleClosed.Copy(other._lineToggleClosed);
            _lineEditModeTypeToggle.Copy(other._lineEditModeTypeToggle);
            /*/// TILING & SELECTION ///*/
            _selectionRotate90XCW.Copy(other._selectionRotate90XCW);
            _selectionRotate90XCCW.Copy(other._selectionRotate90XCCW);
            _selectionRotate90YCW.Copy(other._selectionRotate90YCW);
            _selectionRotate90YCCW.Copy(other._selectionRotate90YCCW);
            _selectionRotate90ZCW.Copy(other._selectionRotate90ZCW);
            _selectionRotate90ZCCW.Copy(other._selectionRotate90ZCCW);
            /*/// SELECTION ///*/
            _selectionTogglePositionHandle.Copy(other._selectionTogglePositionHandle);
            _selectionToggleRotationHandle.Copy(other._selectionToggleRotationHandle);
            _selectionToggleScaleHandle.Copy(other._selectionToggleScaleHandle);
            _selectionEditCustomHandle.Copy(other._selectionEditCustomHandle);
            _selectionToggleSpace.Copy(other._selectionToggleSpace);
            /*/// PALETTE ///*/
            _paletteDeleteBrush.Copy(other._paletteDeleteBrush);
            _palettePreviousBrush.Copy(other._palettePreviousBrush);
            _paletteNextBrush.Copy(other._paletteNextBrush);
            _palettePreviousPalette.Copy(other._palettePreviousPalette);
            _paletteNextPalette.Copy(other._paletteNextPalette);
            _palettePickBrush.Copy(other._palettePickBrush);
            _paletteReplaceSceneSelection.Copy(other._paletteReplaceSceneSelection);
            /*/// TOOLBAR ///*/
            _toolbarPinToggle.Copy(other._toolbarPinToggle);
            _toolbarBrushToggle.Copy(other._toolbarBrushToggle);
            _toolbarGravityToggle.Copy(other._toolbarGravityToggle);
            _toolbarLineToggle.Copy(other._toolbarLineToggle);
            _toolbarShapeToggle.Copy(other._toolbarShapeToggle);
            _toolbarTilingToggle.Copy(other._toolbarTilingToggle);
            _toolbarReplacerToggle.Copy(other._toolbarReplacerToggle);
            _toolbarEraserToggle.Copy(other._toolbarEraserToggle);
            _toolbarSelectionToggle.Copy(other._toolbarSelectionToggle);
            _toolbarExtrudeToggle.Copy(other._toolbarExtrudeToggle);
            _toolbarMirrorToggle.Copy(other._toolbarMirrorToggle);
            /*/// FLOOR ///*/
            _floorRotate90YCW.Copy(other._floorRotate90YCW);
            /*/// WALL ///*/
            _wallHalfTurn.Copy(other._wallHalfTurn);
            /*/// BLOCK ///*/
            _blockPreviewRotate90YCW.Copy(other._blockPreviewRotate90YCW);
            _blockRotate90XCW.Copy(other._blockRotate90XCW);
            _blockRotate90YCW.Copy(other._blockRotate90YCW);
            _blockRotate90ZCW.Copy(other._blockRotate90ZCW);
            _blockRotate90XCCW.Copy(other._blockRotate90XCCW);
            _blockRotate90YCCW.Copy(other._blockRotate90YCCW);
            _blockRotate90ZCCW.Copy(other._blockRotate90ZCCW);
            _toolModesMoveSymmetryOriginToMousePos.Copy(other._toolModesMoveSymmetryOriginToMousePos);

            //Mouse shortcuts
            /*/// PIN ///*/
            _pinScale.Copy(other._pinScale);
            _pinSelectNextItemScroll.Copy(other._pinSelectNextItemScroll);

            _pinRotateAroundY.Copy(other._pinRotateAroundY);
            _pinRotateAroundYSnaped.Copy(other._pinRotateAroundYSnaped);
            _pinRotateAroundX.Copy(other._pinRotateAroundX);
            _pinRotateAroundXSnaped.Copy(other._pinRotateAroundXSnaped);
            _pinRotateAroundZ.Copy(other._pinRotateAroundZ);
            _pinRotateAroundZSnaped.Copy(other._pinRotateAroundZSnaped);

            _pinSurfDist.Copy(other._pinSurfDist);
            /*/// RADIUS ///*/
            _brushRadius.Copy(other._brushRadius);
            /*/// BRUSH & GRAVITY ///*/
            _brushDensity.Copy(other._brushDensity);
            _brushRotate.Copy(other._brushRotate);
            /*/// BRUSH & GRAVITY ///*/
            _gravitySurfDist.Copy(other._gravitySurfDist);
            /*/// LINE & SHAPE ///*/
            _lineEditGap.Copy(other._lineEditGap);
            /*/// LINE ///*/

            /*/// TILING ///*/
            _tilingEditSpacing1.Copy(other._tilingEditSpacing1);
            _tilingEditSpacing2.Copy(other._tilingEditSpacing2);
            /*/// PALETTE ///*/
            _paletteNextBrushScroll.Copy(other._paletteNextBrushScroll);
            _paletteNextPaletteScroll.Copy(other._paletteNextPaletteScroll);
        }

        public static PWBShortcuts GetDefault(int i)
        {
            if (i == 0) return new PWBShortcuts("Default 1");
            else if (i == 1)
            {
                var d2 = new PWBShortcuts("Default 2");
                d2.pinMoveHandlesUp.combination.Set(KeyCode.PageUp);
                d2.pinMoveHandlesDown.combination.Set(KeyCode.PageDown);
                d2.pinSelectPivotHandle.combination.Set(KeyCode.Home);
                d2.pinSelectNextHandle.combination.Set(KeyCode.End);
                d2.pinResetScale.combination.Set(KeyCode.Home, EventModifiers.Control | EventModifiers.Shift);

                d2.pinRotate90YCW.combination.Set(KeyCode.LeftArrow, EventModifiers.Control);
                d2._pinRotate90YCCW.combination.Set(KeyCode.RightArrow, EventModifiers.Control);
                d2.pinRotateAStepYCW.combination.Set(KeyCode.LeftArrow,
                    EventModifiers.Control | EventModifiers.Shift);
                d2.pinRotateAStepYCCW.combination.Set(KeyCode.RightArrow,
                    EventModifiers.Control | EventModifiers.Shift);

                d2.pinRotate90XCW.combination.Set(KeyCode.LeftArrow,
                    EventModifiers.Control | EventModifiers.Alt);
                d2.pinRotate90XCCW.combination.Set(KeyCode.RightArrow,
                    EventModifiers.Control | EventModifiers.Alt);
                d2.pinRotateAStepXCW.combination.Set(KeyCode.LeftArrow,
                    EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift);
                d2.pinRotateAStepXCCW.combination.Set(KeyCode.RightArrow,
                    EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift);

                d2.pinResetRotation.combination.Set(KeyCode.Home, EventModifiers.Control);

                d2.pinAdd1UnitToSurfDist.combination.Set(KeyCode.UpArrow,
                    EventModifiers.Control | EventModifiers.Alt);
                d2.pinSubtract1UnitFromSurfDist.combination.Set(KeyCode.DownArrow,
                    EventModifiers.Control | EventModifiers.Alt);
                d2.pinAdd01UnitToSurfDist.combination.Set(KeyCode.UpArrow,
                    EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift);
                d2.pinSubtract01UnitFromSurfDist.combination.Set(KeyCode.DownArrow,
                    EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift);

                d2.lineToggleCurve.combination.Set(KeyCode.PageDown);
                d2.lineToggleClosed.combination.Set(KeyCode.End);

                d2.selectionRotate90XCW.combination.Set(KeyCode.PageUp,
                    EventModifiers.Control | EventModifiers.Shift);
                d2.selectionRotate90XCCW.combination.Set(KeyCode.PageDown,
                    EventModifiers.Control | EventModifiers.Shift);
                d2.selectionRotate90YCW.combination.Set(KeyCode.LeftArrow,
                    EventModifiers.Control | EventModifiers.Alt);
                d2.selectionRotate90YCCW.combination.Set(KeyCode.RightArrow,
                    EventModifiers.Control | EventModifiers.Alt);
                d2.selectionRotate90ZCW.combination.Set(KeyCode.UpArrow,
                   EventModifiers.Control | EventModifiers.Alt);
                d2.selectionRotate90ZCCW.combination.Set(KeyCode.DownArrow,
                    EventModifiers.Control | EventModifiers.Alt);

                d2.brushRadius.combination.Set(EventModifiers.Shift, PWBMouseCombination.MouseEvents.DRAG_R_H);
                return d2;
            }
            else if (i == 2)
            {
                var d0 = new PWBShortcuts("Default 0");
                //GRID
                d0.gridEnableShortcuts.combination.Set(KeyCode.None);
                d0.gridToggle.firstStepEnabled = false;
                d0.gridToggleSnaping.firstStepEnabled = false;
                d0.gridToggleLock.firstStepEnabled = false;
                d0.gridSetOriginPosition.firstStepEnabled = false;
                d0.gridSetOriginRotation.firstStepEnabled = false;
                d0.gridSetSize.firstStepEnabled = false;
                d0.gridFrameOrigin.firstStepEnabled = false;
                d0.gridTogglePositionHandle.firstStepEnabled = false;
                d0.gridToggleRotationHandle.firstStepEnabled = false;
                d0.gridToggleSpacingHandle.firstStepEnabled = false;
                d0.gridMoveOriginUp.firstStepEnabled = false;
                d0.gridMoveOriginDown.firstStepEnabled = false;
                d0.gridNextOrigin.firstStepEnabled = false;

                //PIN
                d0.pinMoveHandlesUp.combination.Set(KeyCode.J);
                d0.pinMoveHandlesDown.combination.Set(KeyCode.J, EventModifiers.Control | EventModifiers.Shift);
                d0.pinSelectPrevHandle.combination.Set(KeyCode.I);
                d0.pinSelectNextHandle.combination.Set(KeyCode.O);
                d0.pinSelectPivotHandle.combination.Set(KeyCode.L);

                d0.pinToggleRepeatItem.combination.Set(KeyCode.Alpha0);

                d0.pinResetScale.combination.Set(KeyCode.Period);

                d0.pinRotate90YCW.combination.Set(KeyCode.U);
                d0.pinRotate90YCCW.combination.Set(KeyCode.U, EventModifiers.Control | EventModifiers.Alt);
                d0.pinRotateAStepYCW.combination.Set(KeyCode.U, EventModifiers.Shift);
                d0.pinRotateAStepYCCW.combination.Set(KeyCode.U,
                    EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift);

                d0.pinRotate90XCW.combination.Set(KeyCode.Q);
                d0.pinRotate90XCCW.combination.Set(KeyCode.Q, EventModifiers.Control | EventModifiers.Alt);
                d0.pinRotateAStepXCW.combination.Set(KeyCode.Q, EventModifiers.Shift);
                d0.pinRotateAStepXCCW.combination.Set(KeyCode.Q,
                    EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift);

                d0.pinRotate90ZCW.combination.Set(KeyCode.B);
                d0.pinRotate90ZCCW.combination.Set(KeyCode.B, EventModifiers.Control | EventModifiers.Alt);
                d0.pinRotateAStepZCW.combination.Set(KeyCode.B, EventModifiers.Shift);
                d0.pinRotateAStepZCCW.combination.Set(KeyCode.B,
                    EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift);

                d0.pinResetRotation.combination.Set(KeyCode.Comma);

                d0.pinSnapRotationToGrid.combination.Set(KeyCode.G);

                d0.pinAdd1UnitToSurfDist.combination.Set(KeyCode.H);
                d0.pinSubtract1UnitFromSurfDist.combination.Set(KeyCode.N);
                d0.pinAdd01UnitToSurfDist.combination.Set(KeyCode.H, EventModifiers.Shift);
                d0.pinSubtract01UnitFromSurfDist.combination.Set(KeyCode.N, EventModifiers.Shift);

                d0.pinResetSurfDist.combination.Set(KeyCode.M);

                d0.pinSelectPreviousItem.combination.Set(KeyCode.A);
                d0.pinSelectNextItem.combination.Set(KeyCode.D);

                d0.pinFlipX.combination.Set(KeyCode.Minus);
                //BRUSH & GRAVITY
                d0.brushUpdatebrushstroke.combination.Set(KeyCode.U);
                d0.brushResetRotation.combination.Set(KeyCode.Comma);

                //GRAVITY
                d0.gravityAdd1UnitToSurfDist.combination.Set(KeyCode.H);
                d0.gravitySubtract1UnitFromSurfDist.combination.Set(KeyCode.N);
                d0.gravityAdd01UnitToSurfDist.combination.Set(KeyCode.H, EventModifiers.Shift);
                d0.gravitySubtract01UnitFromSurfDist.combination.Set(KeyCode.N, EventModifiers.Shift);

                //EDIT MODE
                d0.editModeSelectParent.combination.Set(KeyCode.T);
                d0.editModeToggle.combination.Set(KeyCode.Period);
                d0.editModeDuplicate.combination.Set(KeyCode.D);

                //LINE
                d0.lineSelectAllPoints.combination.Set(KeyCode.A);
                d0.lineDeselectAllPoints.combination.Set(KeyCode.S);
                d0.lineToggleCurve.combination.Set(KeyCode.U);
                d0.lineToggleClosed.combination.Set(KeyCode.O);
                d0.lineEditModeTypeToggle.combination.Set(KeyCode.Comma);

                //TILING & SELECTION
                d0.selectionRotate90YCW.combination.Set(KeyCode.S);
                d0.selectionRotate90YCCW.combination.Set(KeyCode.S, EventModifiers.Control | EventModifiers.Alt);
                d0.selectionRotate90XCW.combination.Set(KeyCode.C);
                d0.selectionRotate90XCCW.combination.Set(KeyCode.C, EventModifiers.Control | EventModifiers.Alt);
                d0.selectionRotate90ZCW.combination.Set(KeyCode.B);
                d0.selectionRotate90ZCCW.combination.Set(KeyCode.B, EventModifiers.Control | EventModifiers.Alt);

                //SELECTION
                d0.selectionToggleSpace.combination.Set(KeyCode.A);

                //FLOOR
                d0.floorRotate90YCW.combination.Set(KeyCode.S);

                //WALL
                d0.wallHalfTurn.combination.Set(KeyCode.S);

                //BLOCK
                d0.blockPreviewRotate90YCW.combination.Set(KeyCode.S);
                d0.blockRotate90XCW.combination.Set(KeyCode.G);
                d0.blockRotate90YCW.combination.Set(KeyCode.A);
                d0.blockRotate90ZCW.combination.Set(KeyCode.B);
                d0.blockRotate90XCCW.combination.Set(KeyCode.G, EventModifiers.Shift);
                d0.blockRotate90YCCW.combination.Set(KeyCode.A, EventModifiers.Shift);
                d0.blockRotate90ZCCW.combination.Set(KeyCode.B, EventModifiers.Shift);

                return d0;
            }
            return null;
        }
        #endregion

        #region LIST
        public static (string Command, string Shortcut)[] GetShortcuts(PWBShortcut.Group group, EventModifiers modifiers)
        {
            var result = new System.Collections.Generic.List<(string Command, string Shortcut)>();
            if (group == PWBShortcut.Group.NONE) return new (string Command, string Shortcut)[] { };
            var defaultProfile = PWBSettings.shortcuts;
            var keyShortcutsArray = new System.Collections.Generic.List<PWBKeyShortcut>(defaultProfile.keyShortcuts);
            foreach (var shortcut in keyShortcutsArray)
            {
                if ((shortcut.group & group) != group) continue;
                if (shortcut.combination.modifiers == modifiers) result.Add((shortcut.name, shortcut.combination.ToString()));
            }
            var mouseShortcutsArray = new System.Collections.Generic.List<PWBMouseShortcut>(defaultProfile.mouseShortcuts);
            foreach (var shortcut in mouseShortcutsArray)
            {
                if ((shortcut.group & group) != group) continue;
                if (shortcut.combination.modifiers == modifiers) result.Add((shortcut.name, shortcut.combination.ToString()));
            }
            return result.ToArray();
        }

        public static (string Command, string Shortcut)[] GetAllShortcuts(PWBShortcut.Group group, PWBShortcut.Group exclude)
        {
            var result = new System.Collections.Generic.List<(string Command, string Shortcut)>();
            if (group == PWBShortcut.Group.NONE) return new (string Command, string Shortcut)[] { };
            var defaultProfile = PWBSettings.shortcuts;
            var keyShortcutsArray = new System.Collections.Generic.List<PWBKeyShortcut>(defaultProfile.keyShortcuts);
            foreach (var shortcut in keyShortcutsArray)
            {
                if ((shortcut.group & group) != group) continue;
                if (exclude != PWBShortcut.Group.NONE && (shortcut.group & exclude) == exclude) continue;
                result.Add((shortcut.name, shortcut.combination.ToString()));
            }
            var mouseShortcutsArray = new System.Collections.Generic.List<PWBMouseShortcut>(defaultProfile.mouseShortcuts);
            foreach (var shortcut in mouseShortcutsArray)
            {
                if ((shortcut.group & group) != group) continue;
                if (exclude != PWBShortcut.Group.NONE && (shortcut.group & exclude) == exclude) continue;
                result.Add((shortcut.name, shortcut.combination.ToString()));
            }
            return result.ToArray();
        }
        #endregion
    }
}
#pragma warning restore UDR0001
