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
    public partial class ToolModesOverlay : Overlay
    {
        private VisualElement CreateBlockByBlockBrushSettings()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            container.style.marginBottom = 2;
            container.style.marginTop = 2;
            container.style.backgroundColor = new Color(0.2f, 0.25f, 0.2f, 0.3f);
            container.style.paddingTop = 4;
            container.style.paddingBottom = 4;
            container.style.paddingLeft = 4;
            container.style.paddingRight = 0;


            var sizeRow = new VisualElement();
            sizeRow.style.flexDirection = FlexDirection.Row;
            sizeRow.style.alignItems = Align.Center;
            sizeRow.style.marginBottom = 4;

            var sizeLabelText = new Label("Size:");
            sizeLabelText.style.marginRight = 0;
            sizeLabelText.style.unityTextAlign = TextAnchor.MiddleLeft;
            sizeLabelText.style.minWidth = 30;

            var decreaseButton = new Button()
            {
                text = "-",
                tooltip = "Decrease brush size (hold to repeat)"
            };
            decreaseButton.style.width = 24;
            decreaseButton.style.height = 24;
            decreaseButton.style.marginRight = 2;

            bool isDecreasePressed = false;

            decreaseButton.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    evt.StopPropagation();
                    isDecreasePressed = true;
                    BlockToolModes.brushSize--;
                    UnityEditor.SceneView.RepaintAll();

                    _decreaseScheduler = decreaseButton.schedule.Execute(() =>
                    {
                        if (isDecreasePressed)
                        {
                            BlockToolModes.brushSize--;
                            UnityEditor.SceneView.RepaintAll();
                        }
                    }).StartingIn(300).Every(50);
                }
            }, TrickleDown.TrickleDown);

            decreaseButton.RegisterCallback<MouseUpEvent>(evt =>
            {
                isDecreasePressed = false;
                _decreaseScheduler?.Pause();
                evt.StopPropagation();
            }, TrickleDown.TrickleDown);

            decreaseButton.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                isDecreasePressed = false;
                _decreaseScheduler?.Pause();
            });

            var sizeLabel = new Label(BlockToolModes.brushSize.ToString());
            sizeLabel.style.width = 24;
            sizeLabel.style.height = 20;
            sizeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            sizeLabel.style.marginLeft = 2;
            sizeLabel.style.marginRight = 2;
            if (UnityEditor.EditorGUIUtility.isProSkin)
            {
                sizeLabel.style.backgroundColor = new Color(0.35f, 0.35f, 0.35f, 1f);
                sizeLabel.style.color = new Color(0.9f, 0.9f, 0.9f, 9f);
            }
            else
            {
                sizeLabel.style.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
                sizeLabel.style.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            }

            sizeLabel.style.borderTopLeftRadius = 2;
            sizeLabel.style.borderTopRightRadius = 2;
            sizeLabel.style.borderBottomLeftRadius = 2;
            sizeLabel.style.borderBottomRightRadius = 2;

            sizeLabel.schedule.Execute(() =>
            {
                sizeLabel.text = BlockToolModes.brushSize.ToString();
            }).Every(100);

            var increaseButton = new Button()
            {
                text = "+",
                tooltip = "Increase brush size (hold to repeat)"
            };
            increaseButton.style.width = 24;
            increaseButton.style.height = 24;
            increaseButton.style.marginLeft = 2;

            bool isIncreasePressed = false;

            increaseButton.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    evt.StopPropagation();
                    isIncreasePressed = true;
                    BlockToolModes.brushSize++;
                    UnityEditor.SceneView.RepaintAll();

                    _increaseScheduler = increaseButton.schedule.Execute(() =>
                    {
                        if (isIncreasePressed)
                        {
                            BlockToolModes.brushSize++;
                            UnityEditor.SceneView.RepaintAll();
                        }
                    }).StartingIn(300).Every(50);
                }
            }, TrickleDown.TrickleDown);

            increaseButton.RegisterCallback<MouseUpEvent>(evt =>
            {
                isIncreasePressed = false;
                _increaseScheduler?.Pause();
                evt.StopPropagation();
            }, TrickleDown.TrickleDown);

            increaseButton.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                isIncreasePressed = false;
                _increaseScheduler?.Pause();
            });

            sizeRow.Add(sizeLabelText);
            sizeRow.Add(decreaseButton);
            sizeRow.Add(sizeLabel);
            sizeRow.Add(increaseButton);

            var shapeRow = new VisualElement();
            shapeRow.style.flexDirection = FlexDirection.Row;
            shapeRow.style.alignItems = Align.Center;

            var shapeLabelText = new Label("Shape:");
            shapeLabelText.style.marginRight = 0;
            shapeLabelText.style.unityTextAlign = TextAnchor.MiddleLeft;
            shapeLabelText.style.minWidth = 44;

            var shapeChoices = new System.Collections.Generic.List<string> { "Square", "Circle", "Cube", "Sphere"};
            foreach (BlockToolModes.BrushShape shape in System.Enum.GetValues(typeof(BlockToolModes.BrushShape)))
            {
                shapeChoices.Add(shape.ToString());
            }

            var shapeDropdown = new DropdownField(shapeChoices,
                BlockToolModes.selectedBrushShape.ToString());
            shapeDropdown.style.flexGrow = 0;
            shapeDropdown.style.width = 66;

            shapeDropdown.RegisterValueChangedCallback(evt =>
            {
                if (System.Enum.TryParse(evt.newValue, out BlockToolModes.BrushShape newShape))
                {
                    BlockToolModes.selectedBrushShape = newShape;
                    UnityEditor.SceneView.RepaintAll();
                }
            });

            shapeDropdown.schedule.Execute(() =>
            {
                if (shapeDropdown.value != BlockToolModes.selectedBrushShape.ToString())
                {
                    shapeDropdown.SetValueWithoutNotify(BlockToolModes.selectedBrushShape.ToString());
                }
            }).Every(100);

            shapeRow.Add(shapeLabelText);
            shapeRow.Add(shapeDropdown);

            container.Add(sizeRow);
            container.Add(shapeRow);

            return container;
        }
        private VisualElement CreateAxisModesRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 0;
            row.style.marginBottom = 2;
            row.style.paddingTop = 2;
            row.style.paddingBottom = 2;
            row.style.alignItems = Align.Center;

            var resetButton = new Button(() =>
            {
                BlockToolModes.ResetAxisModes();
                UnityEditor.SceneView.RepaintAll();
            })
            {
                tooltip = "Reset axis mode"
            };
            resetButton.style.width = 24;
            resetButton.style.height = 24;
            resetButton.style.marginLeft = 3;
            resetButton.style.marginRight = 3;

            var resetTexture = Resources.Load<Texture2D>($"{iconPath}BlockResetAxis");
            if (resetTexture != null)
            {
                resetButton.style.backgroundImage = new StyleBackground(resetTexture);
            }
            else
            {
                DoLoadIconForButton(resetButton, "BlockResetAxis");
            }

            row.Add(resetButton);

            var axisXButton = new Button(() =>
            {
                BlockToolModes.axisX = !BlockToolModes.axisX;
                UnityEditor.SceneView.RepaintAll();
            })
            {
                tooltip = "X axis"
            };
            axisXButton.style.width = 24;
            axisXButton.style.height = 24;
            axisXButton.style.marginLeft = 3;
            axisXButton.style.marginRight = 3;

            var xTexture = Resources.Load<Texture2D>($"{iconPath}BlockX");
            if (xTexture != null)
            {
                axisXButton.style.backgroundImage = new StyleBackground(xTexture);
            }
            else
            {
                DoLoadIconForButton(axisXButton, "BlockX");
            }

            axisXButton.schedule.Execute(() =>
            {
                if (BlockToolModes.axisX)
                {
                    axisXButton.style.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 1f);
                }
                else
                {
                    axisXButton.style.backgroundColor = StyleKeyword.Null;
                }
            }).Every(100);

            row.Add(axisXButton);

            var axisYButton = new Button(() =>
            {
                BlockToolModes.axisY = !BlockToolModes.axisY;
                UnityEditor.SceneView.RepaintAll();
            })
            {
                tooltip = "Y axis"
            };
            axisYButton.style.width = 24;
            axisYButton.style.height = 24;
            axisYButton.style.marginLeft = 3;
            axisYButton.style.marginRight = 3;

            var yTexture = Resources.Load<Texture2D>($"{iconPath}BlockY");
            if (yTexture != null)
            {
                axisYButton.style.backgroundImage = new StyleBackground(yTexture);
            }
            else
            {
                DoLoadIconForButton(axisYButton, "BlockY");
            }

            axisYButton.schedule.Execute(() =>
            {
                if (BlockToolModes.axisY)
                {
                    axisYButton.style.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 1f);
                }
                else
                {
                    axisYButton.style.backgroundColor = StyleKeyword.Null;
                }
            }).Every(100);

            row.Add(axisYButton);

            var axisZButton = new Button(() =>
            {
                BlockToolModes.axisZ = !BlockToolModes.axisZ;
                UnityEditor.SceneView.RepaintAll();
            })
            {
                tooltip = "Z axis"
            };
            axisZButton.style.width = 24;
            axisZButton.style.height = 24;
            axisZButton.style.marginLeft = 3;
            axisZButton.style.marginRight = 3;

            var zTexture = Resources.Load<Texture2D>($"{iconPath}BlockZ");
            if (zTexture != null)
            {
                axisZButton.style.backgroundImage = new StyleBackground(zTexture);
            }
            else
            {
                DoLoadIconForButton(axisZButton, "BlockZ");
            }

            axisZButton.schedule.Execute(() =>
            {
                if (BlockToolModes.axisZ)
                {
                    axisZButton.style.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 1f);
                }
                else
                {
                    axisZButton.style.backgroundColor = StyleKeyword.Null;
                }
            }).Every(100);

            row.Add(axisZButton);

            return row;
        }
    }
}
#endif
#pragma warning restore UDR0001
