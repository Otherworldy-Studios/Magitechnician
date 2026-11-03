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
        
        private static BrushPropertiesGroupState _blockOverwriteGroupState;
        private void BlockGroup()
        {
            ToolProfileGUI(BlockManager.instance);

            var settings = BlockManager.settings;
            UnityEditor.EditorGUIUtility.labelWidth = 80;

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
                        if (settings.moduleSizeType == TilesUtils.SizeType.CUSTOM) BlockManager.instance.ResetSize();
                    }
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    UnityEditor.EditorGUIUtility.labelWidth = 20;
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        using (new UnityEditor.EditorGUI.DisabledGroupScope(
                            settings.moduleSizeType != TilesUtils.SizeType.CUSTOM))
                        {
                            var x = UnityEditor.EditorGUILayout.FloatField("X", settings.moduleSize.x);
                            var y = UnityEditor.EditorGUILayout.FloatField("Y", settings.moduleSize.y);
                            var z = UnityEditor.EditorGUILayout.FloatField("Z", settings.moduleSize.z);
                            if (check.changed)
                            {
                                settings.moduleSize = new Vector3(x, y, z);
                            }
                        }
                    }
                }

                if (settings.moduleSizeType == TilesUtils.SizeType.CUSTOM)
                {
                    int sizeIndex = 0;
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        UnityEditor.EditorGUIUtility.labelWidth = 70;
                        sizeIndex = UnityEditor.EditorGUILayout.Popup("Cell Size",
                            BlockManager.instance.GetIndexOfSelectedSize(),
                            BlockManager.instance.GetSizesNames());
                        if (check.changed)
                        {
                            BlockManager.instance.SelectSize(sizeIndex);
                        }
                    }
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Reset")) BlockManager.instance.ResetSize();
                        if (GUILayout.Button("Save..."))
                        {
                            RenameWindow.ShowWindow(position.position + Event.current.mousePosition,
                                BlockManager.instance.SaveSize, "Save Size", BlockManager.instance.selectedSizeName);
                        }
                        using (new UnityEditor.EditorGUI.DisabledGroupScope(sizeIndex == 0))
                        {
                            if (GUILayout.Button("Delete"))
                            {
                                BlockManager.instance.DeleteSelectedSize();
                            }
                        }
                    }
                }
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var subtractBrushOffset = UnityEditor.EditorGUILayout.ToggleLeft("Subtract brush local offset",
                        settings.subtractBrushOffset);
                    if (check.changed) settings.subtractBrushOffset = subtractBrushOffset;
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
                    GUILayout.Label("Position", GUILayout.Width(60));
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
                    GUILayout.Label("Rotation", GUILayout.Width(60));
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var angle = UnityEditor.EditorGUILayout.Vector3Field(string.Empty,
                            GridManager.settings.rotation.eulerAngles);
                        if (check.changed)
                        {
                            GridManager.settings.rotation = Quaternion.Euler(angle);
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

            PaintToolSettingsGUI(BlockManager.settings);
            OverwriteBrushPropertiesGUI(BlockManager.settings, ref _blockOverwriteGroupState);
        }
    }
}
#pragma warning restore UDR0001
