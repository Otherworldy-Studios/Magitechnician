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
        private PWBKeyShortcut[] _keyShortcuts = null;
        public PWBKeyShortcut[] keyShortcuts
        {
            get
            {
                if (_keyShortcuts == null)
                    _keyShortcuts = new PWBKeyShortcut[]
                    {
                        /*/// GIZMOS ///*/
                        _gizmosToggleInfotext,
                        /*/// GRID ///*/
                        _gridEnableShortcuts,
                        _gridToggle,
                        _gridToggleSnapping,
                        _gridToggleLock,
                        _gridSetOriginPosition,
                        _gridSetOriginRotation,
                        _gridSetSize,
                        _gridFrameOrigin,
                        _gridTogglePositionHandle,
                        _gridToggleRotationHandle,
                        _gridToggleSpacingHandle,
                        _gridMoveOriginUp,
                        _gridMoveOriginDown,
                        _gridNextOrigin,
                        _gridMoveOriginToMousePos,
                        /*/// SNAP ///*/
                        _snapToggleBoundsSnapping,
                        /*/// PIN ///*/
                        _pinMoveHandlesUp,
                        _pinMoveHandlesDown,
                        _pinSelectNextHandle,
                        _pinSelectPivotHandle,
                        _pinToggleRepeatItem,
                        _pinResetScale,

                        _pinRotate90YCW,
                        _pinRotate90YCCW,
                        _pinRotateAStepYCW,
                        _pinRotateAStepYCCW,

                        _pinRotate90XCW,
                        _pinRotate90XCCW,
                        _pinRotateAStepXCW,
                        _pinRotateAStepXCCW,

                        _pinRotate90ZCW,
                        _pinRotate90ZCCW,
                        _pinRotateAStepZCW,
                        _pinRotateAStepZCCW,

                        _pinResetRotation,
                        _pinSnapRotationToGrid,

                        _pinAdd1UnitToSurfDist,
                        _pinSubtract1UnitFromSurfDist,
                        _pinAdd01UnitToSurfDist,
                        _pinSubtract01UnitFromSurfDist,

                        _pinResetSurfDist,

                        _pinSelectPreviousItem,
                        _pinSelectNextItem,

                        _pinFlipX,
                        /*/// BRUSH & GRAVITY ///*/
                        _brushUpdatebrushstroke,
                        _brushResetRotation,
                        /*/// GRAVITY ///*/
                        _gravityAdd1UnitToSurfDist,
                        _gravitySubtract1UnitFromSurfDist,
                        _gravityAdd01UnitToSurfDist,
                        _gravitySubtract01UnitFromSurfDist,
                        /*/// EDIT MODE ///*/
                        _editModeDeleteItemAndItsChildren,
                        _editModeDeleteItemButNotItsChildren,
                        _editModeSelectParent,
                        _editModeToggle,
                        _editModeDuplicate,
                        /*/// LINE ///*/
                        _lineSelectAllPoints,
                        _lineDeselectAllPoints,
                        _lineToggleCurve,
                        _lineToggleClosed,
                        _lineEditModeTypeToggle,
                        /*/// TILING & SELECTION ///*/
                        _selectionRotate90XCW,
                        _selectionRotate90XCCW,
                        _selectionRotate90YCW,
                        _selectionRotate90YCCW,
                        _selectionRotate90ZCW,
                        _selectionRotate90ZCCW,
                        /*/// SELECTION ///*/
                        _selectionTogglePositionHandle,
                        _selectionToggleRotationHandle,
                        _selectionToggleScaleHandle,
                        _selectionEditCustomHandle,
                        _selectionToggleSpace,
                        _selectionMoveToMousePosition,
                        /*/// PALETTE ///*/
                        _paletteDeleteBrush,
                        _palettePreviousBrush,
                        _paletteNextBrush,
                        _palettePreviousPalette,
                        _paletteNextPalette,
                        _palettePickBrush,
                        _paletteReplaceSceneSelection,
                        /*/// TOOLBAR ///*/
                        _toolbarPinToggle,
                        _toolbarBrushToggle,
                        _toolbarGravityToggle,
                        _toolbarLineToggle,
                        _toolbarShapeToggle,
                        _toolbarTilingToggle,
                        _toolbarReplacerToggle,
                        _toolbarEraserToggle,
                        _toolbarSelectionToggle,
                        _toolbarExtrudeToggle,
                        _toolbarMirrorToggle,
                        _toolbarBlockToggle,
                        /*/// FLOOR ///*/
                        _floorRotate90YCW,
                        /*/// WALL ///*/
                        _wallHalfTurn,
                        /*/// BLOCK ///*/
                        _blockPreviewRotate90YCW,
                        _blockRotate90XCW,
                        _blockRotate90YCW,
                        _blockRotate90ZCW,
                        _blockRotate90XCCW,
                        _blockRotate90YCCW,
                        _blockRotate90ZCCW,
                        /*/// TOOL_MODES ///*/
                        _toolModesMoveSymmetryOriginToMousePos,
                    };
                return _keyShortcuts;
            }
        }
        private System.Collections.Generic.Dictionary<PWBKeyShortcut, PWBKeyShortcut> _keyConflicts
            = new System.Collections.Generic.Dictionary<PWBKeyShortcut, PWBKeyShortcut>();
        public void UpdateConficts()
        {
            _keyConflicts.Clear();
            foreach (var shortcut in keyShortcuts) shortcut.conflicted = false;
            for (int i = 0; i < keyShortcuts.Length; ++i)
            {
                var shortcut1 = keyShortcuts[i];
                if (shortcut1.conflicted) continue;
                if (shortcut1.combination.keyCode == KeyCode.None) continue;
                for (int j = i + 1; j < keyShortcuts.Length; ++j)
                {
                    var shortcut2 = keyShortcuts[j];
                    if (shortcut2.conflicted) continue;
                    if (shortcut2.combination.keyCode == KeyCode.None) continue;

                    if ((shortcut1.group & shortcut2.group) == 0 && (shortcut1.group & PWBShortcut.Group.GLOBAL) == 0
                       && (shortcut2.group & PWBShortcut.Group.GLOBAL) == 0) continue;
                    if (shortcut1 == gridEnableShortcuts && (shortcut2.group & PWBShortcut.Group.GRID) != 0
                        && (shortcut2.group & PWBShortcut.Group.GLOBAL) == 0)
                        continue;

                    if (shortcut1.combination == shortcut2.combination)
                    {
                        shortcut1.conflicted = true;
                        shortcut2.conflicted = true;
                        var combiString = shortcut1.combination.ToString();
                        if (!_keyConflicts.ContainsKey(shortcut1)) _keyConflicts.Add(shortcut1, shortcut2);
                        if (!_keyConflicts.ContainsKey(shortcut2)) _keyConflicts.Add(shortcut2, shortcut1);
                    }
                }
            }
        }
        public bool GetConflictShortcuts(PWBKeyShortcut shortcut1, out PWBKeyShortcut shortcut2)
        {
            shortcut2 = null;
            if (!_keyConflicts.ContainsKey(shortcut1)) UpdateConficts();
            if (!_keyConflicts.ContainsKey(shortcut1)) return false;
            shortcut2 = _keyConflicts[shortcut1];
            return true;
        }

        public bool CheckConflicts(PWBKeyCombination combi, PWBKeyShortcut target, out string conflicts)
        {
            conflicts = string.Empty;
            foreach (var shortcut in keyShortcuts)
            {
                if (target == shortcut) continue;
                if (target.combination.keyCode == KeyCode.None || shortcut.combination.keyCode == KeyCode.None) continue;
                if (combi == shortcut.combination && ((target.group & shortcut.group) != 0
                    || (shortcut.group & PWBShortcut.Group.GLOBAL) != 0 || (target.group & PWBShortcut.Group.GLOBAL) != 0))
                {
                    if (shortcut == gridEnableShortcuts && (target.group & PWBShortcut.Group.GRID) != 0) continue;
                    if (target == gridEnableShortcuts && (shortcut.group & PWBShortcut.Group.GRID) != 0) continue;
                    if (conflicts != string.Empty) conflicts += "\n";
                    conflicts += shortcut.name;
                }
            }
            return conflicts != string.Empty;
        }
    }
}
#pragma warning restore UDR0001
