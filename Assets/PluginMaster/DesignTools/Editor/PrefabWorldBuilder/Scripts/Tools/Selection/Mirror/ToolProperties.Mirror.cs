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
        private static readonly string[] _mirrorActionNames = { "Transform", "Create" };
        private void MirrorGroup()
        {
            ToolProfileGUI(MirrorManager.instance);
            using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
            {
                var mirrorSettings = new MirrorSettings();
                mirrorSettings.Copy(MirrorManager.settings);
                using (var mirrorCheck = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                    {
                        UnityEditor.EditorGUIUtility.labelWidth = 80;
                        mirrorSettings.mirrorPosition = UnityEditor.EditorGUILayout.Vector3Field("Position",
                            mirrorSettings.mirrorPosition);
                        mirrorSettings.mirrorRotation = Quaternion.Euler(UnityEditor.EditorGUILayout.Vector3Field("Rotation",
                            mirrorSettings.mirrorRotation.eulerAngles));
                    }
                    using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                    {
                        UnityEditor.EditorGUIUtility.labelWidth = 110;
                        mirrorSettings.invertScale
                            = UnityEditor.EditorGUILayout.ToggleLeft("Invert scale", mirrorSettings.invertScale);
                        mirrorSettings.reflectRotation
                            = UnityEditor.EditorGUILayout.ToggleLeft("Reflect rotation", mirrorSettings.reflectRotation);
                        mirrorSettings.action = (MirrorSettings.MirrorAction)UnityEditor.EditorGUILayout.Popup("Action",
                            (int)mirrorSettings.action, _mirrorActionNames);
                    }
                    if (mirrorCheck.changed) UnityEditor.SceneView.RepaintAll();
                }

                if (mirrorSettings.action == MirrorSettings.MirrorAction.CREATE)
                {
                    using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                    {
                        mirrorSettings.sameParentAsSource = UnityEditor.EditorGUILayout.ToggleLeft("Same parent as source",
                            mirrorSettings.sameParentAsSource);
                        if (!mirrorSettings.sameParentAsSource)
                        {
                            mirrorSettings.autoCreateParent
                                = UnityEditor.EditorGUILayout.ToggleLeft("Create parent", mirrorSettings.autoCreateParent);
                            if (mirrorSettings.autoCreateParent)
                                mirrorSettings.createSubparentPerPrefab
                                    = UnityEditor.EditorGUILayout.ToggleLeft("Create sub-parent per prefab",
                                    mirrorSettings.createSubparentPerPrefab);
                            else mirrorSettings.parent
                                    = (Transform)UnityEditor.EditorGUILayout.ObjectField("Parent Transform",
                                    mirrorSettings.parent, typeof(Transform), true);
                        }
                    }
                    using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                    {
                        mirrorSettings.overwritePrefabLayer = UnityEditor.EditorGUILayout.ToggleLeft("Overwrite prefab layer",
                            mirrorSettings.overwritePrefabLayer);
                        if (mirrorSettings.overwritePrefabLayer)
                            mirrorSettings.layer = UnityEditor.EditorGUILayout.LayerField("Layer", mirrorSettings.layer);
                    }
                }
                if (check.changed)
                {
                    MirrorManager.settings.Copy(mirrorSettings);
                    UnityEditor.SceneView.RepaintAll();
                }
            }
            EmbedInSurfaceSettingsGUI(MirrorManager.settings);
        }
    }
}
#pragma warning restore UDR0001
