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
        private static readonly string[] _shapeTypeNames = { "Circle", "Polygon" };
        
        private static BrushPropertiesGroupState _shapeOverwriteGroupState;
        private static readonly string[] _shapeDirNames = new string[] { "+X", "-X", "+Y", "-Y", "+Z", "-Z", "Normal to surface" };
        private void ShapeGroup()
        {
            UnityEditor.EditorGUIUtility.labelWidth = 100;
            ToolProfileGUI(ShapeManager.instance);
            EditModeToggle(ShapeManager.instance);
            HandlePosition();
            HandleRotation();
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var shapeType = (ShapeSettings.ShapeType)UnityEditor.EditorGUILayout.Popup("Shape",
                        (int)ShapeManager.settings.shapeType, _shapeTypeNames);
                    if (check.changed)
                    {
                        ShapeManager.settings.shapeType = shapeType;
                        if (shapeType == ShapeSettings.ShapeType.CIRCLE)
                        {
                            ShapeData.instance.UpdateCircleSideCount();
                        }
                        ShapeData.instance.Update(true);
                        PWBIO.UpdateStroke();
                        PWBIO.repaint = true;
                    }
                }
                if (ShapeManager.settings.shapeType == ShapeSettings.ShapeType.POLYGON)
                {
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var sideCount = UnityEditor.EditorGUILayout.IntSlider("Number of sides",
                            ShapeManager.settings.sidesCount, 3, 12);
                        if (check.changed)
                        {
                            ShapeManager.settings.sidesCount = sideCount;
                            ShapeData.instance.UpdateIntersections();
                            PWBIO.UpdateStroke();
                            PWBIO.repaint = true;
                        }
                    }
                }

                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    UnityEditor.EditorGUIUtility.labelWidth = 120;
                    var initialNormaldir = UnityEditor.EditorGUILayout.Popup("Initial axis direction",
                        (int)ShapeManager.settings.initialPlaneNormalDirection, _shapeDirNames);
                    if (check.changed)
                    {
                        ShapeManager.settings.initialPlaneNormalDirection = (ShapeSettings.NormalDirection)initialNormaldir;
                        PWBIO.UpdateStroke();
                        PWBIO.repaint = true;
                    }
                }
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var radius = UnityEditor.EditorGUILayout.FloatField("Radius", ShapeData.instance.radius);
                    if (check.changed)
                    {
                        ShapeData.instance.radius = Mathf.Max(0, radius);
                        ShapeData.instance.Update(clearSelection: false);
                        PWBIO.repaint = true;
                    }
                }
            }
            UnityEditor.EditorGUIUtility.labelWidth = 120;
            LineBaseGUI(ShapeManager.settings);
            PaintSettingsGUI(ShapeManager.settings, ShapeManager.settings);
            OverwriteBrushPropertiesGUI(ShapeManager.settings, ref _shapeOverwriteGroupState);
        }
    }
}
#pragma warning restore UDR0001
