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
        private static readonly string[] _tilingModeNames = { "Auto", "Paint on surface", "Paint on the plane" };
        private static readonly string[] _cellTypeNames = { "Smallest object", "Biggest object", "Custom" };
        
        private static BrushPropertiesGroupState _tilingOverwriteGroupState;
        private void TilingGroup()
        {
            ToolProfileGUI(TilingManager.instance);
            EditModeToggle(TilingManager.instance);
            HandlePosition();
            if (!ToolController.editMode)
            {
                using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                {
                    TilingManager.settings.showPreview = UnityEditor.EditorGUILayout.ToggleLeft("Show Preview",
                        TilingManager.settings.showPreview);
                    if (TilingManager.settings.showPreview)
                        UnityEditor.EditorGUILayout.HelpBox("If you experience slowdown issues, disable preview.",
                            UnityEditor.MessageType.Info);
                    UnityEditor.EditorGUILayout.LabelField("Object count", BrushstrokeManager.itemCount.ToString());
                }
            }
            UnityEditor.EditorGUIUtility.labelWidth = 180;
            using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
            {
                var settings = TilingManager.settings;
                using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                {
                    settings.mode = (TilingSettings.PaintMode)UnityEditor.EditorGUILayout.Popup("Paint mode",
                    (int)settings.mode, _tilingModeNames);
                    using (var angleCheck = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var eulerAngles = settings.rotation.eulerAngles;
                        eulerAngles = UnityEditor.EditorGUILayout.Vector3Field("Plane Rotation", eulerAngles);
                        if (angleCheck.changed)
                        {
                            var newRotation = Quaternion.Euler(eulerAngles);
                            PWBIO.UpdateTilingRotation(newRotation);
                            settings.rotation = newRotation;
                        }
                    }
                    var axisIdx = UnityEditor.EditorGUILayout.Popup("Axis aligned with plane normal: ",
                        settings.axisAlignedWithNormal, _dirNames);
                    settings.axisAlignedWithNormal = axisIdx;
                }
                using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                {
                    UnityEditor.EditorGUIUtility.labelWidth = 76;
                    settings.cellSizeType = (TilesUtils.SizeType)
                        UnityEditor.EditorGUILayout.Popup("Cell size", (int)settings.cellSizeType, _cellTypeNames);
                    using (new UnityEditor.EditorGUI.DisabledGroupScope(
                        settings.cellSizeType != TilesUtils.SizeType.CUSTOM))
                    {
                        settings.cellSize = UnityEditor.EditorGUILayout.Vector2Field("", settings.cellSize);
                    }
                }
                using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                {
                    settings.spacing = UnityEditor.EditorGUILayout.Vector2Field("Spacing", settings.spacing);
                }
                if (check.changed)
                {
                    PWBIO.UpdateStroke();
                    UnityEditor.SceneView.RepaintAll();
                }
            }
            PaintSettingsGUI(TilingManager.settings, TilingManager.settings);
            OverwriteBrushPropertiesGUI(TilingManager.settings, ref _tilingOverwriteGroupState);
        }
    }
}
#pragma warning restore UDR0001
