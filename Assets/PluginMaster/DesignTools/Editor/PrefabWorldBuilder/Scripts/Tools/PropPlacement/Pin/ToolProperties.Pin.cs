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
        private static readonly string[] _pinModeNames = { "Auto", "Paint on surface", "Paint on grid" };

        
        private static BrushPropertiesGroupState _pinOverwriteGroupState;
        private void PinGroup()
        {
            ToolProfileGUI(PinManager.instance);
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var mode = (PinSettings.PaintMode)UnityEditor.EditorGUILayout.Popup("Paint mode",
                        (int)PinManager.settings.mode, _pinModeNames);
                    if (check.changed)
                    {
                        PinManager.settings.mode = mode;
                        UnityEditor.SceneView.RepaintAll();
                    }
                }
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var repeat = UnityEditor.EditorGUILayout.ToggleLeft("Repeat multi-brush item", PinManager.settings.repeat);
                    if (check.changed)
                    {
                        PinManager.settings.repeat = repeat;
                        UnityEditor.SceneView.RepaintAll();
                    }
                }
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var avoidOverlapping = UnityEditor.EditorGUILayout.ToggleLeft("Avoid overlapping",
                        PinManager.settings.avoidOverlapping);
                    if (check.changed)
                    {
                        PinManager.settings.avoidOverlapping = avoidOverlapping;
                        UnityEditor.SceneView.RepaintAll();
                    }
                }
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var snapRotationToGrid = UnityEditor.EditorGUILayout.ToggleLeft("Snap rotation to grid",
                        PinManager.settings.snapRotationToGrid);
                    if (check.changed)
                    {
                        PinManager.settings.snapRotationToGrid = snapRotationToGrid;
                        UnityEditor.SceneView.RepaintAll();
                    }
                }
            }
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                UnityEditor.EditorGUIUtility.labelWidth = 60;
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var flattenTerrain
                        = UnityEditor.EditorGUILayout.ToggleLeft("Flatten the terrain", PinManager.settings.flattenTerrain);
                    if (check.changed)
                    {
                        PinManager.settings.flattenTerrain = flattenTerrain;
                    }
                }
                using (new UnityEditor.EditorGUI.DisabledGroupScope(!PinManager.settings.flattenTerrain))
                {
                    var flatteningSettings = PinManager.settings.flatteningSettings;
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var hardness = UnityEditor.EditorGUILayout.Slider("Hardness", flatteningSettings.hardness, 0, 1);
                        if (check.changed)
                        {
                            flatteningSettings.hardness = hardness;
                        }
                    }
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var padding = UnityEditor.EditorGUILayout.FloatField("Padding", flatteningSettings.padding);
                        if (check.changed)
                        {
                            flatteningSettings.padding = padding;
                        }
                    }
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var clearTrees = UnityEditor.EditorGUILayout.ToggleLeft("Clear trees", flatteningSettings.clearTrees);
                        if (check.changed)
                        {
                            flatteningSettings.clearTrees = clearTrees;
                        }
                    }
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var clearDetails
                            = UnityEditor.EditorGUILayout.ToggleLeft("Clear details", flatteningSettings.clearDetails);
                        if (check.changed)
                        {
                            flatteningSettings.clearDetails = clearDetails;
                        }
                    }
                }
            }
            PaintSettingsGUI(PinManager.settings, PinManager.settings);
            OverwriteBrushPropertiesGUI(PinManager.settings, ref _pinOverwriteGroupState);
        }
    }
}
#pragma warning restore UDR0001
