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
using System.Linq;
using UnityEngine;

namespace PluginMaster
{
    public partial class ToolProperties : UnityEditor.EditorWindow
    {
        private static readonly string[] _heightTypeNames = { "Custom", "Radius" };
        private static readonly string[] _avoidOverlappingTypeNames = { "Disabled", "With Palette Prefabs",
            "With Brush Prefabs", "With Same Prefabs", "With All Objects" };

        
        private static BrushPropertiesGroupState _brushOverwriteGroupState;
        private void BrushGroup()
        {
            ToolProfileGUI(BrushManager.instance);
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                BrushManager.settings.showPreview = UnityEditor.EditorGUILayout.ToggleLeft("Show Brushstroke Preview",
                    BrushManager.settings.showPreview);
                if (BrushManager.settings.showPreview)
                    UnityEditor.EditorGUILayout.HelpBox("The brushstroke preview can cause slowdown issues.",
                        UnityEditor.MessageType.Info);
                UnityEditor.EditorGUILayout.LabelField("Brushstroke triangle count",
                    BrushstrokeManager.triangleCount.ToString());
            }
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                BrushToolBaseSettingsGUI(BrushManager.settings);
                UnityEditor.EditorGUIUtility.labelWidth = 150;
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var avoidOverlapping = (BrushToolSettings.AvoidOverlappingType)
                        UnityEditor.EditorGUILayout.Popup("Avoid Overlapping",
                        (int)BrushManager.settings.avoidOverlapping, _avoidOverlappingTypeNames);
                    if (check.changed)
                    {
                        BrushManager.settings.avoidOverlapping = avoidOverlapping;
                    }
                }
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var overlapPadding = Mathf.Abs(UnityEditor.EditorGUILayout.FloatField("Overlap Padding",
                        BrushManager.settings.overlapPadding));
                    if (check.changed)
                    {
                        BrushManager.settings.overlapPadding = overlapPadding;
                    }
                }
                if (BrushManager.settings.brushShape != BrushToolBase.BrushShape.POINT)
                {
                    using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                    {
                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            var heightType = (BrushToolSettings.HeightType)
                                UnityEditor.EditorGUILayout.Popup("Max Height From center",
                                (int)BrushManager.settings.heightType, _heightTypeNames);
                            if (check.changed)
                            {
                                BrushManager.settings.heightType = heightType;
                                if (heightType == BrushToolSettings.HeightType.RADIUS)
                                    BrushManager.settings.maxHeightFromCenter = BrushManager.settings.radius;
                                UnityEditor.SceneView.RepaintAll();
                            }
                        }
                        using (new UnityEditor.EditorGUI.DisabledGroupScope(
                            BrushManager.settings.heightType == BrushToolSettings.HeightType.RADIUS))
                        {
                            using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                            {
                                var maxHeightFromCenter = Mathf.Abs(UnityEditor.EditorGUILayout.FloatField("Value",
                                    BrushManager.settings.maxHeightFromCenter));
                                if (check.changed)
                                {
                                    BrushManager.settings.maxHeightFromCenter = maxHeightFromCenter;
                                    UnityEditor.SceneView.RepaintAll();
                                }
                            }
                        }
                    }
                }
            }

            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                GUILayout.Label("Surface Filters", UnityEditor.EditorStyles.boldLabel);
                UnityEditor.EditorGUIUtility.labelWidth = 110;
                using (new GUILayout.HorizontalScope())
                {
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var minSlope = BrushManager.settings.slopeFilter.min;
                        var maxSlope = BrushManager.settings.slopeFilter.max;
                        UnityEditor.EditorGUILayout.MinMaxSlider("Slope Angle", ref minSlope, ref maxSlope, 0, 90);
                        minSlope = Mathf.Round(minSlope);
                        maxSlope = Mathf.Round(maxSlope);
                        GUILayout.Label("[" + minSlope.ToString("00") + "�," + maxSlope.ToString("00") + "�]");
                        if (check.changed)
                        {
                            BrushManager.settings.slopeFilter.v1 = minSlope;
                            BrushManager.settings.slopeFilter.v2 = maxSlope;
                            UnityEditor.SceneView.RepaintAll();
                        }
                    }
                }

                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var mask = UnityEditor.EditorGUILayout.MaskField("Layers",
                        EditorGUIUtils.LayerMaskToField(BrushManager.settings.layerFilter),
                        UnityEditorInternal.InternalEditorUtility.layers);
                    if (check.changed)
                    {
                        BrushManager.settings.layerFilter = EditorGUIUtils.FieldToLayerMask(mask);
                        UnityEditor.SceneView.RepaintAll();
                    }
                }

                UnityEditor.EditorGUIUtility.labelWidth = 108;
                var field = EditorGUIUtils.MultiTagField.Instantiate("Tags", BrushManager.settings.tagFilter, null);
                field.OnChange += OnBrushTagFilterChanged;

                bool terrainFilterChanged = false;
                var terrainFilter = EditorGUIUtils.ObjectArrayFieldWithButtons("Terrain Layers",
                    BrushManager.settings.terrainLayerFilter, ref _terrainLayerFilterFoldout, out terrainFilterChanged);
                if (terrainFilterChanged)
                {
                    BrushManager.settings.terrainLayerFilter = terrainFilter.ToArray();
                    UnityEditor.SceneView.RepaintAll();
                }
            }
            PaintSettingsGUI(BrushManager.settings, BrushManager.settings);
            OverwriteBrushPropertiesGUI(BrushManager.settings, ref _brushOverwriteGroupState);
        }


        private bool _terrainLayerFilterFoldout = false;

        private void OnBrushTagFilterChanged(System.Collections.Generic.List<string> prevFilter,
            System.Collections.Generic.List<string> newFilter, string key)
        {

            BrushManager.settings.tagFilter = newFilter;
        }
    }
}
#pragma warning restore UDR0001
