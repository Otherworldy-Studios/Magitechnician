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
        private void EraserGroup()
        {
            UnityEditor.EditorGUIUtility.labelWidth = 60;
            var settings = EraserManager.settings;
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox)) RadiusSlider(settings);
            var actionLabel = "Erase";
            SelectionBrushGroup(settings, actionLabel);
            ModifierGroup(settings, actionLabel);
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var outermostFilter = UnityEditor.EditorGUILayout.ToggleLeft("Outermost prefab filter",
                        settings.outermostPrefabFilter);
                    if (check.changed)
                    {
                        settings.outermostPrefabFilter = outermostFilter;
                    }
                }
                if (!settings.outermostPrefabFilter)
                    GUILayout.Label("When you delete a child of a prefab, the prefab will be unpacked.",
                        UnityEditor.EditorStyles.helpBox);
            }
        }
    }
}
#pragma warning restore UDR0001
