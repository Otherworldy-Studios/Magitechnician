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
    public partial class ToolProperties : UnityEditor.EditorWindow
    {
        
        private static float _maxRadius = 50f;
        private static readonly Vector3[] _dir =
        {
            Vector3.right, Vector3.left,
            Vector3.up, Vector3.down,
            Vector3.forward, Vector3.back
        };
        private static readonly string[] _dirNames = new string[] { "+X", "-X", "+Y", "-Y", "+Z", "-Z" };

        private static readonly string[] _brushShapeOptions = { "Point", "Circle", "Square" };
        private static readonly string[] _spacingOptions = { "Auto", "Custom" };
        private void PaintSettingsGUI(IPaintOnSurfaceToolSettings paintOnSurfaceSettings,
            IPaintToolSettings paintSettings)
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {

                using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                {
                    UnityEditor.EditorGUIUtility.labelWidth = 100;
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var ignoreSceneColliders = UnityEditor.EditorGUILayout.ToggleLeft("Ignore scene Colliders",
                            paintOnSurfaceSettings.ignoreSceneColliders);
                        if (check.changed)
                        {
                            paintOnSurfaceSettings.ignoreSceneColliders = ignoreSceneColliders;
                            if (paintOnSurfaceSettings.ignoreSceneColliders) PWBIO.UpdateSceneColliderSet();
                            UnityEditor.SceneView.RepaintAll();
                        }
                    }
                    UnityEditor.EditorGUIUtility.labelWidth = 150;
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var paintOnMeshesWithoutCollider
                            = UnityEditor.EditorGUILayout.ToggleLeft("Paint on meshes without collider",
                            paintOnSurfaceSettings.paintOnMeshesWithoutCollider);
                        if (check.changed)
                        {
                            paintOnSurfaceSettings.paintOnMeshesWithoutCollider = paintOnMeshesWithoutCollider;
                            if (!paintOnSurfaceSettings.paintOnMeshesWithoutCollider) PWBCore.DestroyTempColliders();
                            UnityEditor.SceneView.RepaintAll();
                        }
                    }
                }

                UnityEditor.EditorGUIUtility.labelWidth = 110;
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var paintOnPalettePrefabs = UnityEditor.EditorGUILayout.ToggleLeft("Paint on palette prefabs",
                        paintOnSurfaceSettings.paintOnPalettePrefabs);
                    var paintOnSelectedOnly = UnityEditor.EditorGUILayout.ToggleLeft("Paint on selected only",
                        paintOnSurfaceSettings.paintOnSelectedOnly);
                    if (check.changed)
                    {
                        paintOnSurfaceSettings.paintOnPalettePrefabs = paintOnPalettePrefabs;
                        paintOnSurfaceSettings.paintOnSelectedOnly = paintOnSelectedOnly;
                        UnityEditor.SceneView.RepaintAll();
                    }
                }
            }
            PaintToolSettingsGUI(paintSettings);
        }
        private void ParentSettingsGUI(IPaintToolSettings paintSettings)
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {

                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var overwriteParentingSettings
                        = UnityEditor.EditorGUILayout.ToggleLeft("Ovewrite parenting settings",
                        paintSettings.overwriteParentingSettings);
                    if (check.changed)
                    {
                        paintSettings.overwriteParentingSettings = overwriteParentingSettings;
                    }
                }
                IToolParentingSettings parentingSettings = paintSettings as IToolParentingSettings;
                if (!paintSettings.overwriteParentingSettings) parentingSettings = PWBCore.staticData.globalParentingSettings;
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var autoCreateParent
                        = UnityEditor.EditorGUILayout.ToggleLeft("Create parent", parentingSettings.autoCreateParent);
                    if (check.changed)
                    {
                        parentingSettings.autoCreateParent = autoCreateParent;
                    }
                }
                if (!parentingSettings.autoCreateParent)
                {
                    if (!paintSettings.setLastSelectedAsParent)
                    {
                        parentingSettings.setSurfaceAsParent = UnityEditor.EditorGUILayout.ToggleLeft("Set surface as parent",
                            parentingSettings.setSurfaceAsParent);
                    }

                    if (!parentingSettings.setSurfaceAsParent)
                    {
                        parentingSettings.setLastSelectedAsParent
                            = UnityEditor.EditorGUILayout.ToggleLeft("Set last selected object as parent",
                            parentingSettings.setLastSelectedAsParent);
                        if (!paintSettings.setLastSelectedAsParent)
                        {
                            using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                            {
                                UnityEditor.EditorGUIUtility.labelWidth = 110;
                                var parent = (Transform)UnityEditor.EditorGUILayout.ObjectField("Parent Transform",
                                    parentingSettings.parent, typeof(Transform), true);
                                if (check.changed)
                                {
                                    parentingSettings.parent = parent;
                                }
                            }
                        }
                    }
                }
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var createSubparent = UnityEditor.EditorGUILayout.ToggleLeft("Create sub-parents per palette",
                   parentingSettings.createSubparentPerPalette);
                    if (check.changed)
                    {
                        parentingSettings.createSubparentPerPalette = createSubparent;
                    }
                }
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var createSubparent = UnityEditor.EditorGUILayout.ToggleLeft("Create sub-parents per tool",
                   parentingSettings.createSubparentPerTool);
                    if (check.changed)
                    {
                        parentingSettings.createSubparentPerTool = createSubparent;
                    }
                }
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var createSubparent = UnityEditor.EditorGUILayout.ToggleLeft("Create sub-parents per brush",
                   parentingSettings.createSubparentPerBrush);
                    if (check.changed)
                    {
                        parentingSettings.createSubparentPerBrush = createSubparent;
                    }
                }
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var createSubparent = UnityEditor.EditorGUILayout.ToggleLeft("Create sub-parents per prefab",
                   parentingSettings.createSubparentPerPrefab);
                    if (check.changed)
                    {

                        parentingSettings.createSubparentPerPrefab = createSubparent;
                    }
                }

            }
        }
        private void OverwriteLayerGUI(IPaintToolSettings paintSettings)
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var overwritePrefabLayer = UnityEditor.EditorGUILayout.ToggleLeft("Overwrite prefab layer",
                        paintSettings.overwritePrefabLayer);
                    int layer = paintSettings.layer;
                    if (paintSettings.overwritePrefabLayer) layer = UnityEditor.EditorGUILayout.LayerField("Layer",
                        paintSettings.layer);
                    if (check.changed)
                    {
                        paintSettings.overwritePrefabLayer = overwritePrefabLayer;
                        paintSettings.layer = layer;
                        UnityEditor.SceneView.RepaintAll();
                    }
                }
            }
        }
        private void PaintToolSettingsGUI(IPaintToolSettings paintSettings)
        {
            ParentSettingsGUI(paintSettings);
            OverwriteLayerGUI(paintSettings);
        }
        private void RadiusSlider(CircleToolBase settings)
        {
            using (new GUILayout.HorizontalScope())
            {
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    if (settings.radius > _maxRadius)
                        _maxRadius = Mathf.Max(Mathf.Floor(settings.radius / 10) * 20f, 10f);
                    UnityEditor.EditorGUIUtility.labelWidth = 60;
                    var radius = UnityEditor.EditorGUILayout.Slider("Radius", settings.radius, 0.05f, _maxRadius);
                    if (check.changed)
                    {
                        settings.radius = radius;
                        UnityEditor.SceneView.RepaintAll();
                    }
                }
                if (GUILayout.Button("|>", GUILayout.Width(20))) _maxRadius *= 2f;
                if (GUILayout.Button("|<", GUILayout.Width(20)))
                    _maxRadius = Mathf.Min(Mathf.Floor(settings.radius / 10f) * 10f + 10f, _maxRadius);
            }
        }
        private void BrushToolBaseSettingsGUI(BrushToolBase settings)
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                UnityEditor.EditorGUIUtility.labelWidth = 60;
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var brushShape = (BrushToolSettings.BrushShape)UnityEditor.EditorGUILayout.Popup("Shape",
                        (int)settings.brushShape, _brushShapeOptions);
                    if (check.changed)
                    {
                        settings.brushShape = brushShape;
                        UnityEditor.SceneView.RepaintAll();
                    }
                }
                if (settings.brushShape != BrushToolBase.BrushShape.POINT)
                {
                    using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                    {
                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            var randomize
                                = UnityEditor.EditorGUILayout.ToggleLeft("Randomize positions", settings.randomizePositions);
                            if (check.changed)
                            {
                                settings.randomizePositions = randomize;
                                UnityEditor.SceneView.RepaintAll();
                            }
                        }
                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            UnityEditor.EditorGUIUtility.labelWidth = 80;
                            var randomness = UnityEditor.EditorGUILayout.Slider("Randomness", settings.randomness, 0f, 1f);
                            if (check.changed)
                            {
                                settings.randomness = randomness;
                                UnityEditor.SceneView.RepaintAll();
                            }
                            UnityEditor.EditorGUIUtility.labelWidth = 60;
                        }
                    }
                    RadiusSlider(settings);
                }
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var density = UnityEditor.EditorGUILayout.IntSlider("Density", settings.density, 0, 100);
                    if (check.changed)
                    {
                        settings.density = density;
                        UnityEditor.SceneView.RepaintAll();
                    }
                }
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                    {
                        UnityEditor.EditorGUIUtility.labelWidth = 90;
                        var spacingType = (BrushToolBase.SpacingType)UnityEditor.EditorGUILayout.Popup("Min Spacing",
                            (int)settings.spacingType, _spacingOptions);
                        var spacing = settings.minSpacing;
                        using (new UnityEditor.EditorGUI.DisabledGroupScope(spacingType != BrushToolBase.SpacingType.CUSTOM))
                        {
                            spacing = UnityEditor.EditorGUILayout.FloatField("Value", settings.minSpacing);
                        }
                        if (check.changed)
                        {
                            settings.spacingType = spacingType;
                            settings.minSpacing = spacing;
                            UnityEditor.SceneView.RepaintAll();
                        }
                    }
                }
                using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                {
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var orientAlongBrushstroke = UnityEditor.EditorGUILayout.ToggleLeft("Orient Along the Brushstroke",
                            settings.orientAlongBrushstroke);
                        var additionalAngle = settings.additionalOrientationAngle;
                        if (orientAlongBrushstroke)
                            additionalAngle = UnityEditor.EditorGUILayout.Vector3Field("Additonal angle", additionalAngle);
                        if (check.changed)
                        {
                            settings.orientAlongBrushstroke = orientAlongBrushstroke;
                            settings.additionalOrientationAngle = additionalAngle;
                            UnityEditor.SceneView.RepaintAll();
                        }
                    }
                }
            }
        }
        private void EmbedInSurfaceSettingsGUI(SelectionToolBaseBasic settings)
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    UnityEditor.EditorGUIUtility.labelWidth = 60;
                    var embedInSurface = UnityEditor.EditorGUILayout.ToggleLeft("Embed On the Surface",
                        settings.embedInSurface);
                    if (check.changed)
                    {
                        settings.embedInSurface = embedInSurface;
                        if (embedInSurface && settings is SelectionToolSettings) PWBIO.EmbedSelectionInSurface();
                        UnityEditor.SceneView.RepaintAll();
                    }
                }
                if (settings.embedInSurface)
                {
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var embedAtPivotHeight = UnityEditor.EditorGUILayout.ToggleLeft("Embed At Pivot Height",
                            settings.embedAtPivotHeight);
                        if (check.changed)
                        {
                            settings.embedAtPivotHeight = embedAtPivotHeight;
                            if (settings.embedInSurface && settings is SelectionToolSettings) PWBIO.EmbedSelectionInSurface();
                            UnityEditor.SceneView.RepaintAll();
                        }
                    }
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        UnityEditor.EditorGUIUtility.labelWidth = 100;
                        var surfaceDistance = UnityEditor.EditorGUILayout.FloatField("Surface Distance",
                            settings.surfaceDistance);
                        if (check.changed)
                        {
                            settings.surfaceDistance = surfaceDistance;
                            if (settings is SelectionToolSettings) PWBIO.EmbedSelectionInSurface();
                            UnityEditor.SceneView.RepaintAll();
                        }
                    }
                    if (settings is SelectionToolBase)
                    {
                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            var selectionSettings = settings as SelectionToolBase;
                            var rotateToTheSurface = UnityEditor.EditorGUILayout.ToggleLeft("Rotate To the Surface",
                                selectionSettings.rotateToTheSurface);
                            if (check.changed)
                            {
                                selectionSettings.rotateToTheSurface = rotateToTheSurface;
                                if (settings.embedInSurface && settings is SelectionToolSettings)
                                    PWBIO.EmbedSelectionInSurface();
                                UnityEditor.SceneView.RepaintAll();
                            }
                        }

                    }
                }
            }
        }

        private struct BrushPropertiesGroupState
        {
            public bool brushPosGroupOpen;
            public bool brushRotGroupOpen;
            public bool brushScaleGroupOpen;
            public bool brushFlipGroupOpen;
        }
        private void OverwriteBrushPropertiesGUI(IPaintToolSettings settings,
            ref BrushPropertiesGroupState state)
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var overwriteBrushProperties = UnityEditor.EditorGUILayout.ToggleLeft("Overwrite Brush Properties",
                        settings.overwriteBrushProperties);
                    if (check.changed)
                    {
                        settings.overwriteBrushProperties = overwriteBrushProperties;
                        UnityEditor.SceneView.RepaintAll();
                    }
                }
                if (PaletteManager.selectedBrush != null)
                    settings.brushSettings.isAsset2D = PaletteManager.selectedBrush.isAsset2D;
                else settings.brushSettings.isAsset2D = false;
                if (settings.overwriteBrushProperties)
                    BrushProperties.BrushFields(settings.brushSettings,
                    ref state.brushPosGroupOpen, ref state.brushRotGroupOpen,
                    ref state.brushScaleGroupOpen, ref state.brushFlipGroupOpen);
            }
        }

        private static readonly string[] _editModeTypeOptions = { "Line nodes", "Line position and rotation" };
        private void EditModeToggle(IPersistentToolController persistentToolController)
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (new GUILayout.HorizontalScope())
                {
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var editMode = UnityEditor.EditorGUILayout.ToggleLeft("Edit Mode", ToolController.editMode);
                        if (check.changed)
                        {
                            ToolController.editMode = editMode;
                            PWBIO.ResetLineRotation();
                            PWBIO.repaint = true;
                            UnityEditor.SceneView.RepaintAll();
                            PWBItemsWindow.RepainWindow();
                        }
                    }
                    if (persistentToolController == LineManager.instance && ToolController.editMode)
                    {
                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            var editModeType = (LineManager.EditModeType)UnityEditor.EditorGUILayout
                            .Popup((int)LineManager.editModeType, _editModeTypeOptions);
                            if (check.changed)
                            {
                                LineManager.editModeType = editModeType;
                                PWBIO.ResetLineRotation();
                                PWBIO.repaint = true;
                                UnityEditor.SceneView.RepaintAll();
                            }
                        }
                    }
                }
                if (ToolController.editMode)
                {
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var applyBrushToexisting = UnityEditor.EditorGUILayout.ToggleLeft(
                            "Apply brush setings to Pre-existing objects", persistentToolController.applyBrushToExisting);
                        if (check.changed)
                        {
                            persistentToolController.applyBrushToExisting = applyBrushToexisting;
                            if (ToolController.current == ToolController.Tool.LINE) PWBIO.PreviewSelectedPersistentLines();
                            else if (ToolController.current == ToolController.Tool.SHAPE) PWBIO.PreviewSelectedPersistentShapes();
                            else if (ToolController.current == ToolController.Tool.TILING)
                                PWBIO.PreviewSelectedPersistentTilings();
                            PWBIO.repaint = true;
                            UnityEditor.SceneView.RepaintAll();
                        }
                    }
                }
                if (ToolController.editMode)
                {
                    if (GUILayout.Button("Open items window")) PWBItemsWindow.ShowWindow();
                }
            }
        }
        private void HandlePosition()
        {
            if (PWBIO.selectedPointIdx < 0) return;
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    PWBIO.handlePosition = UnityEditor.EditorGUILayout.Vector3Field("Handle position", PWBIO.handlePosition);
                    if (check.changed) PWBIO.UpdateHandlePosition();
                }
            }
        }
        private void HandleRotation()
        {
            if (PWBIO.selectedPointIdx < 0) return;
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var eulerAngles = PWBIO.handleRotation.eulerAngles;
                    eulerAngles = UnityEditor.EditorGUILayout.Vector3Field("Handle rotation", eulerAngles);
                    if (check.changed)
                    {
                        var newRotation = Quaternion.Euler(eulerAngles);
                        PWBIO.handleRotation = newRotation;
                        PWBIO.UpdateHandleRotation();
                    }
                }
            }
        }
    }
}
#pragma warning restore UDR0001
