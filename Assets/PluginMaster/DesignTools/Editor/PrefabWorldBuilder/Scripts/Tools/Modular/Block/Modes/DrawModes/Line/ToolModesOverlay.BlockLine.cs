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
using UnityEditor.Overlays;
using UnityEngine.UIElements;

namespace PluginMaster
{
    public partial class ToolModesOverlay : Overlay
    {
        private VisualElement CreateLineBrushSettings()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Column;
            row.style.alignItems = Align.FlexStart;

            var label = new Label("Projection:");
            label.style.marginBottom = 2;
            label.style.marginLeft = 4;
            row.Add(label);

            var choices = new System.Collections.Generic.List<string>
                { "None", "Camera", "Down", "Up", "Back", "Forward", "Left", "Right" };
            var dropdown = new DropdownField(choices, (int)BlockToolModes.projectionAxis);
            dropdown.style.width = 114;
            dropdown.RegisterValueChangedCallback(evt =>
            {
                var index = choices.IndexOf(evt.newValue);
                BlockToolModes.projectionAxis = (BlockToolModes.ProjectionAxis)index;
                UnityEditor.SceneView.RepaintAll();
            });
            row.Add(dropdown);

            return row;
        }
    }
}
#endif
#pragma warning restore UDR0001
