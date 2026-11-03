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
        private VisualElement CreateFaceBrushSettings()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            container.style.alignItems = Align.FlexStart;

            // FaceConectivity
            var conectivityLabel = new Label("Conectivity:");
            conectivityLabel.style.marginBottom = 2;
            conectivityLabel.style.marginLeft = 4;
            container.Add(conectivityLabel);

            var conectivityChoices = new System.Collections.Generic.List<string> { "Prefab", "Geometry" };
            var conectivityDropdown = new DropdownField(conectivityChoices,
                (int)BlockToolModes.faceConectivity);
            conectivityDropdown.style.width = 114;
            conectivityDropdown.RegisterValueChangedCallback(evt =>
            {
                var index = conectivityChoices.IndexOf(evt.newValue);
                BlockToolModes.faceConectivity = (BlockToolModes.FaceConectivity)index;
            });
            container.Add(conectivityDropdown);

            // FaceNeighborSearchingDirections
            var directionsLabel = new Label("Directions:");
            directionsLabel.style.marginTop = 4;
            directionsLabel.style.marginBottom = 2;
            directionsLabel.style.marginLeft = 4;
            container.Add(directionsLabel);

            var directionsChoices = new System.Collections.Generic.List<string> { "4", "8" };
            var directionsDropdown = new DropdownField(directionsChoices,
                (int)BlockToolModes.faceNeighborSearchingDirections);
            directionsDropdown.style.width = 114;
            directionsDropdown.RegisterValueChangedCallback(evt =>
            {
                var index = directionsChoices.IndexOf(evt.newValue);
                BlockToolModes.faceNeighborSearchingDirections = (BlockToolModes.FaceNeighborSearchingDirections)index;
            });
            container.Add(directionsDropdown);

            return container;
        }
    }
}
#endif
#pragma warning restore UDR0001
