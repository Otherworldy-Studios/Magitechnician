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
    public partial class PWBPreferences : UnityEditor.EditorWindow
    {
        private bool _dataGroupOpen = true;
        private bool _unsavedChangesGroupOpen = true;
        private bool _gizmosGroupOpen = true;
        private bool _toolbarGroupOpen = true;
        private bool _pinToolGroupOpen = true;
        private bool _gravityToolGroupOpen = true;
        private bool _BrushesGroupOpen = true;
        private bool _palettesGroupOpen = true;
        private bool _editModeGroupOpen = true;

        private void GeneralSettings()
        {
            _dataGroupOpen
                = UnityEditor.EditorGUILayout.BeginFoldoutHeaderGroup(_dataGroupOpen, "Data Settings");
            if (_dataGroupOpen) DataGroup();
            UnityEditor.EditorGUILayout.EndFoldoutHeaderGroup();

            _unsavedChangesGroupOpen = UnityEditor.EditorGUILayout.BeginFoldoutHeaderGroup(_unsavedChangesGroupOpen,
                "Unsaved Changes");
            if (_unsavedChangesGroupOpen) UnsavedChangesGroup();
            UnityEditor.EditorGUILayout.EndFoldoutHeaderGroup();

            _gizmosGroupOpen = UnityEditor.EditorGUILayout.BeginFoldoutHeaderGroup(_gizmosGroupOpen, "Gizmos");
            if (_gizmosGroupOpen) GizmosGroup();
            UnityEditor.EditorGUILayout.EndFoldoutHeaderGroup();

            _toolbarGroupOpen = UnityEditor.EditorGUILayout.BeginFoldoutHeaderGroup(_toolbarGroupOpen, "Toolbar");
            if (_toolbarGroupOpen) ToolbarGroup();
            UnityEditor.EditorGUILayout.EndFoldoutHeaderGroup();

            _pinToolGroupOpen = UnityEditor.EditorGUILayout.BeginFoldoutHeaderGroup(_pinToolGroupOpen, "Pin Tool");
            if (_pinToolGroupOpen) PinToolGroup();
            UnityEditor.EditorGUILayout.EndFoldoutHeaderGroup();

            _gravityToolGroupOpen = UnityEditor.EditorGUILayout.BeginFoldoutHeaderGroup(_gravityToolGroupOpen, "Gravity Tool");
            if (_gravityToolGroupOpen) GravityToolGroup();
            UnityEditor.EditorGUILayout.EndFoldoutHeaderGroup();

            _editModeGroupOpen = UnityEditor.EditorGUILayout.BeginFoldoutHeaderGroup(_editModeGroupOpen, "Edit mode");
            if (_editModeGroupOpen) EditModeGroup();
            UnityEditor.EditorGUILayout.EndFoldoutHeaderGroup();

            _BrushesGroupOpen = UnityEditor.EditorGUILayout.BeginFoldoutHeaderGroup(_BrushesGroupOpen, "Brushes");
            if (_BrushesGroupOpen) BrushesGroup();
            UnityEditor.EditorGUILayout.EndFoldoutHeaderGroup();

            _palettesGroupOpen = UnityEditor.EditorGUILayout.BeginFoldoutHeaderGroup(_palettesGroupOpen, "Palettes");
            if (_palettesGroupOpen) PalettesGroup();
            UnityEditor.EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DataGroup()
        {
            using (new GUILayout.HorizontalScope(UnityEditor.EditorStyles.helpBox))
            {
                UnityEditor.EditorGUIUtility.labelWidth = 90;
                UnityEditor.EditorGUILayout.LabelField("Data directory"
                    , PWBSettings.fullDataDir, UnityEditor.EditorStyles.textField);
                if (GUILayout.Button("...", GUILayout.Width(29), GUILayout.Height(20)))
                {
                    var directory = UnityEditor.EditorUtility.OpenFolderPanel("Select data directory...",
                    PWBSettings.fullDataDir, "Data");
                    if (System.IO.Directory.Exists(directory)) PWBSettings.SetDataDir(directory);
                }
            }
        }

        private static readonly string[] _unsavedChangesActionNames = { "Ask if want to save", "Save", "Discard" };
        private void UnsavedChangesGroup()
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                UnityEditor.EditorGUIUtility.labelWidth = 45;
                PWBCore.staticData.unsavedChangesAction = (PWBData.UnsavedChangesAction)
                    UnityEditor.EditorGUILayout.Popup("Action",
                    (int)PWBCore.staticData.unsavedChangesAction, _unsavedChangesActionNames);
            }
        }

        private void GizmosGroup()
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                {
                    UnityEditor.EditorGUIUtility.labelWidth = 110;
                    PWBCore.staticData.controPointSize = UnityEditor.EditorGUILayout.IntSlider("Control point size",
                        PWBCore.staticData.controPointSize, 1, 3);
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Selected control point", GUILayout.Width(150));
                        UnityEditor.EditorGUIUtility.labelWidth = 40;
                        PWBCore.staticData.selectedContolPointColor = UnityEditor.EditorGUILayout.ColorField(
                        "Color", PWBCore.staticData.selectedContolPointColor);
                        GUILayout.Space(20);
                        PWBCore.staticData.selectedControlPointBlink = UnityEditor.EditorGUILayout.ToggleLeft(
                            "Blink on mouse move", PWBCore.staticData.selectedControlPointBlink);
                    }
                }
                using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                {
                    PWBCore.staticData.showInfoText
                    = UnityEditor.EditorGUILayout.ToggleLeft("Show info text", PWBCore.staticData.showInfoText);
                }
            }
        }

        private void ToolbarGroup()
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                PWBCore.staticData.closeAllWindowsWhenClosingTheToolbar
                    = UnityEditor.EditorGUILayout.ToggleLeft("Close all windows when closing the toolbar",
                    PWBCore.staticData.closeAllWindowsWhenClosingTheToolbar);
                PWBCore.staticData.openToolPropertiesWhenAToolIsSelected
                    = UnityEditor.EditorGUILayout.ToggleLeft("Open tool properties when a tool is selected",
                    PWBCore.staticData.openToolPropertiesWhenAToolIsSelected);
            }
        }

        private void PinToolGroup()
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                UnityEditor.EditorGUIUtility.labelWidth = 155;
                PinManager.rotationSnapValue = UnityEditor.EditorGUILayout.Slider("Rotation snap value (Deg)",
                    PinManager.rotationSnapValue, 0f, 360f);
            }
        }

        private void GravityToolGroup()
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                UnityEditor.EditorGUIUtility.labelWidth = 182;
                GravityToolController.surfaceDistanceSensitivity
                    = UnityEditor.EditorGUILayout.Slider("Distance to surface sensitivity",
                     GravityToolController.surfaceDistanceSensitivity, 0f, 1f);
            }
        }
        private void BrushesGroup()
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                UnityEditor.EditorGUIUtility.labelWidth = 110;
                PWBCore.staticData.openBrushPropertiesWhenABrushIsSelected
                    = UnityEditor.EditorGUILayout.ToggleLeft("Open brush properties when a brush is selected",
                    PWBCore.staticData.openBrushPropertiesWhenABrushIsSelected);
                PWBCore.staticData.thumbnailLayer = UnityEditor.EditorGUILayout.IntField("Thumbnail layer",
                    PWBCore.staticData.thumbnailLayer);
                PWBCore.staticData.createThumbnailsFolder
                    = UnityEditor.EditorGUILayout.ToggleLeft("Group all thumbnails folders into a single folder",
                    PWBCore.staticData.createThumbnailsFolder);
                PWBCore.staticData.addEnumerationToName
                    = UnityEditor.EditorGUILayout.ToggleLeft("Add numeric suffix to placed prefabs",
                    PWBCore.staticData.addEnumerationToName);
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    PWBCore.staticData.useAssetPreview
                     = UnityEditor.EditorGUILayout.ToggleLeft("Use Unity's built-in asset preview for thumbnails",
                     PWBCore.staticData.useAssetPreview);
                    if (check.changed) PrefabPalette.RepaintWindow();
                }

                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Update render pipeline scripting define symbol"))
                        RenderPipelineDefine.SetRenderPipelineDefineSymbol();
                    GUILayout.FlexibleSpace();
                }
            }
        }
        private void PalettesGroup()
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                PWBCore.staticData.selectTheNextPaletteInAlphabeticalOrder
                        = UnityEditor.EditorGUILayout.ToggleLeft("Select the next palette in alphabetical order",
                        PWBCore.staticData.selectTheNextPaletteInAlphabeticalOrder);
            }
        }

        private void EditModeGroup()
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                UnityEditor.EditorGUIUtility.labelWidth = 120;
                PWBCore.staticData.maxPreviewCountInEditMode
                    = UnityEditor.EditorGUILayout.IntField(new GUIContent("Max Preview Count",
                    "Defines the maximum number of pre-existing objects displayed as preview in Edit Mode." +
                    "This setting can optimize performance, especially for scenes with numerous objects"),
                    PWBCore.staticData.maxPreviewCountInEditMode);
            }
        }
    }
}
#pragma warning restore UDR0001
