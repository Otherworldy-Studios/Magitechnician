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
        private static readonly string[] _spaceOptions = { "Global", "Local" };
        private static readonly string[] _rotationOptions = { "First Object Selected", "Last Object Selected" };
        private static readonly string[] _extrudeSpacingOptions = { "Box Size", "Custom" };
        private static readonly string[] _addRotationOptions = { "Constant", "Random" };
        private void ExtrudeGroup()
        {
            ToolProfileGUI(ExtrudeManager.instance);
            UnityEditor.EditorGUIUtility.labelWidth = 60;
            using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
            {
                var extrudeSettings = ExtrudeManager.settings.Clone();
                using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                {
                    extrudeSettings.space = (Space)(UnityEditor.EditorGUILayout.Popup("Space",
                        (int)extrudeSettings.space, _spaceOptions));
                    if (extrudeSettings.space == Space.Self)
                    {
                        UnityEditor.EditorGUIUtility.labelWidth = 150;
                        extrudeSettings.rotationAccordingTo = (ExtrudeSettings.RotationAccordingTo)UnityEditor
                            .EditorGUILayout.Popup("Set rotation according to",
                            (int)extrudeSettings.rotationAccordingTo, _rotationOptions);
                    }
                }
                UnityEditor.EditorGUIUtility.labelWidth = 60;
                using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                {
                    extrudeSettings.spacingType = (ExtrudeSettings.SpacingType)UnityEditor.EditorGUILayout.Popup("Spacing",
                        (int)extrudeSettings.spacingType, _extrudeSpacingOptions);
                    if (extrudeSettings.spacingType == ExtrudeSettings.SpacingType.BOX_SIZE)
                        extrudeSettings.multiplier
                            = UnityEditor.EditorGUILayout.Vector3Field("Multiplier", extrudeSettings.multiplier);
                    else extrudeSettings.spacing
                            = UnityEditor.EditorGUILayout.Vector3Field("Value", extrudeSettings.spacing);
                }
                if (extrudeSettings.space == Space.World)
                {
                    using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                    {
                        UnityEditor.EditorGUIUtility.labelWidth = 80;
                        extrudeSettings.addRandomRotation = UnityEditor.EditorGUILayout.Popup("Add Rotation",
                            extrudeSettings.addRandomRotation ? 1 : 0, _addRotationOptions) == 1;
                        if (extrudeSettings.addRandomRotation)
                        {
                            extrudeSettings.randomEulerOffset = EditorGUIUtils.Range3Field(string.Empty,
                                extrudeSettings.randomEulerOffset);
                            using (new GUILayout.HorizontalScope())
                            {
                                extrudeSettings.rotateInMultiples = UnityEditor.EditorGUILayout.ToggleLeft
                                    ("Only in multiples of", extrudeSettings.rotateInMultiples);
                                using (new UnityEditor.EditorGUI.DisabledGroupScope(!extrudeSettings.rotateInMultiples))
                                    extrudeSettings.rotationFactor
                                        = UnityEditor.EditorGUILayout.FloatField(extrudeSettings.rotationFactor);
                            }
                        }
                        else extrudeSettings.eulerOffset = UnityEditor.EditorGUILayout.Vector3Field(string.Empty,
                            extrudeSettings.eulerOffset);
                    }
                }

                using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                {
                    extrudeSettings.sameParentAsSource
                        = UnityEditor.EditorGUILayout.ToggleLeft("Same parent as source", extrudeSettings.sameParentAsSource);
                    if (!extrudeSettings.sameParentAsSource)
                    {
                        extrudeSettings.autoCreateParent
                            = UnityEditor.EditorGUILayout.ToggleLeft("Create parent", extrudeSettings.autoCreateParent);
                        if (extrudeSettings.autoCreateParent) extrudeSettings.createSubparentPerPrefab
                                = UnityEditor.EditorGUILayout.ToggleLeft("Create sub-parent per prefab",
                                extrudeSettings.createSubparentPerPrefab);
                        else extrudeSettings.parent = (Transform)UnityEditor.EditorGUILayout.ObjectField("Parent Transform",
                                extrudeSettings.parent, typeof(Transform), true);
                    }
                }
                using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                {
                    extrudeSettings.overwritePrefabLayer
                        = UnityEditor.EditorGUILayout.ToggleLeft("Overwrite prefab layer",
                        extrudeSettings.overwritePrefabLayer);
                    if (extrudeSettings.overwritePrefabLayer)
                        extrudeSettings.layer = UnityEditor.EditorGUILayout.LayerField("Layer", extrudeSettings.layer);
                }

                if (check.changed)
                {
                    ExtrudeManager.settings.Copy(extrudeSettings);
                    UnityEditor.SceneView.RepaintAll();
                    PWBIO.ClearExtrudeAngles();
                }
            }
            EmbedInSurfaceSettingsGUI(ExtrudeManager.settings);
        }
    }
}
#pragma warning restore UDR0001
