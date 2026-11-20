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
        private bool BrushSelectionFields(ref bool brushPosGroupOpen, ref bool brushRotGroupOpen,
            ref bool brushScaleGroupOpen, ref bool brushFlipGroupOpen, string undoMsg, bool isItem, bool showApplyAndDiscard,
            BrushSettings[] settingsArray, int[] selection,
            BrushSettings brushSelectionSettings, BrushSelectionState brushSelectionState)
        {
            if (brushSelectionSettings == null)
                UpdateBrushSelectionSettings(selection, settingsArray, brushSelectionState, brushSelectionSettings);
            UpdateSelectionState(settingsArray, selection, brushSelectionState);

            brushPosGroupOpen = UnityEditor.EditorGUILayout.Foldout(brushPosGroupOpen, "Position");
            UnityEditor.EditorGUIUtility.labelWidth = 110;
            if (brushPosGroupOpen)
            {
                using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                {
                    using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Box(GetStateGUIContent(brushSelectionState.embedInSurface),
                                UnityEditor.EditorStyles.label);

                            using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                            {
                                brushSelectionSettings.embedInSurface
                                    = UnityEditor.EditorGUILayout.ToggleLeft("Embed On the Surface",
                                    brushSelectionSettings.embedInSurface);
                                if (check.changed) brushSelectionState.embedInSurface = SelectionFieldState.CHANGED;
                            }
                            GUILayout.FlexibleSpace();
                        }
                        if (brushSelectionSettings.embedInSurface)
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Box(GetStateGUIContent(brushSelectionState.embedAtPivotHeight),
                                    UnityEditor.EditorStyles.label);

                                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                                {
                                    brushSelectionSettings.embedAtPivotHeight
                                        = UnityEditor.EditorGUILayout.ToggleLeft("Embed At Pivot Height",
                                        brushSelectionSettings.embedAtPivotHeight);
                                    if (check.changed) brushSelectionState.embedAtPivotHeight = SelectionFieldState.CHANGED;
                                }
                                GUILayout.FlexibleSpace();
                            }
                        }
                    }
                    using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Box(GetStateGUIContent(brushSelectionState.randomSurfaceDistance),
                                UnityEditor.EditorStyles.label);

                            using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                            {
                                UnityEditor.EditorGUIUtility.labelWidth = 110;
                                brushSelectionSettings.randomSurfaceDistance
                                    = UnityEditor.EditorGUILayout.Popup("Surface Distance",
                                    brushSelectionSettings.randomSurfaceDistance ? 1 : 0,
                                    new string[] { "Constant", "Random" }) == 1;
                                if (check.changed)
                                    brushSelectionState.randomSurfaceDistance = SelectionFieldState.CHANGED;
                            }
                            GUILayout.FlexibleSpace();
                        }
                        if (brushSelectionSettings.randomSurfaceDistance)
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Box(GetStateGUIContent(brushSelectionState.randomSurfaceDistanceRange),
                                    UnityEditor.EditorStyles.label);

                                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                                {
                                    brushSelectionSettings.randomSurfaceDistanceRange
                                        = EditorGUIUtils.RangeField(string.Empty,
                                        brushSelectionSettings.randomSurfaceDistanceRange);
                                    if (check.changed)
                                        brushSelectionState.randomSurfaceDistanceRange = SelectionFieldState.CHANGED;
                                }
                                GUILayout.FlexibleSpace();
                            }
                        }
                        else
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Box(GetStateGUIContent(brushSelectionState.surfaceDistance),
                                    UnityEditor.EditorStyles.label);

                                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                                {
                                    UnityEditor.EditorGUIUtility.labelWidth = 100;
                                    brushSelectionSettings.surfaceDistance
                                        = UnityEditor.EditorGUILayout.FloatField("Value",
                                        brushSelectionSettings.surfaceDistance);
                                    if (check.changed)
                                        brushSelectionState.surfaceDistance = SelectionFieldState.CHANGED;
                                }
                                GUILayout.FlexibleSpace();
                            }
                        }
                    }
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Box(GetStateGUIContent(brushSelectionState.localPositionOffset),
                            UnityEditor.EditorStyles.label);

                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            brushSelectionSettings.localPositionOffset
                                = UnityEditor.EditorGUILayout.Vector3Field("Local Offset",
                                brushSelectionSettings.localPositionOffset);
                            if (check.changed) brushSelectionState.localPositionOffset = SelectionFieldState.CHANGED;
                        }
                        GUILayout.FlexibleSpace();
                    }
                }
            }

            brushRotGroupOpen = UnityEditor.EditorGUILayout.Foldout(brushRotGroupOpen, "Rotation");
            if (brushRotGroupOpen)
            {
                using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                {

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Box(GetStateGUIContent(brushSelectionState.rotateToTheSurface),
                            UnityEditor.EditorStyles.label);

                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            brushSelectionSettings.rotateToTheSurface
                                = UnityEditor.EditorGUILayout.ToggleLeft("Rotate to the Surface",
                                brushSelectionSettings.rotateToTheSurface);
                            if (check.changed) brushSelectionState.rotateToTheSurface = SelectionFieldState.CHANGED;
                        }
                        GUILayout.FlexibleSpace();
                    }
                    if (brushSelectionSettings.rotateToTheSurface)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Box(GetStateGUIContent(brushSelectionState.alwaysOrientUp),
                                UnityEditor.EditorStyles.label);
                            using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                            {
                                brushSelectionSettings.alwaysOrientUp
                                    = UnityEditor.EditorGUILayout.ToggleLeft("Always orient up",
                                    brushSelectionSettings.alwaysOrientUp);
                                if (check.changed) brushSelectionState.alwaysOrientUp = SelectionFieldState.CHANGED;
                            }
                            GUILayout.FlexibleSpace();
                        }
                    }
                    using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Box(GetStateGUIContent(brushSelectionState.addRandomRotation),
                                UnityEditor.EditorStyles.label);

                            using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                            {
                                UnityEditor.EditorGUIUtility.labelWidth = 100;
                                brushSelectionSettings.addRandomRotation
                                    = UnityEditor.EditorGUILayout.Popup("Add Rotation",
                                    brushSelectionSettings.addRandomRotation ? 1 : 0,
                                    new string[] { "Constant", "Random" }) == 1;
                                if (check.changed)
                                    brushSelectionState.addRandomRotation = SelectionFieldState.CHANGED;
                            }
                            GUILayout.FlexibleSpace();
                        }
                        if (brushSelectionSettings.addRandomRotation)
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Box(GetStateGUIContent(brushSelectionState.randomEulerOffset),
                                    UnityEditor.EditorStyles.label);

                                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                                {
                                    brushSelectionSettings.randomEulerOffset
                                        = EditorGUIUtils.Range3Field(string.Empty,
                                        brushSelectionSettings.randomEulerOffset);
                                    if (check.changed)
                                        brushSelectionState.randomEulerOffset = SelectionFieldState.CHANGED;
                                }
                                GUILayout.FlexibleSpace();
                            }
                        }
                        else
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Box(GetStateGUIContent(brushSelectionState.eulerOffset),
                                    UnityEditor.EditorStyles.label);

                                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                                {
                                    brushSelectionSettings.eulerOffset = UnityEditor.EditorGUILayout.Vector3Field(string.Empty,
                                        brushSelectionSettings.eulerOffset);
                                    if (check.changed) brushSelectionState.eulerOffset = SelectionFieldState.CHANGED;
                                }
                                GUILayout.FlexibleSpace();
                            }
                        }
                    }

                }
            }

            brushScaleGroupOpen = UnityEditor.EditorGUILayout.Foldout(brushScaleGroupOpen, "Scale");
            if (brushScaleGroupOpen)
            {
                using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Box(GetStateGUIContent(brushSelectionState.randomScaleMultiplier),
                            UnityEditor.EditorStyles.label);

                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            UnityEditor.EditorGUIUtility.labelWidth = 100;
                            brushSelectionSettings.randomScaleMultiplier = UnityEditor.EditorGUILayout.Popup("Multiplier",
                                brushSelectionSettings.randomScaleMultiplier ? 1
                                : 0, new string[] { "Constant", "Random" }) == 1;
                            if (check.changed)
                                brushSelectionState.randomScaleMultiplier = SelectionFieldState.CHANGED;
                        }
                        GUILayout.FlexibleSpace();
                    }
                    GUILayout.Space(4);
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Box(GetStateGUIContent(brushSelectionState.separateScaleAxes),
                            UnityEditor.EditorStyles.label);

                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            brushSelectionSettings.separateScaleAxes = UnityEditor.EditorGUILayout.ToggleLeft("Separate Axes",
                                brushSelectionSettings.separateScaleAxes);
                            if (check.changed) brushSelectionState.separateScaleAxes = SelectionFieldState.CHANGED;
                        }
                        GUILayout.FlexibleSpace();
                    }

                    if (brushSelectionSettings.separateScaleAxes)
                    {
                        if (brushSelectionSettings.randomScaleMultiplier)
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Box(GetStateGUIContent(brushSelectionState.randomScaleMultiplierRange),
                                    UnityEditor.EditorStyles.label);

                                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                                {
                                    var range3 = EditorGUIUtils.Range3Field(string.Empty,
                                        brushSelectionSettings.randomScaleMultiplierRange);
                                    if (Mathf.Approximately(range3.x.v1, 0))
                                        range3.x.v1 = brushSelectionSettings.randomScaleMultiplierRange.x.v1;
                                    if (Mathf.Approximately(range3.x.v2, 0))
                                        range3.x.v2 = brushSelectionSettings.randomScaleMultiplierRange.x.v2;

                                    if (Mathf.Approximately(range3.y.v1, 0))
                                        range3.y.v1 = brushSelectionSettings.randomScaleMultiplierRange.y.v1;
                                    if (Mathf.Approximately(range3.y.v2, 0))
                                        range3.y.v2 = brushSelectionSettings.randomScaleMultiplierRange.y.v2;

                                    if (Mathf.Approximately(range3.z.v1, 0))
                                        range3.z.v1 = brushSelectionSettings.randomScaleMultiplierRange.z.v1;
                                    if (Mathf.Approximately(range3.z.v2, 0))
                                        range3.z.v2 = brushSelectionSettings.randomScaleMultiplierRange.z.v2;

                                    brushSelectionSettings.randomScaleMultiplierRange = range3;
                                    if (check.changed && range3 != brushSelectionSettings.randomScaleMultiplierRange)
                                        brushSelectionState.randomScaleMultiplierRange = SelectionFieldState.CHANGED;
                                }
                                GUILayout.FlexibleSpace();
                            }
                        }
                        else
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Box(GetStateGUIContent(brushSelectionState.scaleMultiplier),
                                    UnityEditor.EditorStyles.label);

                                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                                {
                                    var mult = UnityEditor.EditorGUILayout.Vector3Field(string.Empty,
                                        brushSelectionSettings.scaleMultiplier);
                                    if (Mathf.Approximately(mult.x, 0)) mult.x = brushSelectionSettings.scaleMultiplier.x;
                                    if (Mathf.Approximately(mult.y, 0)) mult.y = brushSelectionSettings.scaleMultiplier.y;
                                    if (Mathf.Approximately(mult.z, 0)) mult.z = brushSelectionSettings.scaleMultiplier.z;
                                    brushSelectionSettings.scaleMultiplier = mult;
                                    if (check.changed)
                                        brushSelectionState.scaleMultiplier = SelectionFieldState.CHANGED;
                                }
                                GUILayout.FlexibleSpace();
                            }
                        }
                    }
                    else
                    {
                        if (brushSelectionSettings.randomScaleMultiplier)
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Box(GetStateGUIContent(brushSelectionState.randomScaleMultiplierRange),
                                    UnityEditor.EditorStyles.label);

                                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                                {
                                    var range = EditorGUIUtils.RangeField(string.Empty,
                                        brushSelectionSettings.randomScaleMultiplierRange.x);
                                    if (Mathf.Approximately(range.v1, 0))
                                        range.v1 = brushSelectionSettings.randomScaleMultiplierRange.x.v1;
                                    if (Mathf.Approximately(range.v2, 0))
                                        range.v1 = brushSelectionSettings.randomScaleMultiplierRange.x.v2;
                                    brushSelectionSettings.randomScaleMultiplierRange.z
                                        = brushSelectionSettings.randomScaleMultiplierRange.y
                                        = brushSelectionSettings.randomScaleMultiplierRange.x
                                        = range;
                                    if (check.changed && range != brushSelectionSettings.randomScaleMultiplierRange.x)
                                        brushSelectionState.randomScaleMultiplierRange = SelectionFieldState.CHANGED;
                                }
                                GUILayout.FlexibleSpace();
                            }
                        }
                        else
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Box(GetStateGUIContent(brushSelectionState.scaleMultiplier),
                                    UnityEditor.EditorStyles.label);

                                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                                {
                                    var multiplier = UnityEditor.EditorGUILayout.FloatField("Value",
                                        brushSelectionSettings.scaleMultiplier.x);
                                    if (!Mathf.Approximately(multiplier, 0))
                                    {
                                        brushSelectionSettings.scaleMultiplier = Vector3.one * multiplier;
                                        if (check.changed)
                                            brushSelectionState.scaleMultiplier = SelectionFieldState.CHANGED;
                                    }
                                }
                                GUILayout.FlexibleSpace();
                            }
                        }
                    }
                }
            }
            bool isAsset2D = true;
            foreach (var idx in selection)
            {
                var settings = settingsArray[idx];
                if (!settings.isAsset2D)
                {
                    isAsset2D = false;
                    break;
                }
            }
            if (isAsset2D)
            {
                brushFlipGroupOpen = UnityEditor.EditorGUILayout.Foldout(brushFlipGroupOpen, "Flip");
                if (brushFlipGroupOpen)
                {
                    using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Box(GetStateGUIContent(brushSelectionState.flipX),
                                UnityEditor.EditorStyles.label);

                            using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                            {
                                brushSelectionSettings.flipX
                                    = (BrushSettings.FlipAction)UnityEditor.EditorGUILayout.Popup("Flip X: ",
                                 (int)brushSelectionSettings.flipX, new string[] { "No", "Yes", "Random" });
                                if (check.changed) brushSelectionState.flipX = SelectionFieldState.CHANGED;
                            }
                            GUILayout.FlexibleSpace();
                        }
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Box(GetStateGUIContent(brushSelectionState.flipY),
                                UnityEditor.EditorStyles.label);

                            using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                            {
                                brushSelectionSettings.flipY
                                    = (BrushSettings.FlipAction)UnityEditor.EditorGUILayout.Popup("Flip Y: ",
                                 (int)brushSelectionSettings.flipY, new string[] { "No", "Yes", "Random" });
                                if (check.changed) brushSelectionState.flipY = SelectionFieldState.CHANGED;
                            }
                            GUILayout.FlexibleSpace();
                        }
                    }
                }
            }
            if (showApplyAndDiscard)
                return ApplyDiscardButtons(undoMsg, isItem, settingsArray, selection,
                    brushSelectionSettings, brushSelectionState);
            return false;
        }

        private bool ApplyDiscardButtons(string undoMsg, bool isItem,
            BrushSettings[] settingsArray, int[] selection,
            BrushSettings brushSelectionSettings, BrushSelectionState brushSelectionState)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new UnityEditor.EditorGUI.DisabledGroupScope(!brushSelectionState.changed))
                {
                    if (GUILayout.Button("Discard")) UpdateBrushSelectionSettings(selection, settingsArray,
                        brushSelectionState, brushSelectionSettings);
                }
                if (GUILayout.Button("Apply"))
                {
                    foreach (var idx in selection)
                    {
                        var brush = isItem ? (BrushSettings)PaletteManager.selectedBrush.GetItemAt(idx)
                            : PaletteManager.selectedPalette.GetBrush(idx);
                        brush.surfaceDistance = brushSelectionSettings.surfaceDistance;
                        brush.randomSurfaceDistance = brushSelectionSettings.randomSurfaceDistance;
                        brush.randomSurfaceDistanceRange = brushSelectionSettings.randomSurfaceDistanceRange;
                        brush.embedInSurface = brushSelectionSettings.embedInSurface;
                        brush.embedAtPivotHeight = brushSelectionSettings.embedAtPivotHeight;
                        brush.localPositionOffset = brushSelectionSettings.localPositionOffset;

                        brush.rotateToTheSurface = brushSelectionSettings.rotateToTheSurface;
                        brush.eulerOffset = brushSelectionSettings.eulerOffset;
                        brush.addRandomRotation = brushSelectionSettings.addRandomRotation;
                        brush.randomEulerOffset = brushSelectionSettings.randomEulerOffset;
                        brush.alwaysOrientUp = brushSelectionSettings.alwaysOrientUp;

                        brush.separateScaleAxes = brushSelectionSettings.separateScaleAxes;
                        brush.scaleMultiplier = brushSelectionSettings.scaleMultiplier;
                        brush.randomScaleMultiplier = brushSelectionSettings.randomScaleMultiplier;
                        brush.randomScaleMultiplierRange = brushSelectionSettings.randomScaleMultiplierRange;

                        brush.flipX = brushSelectionSettings.flipX;
                        brush.flipY = brushSelectionSettings.flipY;

                        if (ToolController.current == ToolController.Tool.PIN
                            && (brushSelectionState.embedInSurface == SelectionFieldState.CHANGED
                            || brushSelectionState.embedAtPivotHeight == SelectionFieldState.CHANGED)) PWBIO.ResetPinValues();
                    }
                    PaletteManager.selectedPalette.Save();
                    UpdateBrushSelectionSettings(selection, settingsArray, brushSelectionState, brushSelectionSettings);
                    return true;
                }
            }
            return false;
        }
    }
}
#pragma warning restore UDR0001
