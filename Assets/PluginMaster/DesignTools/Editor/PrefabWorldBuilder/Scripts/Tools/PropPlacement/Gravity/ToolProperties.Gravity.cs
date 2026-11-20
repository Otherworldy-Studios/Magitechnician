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
        
        private static BrushPropertiesGroupState _gravityOverwriteGroupState;

        private static readonly string[] _tempCollidersActionNames = { "Disabled",
            "In scene", "In camera" };
        private void GravityGroup()
        {
            ToolProfileGUI(GravityToolController.instance);
            BrushToolBaseSettingsGUI(GravityToolController.settings);
            UnityEditor.EditorGUIUtility.labelWidth = 120;
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                var settings = GravityToolController.settings.Clone();
                var data = settings.simData;
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    settings.height = UnityEditor.EditorGUILayout.FloatField("Height", settings.height);
                    data.maxIterations = UnityEditor.EditorGUILayout.IntField("Max Iterations", data.maxIterations);
                    data.maxSpeed = UnityEditor.EditorGUILayout.FloatField("Max Speed", data.maxSpeed);
                    data.maxAngularSpeed = UnityEditor.EditorGUILayout.FloatField("Max Angular Speed", data.maxAngularSpeed);
                    data.mass = UnityEditor.EditorGUILayout.FloatField("Mass", data.mass);
                    data.drag = UnityEditor.EditorGUILayout.FloatField("Drag", data.drag);
                    data.angularDrag = UnityEditor.EditorGUILayout.FloatField("Angular Drag", data.angularDrag);
                    if (check.changed)
                    {
                        GravityToolController.settings.Copy(settings);
                        UnityEditor.SceneView.RepaintAll();
                    }
                }

                using (new GUILayout.HorizontalScope())
                {
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        UnityEditor.EditorGUIUtility.labelWidth = 130;
                        var tempColAction = (GravityToolSettings.TempCollidersAction)
                            UnityEditor.EditorGUILayout.Popup("Create Tempcolliders",
                            (int)GravityToolController.settings.tempCollidersAction, _tempCollidersActionNames);
                        if (check.changed)
                        {
                            GravityToolController.settings.tempCollidersAction = tempColAction;
                            PWBCore.UpdateTempColliders();
                            UnityEditor.SceneView.RepaintAll();
                        }
                    }

                    using (new UnityEditor.EditorGUI.DisabledGroupScope(!GravityToolController.settings.createTempColliders))
                        if (GUILayout.Button(_updateButtonContent, reloadBtnStyle))
                            PWBCore.UpdateTempColliders();

                }
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    data.ignoreSceneColliders = UnityEditor.EditorGUILayout.ToggleLeft("Ignore Scene Colliders",
                            data.ignoreSceneColliders);
                    using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                    {
                        data.changeLayer
                            = UnityEditor.EditorGUILayout.ToggleLeft("Change Layer Temporarily", data.changeLayer);
                        if (data.changeLayer)
                            data.tempLayer = UnityEditor.EditorGUILayout.LayerField("Temp layer", data.tempLayer);
                    }
                    if (check.changed)
                    {
                        GravityToolController.settings.Copy(settings);
                        UnityEditor.SceneView.RepaintAll();
                    }
                }
            }
            PaintToolSettingsGUI(GravityToolController.settings);
            OverwriteBrushPropertiesGUI(GravityToolController.settings, ref _gravityOverwriteGroupState);
        }
    }
}
#pragma warning restore UDR0001
