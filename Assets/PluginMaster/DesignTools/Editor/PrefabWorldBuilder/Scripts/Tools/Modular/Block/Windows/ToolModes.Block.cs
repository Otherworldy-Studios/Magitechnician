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
    public partial class ToolModes
    {
        #region BLOCK FIELDS
        private static readonly Color _selectedColor = new Color(0.3f, 0.5f, 0.8f, 1f);
        private static readonly string[] _brushShapeOptions
            = { "Square", "Circle", "Cube", "Sphere" };
        private static readonly string[] _projectionOptions
            = { "None", "Camera", "Down", "Up", "Back", "Forward", "Left", "Right" };
        private static readonly string[] _conectivityOptions = { "Prefab", "Geometry" };
        private static readonly string[] _directionsOptions = { "4", "8" };
        private static readonly string[] _regionDirectionsOptions = { "4", "8" };
        private static readonly string[] _selectionModeOptions = { "Current", "Face", "Box" };
        private static readonly string[] _replaceModeOptions = { "Single", "Region" };

        
        private static System.Collections.Generic.Dictionary<string, Texture2D> _iconCache
            = new System.Collections.Generic.Dictionary<string, Texture2D>();
        #endregion

        #region HELPERS
        private static string blockIconPath
            => UnityEditor.EditorGUIUtility.isProSkin ? "Sprites/" : "Sprites/LightTheme/";

        private static Texture2D GetBlockIcon(string name)
        {
            if (_iconCache.TryGetValue(name, out var texture) && texture != null)
                return texture;
            texture = Resources.Load<Texture2D>($"{blockIconPath}{name}");
            if (texture != null) _iconCache[name] = texture;
            return texture;
        }

        private bool TextToggleButton(string text, string tooltip,
            bool isSelected, float width = 75f)
        {
            var originalBg = GUI.backgroundColor;
            if (isSelected) GUI.backgroundColor = _selectedColor;
            var result = GUILayout.Button(new GUIContent(text, tooltip),
                GUILayout.Width(width), GUILayout.Height(28));
            GUI.backgroundColor = originalBg;
            return result;
        }

        private bool ImageToggleButton(string iconName, string tooltip, bool isSelected)
        {
            var originalBg = GUI.backgroundColor;
            if (isSelected) GUI.backgroundColor = _selectedColor;
            var texture = GetBlockIcon(iconName);
            var content = texture != null
                ? new GUIContent(texture, tooltip) : new GUIContent("?", tooltip);
            var result = GUILayout.Button(content, GUILayout.Width(36), GUILayout.Height(28));
            GUI.backgroundColor = originalBg;
            return result;
        }

        private void BlockSeparatorGUI()
        {
            GUILayout.Space(4);
            var rect = UnityEditor.EditorGUILayout.GetControlRect(false, 1);
            UnityEditor.EditorGUI.DrawRect(rect,
                new Color(0.3f, 0.3f, 0.3f, 0.5f));
            GUILayout.Space(4);
        }
        #endregion

        #region BLOCK MODES GUI
        private void BlockModesGUI()
        {
            var editMode = BlockToolModes.selectedEditMode;
            var isSelect = editMode == BlockToolModes.EditMode.SELECT;
            var showDrawModes = editMode == BlockToolModes.EditMode.ATTACH
                || editMode == BlockToolModes.EditMode.ERASE
                || (isSelect
                    && BlockToolModes.selectMode == BlockToolModes.SelecMode.BRUSH);

            EditModeButtonsGUI(editMode);
            EditModeImageButtonsGUI(editMode);
            BlockSeparatorGUI();

            if (isSelect) SelectModesGUI();
            if (isSelect
                && BlockToolModes.selectMode == BlockToolModes.SelecMode.BRUSH)
                BlockSeparatorGUI();
            if (showDrawModes) DrawModesGUI();

            BlockSettingsGUI(editMode, showDrawModes, isSelect);

            if (showDrawModes)
            {
                BlockSeparatorGUI();
                MirrorModesGUI();
                if (BlockToolModes.selectedDrawMode
                    == BlockToolModes.DrawMode.BLOCK_BY_BLOCK)
                    AxisModesGUI();
            }
        }
        #endregion

        #region EDIT MODES
        private void EditModeButtonsGUI(BlockToolModes.EditMode current)
        {
            using (new GUILayout.HorizontalScope())
            {
                if (TextToggleButton("Attach", "Attach blocks",
                    current == BlockToolModes.EditMode.ATTACH))
                {
                    BlockToolModes.selectedEditMode = BlockToolModes.EditMode.ATTACH;
                    UnityEditor.SceneView.RepaintAll();
                }
                if (TextToggleButton("Erase", "Erase blocks",
                    current == BlockToolModes.EditMode.ERASE))
                {
                    BlockToolModes.selectedEditMode = BlockToolModes.EditMode.ERASE;
                    UnityEditor.SceneView.RepaintAll();
                }
            }
        }

        private void EditModeImageButtonsGUI(BlockToolModes.EditMode current)
        {
            using (new GUILayout.HorizontalScope())
            {
                if (ImageToggleButton("BlockMove", "Move blocks",
                    current == BlockToolModes.EditMode.MOVE))
                {
                    BlockToolModes.selectedEditMode = BlockToolModes.EditMode.MOVE;
                    UnityEditor.SceneView.RepaintAll();
                }
                if (ImageToggleButton("BlockSelect", "Select",
                    current == BlockToolModes.EditMode.SELECT))
                {
                    BlockToolModes.selectedEditMode = BlockToolModes.EditMode.SELECT;
                    UnityEditor.SceneView.RepaintAll();
                }
                if (ImageToggleButton("BlockReplace", "Replace",
                    current == BlockToolModes.EditMode.REPLACE))
                {
                    BlockToolModes.selectedEditMode = BlockToolModes.EditMode.REPLACE;
                    UnityEditor.SceneView.RepaintAll();
                }
                if (ImageToggleButton("BlockPick", "Pick Brush",
                    current == BlockToolModes.EditMode.PICK))
                {
                    BlockToolModes.selectedEditMode = BlockToolModes.EditMode.PICK;
                    UnityEditor.SceneView.RepaintAll();
                }
            }
        }
        #endregion

        #region SELECT MODES
        private void SelectModesGUI()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(15);
                if (ImageToggleButton("BlockSelect", "Rect Select",
                    BlockToolModes.selectMode == BlockToolModes.SelecMode.RECT))
                {
                    BlockToolModes.selectMode = BlockToolModes.SelecMode.RECT;
                    UnityEditor.SceneView.RepaintAll();
                }
                if (ImageToggleButton("BlockBrushSelect", "Brush Select",
                    BlockToolModes.selectMode == BlockToolModes.SelecMode.BRUSH))
                {
                    BlockToolModes.selectMode = BlockToolModes.SelecMode.BRUSH;
                    UnityEditor.SceneView.RepaintAll();
                }
                if (ImageToggleButton("BlockRegionSelect", "Region Select",
                    BlockToolModes.selectMode == BlockToolModes.SelecMode.REGION))
                {
                    BlockToolModes.selectMode = BlockToolModes.SelecMode.REGION;
                    UnityEditor.SceneView.RepaintAll();
                }
            }
        }
        #endregion

        #region DRAW MODES
        private void DrawModesGUI()
        {
            var drawMode = BlockToolModes.selectedDrawMode;
            using (new GUILayout.HorizontalScope())
            {
                if (ImageToggleButton("BlockByBlock", "Block by block Mode",
                    drawMode == BlockToolModes.DrawMode.BLOCK_BY_BLOCK))
                {
                    BlockToolModes.selectedDrawMode
                        = BlockToolModes.DrawMode.BLOCK_BY_BLOCK;
                    UnityEditor.SceneView.RepaintAll();
                }
                if (ImageToggleButton("BlockLine", "Geometry Mode",
                    drawMode == BlockToolModes.DrawMode.LINE))
                {
                    BlockToolModes.selectedDrawMode = BlockToolModes.DrawMode.LINE;
                    UnityEditor.SceneView.RepaintAll();
                }
                if (ImageToggleButton("BlockFace", "Face Mode",
                    drawMode == BlockToolModes.DrawMode.FACE))
                {
                    BlockToolModes.selectedDrawMode = BlockToolModes.DrawMode.FACE;
                    UnityEditor.SceneView.RepaintAll();
                }
                if (ImageToggleButton("BlockBox", "Box Mode",
                    drawMode == BlockToolModes.DrawMode.BOX))
                {
                    BlockToolModes.selectedDrawMode = BlockToolModes.DrawMode.BOX;
                    UnityEditor.SceneView.RepaintAll();
                }
            }
        }
        #endregion

        #region MIRROR MODES
        private void MirrorModesGUI()
        {
            using (new GUILayout.HorizontalScope())
            {
                if (ImageToggleButton("BlockResetMirror",
                    "Reset mirror modes", false))
                {
                    ModularToolModes.ResetMirrorModes();
                    UnityEditor.SceneView.RepaintAll();
                }
                if (ImageToggleButton("BlockX", "Mirror on X axis",
                    ModularToolModes.mirrorX))
                {
                    ModularToolModes.mirrorX = !ModularToolModes.mirrorX;
                    UnityEditor.SceneView.RepaintAll();
                }
                using (new UnityEditor.EditorGUI.DisabledGroupScope(
                    ToolController.current == ToolController.Tool.FLOOR))
                {
                    if (ImageToggleButton("BlockY", "Mirror on Y axis",
                        ModularToolModes.mirrorY))
                    {
                        ModularToolModes.mirrorY = !ModularToolModes.mirrorY;
                        UnityEditor.SceneView.RepaintAll();
                    }
                }
                if (ImageToggleButton("BlockZ", "Mirror on Z axis",
                    ModularToolModes.mirrorZ))
                {
                    ModularToolModes.mirrorZ = !ModularToolModes.mirrorZ;
                    UnityEditor.SceneView.RepaintAll();
                }
            }

            bool anyMirror = ModularToolModes.mirrorX || ModularToolModes.mirrorY || ModularToolModes.mirrorZ;
            if (anyMirror)
            {
                UnityEditor.EditorGUI.BeginChangeCheck();
                ModularToolModes.reflectRotation = UnityEditor.EditorGUILayout.ToggleLeft(
                    new GUIContent("Reflect rotation", "Reflect rotation of mirrored objects"),
                    ModularToolModes.reflectRotation);
                if (UnityEditor.EditorGUI.EndChangeCheck())
                    UnityEditor.SceneView.RepaintAll();

                if (ModularToolModes.reflectRotation && ToolController.current == ToolController.Tool.BLOCK)
                {
                    UnityEditor.EditorGUI.BeginChangeCheck();
                    ModularToolModes.autoReflectRotation = UnityEditor.EditorGUILayout.ToggleLeft(
                        new GUIContent("Auto", "Auto determine rotation axes based on mirror axes"),
                        ModularToolModes.autoReflectRotation);
                    if (UnityEditor.EditorGUI.EndChangeCheck())
                        UnityEditor.SceneView.RepaintAll();

                    using (new UnityEditor.EditorGUI.DisabledGroupScope(ModularToolModes.autoReflectRotation))
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Space(36);
                            if (ImageToggleButton("BlockX", "Reflect rotation around X axis (180°)",
                                ModularToolModes.reflectRotationX))
                            {
                                ModularToolModes.reflectRotationX = !ModularToolModes.reflectRotationX;
                                UnityEditor.SceneView.RepaintAll();
                            }
                            if (ImageToggleButton("BlockY", "Reflect rotation around Y axis (180°)",
                                ModularToolModes.reflectRotationY))
                            {
                                ModularToolModes.reflectRotationY = !ModularToolModes.reflectRotationY;
                                UnityEditor.SceneView.RepaintAll();
                            }
                            if (ImageToggleButton("BlockZ", "Reflect rotation around Z axis (180°)",
                                ModularToolModes.reflectRotationZ))
                            {
                                ModularToolModes.reflectRotationZ = !ModularToolModes.reflectRotationZ;
                                UnityEditor.SceneView.RepaintAll();
                            }
                        }
                    }
                }

                UnityEditor.EditorGUI.BeginChangeCheck();
                BlockToolModes.reflectScale = UnityEditor.EditorGUILayout.ToggleLeft(
                    new GUIContent("Reflect Scale", "Invert scale on mirrored axes"),
                    BlockToolModes.reflectScale);
                if (UnityEditor.EditorGUI.EndChangeCheck())
                    UnityEditor.SceneView.RepaintAll();
            }
        }
        #endregion

        #region AXIS MODES
        private void AxisModesGUI()
        {
            using (new GUILayout.HorizontalScope())
            {
                if (ImageToggleButton("BlockResetAxis",
                    "Reset axis mode", false))
                {
                    BlockToolModes.ResetAxisModes();
                    UnityEditor.SceneView.RepaintAll();
                }
                if (ImageToggleButton("BlockX", "X axis",
                    BlockToolModes.axisX))
                {
                    BlockToolModes.axisX = !BlockToolModes.axisX;
                    UnityEditor.SceneView.RepaintAll();
                }
                if (ImageToggleButton("BlockY", "Y axis",
                    BlockToolModes.axisY))
                {
                    BlockToolModes.axisY = !BlockToolModes.axisY;
                    UnityEditor.SceneView.RepaintAll();
                }
                if (ImageToggleButton("BlockZ", "Z axis",
                    BlockToolModes.axisZ))
                {
                    BlockToolModes.axisZ = !BlockToolModes.axisZ;
                    UnityEditor.SceneView.RepaintAll();
                }
            }
        }
        #endregion
    }
}
#pragma warning restore UDR0001
