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
#if UNITY_2021_2_OR_NEWER
using UnityEngine;
using UnityEditor.Overlays;
using UnityEngine.UIElements;

namespace PluginMaster
{
    [Overlay(typeof(UnityEditor.SceneView), "PWB/Tool Modes", true)]
    public partial class ToolModesOverlay : Overlay
    {
        
        private static ToolModesOverlay _instance;

        public ToolModesOverlay()
        {
            _instance = this;
            CreatePanelContent();
        }

        public static void ShowWindow()
        {
            if (_instance != null)
                _instance.displayed = true;
        }
        public static void SetDisplayed(bool value)
        {
            if (_instance != null)
                _instance.displayed = value;
        }
        public static bool hasInstance => _instance != null;
        public static bool isDisplayed => _instance != null && _instance.displayed;
        public override VisualElement CreatePanelContent()
        {
            var root = new VisualElement { name = "Tool Modes Panel" };
#if UNITY_2022_2_OR_NEWER
            collapsedIcon = Resources.Load<Texture2D>($"{ToggleManager.iconPath}ToolModes");
#endif

            switch (ToolController.current)
            {
                case ToolController.Tool.BLOCK:
                    return CreateBlockModes(root);
#if PWB_FLOOR_TOOL_MODES
                case ToolController.Tool.FLOOR:
                    return CreateFloorModes(root);
#endif
                default:
                     return root;
            }
        }
        public static string iconPath => UnityEditor.EditorGUIUtility.isProSkin ? "Sprites/" : "Sprites/LightTheme/";
        private async void DoLoadIconForButton(Button button, string iconName)
        {
            await System.Threading.Tasks.Task.Delay(1000);
            var texture = Resources.Load<Texture2D>($"{iconPath}{iconName}");
            if (texture != null)
            {
                button.style.backgroundImage = new StyleBackground(texture);
            }
            else
            {
                DoLoadIconForButton(button, iconName);
            }
        }
    }
}
#endif
#pragma warning restore UDR0001
