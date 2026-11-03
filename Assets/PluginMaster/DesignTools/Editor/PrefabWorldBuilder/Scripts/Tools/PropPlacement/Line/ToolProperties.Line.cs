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
        private static readonly string[] _lineModeNames = { "Auto", "Paint on surface", "Paint on the line" };
        private static readonly string[] _lineSpacingNames = { "Bounds", "Constant" };
        private static readonly string[] _lineAxesAlongTheLineNames = { "X", "Z" };

        private static readonly string[] _shapeProjDirNames = new string[]
        { "+X", "-X", "+Y", "-Y", "+Z", "-Z", "Perpendicular to plane", "From center", "To center" };


        private static int _lineProjDirIdx = 6;
        private static BrushPropertiesGroupState _lineOverwriteGroupState;

        private void LineBaseGUI<SETTINGS>(SETTINGS lineSettings) where SETTINGS : LineSettings
        {
            void OnValueChanged()
            {
                PWBIO.UpdateStroke();
                PWBIO.repaint = true;
            }
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var mode = (PaintOnSurfaceToolSettingsBase.PaintMode)
                    UnityEditor.EditorGUILayout.Popup("Paint Mode", (int)lineSettings.mode, _lineModeNames);
                    if (check.changed)
                    {
                        lineSettings.mode = mode;
                        OnValueChanged();
                    }
                }
                if (lineSettings is ShapeSettings)
                {
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var parallelToTheSurface = UnityEditor.EditorGUILayout.ToggleLeft(
                            lineSettings.mode == PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE
                            ? "Place objects perpendicular to the plane"
                            : "Place objects perpendicular to the surface",
                            lineSettings.perpendicularToTheSurface);
                        if (check.changed)
                        {
                            lineSettings.perpendicularToTheSurface = parallelToTheSurface;
                            OnValueChanged();
                        }
                    }
                }
                else
                {
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var parallelToTheSurface
                            = UnityEditor.EditorGUILayout.ToggleLeft("Place objects perpendicular to the " +
                            (lineSettings.mode == PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE ? "line" : "surface"),
                            lineSettings.perpendicularToTheSurface);
                        if (check.changed)
                        {
                            lineSettings.perpendicularToTheSurface = parallelToTheSurface;
                            OnValueChanged();
                        }
                    }
                }
                var dirNames = lineSettings is ShapeSettings ? _shapeProjDirNames : _dirNames;
                var shapeSettings = lineSettings as ShapeSettings;
                if (shapeSettings != null)
                {
                    switch (shapeSettings.projectionDirectionType)
                    {
                        case ShapeSettings.ShapeProjectionDirection.AXIS:
                            _lineProjDirIdx = System.Array.IndexOf(_dir, lineSettings.projectionDirection);
                            break;
                        case ShapeSettings.ShapeProjectionDirection.PLANE_NORMAL:
                            _lineProjDirIdx = 6;
                            break;
                        case ShapeSettings.ShapeProjectionDirection.FROM_CENTER:
                            _lineProjDirIdx = 7;
                            break;
                        case ShapeSettings.ShapeProjectionDirection.TO_CENTER:
                            _lineProjDirIdx = 8;
                            break;
                    }
                }
                else _lineProjDirIdx = System.Array.IndexOf(_dir, lineSettings.projectionDirection);
                if (_lineProjDirIdx == -1) _lineProjDirIdx = 3;

                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    _lineProjDirIdx = UnityEditor.EditorGUILayout.Popup("Projection Direction", _lineProjDirIdx, dirNames);
                    if (check.changed)
                    {
                        if (shapeSettings != null)
                        {
                            if (_lineProjDirIdx == 6)
                                shapeSettings.projectionDirectionType = ShapeSettings.ShapeProjectionDirection.PLANE_NORMAL;
                            else if (_lineProjDirIdx == 7)
                                shapeSettings.projectionDirectionType
                                    = ShapeSettings.ShapeProjectionDirection.FROM_CENTER;
                            else if (_lineProjDirIdx == 8)
                                shapeSettings.projectionDirectionType
                                    = ShapeSettings.ShapeProjectionDirection.TO_CENTER;
                            else
                                shapeSettings.projectionDirectionType = ShapeSettings.ShapeProjectionDirection.AXIS;
                        }
                        if (_lineProjDirIdx == 6) lineSettings.projectionDirection = PWBIO.GetShapePlaneNormal();
                        else if (_lineProjDirIdx < 6) lineSettings.projectionDirection = _dir[_lineProjDirIdx];
                        OnValueChanged();
                    }
                }
            }
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (new UnityEditor.EditorGUI.DisabledGroupScope(lineSettings is ShapeSettings
                    && ((lineSettings as ShapeSettings).projectionDirectionType
                    == ShapeSettings.ShapeProjectionDirection.FROM_CENTER
                    || (lineSettings as ShapeSettings).projectionDirectionType
                    == ShapeSettings.ShapeProjectionDirection.TO_CENTER)))
                {
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var objectsOrientedAlongTheLine
                        = UnityEditor.EditorGUILayout.ToggleLeft("Orient Along the Line",
                        lineSettings.objectsOrientedAlongTheLine);
                        if (check.changed)
                        {
                            lineSettings.objectsOrientedAlongTheLine = objectsOrientedAlongTheLine;
                            OnValueChanged();
                        }
                    }
                }

                if (lineSettings.objectsOrientedAlongTheLine)
                {
                    UnityEditor.EditorGUIUtility.labelWidth = 170;
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var axisOrientedAlongTheLine = UnityEditor.EditorGUILayout.Popup("Axis Oriented Along the Line",
                        lineSettings.axisOrientedAlongTheLine == AxesUtils.Axis.X ? 0 : 1,
                        _lineAxesAlongTheLineNames) == 0 ? AxesUtils.Axis.X : AxesUtils.Axis.Z;
                        if (check.changed)
                        {
                            lineSettings.axisOrientedAlongTheLine = axisOrientedAlongTheLine;
                            OnValueChanged();
                        }
                    }
                }
            }
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                UnityEditor.EditorGUIUtility.labelWidth = 120;
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var spacingType = (LineSettings.SpacingType)
                    UnityEditor.EditorGUILayout.Popup("Spacing", (int)lineSettings.spacingType, _lineSpacingNames);
                    if (check.changed)
                    {
                        lineSettings.spacingType = spacingType;
                        OnValueChanged();
                    }
                }
                if (lineSettings.spacingType == LineSettings.SpacingType.CONSTANT)
                {
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var spacing = UnityEditor.EditorGUILayout.FloatField("Value", lineSettings.spacing);
                        if (check.changed)
                        {
                            lineSettings.spacing = spacing;
                            OnValueChanged();
                        }
                    }
                }
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var gapSize = UnityEditor.EditorGUILayout.FloatField("Gap Size", lineSettings.gapSize);
                    if (check.changed)
                    {
                        if (PaletteManager.selectedBrushIdx >= 0 && PaletteManager.selectedBrush != null)
                        {
                            var spacing = lineSettings.spacingType == LineSettings.SpacingType.CONSTANT
                                ? lineSettings.spacing : PaletteManager.selectedBrush.minBrushMagnitude;
                            var min = Mathf.Min(0, 0.05f - spacing);
                            gapSize = Mathf.Max(min, gapSize);
                        }
                        lineSettings.gapSize = gapSize;
                        OnValueChanged();
                    }
                }
            }
        }

        private void LineGroup()
        {
            ToolProfileGUI(LineManager.instance);
            EditModeToggle(LineManager.instance);
            HandlePosition();
            UnityEditor.EditorGUIUtility.labelWidth = 120;
            LineBaseGUI(LineManager.settings);
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var closed = UnityEditor.EditorGUILayout.ToggleLeft("Closed Path", PWBIO.lineData.closed);
                    if (check.changed)
                    {
                        PWBIO.lineData.closed = closed;
                        PWBIO.UpdateStroke();
                        UnityEditor.SceneView.RepaintAll();
                        PWBIO.repaint = true;
                    }
                }
            }
            PaintSettingsGUI(LineManager.settings, LineManager.settings);
            OverwriteBrushPropertiesGUI(LineManager.settings, ref _lineOverwriteGroupState);
        }
    }
}
#pragma warning restore UDR0001
