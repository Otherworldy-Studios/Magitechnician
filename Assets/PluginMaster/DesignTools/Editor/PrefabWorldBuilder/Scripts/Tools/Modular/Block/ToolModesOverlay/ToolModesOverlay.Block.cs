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
        #region ROOT
        private VisualElement _drawModesRow;
        private VisualElement _brushSettingsElement;
        private VisualElement _mirrorModesRow;
        private VisualElement _axisModesRow;
        private VisualElement _mirrorModeSeparator;
        private VisualElement _axisModeSeparator;
        private VisualElement _drawModeSeparator;
        private IVisualElementScheduledItem _decreaseScheduler;
        private IVisualElementScheduledItem _increaseScheduler;

        private BlockToolModes.EditMode _prevEditMode = (BlockToolModes.EditMode)(-1);
        private BlockToolModes.DrawMode _prevDrawMode = (BlockToolModes.DrawMode)(-1);
        private BlockToolModes.SelecMode _prevSelectMode = (BlockToolModes.SelecMode)(-1);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Domain reload", "UDR0004:Domain Reload Analyzer")]
        public VisualElement CreateBlockModes(VisualElement root)
        {
            root = new VisualElement { name = "Block Tool Modes" };
            root.style.flexDirection = FlexDirection.Column;
            root.style.paddingTop = 2;
            root.style.paddingBottom = 2;
            root.style.paddingLeft = 2;
            root.style.paddingRight = 0;

            var mainEditModesRow = CreateEditModeButtonRow(new[]
            {
                ("Attach", "Attach blocks", BlockToolModes.EditMode.ATTACH),
                ("Erase", "Erase blocks", BlockToolModes.EditMode.ERASE)
            });
            root.Add(mainEditModesRow);

            var selectionEditModesRow = CreateEditModeImageButtonRow(new[]
            {
                ("BlockMove", "Move blocks", BlockToolModes.EditMode.MOVE),
                ("BlockSelect", "Select", BlockToolModes.EditMode.SELECT),
                ("BlockReplace", "Replace", BlockToolModes.EditMode.REPLACE),
                ("BlockPick", "Pick Brush", BlockToolModes.EditMode.PICK)
            });
            root.Add(selectionEditModesRow);

            EditModeToolsSettings(root);

            _mirrorModeSeparator = CreateHorizontalSeparator();
            root.Add(_mirrorModeSeparator);
            _mirrorModesRow = CreateMirrorModesRow(enableYAxis: true);
            root.Add(_mirrorModesRow);
            
            _axisModeSeparator = CreateHorizontalSeparator();
            root.Add(_axisModeSeparator);
            _axisModesRow = CreateAxisModesRow();
            root.Add(_axisModesRow);

            BlockToolModes.OnBlockToolModeChanged -= OnBlockToolModeChanged;
            BlockToolModes.OnBlockToolModeChanged += OnBlockToolModeChanged;
            root.RegisterCallback<DetachFromPanelEvent>(_ => BlockToolModes.OnBlockToolModeChanged -= OnBlockToolModeChanged);

            UpdateButtonStates();

            return root;
        }
        private void OnBlockToolModeChanged()
        {
            UpdateButtonStates();
            UnityEditor.SceneView.RepaintAll();
        }
        private VisualElement CreateHorizontalSeparator()
        {
            var separator = new VisualElement();
            separator.style.height = 1;
            separator.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            separator.style.marginTop = 4;
            separator.style.marginBottom = 4;
            separator.style.marginLeft = 0;
            separator.style.marginRight = 0;
            return separator;
        }
        #endregion
        #region EDIT MODES
        private VisualElement CreateEditModeButtonRow((string text, string tooltip, BlockToolModes.EditMode tool)[] buttons)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 2;

            foreach (var buttonData in buttons)
            {
                if (string.IsNullOrEmpty(buttonData.text))
                {
                    var spacer = new VisualElement();
                    spacer.style.width = 54;
                    spacer.style.height = 24;
                    spacer.style.marginLeft = 3;
                    spacer.style.marginRight = 3;
                    row.Add(spacer);
                    continue;
                }

                var button = new Button(() =>
                {
                    BlockToolModes.selectedEditMode = buttonData.tool;
                    UpdateDrawModesRowVisibility();
                    UpdateSettingsRowVisibility();
                    UpdateMirrorModesRowVisibility();
                    UpdateAxisModesRowVisibility();
                    UnityEditor.SceneView.RepaintAll();
                })
                {
                    text = buttonData.text,
                    tooltip = buttonData.tooltip
                };

                button.style.width = 54;
                button.style.height = 24;
                button.style.marginLeft = 3;
                button.style.marginRight = 3;

                button.schedule.Execute(() =>
                {
                    if (BlockToolModes.selectedEditMode == buttonData.tool)
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

        private VisualElement CreateEditModeImageButtonRow((string imageName,
            string tooltip, BlockToolModes.EditMode tool)[] buttons)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 0;

            foreach (var buttonData in buttons)
            {
                var button = new Button(() =>
                {
                    BlockToolModes.selectedEditMode = buttonData.tool;
                    UpdateDrawModesRowVisibility();
                    UpdateSettingsRowVisibility();
                    UpdateMirrorModesRowVisibility();
                    UpdateAxisModesRowVisibility();
                    UpdateSelectModesRowVisibility();
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
                    if (BlockToolModes.selectedEditMode == buttonData.tool)
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
        #endregion
        #region TOOL SETTINGS
        private void EditModeToolsSettings(VisualElement root)
        {
            var separator = CreateHorizontalSeparator();
            root.Add(separator);
            _selectModesRow = CreateSelectModesButtonRow(new[]
            {
                ("BlockSelect", "Rect Select", BlockToolModes.SelecMode.RECT),
                ("BlockBrushSelect", "Brush Select", BlockToolModes.SelecMode.BRUSH),
                ("BlockRegionSelect", "Region Select", BlockToolModes.SelecMode.REGION)
            });
            root.Add(_selectModesRow);
            _drawModeSeparator = CreateHorizontalSeparator();
            root.Add(_drawModeSeparator);
            _drawModesRow = CreateDrawModeButtonRow(new[]
            {
                ("BlockByBlock", "Block by block Mode", BlockToolModes.DrawMode.BLOCK_BY_BLOCK),
                ("BlockLine", "Geometry Mode", BlockToolModes.DrawMode.LINE),
                ("BlockFace", "Face Mode", BlockToolModes.DrawMode.FACE),
                ("BlockBox", "Box Mode", BlockToolModes.DrawMode.BOX)
            });
            root.Add(_drawModesRow);

            _brushSettingsElement = new VisualElement();
            root.Add(_brushSettingsElement);

            UpdateDrawModesRowVisibility();
            UpdateSelectModesRowVisibility();
            UpdateSettingsRowVisibility();
        }

        private VisualElement CreateSettingsVisualElement()
        {
            if (BlockToolModes.selectedEditMode == BlockToolModes.EditMode.ATTACH ||
               BlockToolModes.selectedEditMode == BlockToolModes.EditMode.ERASE ||
                (BlockToolModes.selectedEditMode == BlockToolModes.EditMode.SELECT &&
                BlockToolModes.selectMode == BlockToolModes.SelecMode.BRUSH))
            {
                if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.BLOCK_BY_BLOCK)
                    return CreateBlockByBlockBrushSettings();
                if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.LINE)
                    return CreateLineBrushSettings();
                if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.FACE)
                    return CreateFaceBrushSettings();
            }
            else if (BlockToolModes.selectedEditMode == BlockToolModes.EditMode.MOVE)
                return CreateMoveSettings();
            else if(BlockToolModes.selectedEditMode == BlockToolModes.EditMode.SELECT
                && BlockToolModes.selectMode == BlockToolModes.SelecMode.REGION)
                return CreateSelectRegionSettings();
            else if(BlockToolModes.selectedEditMode == BlockToolModes.EditMode.REPLACE)
                return CreateReplaceSettings();
            return new VisualElement();
        }

        private VisualElement CreateDrawModeButtonRow((string imageName, string tooltip,
            BlockToolModes.DrawMode tool)[] buttons)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 2;

            foreach (var buttonData in buttons)
            {
                var button = new Button(() =>
                {
                    BlockToolModes.selectedDrawMode = buttonData.tool;
                    UpdateSettingsRowVisibility();
                    UpdateAxisModesRowVisibility();
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
                    if (BlockToolModes.selectedDrawMode == buttonData.tool)
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
        
        #endregion
        #region ELEMENTS VISIBILITY
        private void UpdateSettingsRowVisibility()
        {
            if (_brushSettingsElement == null) return;

            if (_brushSettingsElement.childCount == 0 || _prevEditMode != BlockToolModes.selectedEditMode
                || _prevDrawMode != BlockToolModes.selectedDrawMode || _prevSelectMode != BlockToolModes.selectMode)
            {
                _brushSettingsElement.Clear();
                _brushSettingsElement.Add(CreateSettingsVisualElement());
            }

            bool shouldShow = BlockToolModes.selectedEditMode == BlockToolModes.EditMode.ATTACH ||
                BlockToolModes.selectedEditMode == BlockToolModes.EditMode.ERASE ||
                BlockToolModes.selectedEditMode == BlockToolModes.EditMode.MOVE ||
                 BlockToolModes.selectedEditMode == BlockToolModes.EditMode.REPLACE ||
                (BlockToolModes.selectedEditMode == BlockToolModes.EditMode.SELECT &&
                (BlockToolModes.selectMode == BlockToolModes.SelecMode.BRUSH
                || BlockToolModes.selectMode == BlockToolModes.SelecMode.REGION) );
            _brushSettingsElement.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void UpdateAxisModesRowVisibility()
        {
            if (_axisModesRow == null) return;

            bool shouldShow = (BlockToolModes.selectedEditMode == BlockToolModes.EditMode.ATTACH ||
                BlockToolModes.selectedEditMode == BlockToolModes.EditMode.ERASE ||
                (BlockToolModes.selectedEditMode == BlockToolModes.EditMode.SELECT &&
                BlockToolModes.selectMode == BlockToolModes.SelecMode.BRUSH)) &&
                BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.BLOCK_BY_BLOCK;

            _axisModesRow.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
            _axisModeSeparator.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
        }
        private void UpdateMirrorModesRowVisibility()
        {
            if (_mirrorModesRow == null) return;
            bool shouldShow = BlockToolModes.selectedEditMode == BlockToolModes.EditMode.ATTACH ||
                BlockToolModes.selectedEditMode == BlockToolModes.EditMode.ERASE ||
                (BlockToolModes.selectedEditMode == BlockToolModes.EditMode.SELECT &&
                BlockToolModes.selectMode == BlockToolModes.SelecMode.BRUSH);
            _mirrorModesRow.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
            _mirrorModeSeparator.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
        }
        private void UpdateDrawModesRowVisibility()
        {
            if (_drawModesRow == null) return;

            bool shouldShow = BlockToolModes.selectedEditMode == BlockToolModes.EditMode.ATTACH ||
                BlockToolModes.selectedEditMode == BlockToolModes.EditMode.ERASE ||
                (BlockToolModes.selectedEditMode == BlockToolModes.EditMode.SELECT &&
                BlockToolModes.selectMode == BlockToolModes.SelecMode.BRUSH);

            _drawModesRow.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;

            bool separatorShouldShow = BlockToolModes.selectedEditMode == BlockToolModes.EditMode.SELECT &&
                BlockToolModes.selectMode == BlockToolModes.SelecMode.BRUSH;
            _drawModeSeparator.style.display = separatorShouldShow ? DisplayStyle.Flex : DisplayStyle.None;
        }
        private void UpdateButtonStates()
        {
            UpdateDrawModesRowVisibility();
            UpdateSettingsRowVisibility();
            UpdateMirrorModesRowVisibility();
            UpdateAxisModesRowVisibility();
            UpdateSelectModesRowVisibility();

            _prevEditMode = BlockToolModes.selectedEditMode;
            _prevDrawMode = BlockToolModes.selectedDrawMode;
            _prevSelectMode = BlockToolModes.selectMode;
        }
        #endregion
    }
}
#endif
#pragma warning restore UDR0001
