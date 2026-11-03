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
        private const string BRUSH_SETTINGS_UNDO_MSG = "Brush Settings";

        private bool _brushGroupOpen = true;
        private bool _brushPosGroupOpen = false;
        private bool _brushRotGroupOpen = false;
        private bool _brushScaleGroupOpen = false;
        private bool _brushFlipGroupOpen = false;

        private BrushSelectionState _brushSelectionState = new BrushSelectionState();
        private BrushSettings _brushSelectionSettings = new BrushSettings();
        private void UpdateBrushSelectionSettings()
        {
            if (PaletteManager.selectedBrushIdx == -1) return;
            UpdateBrushSelectionSettings(PaletteManager.idxSelection, PaletteManager.selectedPalette.brushes,
                _brushSelectionState, _brushSelectionSettings);
            _selection.Clear();
            _selection.Add(0);
            _selectedItemIdx = 0;
            if (PaletteManager.selectedBrush == null)
            {
                PaletteManager.ClearSelection();
                return;
            }
            UpdateBrushSelectionSettings(_selection.ToArray(), PaletteManager.selectedBrush.items,
                _itemSelectionState, _itemSelectionSettings);
        }


        public static bool BrushFields(BrushSettings brush, ref bool brushPosGroupOpen, ref bool brushRotGroupOpen,
            ref bool brushScaleGroupOpen, ref bool brush2DGroupOpen)
        {
            bool changed = false;
            DrawPositionSettings(brush, ref brushPosGroupOpen, ref changed);
            DrawRotationSettings(brush, ref brushRotGroupOpen, ref changed);
            DrawScaleSettings(brush, ref brushScaleGroupOpen, ref changed);
            if (brush.isAsset2D) Draw2DSettings(brush, ref brush2DGroupOpen, ref changed);
            if (changed)
            {
                brush.UpdateBottomVertices();
                PaletteManager.selectedPalette.Save();
                BrushstrokeManager.UpdateBrushstroke();
                if (ToolController.current == ToolController.Tool.TILING) PWBIO.UpdateCellSize();
            }
            return changed;
        }
        private static void DrawPositionSettings(BrushSettings brush, ref bool groupOpen, ref bool changed)
        {
            groupOpen = UnityEditor.EditorGUILayout.Foldout(groupOpen, "Position");
            if (!groupOpen) return;
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                {
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var embedInSurface = UnityEditor.EditorGUILayout.ToggleLeft("Embed On the Surface",
                            brush.embedInSurface);
                        if (check.changed)
                        {
                            changed = true;
                            brush.embedInSurface = embedInSurface;
                        }
                    }
                    if (brush.embedInSurface)
                    {
                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            var embedAtPivotHeight = UnityEditor.EditorGUILayout.ToggleLeft("Embed At Pivot Height",
                                brush.embedAtPivotHeight);
                            if (check.changed)
                            {
                                changed = true;
                                brush.embedAtPivotHeight = embedAtPivotHeight;
                            }
                        }
                    }

                    using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                    {
                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            UnityEditor.EditorGUIUtility.labelWidth = 110;
                            var randomSurfaceDistance = UnityEditor.EditorGUILayout.Popup("Surface distance",
                                brush.randomSurfaceDistance ? 1 : 0, new string[] { "Constant", "Random" }) == 1;
                            if (check.changed)
                            {
                                changed = true;
                                brush.randomSurfaceDistance = randomSurfaceDistance;
                            }
                        }
                        if (brush.randomSurfaceDistance)
                        {
                            using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                            {
                                var randomSurfaceDistanceRange = EditorGUIUtils.RangeField(string.Empty,
                                brush.randomSurfaceDistanceRange);
                                if (check.changed)
                                {
                                    changed = true;
                                    brush.randomSurfaceDistanceRange = randomSurfaceDistanceRange;
                                }
                            }
                        }
                        else
                        {
                            using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                            {
                                var surfaceDistance = UnityEditor.EditorGUILayout.FloatField("Value",
                                    brush.surfaceDistance);
                                if (check.changed)
                                {
                                    changed = true;
                                    brush.surfaceDistance = surfaceDistance;
                                }
                            }
                        }
                    }
                    using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                    {
                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            var localPositionOffset = UnityEditor.EditorGUILayout.Vector3Field("Local Offset",
                                brush.localPositionOffset);
                            if (check.changed)
                            {
                                changed = true;
                                brush.localPositionOffset = localPositionOffset;
                            }
                        }
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Pick offset");
                            if (GUILayout.Button("X")) PWBIO.EnableOffsetPicking(AxesUtils.Axis.X, brush);
                            if (GUILayout.Button("Y")) PWBIO.EnableOffsetPicking(AxesUtils.Axis.Y, brush);
                            if (GUILayout.Button("Z")) PWBIO.EnableOffsetPicking(AxesUtils.Axis.Z, brush);
                        }
                    }

                }
            }
        }
        private static void DrawRotationSettings(BrushSettings brush, ref bool groupOpen, ref bool changed)
        {
            groupOpen = UnityEditor.EditorGUILayout.Foldout(groupOpen, "Rotation");
            if (!groupOpen) return;
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var rotateToTheSurface = UnityEditor.EditorGUILayout.ToggleLeft("Rotate to the Surface",
                            brush.rotateToTheSurface);
                    if (check.changed)
                    {
                        changed = true;
                        brush.rotateToTheSurface = rotateToTheSurface;
                    }
                }
                if (brush.rotateToTheSurface)
                {
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var alwaysOrientUp = UnityEditor.EditorGUILayout.ToggleLeft("Always orient up",
                        brush.alwaysOrientUp);
                        if (check.changed)
                        {
                            changed = true;
                            brush.alwaysOrientUp = alwaysOrientUp;
                        }
                    }
                }
                using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                {
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        UnityEditor.EditorGUIUtility.labelWidth = 100;
                        var addRandomRotation = UnityEditor.EditorGUILayout.Popup("Add Rotation",
                            brush.addRandomRotation ? 1 : 0, new string[] { "Constant", "Random" }) == 1;
                        if (check.changed)
                        {
                            changed = true;
                            brush.addRandomRotation = addRandomRotation;
                        }
                    }
                    if (brush.addRandomRotation)
                    {
                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            var randomEulerOffset = EditorGUIUtils.Range3Field(string.Empty, brush.randomEulerOffset);
                            if (check.changed)
                            {
                                changed = true;
                                brush.randomEulerOffset = randomEulerOffset;
                            }
                        }
                        using (new GUILayout.HorizontalScope())
                        {
                            using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                            {
                                UnityEditor.EditorGUIUtility.labelWidth = 80;
                                var rotateInMultiples = UnityEditor.EditorGUILayout.ToggleLeft
                                    ("Only in multiples of", brush.rotateInMultiples);
                                if (check.changed)
                                {
                                    changed = true;
                                    brush.rotateInMultiples = rotateInMultiples;
                                }
                            }
                            using (new UnityEditor.EditorGUI.DisabledGroupScope(!brush.rotateInMultiples))
                            {
                                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                                {
                                    var rotationFactor = UnityEditor.EditorGUILayout.FloatField(brush.rotationFactor);
                                    if (check.changed)
                                    {
                                        changed = true;
                                        brush.rotationFactor = rotationFactor;
                                    }
                                }
                            }
                        }
                    }
                    else // constant
                    {
                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            var eulerOffset = UnityEditor.EditorGUILayout.Vector3Field(string.Empty, brush.eulerOffset);
                            if (check.changed)
                            {
                                changed = true;
                                brush.eulerOffset = eulerOffset;
                            }
                        }
                    }
                }
            }
        }
        private static void DrawScaleSettings(BrushSettings brush, ref bool groupOpen, ref bool changed)
        {
            groupOpen = UnityEditor.EditorGUILayout.Foldout(groupOpen, "Scale");
            if (!groupOpen) return;
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (new GUILayout.HorizontalScope())
                {
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        UnityEditor.EditorGUIUtility.labelWidth = 100;
                        var randomScaleMultiplier = UnityEditor.EditorGUILayout.Popup("Multiplier",
                            brush.randomScaleMultiplier ? 1 : 0, new string[] { "Constant", "Random" }) == 1;
                        if (check.changed)
                        {
                            changed = true;
                            brush.randomScaleMultiplier = randomScaleMultiplier;
                        }
                    }
                    GUILayout.Space(4);
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var separateScaleAxes = UnityEditor.EditorGUILayout.ToggleLeft("Separate Axes",
                            brush.separateScaleAxes, GUILayout.Width(102));
                        if (check.changed)
                        {
                            changed = true;
                            brush.separateScaleAxes = separateScaleAxes;
                        }
                    }
                }
                if (brush.separateScaleAxes)
                {
                    if (brush.randomScaleMultiplier)
                    {
                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            var range3 = EditorGUIUtils.Range3Field(string.Empty, brush.randomScaleMultiplierRange);
                            if (Mathf.Approximately(range3.x.v1, 0))
                                range3.x.v1 = brush.randomScaleMultiplierRange.x.v1;
                            if (Mathf.Approximately(range3.x.v2, 0))
                                range3.x.v2 = brush.randomScaleMultiplierRange.x.v2;

                            if (Mathf.Approximately(range3.y.v1, 0))
                                range3.y.v1 = brush.randomScaleMultiplierRange.y.v1;
                            if (Mathf.Approximately(range3.y.v2, 0))
                                range3.y.v2 = brush.randomScaleMultiplierRange.y.v2;

                            if (Mathf.Approximately(range3.z.v1, 0))
                                range3.z.v1 = brush.randomScaleMultiplierRange.z.v1;
                            if (Mathf.Approximately(range3.z.v2, 0))
                                range3.z.v2 = brush.randomScaleMultiplierRange.z.v2;
                            if (check.changed)
                            {
                                changed = true;
                                brush.randomScaleMultiplierRange = range3;
                            }
                        }
                    }
                    else
                    {
                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            var mult = UnityEditor.EditorGUILayout.Vector3Field(string.Empty, brush.scaleMultiplier);
                            if (Mathf.Approximately(mult.x, 0)) mult.x = brush.scaleMultiplier.x;
                            if (Mathf.Approximately(mult.y, 0)) mult.y = brush.scaleMultiplier.y;
                            if (Mathf.Approximately(mult.z, 0)) mult.z = brush.scaleMultiplier.z;
                            if (check.changed)
                            {
                                changed = true;
                                brush.scaleMultiplier = mult;
                            }
                        }
                    }
                }
                else
                {
                    if (brush.randomScaleMultiplier)
                    {
                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            var range = EditorGUIUtils.RangeField(string.Empty, brush.randomScaleMultiplierRange.x);
                            if (Mathf.Approximately(range.v1, 0)) range.v1 = brush.randomScaleMultiplierRange.x.v1;
                            if (Mathf.Approximately(range.v2, 0)) range.v2 = brush.randomScaleMultiplierRange.x.v2;
                            if (check.changed)
                            {
                                changed = true;
                                brush.randomScaleMultiplierRange.z = brush.randomScaleMultiplierRange.y
                                = brush.randomScaleMultiplierRange.x = range;
                            }
                        }
                    }
                    else
                    {
                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            var value = UnityEditor.EditorGUILayout.FloatField("Value: ", brush.scaleMultiplier.x);
                            var scaleMultiplier = brush.scaleMultiplier;
                            if (!Mathf.Approximately(value, 0)) scaleMultiplier = new Vector3(value, value, value);
                            if (check.changed)
                            {
                                changed = true;
                                brush.scaleMultiplier = scaleMultiplier;
                            }
                        }
                    }
                }
            }
        }
        private static void Draw2DSettings(BrushSettings brush, ref bool groupOpen, ref bool changed)
        {
            groupOpen = UnityEditor.EditorGUILayout.Foldout(groupOpen, "2D");
            if (!groupOpen) return;
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var flipX = (BrushSettings.FlipAction)UnityEditor.EditorGUILayout.Popup("Flip X: ",
                        (int)brush.flipX, new string[] { "No", "Yes", "Random" });
                    if (check.changed)
                    {
                        changed = true;
                        brush.flipX = flipX;
                    }
                }
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var flipY = (BrushSettings.FlipAction)UnityEditor.EditorGUILayout.Popup("Flip Y: ",
                        (int)brush.flipY, new string[] { "No", "Yes", "Random" });
                    if (check.changed)
                    {
                        changed = true;
                        brush.flipY = flipY;
                    }
                }
            }
        }
        private void BrushGroup()
        {
            var brush = PaletteManager.selectedBrush;
            if (brush == null) return;
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                UnityEditor.EditorGUIUtility.labelWidth = 50;
                brush.name = UnityEditor.EditorGUILayout.DelayedTextField("Name", brush.name);
                if (BrushFields(brush, ref _brushPosGroupOpen, ref _brushRotGroupOpen,
                    ref _brushScaleGroupOpen, ref _brushFlipGroupOpen)) PWBIO.UpdateSelectedPersistentObject();
            }
        }
    }
}
#pragma warning restore UDR0001
