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
        private void SelectionGroup()
        {
            ToolProfileGUI(SelectionToolController.instance);
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    UnityEditor.EditorGUIUtility.labelWidth = 90;
                    var handleSpace = (Space)(UnityEditor.EditorGUILayout.Popup("Handle Space",
                        (int)SelectionToolController.settings.handleSpace, _spaceOptions));
                    if (SelectionManager.topLevelSelection.Length > 1) SelectionToolController.settings.boxSpace = Space.World;
                    var boxSpace = SelectionToolController.settings.boxSpace;
                    using (new UnityEditor.EditorGUI.DisabledGroupScope(SelectionManager.topLevelSelection.Length > 1))
                    {
                        boxSpace = (Space)(UnityEditor.EditorGUILayout.Popup("Box Space",
                            (int)SelectionToolController.settings.boxSpace, _spaceOptions));
                    }
                    if (check.changed)
                    {
                        SelectionToolController.settings.handleSpace = handleSpace;
                        SelectionToolController.settings.boxSpace = boxSpace;
                        PWBIO.ResetSelectionRotation();
                        UnityEditor.SceneView.RepaintAll();
                    }
                }
            }
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                GUILayout.Label("Selection Filters", UnityEditor.EditorStyles.boldLabel);
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    UnityEditor.EditorGUIUtility.labelWidth = 90;
                    var paletteFilter = UnityEditor.EditorGUILayout.ToggleLeft("Prefabs from selected palette only",
                        SelectionToolController.settings.paletteFilter);
                    var brushFilter = UnityEditor.EditorGUILayout.ToggleLeft("Prefabs from selected brush only",
                        SelectionToolController.settings.brushFilter);
                    var layerMask = UnityEditor.EditorGUILayout.MaskField("Layers",
                        EditorGUIUtils.LayerMaskToField(SelectionToolController.settings.layerFilter),
                        UnityEditorInternal.InternalEditorUtility.layers);
                    var tagField = EditorGUIUtils.MultiTagField.Instantiate("Tags",
                        SelectionToolController.settings.tagFilter, null);
                    tagField.OnChange += OnSelectionTagFilterChanged;
                    if (check.changed)
                    {
                        SelectionToolController.settings.paletteFilter = paletteFilter;
                        SelectionToolController.settings.brushFilter = brushFilter;
                        SelectionToolController.settings.layerFilter = EditorGUIUtils.FieldToLayerMask(layerMask);
                        PWBIO.ApplySelectionFilters();
                        UnityEditor.SceneView.RepaintAll();
                    }
                }
            }
            EmbedInSurfaceSettingsGUI(SelectionToolController.settings);
        }

        private void OnSelectionTagFilterChanged(System.Collections.Generic.List<string> prevFilter,
            System.Collections.Generic.List<string> newFilter, string key)
        {

            SelectionToolController.settings.tagFilter = newFilter;
            PWBIO.ApplySelectionFilters();
            UnityEditor.SceneView.RepaintAll();
        }
    }
}
#pragma warning restore UDR0001
