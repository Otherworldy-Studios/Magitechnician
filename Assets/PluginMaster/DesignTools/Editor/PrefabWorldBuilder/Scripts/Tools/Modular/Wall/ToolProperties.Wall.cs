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
        
        private static BrushPropertiesGroupState _wallOverwriteGroupState;
        private void WallGroup()
        {
            ToolProfileGUI(WallManager.instance);
            var settings = WallManager.settings;
            UnityEditor.EditorGUIUtility.labelWidth = 80;
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                settings.autoCalculateAxes = UnityEditor.EditorGUILayout.ToggleLeft("Auto calculate axes",
                    settings.autoCalculateAxes);
                using (new UnityEditor.EditorGUI.DisabledGroupScope(settings.autoCalculateAxes))
                {
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var upAxisIdx = UnityEditor.EditorGUILayout.Popup("Upward axis",
                         settings.upwardAxis, _dirNames);
                        if (check.changed) settings.upwardAxis = upAxisIdx;
                    }
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var forwardAxisIdx = UnityEditor.EditorGUILayout.Popup("Forward axis",
                         settings.forwardAxis, _dirNames);
                        if (check.changed) settings.forwardAxis = forwardAxisIdx;
                    }
                }
            }
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                UnityEditor.EditorGUIUtility.labelWidth = 90;
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var moduleSizeType = (TilesUtils.SizeType)
                    UnityEditor.EditorGUILayout.Popup("Cell size type", (int)settings.moduleSizeType, _cellTypeNames);
                    if (check.changed)
                    {
                        settings.moduleSizeType = moduleSizeType;
                        if (settings.moduleSizeType == TilesUtils.SizeType.CUSTOM) WallManager.instance.ResetSize();
                    }
                }

                using (new UnityEditor.EditorGUI.DisabledGroupScope(
                    settings.moduleSizeType != TilesUtils.SizeType.CUSTOM))
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            UnityEditor.EditorGUIUtility.labelWidth = 50;
                            var size = UnityEditor.EditorGUILayout.FloatField("Length",
                                AxesUtils.GetAxisValue(settings.moduleSize, WallManager.wallLenghtAxis));
                            if (check.changed) settings.SetCustomLength(size);
                        }
                        GUILayout.Space(10);
                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            UnityEditor.EditorGUIUtility.labelWidth = 70;
                            var thickness = UnityEditor.EditorGUILayout.FloatField("Thickness", WallManager.wallThickness);
                            if (check.changed) settings.SetThickness(thickness);
                        }
                    }
                }
                if (settings.moduleSizeType == TilesUtils.SizeType.CUSTOM)
                {
                    using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                    {
                        int sizeIndex = 0;
                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            UnityEditor.EditorGUIUtility.labelWidth = 80;
                            sizeIndex = UnityEditor.EditorGUILayout.Popup("Wall Length",
                                WallManager.instance.GetIndexOfSelectedSize(),
                                WallManager.instance.GetSizesNames());
                            if (check.changed)
                            {
                                WallManager.instance.SelectSize(sizeIndex);
                            }
                        }
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Reset")) WallManager.instance.ResetSize();
                            if (GUILayout.Button("Save..."))
                            {
                                RenameWindow.ShowWindow(position.position + Event.current.mousePosition,
                                    WallManager.instance.SaveSize, "Save Size", WallManager.instance.selectedSizeName);
                            }
                            using (new UnityEditor.EditorGUI.DisabledGroupScope(sizeIndex == 0))
                            {
                                if (GUILayout.Button("Delete"))
                                {
                                    WallManager.instance.DeleteSelectedSize();
                                }
                            }
                        }
                    }
                }
                else
                {
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var subtractBrushOffset = UnityEditor.EditorGUILayout.ToggleLeft("Subtract brush local offset",
                            settings.subtractBrushOffset);
                        if (check.changed) settings.subtractBrushOffset = subtractBrushOffset;
                    }
                }
            }
            using (new GUILayout.HorizontalScope(UnityEditor.EditorStyles.helpBox))
            {
                UnityEditor.EditorGUIUtility.labelWidth = 60;
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var spacing = UnityEditor.EditorGUILayout.FloatField("Spacing", settings.spacing.x);
                    if (check.changed)
                    {
                        settings.spacing = new Vector3(spacing, 0, spacing);
                    }
                }
            }


            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                int originIndex = 0;
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    UnityEditor.EditorGUIUtility.labelWidth = 70;
                    originIndex = UnityEditor.EditorGUILayout.Popup("Grid origin",
                        GridManager.settings.GetIndexOfSelectedOrigin(), GridManager.settings.GetOriginNames());
                    if (check.changed)
                    {
                        GridManager.settings.SelectOrigin(originIndex);
                        UnityEditor.SceneView.RepaintAll();
                    }
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Position");
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var origin = UnityEditor.EditorGUILayout.Vector3Field(string.Empty,
                            GridManager.settings.origin);
                        if (check.changed)
                        {
                            GridManager.settings.origin = origin;
                            UnityEditor.SceneView.RepaintAll();
                        }
                    }
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Rotation");
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var angles = UnityEditor.EditorGUILayout.Vector3Field(string.Empty,
                            GridManager.settings.rotation.eulerAngles);
                        if (check.changed)
                        {
                            GridManager.settings.rotation = Quaternion.Euler(angles);
                            UnityEditor.SceneView.RepaintAll();
                        }
                    }
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Reset"))
                    {
                        GridManager.settings.ResetOrigin();
                    }
                    if (GUILayout.Button("Save..."))
                    {
                        RenameWindow.ShowWindow(position.position + Event.current.mousePosition,
                            GridManager.settings.SaveGridOrigin, "Save Origin", GridManager.settings.selectedOrigin);
                    }
                    using (new UnityEditor.EditorGUI.DisabledGroupScope(originIndex == 0))
                    {
                        if (GUILayout.Button("Delete"))
                        {
                            GridManager.settings.DeleteSelectedOrigin();
                        }
                    }
                }
            }
            PaintToolSettingsGUI(FloorManager.settings);
            OverwriteBrushPropertiesGUI(FloorManager.settings, ref _floorOverwriteGroupState);
        }
    }
}
#pragma warning restore UDR0001
