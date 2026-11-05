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
using UnityEngine;
using UnityEngine.UIElements;

namespace PluginMaster
{
    public partial class ToolModesOverlay : Overlay
    {
        private VisualElement _selectModesRow;
        private void UpdateSelectModesRowVisibility()
        {
            if (_selectModesRow == null) return;

            bool shouldShow = BlockToolModes.selectedEditMode == BlockToolModes.EditMode.SELECT;

            _selectModesRow.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private VisualElement CreateSelectModesButtonRow((string imageName,
           string tooltip, BlockToolModes.SelecMode selectMode)[] buttons)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 2;
            row.style.marginLeft = 15;

            foreach (var buttonData in buttons)
            {
                var button = new Button(() =>
                {
                    BlockToolModes.selectMode = buttonData.selectMode;
                    UnityEditor.SceneView.RepaintAll();
                })
                {
                    tooltip = buttonData.tooltip
                };

                button.style.width = 24;
                button.style.height = 24;
                button.style.marginLeft = 3;
                button.style.marginRight = 3;

                var texture = Resources.Load<Texture2D>($"{iconPath}{buttonData.imageName}");
                if (texture != null)
                {
                    button.style.backgroundImage = new StyleBackground(texture);
                }
                else
                {
                    DoLoadIconForButton(button, buttonData.imageName);
                }

                button.schedule.Execute(() =>
                {
                    if (BlockToolModes.selectMode == buttonData.selectMode)
                    {
                        button.style.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 1f);
                    }
                    else
                    {
                        button.style.backgroundColor = StyleKeyword.Null;
                    }
                }).Every(100);

                row.Add(button);
            }

            return row;
        }

        private VisualElement CreateSelectRegionSettings()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            container.style.alignItems = Align.FlexStart;

            var connectivityLabel = new Label("Connectivity:");
            connectivityLabel.style.marginTop = 4;
            connectivityLabel.style.marginBottom = 2;
            connectivityLabel.style.marginLeft = 4;
            container.Add(connectivityLabel);

            var connectivityChoices = new System.Collections.Generic.List<string> { "Prefab", "Geometry" };
            var connectivityDropdown = new DropdownField(connectivityChoices,
                (int)BlockToolModes.regionConectivity);
            connectivityDropdown.style.width = 114;
            connectivityDropdown.RegisterValueChangedCallback(evt =>
            {
                var index = connectivityChoices.IndexOf(evt.newValue);
                BlockToolModes.regionConectivity = (BlockToolModes.RegionConectivity)index;
            });
            container.Add(connectivityDropdown);

            var directionsLabel = new Label("Directions:");
            directionsLabel.style.marginTop = 4;
            directionsLabel.style.marginBottom = 2;
            directionsLabel.style.marginLeft = 4;
            container.Add(directionsLabel);

            var directionsChoices = new System.Collections.Generic.List<string> { "4 Directions", "8 Directions" };
            var directionsDropdown = new DropdownField(directionsChoices,
                (int)BlockToolModes.regionSelectNeighborSearchingDirections);
            directionsDropdown.style.width = 114;
            directionsDropdown.RegisterValueChangedCallback(evt =>
            {
                var index = directionsChoices.IndexOf(evt.newValue);
                BlockToolModes.regionSelectNeighborSearchingDirections
                    = (BlockToolModes.RegionSelectNeighborSearchingDirections)index;
            });
            container.Add(directionsDropdown);

            return container;
        }
    }
}
#endif
#pragma warning restore UDR0001
