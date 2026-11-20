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
        private VisualElement CreateMirrorModesRow(bool enableYAxis)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;

            #region MIRROR X/Y/Z BUTTONS
            var mirrorRow = new VisualElement();
            mirrorRow.style.flexDirection = FlexDirection.Row;
            mirrorRow.style.marginTop = 0;
            mirrorRow.style.marginBottom = 0;
            mirrorRow.style.paddingTop = 2;
            mirrorRow.style.paddingBottom = 2;
            mirrorRow.style.alignItems = Align.Center;

            var resetButton = new Button(() =>
            {
                ModularToolModes.ResetMirrorModes();
                UnityEditor.SceneView.RepaintAll();
            })
            {
                tooltip = "Reset mirror modes"
            };
            resetButton.style.width = 24;
            resetButton.style.height = 24;
            resetButton.style.marginLeft = 3;
            resetButton.style.marginRight = 3;

            var resetTexture = Resources.Load<Texture2D>($"{iconPath}BlockResetMirror");
            if (resetTexture != null)
            {
                resetButton.style.backgroundImage = new StyleBackground(resetTexture);
            }
            else
            {
                DoLoadIconForButton(resetButton, "BlockResetMirror");
            }
            mirrorRow.Add(resetButton);

            var mirrorXButton = new Button(() =>
            {
                ModularToolModes.mirrorX = !ModularToolModes.mirrorX;
                UnityEditor.SceneView.RepaintAll();
            })
            {
                tooltip = "Mirror on X axis"
            };
            mirrorXButton.style.width = 24;
            mirrorXButton.style.height = 24;
            mirrorXButton.style.marginLeft = 3;
            mirrorXButton.style.marginRight = 3;

            var xTexture = Resources.Load<Texture2D>($"{iconPath}BlockX");
            if (xTexture != null)
            {
                mirrorXButton.style.backgroundImage = new StyleBackground(xTexture);
            }
            else
            {
                DoLoadIconForButton(mirrorXButton, "BlockX");
            }
            mirrorXButton.schedule.Execute(() =>
            {
                if (ModularToolModes.mirrorX)
                {
                    mirrorXButton.style.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 1f);
                }
                else
                {
                    mirrorXButton.style.backgroundColor = StyleKeyword.Null;
                }
            }).Every(100);

            mirrorRow.Add(mirrorXButton);

            var mirrorYButton = new Button(() =>
            {
                if (!enableYAxis) return;
                ModularToolModes.mirrorY = !ModularToolModes.mirrorY;
                UnityEditor.SceneView.RepaintAll();
            })
            {
                tooltip = enableYAxis ? "Mirror on Y axis" : "Mirror on Y axis (not available for this tool)"
            };
            mirrorYButton.style.width = 24;
            mirrorYButton.style.height = 24;
            mirrorYButton.style.marginLeft = 3;
            mirrorYButton.style.marginRight = 3;

            if (!enableYAxis)
            {
                ModularToolModes.mirrorY = false;
                mirrorYButton.SetEnabled(false);
                mirrorYButton.style.opacity = 0.35f;
            }

            var yTexture = Resources.Load<Texture2D>($"{iconPath}BlockY");
            if (yTexture != null)
            {
                mirrorYButton.style.backgroundImage = new StyleBackground(yTexture);
            }
            else
            {
                DoLoadIconForButton(mirrorYButton, "BlockY");
            }
            mirrorYButton.schedule.Execute(() =>
            {
                if (!enableYAxis) return;
                if (ModularToolModes.mirrorY)
                {
                    mirrorYButton.style.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 1f);
                }
                else
                {
                    mirrorYButton.style.backgroundColor = StyleKeyword.Null;
                }
            }).Every(100);

            mirrorRow.Add(mirrorYButton);

            var mirrorZButton = new Button(() =>
            {
                ModularToolModes.mirrorZ = !ModularToolModes.mirrorZ;
                UnityEditor.SceneView.RepaintAll();
            })
            {
                tooltip = "Mirror on Z axis"
            };
            mirrorZButton.style.width = 24;
            mirrorZButton.style.height = 24;
            mirrorZButton.style.marginLeft = 3;
            mirrorZButton.style.marginRight = 3;

            var zTexture = Resources.Load<Texture2D>($"{iconPath}BlockZ");
            if (zTexture != null)
            {
                mirrorZButton.style.backgroundImage = new StyleBackground(zTexture);
            }
            else
            {
                DoLoadIconForButton(mirrorZButton, "BlockZ");
            }

            mirrorZButton.schedule.Execute(() =>
            {
                if (ModularToolModes.mirrorZ)
                {
                    mirrorZButton.style.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 1f);
                }
                else
                {
                    mirrorZButton.style.backgroundColor = StyleKeyword.Null;
                }
            }).Every(100);

            mirrorRow.Add(mirrorZButton);

            container.Add(mirrorRow);
            #endregion

            #region REFLECT ROTATION TOGGLES
            var reflectRotationRow = new VisualElement();
            reflectRotationRow.style.flexDirection = FlexDirection.Row;
            reflectRotationRow.style.paddingTop = 2;
            reflectRotationRow.style.paddingBottom = 2;
            reflectRotationRow.style.alignItems = Align.Center;

            var reflectRotationToggle = new Toggle()
            {
                text = "Reflect rotation",
                tooltip = "Reflect rotation of mirrored objects",
                value = ModularToolModes.reflectRotation
            };
            reflectRotationToggle.style.marginLeft = 3;
            reflectRotationToggle.style.marginRight = 6;
            reflectRotationToggle.RegisterValueChangedCallback(evt =>
            {
                ModularToolModes.reflectRotation = evt.newValue;
                UnityEditor.SceneView.RepaintAll();
            });
            reflectRotationToggle.schedule.Execute(() =>
            {
                reflectRotationToggle.SetValueWithoutNotify(ModularToolModes.reflectRotation);
            }).Every(100);
            reflectRotationRow.Add(reflectRotationToggle);

            container.Add(reflectRotationRow);

            reflectRotationRow.schedule.Execute(() =>
            {
                bool anyMirror = ModularToolModes.mirrorX || ModularToolModes.mirrorY || ModularToolModes.mirrorZ;
                reflectRotationRow.style.display = anyMirror ? DisplayStyle.Flex : DisplayStyle.None;
            }).Every(100);
            if (enableYAxis)
            {
                var autoReflectRow = new VisualElement();
                autoReflectRow.style.flexDirection = FlexDirection.Row;
                autoReflectRow.style.paddingTop = 2;
                autoReflectRow.style.paddingBottom = 2;
                autoReflectRow.style.alignItems = Align.Center;

                var autoReflectToggle = new Toggle()
                {
                    text = "Auto",
                    tooltip = "Auto determine rotation axes based on mirror axes",
                    value = ModularToolModes.autoReflectRotation
                };
                autoReflectToggle.style.marginLeft = 3;
                autoReflectToggle.style.marginRight = 3;
                autoReflectToggle.RegisterValueChangedCallback(evt =>
                {
                    ModularToolModes.autoReflectRotation = evt.newValue;
                    UnityEditor.SceneView.RepaintAll();
                });
                autoReflectToggle.schedule.Execute(() =>
                {
                    autoReflectToggle.SetValueWithoutNotify(ModularToolModes.autoReflectRotation);
                }).Every(100);
                autoReflectRow.Add(autoReflectToggle);

                autoReflectRow.schedule.Execute(() =>
                {
                    bool anyMirror = ModularToolModes.mirrorX || ModularToolModes.mirrorY || ModularToolModes.mirrorZ;
                    autoReflectRow.style.display = anyMirror && ModularToolModes.reflectRotation
                        ? DisplayStyle.Flex : DisplayStyle.None;
                }).Every(100);

                container.Add(autoReflectRow);
            }
            #endregion

            #region REFLECT ROTATION X/Y/Z BUTTONS
            if (enableYAxis)
            {
                var reflectAxisRow = new VisualElement();
                reflectAxisRow.style.flexDirection = FlexDirection.Row;
                reflectAxisRow.style.paddingTop = 2;
                reflectAxisRow.style.paddingBottom = 2;
                reflectAxisRow.style.alignItems = Align.Center;

                var reflectRotXButton = new Button(() =>
                {
                    if (!ModularToolModes.autoReflectRotation)
                    {
                        ModularToolModes.reflectRotationX = !ModularToolModes.reflectRotationX;
                        UnityEditor.SceneView.RepaintAll();
                    }
                })
                {
                    tooltip = "Reflect rotation around X axis (180°)"
                };
                reflectRotXButton.style.width = 24;
                reflectRotXButton.style.height = 24;
                reflectRotXButton.style.marginLeft = 3;
                reflectRotXButton.style.marginRight = 3;

                var rotXTexture = Resources.Load<Texture2D>($"{iconPath}BlockX");
                if (rotXTexture != null)
                {
                    reflectRotXButton.style.backgroundImage = new StyleBackground(rotXTexture);
                }
                else
                {
                    DoLoadIconForButton(reflectRotXButton, "BlockX");
                }
                reflectRotXButton.schedule.Execute(() =>
                {
                    if (ModularToolModes.reflectRotationX)
                    {
                        reflectRotXButton.style.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 1f);
                    }
                    else
                    {
                        reflectRotXButton.style.backgroundColor = StyleKeyword.Null;
                    }
                    reflectRotXButton.style.opacity = ModularToolModes.autoReflectRotation ? 0.5f : 1f;
                }).Every(100);
                reflectAxisRow.Add(reflectRotXButton);

                var reflectRotYButton = new Button(() =>
                {
                    if (!ModularToolModes.autoReflectRotation)
                    {
                        ModularToolModes.reflectRotationY = !ModularToolModes.reflectRotationY;
                        UnityEditor.SceneView.RepaintAll();
                    }
                })
                {
                    tooltip = "Reflect rotation around Y axis (180°)"
                };
                reflectRotYButton.style.width = 24;
                reflectRotYButton.style.height = 24;
                reflectRotYButton.style.marginLeft = 3;
                reflectRotYButton.style.marginRight = 3;

                var rotYTexture = Resources.Load<Texture2D>($"{iconPath}BlockY");
                if (rotYTexture != null)
                {
                    reflectRotYButton.style.backgroundImage = new StyleBackground(rotYTexture);
                }
                else
                {
                    DoLoadIconForButton(reflectRotYButton, "BlockY");
                }
                reflectRotYButton.schedule.Execute(() =>
                {
                    if (ModularToolModes.reflectRotationY)
                    {
                        reflectRotYButton.style.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 1f);
                    }
                    else
                    {
                        reflectRotYButton.style.backgroundColor = StyleKeyword.Null;
                    }
                    reflectRotYButton.style.opacity = ModularToolModes.autoReflectRotation ? 0.5f : 1f;
                }).Every(100);
                reflectAxisRow.Add(reflectRotYButton);

                var reflectRotZButton = new Button(() =>
                {
                    if (!ModularToolModes.autoReflectRotation)
                    {
                        ModularToolModes.reflectRotationZ = !ModularToolModes.reflectRotationZ;
                        UnityEditor.SceneView.RepaintAll();
                    }
                })
                {
                    tooltip = "Reflect rotation around Z axis (180°)"
                };
                reflectRotZButton.style.width = 24;
                reflectRotZButton.style.height = 24;
                reflectRotZButton.style.marginLeft = 3;
                reflectRotZButton.style.marginRight = 3;

                var rotZTexture = Resources.Load<Texture2D>($"{iconPath}BlockZ");
                if (rotZTexture != null)
                {
                    reflectRotZButton.style.backgroundImage = new StyleBackground(rotZTexture);
                }
                else
                {
                    DoLoadIconForButton(reflectRotZButton, "BlockZ");
                }
                reflectRotZButton.schedule.Execute(() =>
                {
                    if (ModularToolModes.reflectRotationZ)
                    {
                        reflectRotZButton.style.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 1f);
                    }
                    else
                    {
                        reflectRotZButton.style.backgroundColor = StyleKeyword.Null;
                    }
                    reflectRotZButton.style.opacity = ModularToolModes.autoReflectRotation ? 0.5f : 1f;
                }).Every(100);
                reflectAxisRow.Add(reflectRotZButton);

                reflectAxisRow.schedule.Execute(() =>
                {
                    bool anyMirror = ModularToolModes.mirrorX || ModularToolModes.mirrorY || ModularToolModes.mirrorZ;
                    reflectAxisRow.style.display = anyMirror && ModularToolModes.reflectRotation
                        ? DisplayStyle.Flex : DisplayStyle.None;
                }).Every(100);

                container.Add(reflectAxisRow);
            }
            #endregion

            #region REFLECT SCALE TOGGLE
            var reflectScaleRow = new VisualElement();
            reflectScaleRow.style.flexDirection = FlexDirection.Row;
            reflectScaleRow.style.paddingTop = 2;
            reflectScaleRow.style.paddingBottom = 2;
            reflectScaleRow.style.alignItems = Align.Center;

            var reflectScaleToggle = new Toggle()
            {
                text = "Reflect Scale",
                tooltip = "Invert scale on mirrored axes",
                value = ModularToolModes.reflectScale
            };
            reflectScaleToggle.style.marginLeft = 3;
            reflectScaleToggle.style.marginRight = 3;
            reflectScaleToggle.RegisterValueChangedCallback(evt =>
            {
                ModularToolModes.reflectScale = evt.newValue;
                UnityEditor.SceneView.RepaintAll();
            });
            reflectScaleToggle.schedule.Execute(() =>
            {
                reflectScaleToggle.SetValueWithoutNotify(ModularToolModes.reflectScale);
            }).Every(100);
            reflectScaleRow.Add(reflectScaleToggle);

            container.Add(reflectScaleRow);
            reflectScaleRow.schedule.Execute(() =>
            {
                bool anyMirror = ModularToolModes.mirrorX || ModularToolModes.mirrorY || ModularToolModes.mirrorZ;
                reflectScaleRow.style.display = anyMirror ? DisplayStyle.Flex : DisplayStyle.None;
            }).Every(100);
            #endregion

            return container;
        }
    }
}
#endif
#pragma warning restore UDR0001
