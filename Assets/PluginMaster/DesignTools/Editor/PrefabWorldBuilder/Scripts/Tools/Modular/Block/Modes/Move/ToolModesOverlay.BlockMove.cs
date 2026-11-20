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
        private VisualElement CreateMoveSettings()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            container.style.alignItems = Align.FlexStart;

            // MoveSelectionMode
            var selectionModeLabel = new Label("Selection Mode:");
            selectionModeLabel.style.marginBottom = 2;
            selectionModeLabel.style.marginLeft = 4;
            container.Add(selectionModeLabel);

            var selectionModeChoices = new System.Collections.Generic.List<string> { "Current", "Face", "Box" };
            var selectionModeDropdown = new DropdownField(selectionModeChoices,
                (int)BlockToolModes.moveSelectionMode);
            selectionModeDropdown.style.width = 114;
            selectionModeDropdown.RegisterValueChangedCallback(evt =>
            {
                var index = selectionModeChoices.IndexOf(evt.newValue);
                BlockToolModes.moveSelectionMode = (BlockToolModes.MoveSelectionMode)index;
            });
            container.Add(selectionModeDropdown);

            // MoveConectivity
            var connectivityLabel = new Label("Connectivity:");
            connectivityLabel.style.marginTop = 4;
            connectivityLabel.style.marginBottom = 2;
            connectivityLabel.style.marginLeft = 4;
            container.Add(connectivityLabel);

            var connectivityChoices = new System.Collections.Generic.List<string> { "Prefab", "Geometry" };
            var connectivityDropdown = new DropdownField(connectivityChoices,
                (int)BlockToolModes.moveConectivity);
            connectivityDropdown.style.width = 114;
            connectivityDropdown.RegisterValueChangedCallback(evt =>
            {
                var index = connectivityChoices.IndexOf(evt.newValue);
                BlockToolModes.moveConectivity = (BlockToolModes.MoveConectivity)index;
            });
            container.Add(connectivityDropdown);

            // MoveNeighborSearchingDirections
            var directionsLabel = new Label("Directions:");
            directionsLabel.style.marginTop = 4;
            directionsLabel.style.marginBottom = 2;
            directionsLabel.style.marginLeft = 4;
            container.Add(directionsLabel);

            var directionsChoices = new System.Collections.Generic.List<string> { "4", "8" };
            var directionsDropdown = new DropdownField(directionsChoices,
                (int)BlockToolModes.moveNeighborSearchingDirections);
            directionsDropdown.style.width = 114;
            directionsDropdown.RegisterValueChangedCallback(evt =>
            {
                var index = directionsChoices.IndexOf(evt.newValue);
                BlockToolModes.moveNeighborSearchingDirections = (BlockToolModes.MoveNeighborSearchingDirections)index;
            });
            container.Add(directionsDropdown);

            return container;
        }
    }
}
#endif
#pragma warning restore UDR0001
