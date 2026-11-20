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
        
        private static BrushPropertiesGroupState _replacerOverwriteGroupState;
        private static readonly string[] _replacerModeOptions = { "Target Center", "Target Pivot", "On Surface" };
        private void ReplacerGroup()
        {
            UnityEditor.EditorGUIUtility.labelWidth = 60;
            var settings = ReplacerManager.settings;
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox)) RadiusSlider(settings);
            var actionLabel = "Replace";
            SelectionBrushGroup(settings, actionLabel);
            ModifierGroup(settings, actionLabel);
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var positionMode = (ReplacerSettings.PositionMode)UnityEditor.EditorGUILayout.Popup("Position",
                        (int)settings.positionMode, _replacerModeOptions);
                    if (check.changed)
                    {
                        settings.positionMode = positionMode;
                    }
                }
                var keepTargetSize = settings.keepTargetSize;
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    keepTargetSize = UnityEditor.EditorGUILayout.ToggleLeft("Keep target size", settings.keepTargetSize);

                    if (check.changed)
                    {
                        settings.keepTargetSize = keepTargetSize;
                    }
                }
                if (keepTargetSize)
                {
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var maintainProportions = UnityEditor.EditorGUILayout.ToggleLeft("Maintain proportions",
                            settings.maintainProportions);
                        if (check.changed)
                        {
                            settings.maintainProportions = maintainProportions;
                        }
                    }
                }
            }
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var outermostFilter = UnityEditor.EditorGUILayout.ToggleLeft("Outermost prefab filter",
                        settings.outermostPrefabFilter);
                    if (check.changed) settings.outermostPrefabFilter = outermostFilter;
                }
                if (!settings.outermostPrefabFilter)
                    GUILayout.Label("When you replace a child of a prefab, the prefab will be unpacked.",
                        UnityEditor.EditorStyles.helpBox);
            }
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                settings.sameParentAsTarget = UnityEditor.EditorGUILayout.ToggleLeft("Same Parent as the target",
                    settings.sameParentAsTarget);
                if (!settings.sameParentAsTarget) ParentSettingsGUI(ReplacerManager.settings);
            }
            OverwriteLayerGUI(ReplacerManager.settings);
            OverwriteBrushPropertiesGUI(ReplacerManager.settings, ref _replacerOverwriteGroupState);
            using (new GUILayout.HorizontalScope(UnityEditor.EditorStyles.helpBox))
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Replace all selected"))
                {
                    PWBIO.ReplaceAllSelected();
                    UnityEditor.SceneView.RepaintAll();
                }
                GUILayout.FlexibleSpace();
            }
        }
    }
}
#pragma warning restore UDR0001
