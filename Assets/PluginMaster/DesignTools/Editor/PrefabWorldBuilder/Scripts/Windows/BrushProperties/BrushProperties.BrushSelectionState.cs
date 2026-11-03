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
        private enum SelectionFieldState { SAME, MIXED, CHANGED }
        private class BrushSelectionState
        {
            public SelectionFieldState surfaceDistance = SelectionFieldState.SAME;
            public SelectionFieldState randomSurfaceDistance = SelectionFieldState.SAME;
            public SelectionFieldState randomSurfaceDistanceRange = SelectionFieldState.SAME;
            public SelectionFieldState embedInSurface = SelectionFieldState.SAME;
            public SelectionFieldState embedAtPivotHeight = SelectionFieldState.SAME;
            public SelectionFieldState localPositionOffset = SelectionFieldState.SAME;
            public SelectionFieldState rotateToTheSurface = SelectionFieldState.SAME;
            public SelectionFieldState eulerOffset = SelectionFieldState.SAME;
            public SelectionFieldState addRandomRotation = SelectionFieldState.SAME;
            public SelectionFieldState randomEulerOffset = SelectionFieldState.SAME;
            public SelectionFieldState alwaysOrientUp = SelectionFieldState.SAME;
            public SelectionFieldState separateScaleAxes = SelectionFieldState.SAME;
            public SelectionFieldState scaleMultiplier = SelectionFieldState.SAME;
            public SelectionFieldState randomScaleMultiplier = SelectionFieldState.SAME;
            public SelectionFieldState randomScaleMultiplierRange = SelectionFieldState.SAME;
            public SelectionFieldState flipX = SelectionFieldState.SAME;
            public SelectionFieldState flipY = SelectionFieldState.SAME;
            public virtual bool changed
                => surfaceDistance == SelectionFieldState.CHANGED
                || randomSurfaceDistance == SelectionFieldState.CHANGED
                || randomSurfaceDistanceRange == SelectionFieldState.CHANGED
                || embedInSurface == SelectionFieldState.CHANGED
                || embedAtPivotHeight == SelectionFieldState.CHANGED
                || localPositionOffset == SelectionFieldState.CHANGED
                || rotateToTheSurface == SelectionFieldState.CHANGED
                || eulerOffset == SelectionFieldState.CHANGED
                || addRandomRotation == SelectionFieldState.CHANGED
                || randomEulerOffset == SelectionFieldState.CHANGED
                || alwaysOrientUp == SelectionFieldState.CHANGED
                || separateScaleAxes == SelectionFieldState.CHANGED
                || scaleMultiplier == SelectionFieldState.CHANGED
                || randomScaleMultiplier == SelectionFieldState.CHANGED
                || randomScaleMultiplierRange == SelectionFieldState.CHANGED
                || flipX == SelectionFieldState.CHANGED
                || flipY == SelectionFieldState.CHANGED;
            public virtual void Reset()
            {
                surfaceDistance = SelectionFieldState.SAME;
                randomSurfaceDistance = SelectionFieldState.SAME;
                randomSurfaceDistanceRange = SelectionFieldState.SAME;
                embedInSurface = SelectionFieldState.SAME;
                embedAtPivotHeight = SelectionFieldState.SAME;
                localPositionOffset = SelectionFieldState.SAME;
                rotateToTheSurface = SelectionFieldState.SAME;
                eulerOffset = SelectionFieldState.SAME;
                addRandomRotation = SelectionFieldState.SAME;
                randomEulerOffset = SelectionFieldState.SAME;
                alwaysOrientUp = SelectionFieldState.SAME;
                separateScaleAxes = SelectionFieldState.SAME;
                scaleMultiplier = SelectionFieldState.SAME;
                randomScaleMultiplier = SelectionFieldState.SAME;
                randomScaleMultiplierRange = SelectionFieldState.SAME;
                flipX = SelectionFieldState.SAME;
                flipY = SelectionFieldState.SAME;
            }
        }

        private void UpdateSelectionState(BrushSettings[] settingsArray,
            int[] selection, BrushSelectionState brushSelectionState)
        {
            for (int i = 0; i < selection.Length - 1; ++i)
            {
                var brush = settingsArray[selection[i]];
                var nextBrush = settingsArray[selection[i + 1]];
                if (brushSelectionState.embedInSurface != SelectionFieldState.CHANGED
                    && brush.embedInSurface != nextBrush.embedInSurface)
                    brushSelectionState.embedInSurface = SelectionFieldState.MIXED;
                if (brushSelectionState.embedAtPivotHeight != SelectionFieldState.CHANGED
                    && brush.embedAtPivotHeight != nextBrush.embedAtPivotHeight)
                    brushSelectionState.embedInSurface = SelectionFieldState.MIXED;
                if (brushSelectionState.surfaceDistance != SelectionFieldState.CHANGED
                    && brush.surfaceDistance != nextBrush.surfaceDistance)
                    brushSelectionState.surfaceDistance = SelectionFieldState.MIXED;
                if (brushSelectionState.randomSurfaceDistance != SelectionFieldState.CHANGED
                    && brush.randomSurfaceDistance != nextBrush.randomSurfaceDistance)
                    brushSelectionState.randomSurfaceDistance = SelectionFieldState.MIXED;
                if (brushSelectionState.randomSurfaceDistanceRange != SelectionFieldState.CHANGED
                    && brush.randomSurfaceDistanceRange != nextBrush.randomSurfaceDistanceRange)
                    brushSelectionState.randomSurfaceDistanceRange = SelectionFieldState.MIXED;
                if (brushSelectionState.localPositionOffset != SelectionFieldState.CHANGED
                    && brush.localPositionOffset != nextBrush.localPositionOffset)
                    brushSelectionState.localPositionOffset = SelectionFieldState.MIXED;
                if (brushSelectionState.rotateToTheSurface != SelectionFieldState.CHANGED
                    && brush.rotateToTheSurface != nextBrush.rotateToTheSurface)
                    brushSelectionState.rotateToTheSurface = SelectionFieldState.MIXED;
                if (brushSelectionState.addRandomRotation != SelectionFieldState.CHANGED
                    && brush.addRandomRotation != nextBrush.addRandomRotation)
                    brushSelectionState.addRandomRotation = SelectionFieldState.MIXED;
                if (brushSelectionState.eulerOffset != SelectionFieldState.CHANGED
                    && brush.eulerOffset != nextBrush.eulerOffset)
                    brushSelectionState.eulerOffset = SelectionFieldState.MIXED;
                if (brushSelectionState.randomEulerOffset != SelectionFieldState.CHANGED
                    && brush.randomEulerOffset != nextBrush.randomEulerOffset)
                    brushSelectionState.randomEulerOffset = SelectionFieldState.MIXED;
                if (brushSelectionState.randomScaleMultiplier != SelectionFieldState.CHANGED
                    && brush.randomScaleMultiplier != nextBrush.randomScaleMultiplier)
                    brushSelectionState.randomScaleMultiplier = SelectionFieldState.MIXED;
                if (brushSelectionState.alwaysOrientUp != SelectionFieldState.CHANGED
                    && brush.alwaysOrientUp != nextBrush.alwaysOrientUp)
                    brushSelectionState.alwaysOrientUp = SelectionFieldState.MIXED;
                if (brushSelectionState.separateScaleAxes != SelectionFieldState.CHANGED
                    && brush.separateScaleAxes != nextBrush.separateScaleAxes)
                    brushSelectionState.separateScaleAxes = SelectionFieldState.MIXED;
                if (brushSelectionState.scaleMultiplier != SelectionFieldState.CHANGED
                    && brush.scaleMultiplier != nextBrush.scaleMultiplier)
                    brushSelectionState.scaleMultiplier = SelectionFieldState.MIXED;
                if (brushSelectionState.randomScaleMultiplierRange != SelectionFieldState.CHANGED
                    && brush.randomScaleMultiplierRange != nextBrush.randomScaleMultiplierRange)
                    brushSelectionState.randomScaleMultiplierRange = SelectionFieldState.MIXED;
                if (brushSelectionState.flipX != SelectionFieldState.CHANGED
                   && brush.flipX != nextBrush.flipX)
                    brushSelectionState.flipX = SelectionFieldState.MIXED;
                if (brushSelectionState.flipY != SelectionFieldState.CHANGED
                   && brush.flipY != nextBrush.flipY)
                    brushSelectionState.flipY = SelectionFieldState.MIXED;
            }
        }

        private GUIContent _sameStateIcon = null;
        private GUIContent _mixedStateIcon = null;
        private GUIContent _changedStateIcon = null;
        private GUIContent GetStateGUIContent(SelectionFieldState state)
                => state == SelectionFieldState.SAME ? _sameStateIcon : state == SelectionFieldState.MIXED
                ? _mixedStateIcon : _changedStateIcon;
        private void UpdateBrushSelectionSettings(int[] selection, BrushSettings[] settingsArray,
            BrushSelectionState brushSelectionState, BrushSettings brushSelectionSettings)
        {
            if (brushSelectionSettings == null) brushSelectionSettings = settingsArray[selection[0]].Clone();
            if (selection.Length == 0) return;
            if (settingsArray.Length <= selection[0]) return;
            brushSelectionState.Reset();
            if (selection.Length > 0) brushSelectionSettings.Copy(settingsArray[selection[0]]);
            if (focusedWindow == this) GUI.FocusControl(null);
            _repaint = true;
        }
    }
}
#pragma warning restore UDR0001
