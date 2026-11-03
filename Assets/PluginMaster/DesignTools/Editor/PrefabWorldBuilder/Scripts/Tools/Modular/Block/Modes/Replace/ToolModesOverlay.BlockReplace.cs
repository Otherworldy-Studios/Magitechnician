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
        private VisualElement CreateReplaceSettings()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            container.style.alignItems = Align.FlexStart;

            var replaceModeLabel = new Label("Replace Mode:");
            replaceModeLabel.style.marginBottom = 2;
            replaceModeLabel.style.marginLeft = 4;
            container.Add(replaceModeLabel);

            var replaceModeChoices = new System.Collections.Generic.List<string> { "Single", "Region" };
            var replaceModeDropdown = new DropdownField(replaceModeChoices,
                (int)BlockToolModes.replaceMode);
            replaceModeDropdown.style.width = 114;
            container.Add(replaceModeDropdown);

            var isRegion = BlockToolModes.replaceMode == BlockToolModes.ReplaceMode.REGION;
            var regionDisplay = isRegion ? DisplayStyle.Flex : DisplayStyle.None;

            // ReplaceSelectionMode
            var selectionModeLabel = new Label("Selection Mode:");
            selectionModeLabel.style.marginTop = 4;
            selectionModeLabel.style.marginBottom = 2;
            selectionModeLabel.style.marginLeft = 4;
            selectionModeLabel.style.display = regionDisplay;
            container.Add(selectionModeLabel);

            var selectionModeChoices = new System.Collections.Generic.List<string> { "Current", "Face", "Box" };
            var selectionModeDropdown = new DropdownField(selectionModeChoices,
                (int)BlockToolModes.replaceSelectionMode);
            selectionModeDropdown.style.width = 114;
            selectionModeDropdown.style.display = regionDisplay;
            selectionModeDropdown.RegisterValueChangedCallback(evt =>
            {
                var index = selectionModeChoices.IndexOf(evt.newValue);
                BlockToolModes.replaceSelectionMode = (BlockToolModes.ReplaceSelectionMode)index;
            });
            container.Add(selectionModeDropdown);

            var connectivityLabel = new Label("Connectivity:");
            connectivityLabel.style.marginTop = 4;
            connectivityLabel.style.marginBottom = 2;
            connectivityLabel.style.marginLeft = 4;
            connectivityLabel.style.display = regionDisplay;
            container.Add(connectivityLabel);

            var connectivityChoices = new System.Collections.Generic.List<string> { "Prefab", "Geometry" };
            var connectivityDropdown = new DropdownField(connectivityChoices,
                (int)BlockToolModes.replaceRegionConectivity);
            connectivityDropdown.style.width = 114;
            connectivityDropdown.style.display = regionDisplay;
            connectivityDropdown.RegisterValueChangedCallback(evt =>
            {
                var index = connectivityChoices.IndexOf(evt.newValue);
                BlockToolModes.replaceRegionConectivity
                    = (BlockToolModes.ReplaceRegionConectivity)index;
            });
            container.Add(connectivityDropdown);

            var directionsLabel = new Label("Directions:");
            directionsLabel.style.marginTop = 4;
            directionsLabel.style.marginBottom = 2;
            directionsLabel.style.marginLeft = 4;
            directionsLabel.style.display = regionDisplay;
            container.Add(directionsLabel);

            var directionsChoices = new System.Collections.Generic.List<string> { "4", "8" };
            var directionsDropdown = new DropdownField(directionsChoices,
                (int)BlockToolModes.replaceRegionSelectNeighborSearchingDirections);
            directionsDropdown.style.width = 114;
            directionsDropdown.style.display = regionDisplay;
            directionsDropdown.RegisterValueChangedCallback(evt =>
            {
                var index = directionsChoices.IndexOf(evt.newValue);
                BlockToolModes.replaceRegionSelectNeighborSearchingDirections
                    = (BlockToolModes.ReplaceRegionSelectNeighborSearchingDirections)index;
            });
            container.Add(directionsDropdown);

            replaceModeDropdown.RegisterValueChangedCallback(evt =>
            {
                var index = replaceModeChoices.IndexOf(evt.newValue);
                BlockToolModes.replaceMode = (BlockToolModes.ReplaceMode)index;
                var display = BlockToolModes.replaceMode == BlockToolModes.ReplaceMode.REGION
                    ? DisplayStyle.Flex : DisplayStyle.None;
                selectionModeLabel.style.display = display;
                selectionModeDropdown.style.display = display;
                connectivityLabel.style.display = display;
                connectivityDropdown.style.display = display;
                directionsLabel.style.display = display;
                directionsDropdown.style.display = display;
            });

            return container;
        }
    }
}
#endif
#pragma warning restore UDR0001
