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
        private PWBMouseShortcut _pinScale = new PWBMouseShortcut("Edit Scale",
           PWBShortcut.Group.PIN, EventModifiers.Control, PWBMouseCombination.MouseEvents.SCROLL_WHEEL);
        [SerializeField]
        private PWBMouseShortcut _pinSelectNextItemScroll
            = new PWBMouseShortcut("Select prev/next item in the multi-brush",
                PWBShortcut.Group.PIN, EventModifiers.Control | EventModifiers.Alt,
                PWBMouseCombination.MouseEvents.SCROLL_WHEEL);

        [SerializeField]
        private PWBMouseShortcut _pinRotateAroundY = new PWBMouseShortcut("Rotate freely around Y",
           PWBShortcut.Group.PIN, EventModifiers.Control, PWBMouseCombination.MouseEvents.DRAG_R_H);
        [SerializeField]
        private PWBMouseShortcut _pinRotateAroundYSnaped
            = new PWBMouseShortcut("Rotate freely around Y in steps",
                PWBShortcut.Group.PIN, EventModifiers.Control | EventModifiers.Alt, PWBMouseCombination.MouseEvents.DRAG_R_H);

        [SerializeField]
        private PWBMouseShortcut _pinRotateAroundX = new PWBMouseShortcut("Rotate freely around X",
           PWBShortcut.Group.PIN, EventModifiers.Control, PWBMouseCombination.MouseEvents.DRAG_M_V);
        [SerializeField]
        private PWBMouseShortcut _pinRotateAroundXSnaped
            = new PWBMouseShortcut("Rotate freely around X in steps",
                PWBShortcut.Group.PIN, EventModifiers.Control | EventModifiers.Alt, PWBMouseCombination.MouseEvents.DRAG_M_V);

        [SerializeField]
        private PWBMouseShortcut _pinRotateAroundZ = new PWBMouseShortcut("Rotate freely around Z",
           PWBShortcut.Group.PIN, EventModifiers.Control | EventModifiers.Shift, PWBMouseCombination.MouseEvents.DRAG_M_V);
        [SerializeField]
        private PWBMouseShortcut _pinRotateAroundZSnaped
            = new PWBMouseShortcut("Rotate freely around Z in steps", PWBShortcut.Group.PIN,
                EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift, PWBMouseCombination.MouseEvents.DRAG_M_V);

        [SerializeField]
        private PWBMouseShortcut _pinSurfDist = new PWBMouseShortcut("Edit distance to the surface",
           PWBShortcut.Group.PIN,
           EventModifiers.Control | EventModifiers.Shift, PWBMouseCombination.MouseEvents.DRAG_R_V);

        public PWBMouseShortcut pinScale => _pinScale;
        public PWBMouseShortcut pinSelectNextItemScroll => _pinSelectNextItemScroll;

        public PWBMouseShortcut pinRotateAroundY => _pinRotateAroundY;
        public PWBMouseShortcut pinRotateAroundYSnaped => _pinRotateAroundYSnaped;
        public PWBMouseShortcut pinRotateAroundX => _pinRotateAroundX;
        public PWBMouseShortcut pinRotateAroundXSnaped => _pinRotateAroundXSnaped;
        public PWBMouseShortcut pinRotateAroundZ => _pinRotateAroundZ;
        public PWBMouseShortcut pinRotateAroundZSnaped => _pinRotateAroundZSnaped;

        public PWBMouseShortcut pinSurfDist => _pinSurfDist;
        #endregion

        #region RADIUS
        [SerializeField]
        private PWBMouseShortcut _brushRadius = new PWBMouseShortcut("Change radius",
           PWBShortcut.Group.BRUSH | PWBShortcut.Group.GRAVITY
            | PWBShortcut.Group.ERASER | PWBShortcut.Group.REPLACER | PWBShortcut.Group.CIRCLE_SELECT,
           EventModifiers.Control, PWBMouseCombination.MouseEvents.SCROLL_WHEEL);
        public PWBMouseShortcut brushRadius => _brushRadius;
        #endregion

        #region BRUSH & GRAVITY
        [SerializeField]
        private PWBMouseShortcut _brushDensity = new PWBMouseShortcut("Edit density",
           PWBShortcut.Group.BRUSH | PWBShortcut.Group.GRAVITY,
           EventModifiers.Control | EventModifiers.Alt, PWBMouseCombination.MouseEvents.SCROLL_WHEEL);
        [SerializeField]
        private PWBMouseShortcut _brushRotate = new PWBMouseShortcut("Rotate brush",
           PWBShortcut.Group.BRUSH | PWBShortcut.Group.GRAVITY,
           EventModifiers.Control, PWBMouseCombination.MouseEvents.DRAG_R_H);

        public PWBMouseShortcut brushDensity => _brushDensity;
        public PWBMouseShortcut brushRotate => _brushRotate;
        #endregion

        #region GRAVITY
        [SerializeField]
        private PWBMouseShortcut _gravitySurfDist
            = new PWBMouseShortcut("Edit distance to the surface", PWBShortcut.Group.GRAVITY,
           EventModifiers.Control | EventModifiers.Shift, PWBMouseCombination.MouseEvents.DRAG_R_V);
        public PWBMouseShortcut gravitySurfDist => _gravitySurfDist;
        #endregion

        #region LINE & SHAPE
        [SerializeField]
        private PWBMouseShortcut _lineEditGap
            = new PWBMouseShortcut("Edit gap size", PWBShortcut.Group.LINE | PWBShortcut.Group.SHAPE,
           EventModifiers.Control | EventModifiers.Shift, PWBMouseCombination.MouseEvents.DRAG_R_H);
        public PWBMouseShortcut lineEditGap => _lineEditGap;
        #endregion

        #region TILING
        [SerializeField]
        private PWBMouseShortcut _tilingEditSpacing1 = new PWBMouseShortcut("Edit spacing on axis 1", PWBShortcut.Group.TILING,
           EventModifiers.Control, PWBMouseCombination.MouseEvents.DRAG_R_H);
        [SerializeField]
        private PWBMouseShortcut _tilingEditSpacing2 = new PWBMouseShortcut("Edit spacing on axis 2", PWBShortcut.Group.TILING,
           EventModifiers.Control | EventModifiers.Shift, PWBMouseCombination.MouseEvents.DRAG_R_H);
        public PWBMouseShortcut tilingEditSpacing1 => _tilingEditSpacing1;
        public PWBMouseShortcut tilingEditSpacing2 => _tilingEditSpacing2;
        #endregion

        #region PALETTE
        [SerializeField]
        private PWBMouseShortcut _paletteNextBrushScroll = new PWBMouseShortcut("Select prev/next brush",
            PWBShortcut.Group.PALETTE | PWBShortcut.Group.GLOBAL,
           EventModifiers.Control | EventModifiers.Shift, PWBMouseCombination.MouseEvents.SCROLL_WHEEL);
        [SerializeField]
        private PWBMouseShortcut _paletteNextPaletteScroll = new PWBMouseShortcut("Select prev/next palette",
            PWBShortcut.Group.PALETTE | PWBShortcut.Group.GLOBAL,
           EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift, PWBMouseCombination.MouseEvents.SCROLL_WHEEL);
        public PWBMouseShortcut paletteNextBrushScroll => _paletteNextBrushScroll;
        public PWBMouseShortcut paletteNextPaletteScroll => _paletteNextPaletteScroll;
        #endregion

        #region CONFLICTS
        private PWBMouseShortcut[] _mouseShortcuts = null;
        public PWBMouseShortcut[] mouseShortcuts
        {
            get
            {
                if (_mouseShortcuts == null)
                    _mouseShortcuts = new PWBMouseShortcut[]
                    {
                        /*/// PIN ///*/
                        _pinScale,
                        _pinSelectNextItemScroll,

                        _pinRotateAroundY,
                        _pinRotateAroundYSnaped,
                        _pinRotateAroundX,
                        _pinRotateAroundXSnaped,
                        _pinRotateAroundZ,
                        _pinRotateAroundZSnaped,

                        _pinSurfDist,
                        /*/// RADIUS ///*/
                        _brushRadius,
                        /*/// BRUSH & GRAVITY ///*/
                        _brushDensity,
                        _brushRotate,
                        /*/// BRUSH & GRAVITY ///*/
                        _gravitySurfDist,
                        /*/// LINE & SHAPE ///*/
                        _lineEditGap,
                        /*/// LINE ///*/
                        
                        /*/// TILING ///*/
                        _tilingEditSpacing1,
                        _tilingEditSpacing2,
                        /*/// PALETTE ///*/
                        _paletteNextBrushScroll,
                        _paletteNextPaletteScroll

                    };
                return _mouseShortcuts;
            }
        }

        public void UpdateMouseConficts()
        {
            foreach (var scrollShortcut in mouseShortcuts) scrollShortcut.conflicted = false;
            for (int i = 0; i < mouseShortcuts.Length; ++i)
            {
                var shortcut1 = mouseShortcuts[i];
                if (shortcut1.conflicted) continue;
                if (shortcut1.combination.modifiers == EventModifiers.None) continue;
                for (int j = i + 1; j < mouseShortcuts.Length; ++j)
                {
                    var shortcut2 = mouseShortcuts[j];
                    if (shortcut2.conflicted) continue;
                    if (shortcut2.combination.modifiers == EventModifiers.None) continue;
                    if ((shortcut1.group & shortcut2.group) == 0 && (shortcut1.group & PWBShortcut.Group.GLOBAL) == 0
                        && (shortcut1.group & PWBShortcut.Group.GLOBAL) == 0) continue;
                    if (shortcut1.combination == shortcut2.combination)
                    {
                        shortcut1.conflicted = true;
                        shortcut2.conflicted = true;
                    }
                }
            }
        }

        public bool CheckMouseConflicts(PWBMouseCombination combi, PWBMouseShortcut target, out string conflicts)
        {
            conflicts = string.Empty;
            foreach (var shortcut in mouseShortcuts)
            {
                if (target == shortcut) continue;
                if (target.combination.modifiers == EventModifiers.None
                    || shortcut.combination.modifiers == EventModifiers.None) continue;
                if (combi == shortcut.combination && ((target.group & shortcut.group) != 0
                    || (shortcut.group & PWBShortcut.Group.GLOBAL) != 0 || (target.group & PWBShortcut.Group.GLOBAL) != 0))
                {
                    if (conflicts != string.Empty) conflicts += "\n";
                    conflicts += shortcut.name;
                }
            }
            return conflicts != string.Empty;
        }

        public bool CombinationExist(PWBMouseCombination combi, PWBShortcut.Group group)
        {
            foreach (var shortcut in mouseShortcuts)
            {
                if (combi == shortcut.combination && ((group & shortcut.group) != 0
                    || (shortcut.group & PWBShortcut.Group.GLOBAL) != 0 || (group & PWBShortcut.Group.GLOBAL) != 0))
                    return true;
            }
            return false;
        }
        #endregion
    }
}
#pragma warning restore UDR0001
