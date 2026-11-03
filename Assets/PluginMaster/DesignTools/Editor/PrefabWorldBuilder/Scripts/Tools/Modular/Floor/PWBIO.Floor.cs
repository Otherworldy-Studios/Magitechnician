/*
Copyright(c) Omar Duarte
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
    public static partial class PWBIO
    {

        #region HANDLERS
        private static void FloorInitializeOnLoad()
        {
            FloorManager.settings.OnDataChanged += OnFloorSettingsChanged;
            BrushSettings.OnBrushSettingsChanged += UpdateFloorSettingsOnBrushChanged;
            GridManager.settings.OnGridOriginChange += OnFloorGridOriginChange;
        }

        private static void SetSnapStepToFloorCellSize()
        {
            GridManager.settings.step = FloorManager.settings.moduleSize + FloorManager.settings.spacing;
            UnityEditor.SceneView.RepaintAll();
        }

        private static void OnFloorSettingsChanged()
        {
            repaint = true;
            BrushstrokeManager.UpdateFloorBrushstroke(setNextIdx: false);
            SetSnapStepToFloorCellSize();
        }

        public static void UpdateFloorSettingsOnBrushChanged()
        {
            if (ToolController.current != ToolController.Tool.FLOOR) return;
            FloorManager.quarterTurns = 0;
            FloorManager.settings.UpdateCellSize();
            SetSnapStepToFloorCellSize();
            FloorManager.state = FloorManager.ToolState.FIRST_CORNER;
        }

        public static void OnFloorGridOriginChange()
        {
            if (ToolController.current != ToolController.Tool.FLOOR) return;
            repaint = true;
            BrushstrokeManager.UpdateFloorBrushstroke(setNextIdx: false);
            SetSnapStepToFloorCellSize();
        }
        #endregion

        public static void OnFloorEnabled()
        {
            ModularToolModes.ResetEditMode();
            UpdateOctree();
            GridManager.settings.radialGridEnabled = false;
            GridManager.settings.gridOnY = true;
            GridManager.settings.visibleGrid = true;
            GridManager.settings.lockedGrid = true;
            GridManager.settings.snappingOnX = true;
            GridManager.settings.snappingOnZ = true;
            GridManager.settings.snappingEnabled = true;
            UpdateFloorSettingsOnBrushChanged();
            GridManager.settings.DataChanged(repaint: true);
            FloorManager.state = FloorManager.ToolState.FIRST_CORNER;
            FloorManager.quarterTurns = 0;
            ModularToolModes.ResetMirrorModes();
            ModularToolModes.ResetReflectRotation();
            ModularToolModes.autoReflectRotation = true;
        }

        public static void ToggleReflectRotation()
        {
            ModularToolModes.reflectRotation = !ModularToolModes.reflectRotation;
            if (ModularToolModes.reflectRotation)
            {
                ModularToolModes.autoReflectRotation = true;
                ModularToolModes.reflectRotationY = true;
            }
        }
        private static void FloorToolDuringSceneGUI(UnityEditor.SceneView sceneView)
        {
            if (PaletteManager.selectedBrush == null) return;
            var mousePos2D = Event.current.mousePosition;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePos2D);
            var mousePos3D = Vector3.zero;
            var localMousePos3D = Vector3.zero;
            if (GridRaycast(mouseRay, out RaycastHit gridHit))
                mousePos3D = SnapFloorTilePosition(gridHit.point, out localMousePos3D);
            else return;
            if (FloorInput(sceneView.camera, mousePos3D)) return;

            if (ModularToolModes.selectedEditMode == ModularToolModes.EditMode.ATTACH)
            {
                switch (FloorManager.state)
                {
                    case FloorManager.ToolState.FIRST_CORNER:
                        PreviewFloorSingleTile(sceneView.camera, mousePos3D);
                        break;
                    case FloorManager.ToolState.SECOND_CORNER:
                        PreviewFloorRectangle(sceneView.camera);
                        break;
                }
            }
            FloorInfoText(sceneView, localMousePos3D);
        }

        private static void FloorInfoText(UnityEditor.SceneView sceneView, Vector3 localMousePos3D)
        {
            if (!PWBCore.staticData.showInfoText) return;
            var localX = Mathf.RoundToInt(localMousePos3D.x / GridManager.settings.step.x);
            if (localX >= 0) ++localX;
            var localZ = Mathf.RoundToInt(localMousePos3D.z / GridManager.settings.step.z);
            if (localZ >= 0) ++localZ;
            var labelTexts = new string[]
            {
                $"Position: (X: {localX}, Z: {localZ})",
                $"Size: (X: {BrushstrokeManager.cellsCountX}, Z: {BrushstrokeManager.cellsCountZ})"
            };
            InfoText.Draw(sceneView, labelTexts);
        }

        private static Vector3 _floorSecondCorner = Vector3.zero;

        private static bool FloorInput(Camera camera, Vector3 mousePos3D)
        {
            if ((Event.current.type == EventType.KeyUp || Event.current.type == EventType.KeyDown))
            {
                if (Event.current.control && !Event.current.alt && !Event.current.shift)
                    _modularDeleteMode = true;
                else if (_modularDeleteMode && (!Event.current.control || Event.current.alt || Event.current.shift))
                {
                    _modularDeleteMode = false;
                    FloorManager.state = FloorManager.ToolState.FIRST_CORNER;
                    BrushstrokeManager.UpdateFloorBrushstroke(setNextIdx: true);
                    return true;
                }
            }

            if (PWBSettings.shortcuts.floorRotate90YCW.Check())
            {
                ++FloorManager.quarterTurns;
                if (FloorManager.quarterTurns >= 4) FloorManager.quarterTurns = 0;
                FloorManager.settings.UpdateCellSize();
                SetSnapStepToFloorCellSize();
                FloorManager.state = FloorManager.ToolState.FIRST_CORNER;
                BrushstrokeManager.UpdateFloorBrushstroke(setNextIdx: false);
                return true;
            }

            if (Event.current.button == 0)
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    FloorManager.state = FloorManager.ToolState.SECOND_CORNER;
                    FloorManager.secondCorner = FloorManager.firstCorner = mousePos3D;
                    BrushstrokeManager.UpdateFloorBrushstroke(setNextIdx: false, _modularDeleteMode);
                    return true;
                }
                if (FloorManager.state == FloorManager.ToolState.SECOND_CORNER)
                {
                    if (Event.current.type == EventType.MouseDrag)
                    {
                        FloorManager.secondCorner = mousePos3D;
                        if (_floorSecondCorner != FloorManager.secondCorner)
                            BrushstrokeManager.UpdateFloorBrushstroke(setNextIdx: true, _modularDeleteMode);
                    }
                    if (Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseMove)
                    {
                        FloorManager.secondCorner = mousePos3D;
                        var paintStrokeCount = _paintStroke.Count;
                        if (_modularDeleteMode)
                        {
                            BrushstrokeManager.UpdateFloorBrushstroke(setNextIdx: false, _modularDeleteMode);
                            DeleteFloor();
                        }
                        else Paint(FloorManager.settings);
                        FloorManager.state = FloorManager.ToolState.FIRST_CORNER;
                        BrushstrokeManager.UpdateFloorBrushstroke(setNextIdx: paintStrokeCount == 1);
                        return true;
                    }
                }
                _floorSecondCorner = FloorManager.secondCorner;
            }

            if (Event.current.isKey && Event.current.keyCode == KeyCode.Escape)
            {
                FloorManager.state = FloorManager.ToolState.FIRST_CORNER;
                BrushstrokeManager.UpdateFloorBrushstroke(setNextIdx: false);
                return true;
            }
            return false;
        }

        private static Vector3 GetFloorItemPosition(GameObject prefab, Vector3 scaleMult,
            Quaternion itemRotation, Vector3 cellCenter, Vector3 moduleSize)
        {
            var centerToPivot = GetCenterToPivot(prefab, scaleMult, itemRotation);
            var itemPosition = cellCenter + centerToPivot;

            var itemBounds = BoundsUtils.GetBoundsRecursive(prefab.transform, itemRotation, scaleMult);
            var gridUp = GridManager.settings.rotation * Vector3.up;

            var heightOffset = 0f;
            switch (FloorManager.settings.gridAlignment)
            {
                case FloorSettings.GridAlignment.TOP_FACE:
                    heightOffset = (moduleSize.y - itemBounds.size.y) * 0.5f;
                    break;
                case FloorSettings.GridAlignment.BOTTOM_FACE:
                    heightOffset = (moduleSize.y + itemBounds.size.y) * 0.5f;
                    break;
                case FloorSettings.GridAlignment.PIVOT:
                default:
                    var centerToPivotLocal = Quaternion.Inverse(itemRotation) * centerToPivot;
                    heightOffset = moduleSize.y * 0.5f - centerToPivotLocal.y;
                    break;
            }
            return itemPosition + gridUp * heightOffset;
        }
        private static void PreviewFloorSingleTile(Camera camera, Vector3 mousePos3D)
        {
            BrushstrokeItem[] brushstroke = BrushstrokeManager.brushstroke;
            if (brushstroke.Length == 0) return;

            var strokeItem = brushstroke[0].Clone();
            if (strokeItem.settings == null)
            {
                BrushstrokeManager.UpdateFloorBrushstroke(setNextIdx: false);
                return;
            }

            var prefab = strokeItem.settings.prefab;
            if (prefab == null) return;

            var toolSettings = FloorManager.settings;
            var itemRotation = Quaternion.Euler(strokeItem.additionalAngle);
            var previewRotation = itemRotation * Quaternion.Inverse(prefab.transform.rotation);
            var baseCellCenter = mousePos3D;

            BrushSettings brush = strokeItem.settings;
            if (toolSettings.overwriteBrushProperties) brush = toolSettings.brushSettings;

            var brushOffset = toolSettings.subtractBrushOffset
                ? brush.localPositionOffset
                : Vector3.zero;

            var rotatedBrushOffset = brushOffset;
            var quarterTurnsRotation = Quaternion.identity;
            if (FloorManager.quarterTurns > 0)
            {
                quarterTurnsRotation = Quaternion.AngleAxis(
                    FloorManager.quarterTurns * 90,
                    toolSettings.upwardAxis);
                rotatedBrushOffset = quarterTurnsRotation * rotatedBrushOffset;
            }

            var pureCellCenter = baseCellCenter
                + GridManager.settings.rotation * (rotatedBrushOffset * 0.5f);

            var cellCenter = baseCellCenter + itemRotation * brush.localPositionOffset;

            if (_modularDeleteMode)
            {
                if (toolSettings.subtractBrushOffset)
                {
                    var r = GridManager.settings.rotation * quarterTurnsRotation;
                    cellCenter -= r * (brush.localPositionOffset * 0.5f);
                }

                Graphics.DrawMesh(cubeMesh,
                    Matrix4x4.TRS(cellCenter, GridManager.settings.rotation, toolSettings.moduleSize),
                    transparentRedMaterial2, 0, camera);

                foreach (var mt in GetFloorMirroredTransforms(pureCellCenter, Vector3.zero))
                {
                    var mirroredItemRotation = mt.rotationOffset * itemRotation;
                    var mirroredCell = mt.position;

                    if (!toolSettings.subtractBrushOffset)
                        mirroredCell += mirroredItemRotation * brush.localPositionOffset;

                    Graphics.DrawMesh(cubeMesh,
                        Matrix4x4.TRS(mirroredCell, GridManager.settings.rotation, toolSettings.moduleSize),
                        transparentRedMaterial2, 0, camera);
                }

                return;
            }

            var halfStep = Mathf.Min(GridManager.settings.step.x, GridManager.settings.step.z) * 0.4999;
            var halfCellSize = toolSettings.moduleSize / 2;

            if (IsFloorCellOccupied(cellCenter, halfCellSize, itemRotation, halfStep)) return;

            var scaleMult = strokeItem.scaleMultiplier;
            var itemPosition = GetFloorItemPosition(prefab, scaleMult, itemRotation, cellCenter, toolSettings.moduleSize);
            var translateMatrix = Matrix4x4.Translate(-prefab.transform.position);
            var rootToWorld = Matrix4x4.TRS(itemPosition, previewRotation, scaleMult) * translateMatrix;
            var layer = toolSettings.overwritePrefabLayer ? toolSettings.layer : prefab.layer;

            PreviewBrushItem(prefab, rootToWorld, layer, camera,
                redMaterial: false, reverseTriangles: false, flipX: false, flipY: false);

            var itemScale = Vector3.Scale(prefab.transform.localScale, scaleMult);
            _paintStroke.Clear();
            _paintStroke.Add(new PaintStrokeItem(prefab, strokeItem.settings.guid, itemPosition,
                itemRotation * prefab.transform.rotation, itemScale, layer, toolSettings.parent,
                surface: null, flipX: false, flipY: false));

            foreach (var mt in GetFloorMirroredTransforms(pureCellCenter, brushOffset))
                PreviewFloorMirroredTile(camera, mt, strokeItem, itemRotation, prefab, toolSettings, brush, halfStep);
        }

        private static void PreviewFloorRectangle(Camera camera)
        {
            BrushstrokeItem[] brushstroke = null;
            if (PreviewIfBrushtrokestaysTheSame(out brushstroke, camera, forceUpdate: _paintStroke.Count == 0))
                if (!_modularDeleteMode) return;
            if (brushstroke.Length == 0) return;

            _paintStroke.Clear();
            var toolSettings = FloorManager.settings;
            var halfCellSize = toolSettings.moduleSize / 2;
            var halfStep = Mathf.Min(GridManager.settings.step.x, GridManager.settings.step.z) * 0.4999;
            if (_modularDeleteMode) _floorDeleteStroke.Clear();

            for (int i = 0; i < brushstroke.Length; ++i)
            {
                var strokeItem = brushstroke[i];
                if (strokeItem.settings == null) return;

                var prefab = strokeItem.settings.prefab;
                if (prefab == null) return;

                var scaleMult = strokeItem.scaleMultiplier;
                var itemRotation = Quaternion.Euler(strokeItem.additionalAngle);
                var quarterTurns = FloorManager.quarterTurns;
                if (FloorManager.settings.swapXZ) ++quarterTurns;
                if (quarterTurns > 0)
                    itemRotation = itemRotation * Quaternion.AngleAxis(90 * quarterTurns, toolSettings.upwardAxis);

                var baseCellCenter = strokeItem.tangentPosition;
                var cellCenter = baseCellCenter;

                BrushSettings brush = strokeItem.settings;
                if (toolSettings.overwriteBrushProperties) brush = toolSettings.brushSettings;

                var brushOffset = toolSettings.subtractBrushOffset
                    ? brush.localPositionOffset
                    : Vector3.zero;

                var rotatedBrushOffset = brushOffset;
                if (FloorManager.quarterTurns > 0)
                    rotatedBrushOffset = Quaternion.AngleAxis(
                        FloorManager.quarterTurns * 90,
                        toolSettings.upwardAxis) * rotatedBrushOffset;

                var mirrorBaseCellCenter = baseCellCenter;
                if (toolSettings.subtractBrushOffset)
                {
                    mirrorBaseCellCenter -= GridManager.settings.rotation * (rotatedBrushOffset * 0.5f);
                }
                else
                {
                    var baseOffsetRotation = GridManager.settings.rotation
                        * Quaternion.FromToRotation(Vector3.up, toolSettings.upwardAxis);
                    mirrorBaseCellCenter -= baseOffsetRotation * brush.localPositionOffset;
                }

                if (_modularDeleteMode)
                {
                    if (toolSettings.subtractBrushOffset)
                    {
                        var r = GridManager.settings.rotation;
                        if (FloorManager.quarterTurns > 0)
                            r *= Quaternion.AngleAxis(FloorManager.quarterTurns * 90, toolSettings.upwardAxis);
                        cellCenter -= r * (brush.localPositionOffset * 0.5f);
                    }

                    Graphics.DrawMesh(cubeMesh,
                        Matrix4x4.TRS(cellCenter, GridManager.settings.rotation, toolSettings.moduleSize),
                        transparentRedMaterial2, layer: 0, camera);
                    _floorDeleteStroke.Add(new Pose(cellCenter, Quaternion.Euler(strokeItem.additionalAngle)));

                    foreach (var mt in GetFloorMirroredTransforms(mirrorBaseCellCenter, Vector3.zero))
                    {
                        var mirroredItemRotation = mt.rotationOffset * itemRotation;
                        var mirroredCell = mt.position;

                        if (!toolSettings.subtractBrushOffset)
                            mirroredCell += mirroredItemRotation * brush.localPositionOffset;

                        Graphics.DrawMesh(cubeMesh,
                            Matrix4x4.TRS(mirroredCell, GridManager.settings.rotation, toolSettings.moduleSize),
                            transparentRedMaterial2, layer: 0, camera);
                        _floorDeleteStroke.Add(new Pose(mirroredCell, Quaternion.Euler(strokeItem.additionalAngle)));
                    }

                    continue;
                }

                var itemPosition = GetFloorItemPosition(prefab, scaleMult, itemRotation, cellCenter, toolSettings.moduleSize);

                var nearbyObjects = new System.Collections.Generic.List<GameObject>();
                boundsOctree.GetColliding(cellCenter, halfCellSize, GridManager.settings.rotation,
                    itemRotation, nearbyObjects);

                bool mainOccupied = false;
                foreach (var obj in nearbyObjects)
                {
                    if (obj == null || !obj.activeInHierarchy) continue;
                    if ((BoundsUtils.GetBoundsRecursive(obj.transform).center - cellCenter).magnitude > halfStep)
                        continue;
                    if (PaletteManager.selectedPalette.ContainsSceneObject(obj))
                    {
                        mainOccupied = true;
                        break;
                    }
                }
                if (mainOccupied) continue;

                var layer = toolSettings.overwritePrefabLayer ? toolSettings.layer : prefab.layer;
                var previewRotation = Quaternion.Inverse(prefab.transform.rotation) * itemRotation;
                var previewRootToWorld = Matrix4x4.TRS(
                    itemPosition + previewRotation * -prefab.transform.position, previewRotation, scaleMult);

                PreviewBrushItem(prefab, previewRootToWorld, layer, camera,
                    redMaterial: false, reverseTriangles: false, flipX: false, flipY: false);
                _previewData.Add(new PreviewData(prefab, previewRootToWorld, layer, flipX: false, flipY: false));

                var itemScale = Vector3.Scale(prefab.transform.localScale, scaleMult);
                _paintStroke.Add(new PaintStrokeItem(prefab, strokeItem.settings.guid, itemPosition, itemRotation,
                    itemScale, layer, toolSettings.parent, surface: null, flipX: false, flipY: false));

                foreach (var mt in GetFloorMirroredTransforms(mirrorBaseCellCenter, brushOffset))
                {
                    var mirroredItemRotation = mt.rotationOffset * itemRotation;
                    var mirroredScaleMult = Vector3.Scale(scaleMult, mt.scaleMultiplier);
                    var mirroredCell = toolSettings.subtractBrushOffset
                        ? mt.position
                        : mt.position + mirroredItemRotation * brush.localPositionOffset;

                    var mirroredNearby = new System.Collections.Generic.List<GameObject>();
                    boundsOctree.GetColliding(mirroredCell, halfCellSize,
                        GridManager.settings.rotation, mirroredItemRotation, mirroredNearby);

                    bool occupied = false;
                    foreach (var obj in mirroredNearby)
                    {
                        if (obj == null || !obj.activeInHierarchy) continue;
                        if ((BoundsUtils.GetBoundsRecursive(obj.transform).center - mirroredCell).magnitude > halfStep)
                            continue;
                        if (PaletteManager.selectedPalette.ContainsSceneObject(obj))
                        {
                            occupied = true;
                            break;
                        }
                    }
                    if (occupied) continue;

                    var mirroredItemPosition = GetFloorItemPosition(prefab, mirroredScaleMult,
                        mirroredItemRotation, mirroredCell, toolSettings.moduleSize);
                    var mirroredPreviewRot = mirroredItemRotation * Quaternion.Inverse(prefab.transform.rotation);
                    var mirroredRootToWorld = Matrix4x4.TRS(mirroredItemPosition, mirroredPreviewRot, mirroredScaleMult)
                        * Matrix4x4.Translate(-prefab.transform.position);
                    var reverseTriangles = (mt.scaleMultiplier.x * mt.scaleMultiplier.y * mt.scaleMultiplier.z) < 0;

                    PreviewBrushItem(prefab, mirroredRootToWorld, layer, camera,
                        redMaterial: false, reverseTriangles: reverseTriangles, flipX: false, flipY: false);
                    _previewData.Add(new PreviewData(prefab, mirroredRootToWorld, layer, flipX: false, flipY: false));

                    var mirroredItemScale = Vector3.Scale(prefab.transform.localScale, mirroredScaleMult);
                    _paintStroke.Add(new PaintStrokeItem(prefab, strokeItem.settings.guid, mirroredItemPosition,
                        mirroredItemRotation * prefab.transform.rotation, mirroredItemScale, layer, toolSettings.parent,
                        surface: null, flipX: false, flipY: false));
                }
            }
        }

        private static System.Collections.Generic.HashSet<Pose> _floorDeleteStroke
            = new System.Collections.Generic.HashSet<Pose>();

        private static void DeleteFloor()
        {
            if (_floorDeleteStroke.Count == 0) return;
            var toolSettings = FloorManager.settings;
            var toBeDeleted = new System.Collections.Generic.HashSet<GameObject>();
            var halfCellSize = toolSettings.moduleSize / 2;
            foreach (var cellPose in _floorDeleteStroke)
            {
                var nearbyObjects = new System.Collections.Generic.List<GameObject>();
                boundsOctree.GetColliding(cellPose.position, halfCellSize,
                    GridManager.settings.rotation, cellPose.rotation, nearbyObjects);
                if (nearbyObjects.Count == 0) continue;
                foreach (var obj in nearbyObjects)
                {
                    if (obj == null || !obj.activeInHierarchy) continue;
                    var centerDistance = (BoundsUtils.GetBoundsRecursive(obj.transform).center - cellPose.position).magnitude;
                    var halfStep = Mathf.Min(GridManager.settings.step.x, GridManager.settings.step.z) * 0.4999;
                    if (centerDistance > halfStep) continue;
                    if (PaletteManager.selectedPalette.ContainsSceneObject(obj)) toBeDeleted.Add(obj);
                }
            }
            void EraseObject(GameObject obj)
            {
                if (obj == null) return;
                var root = UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(obj);
                if (root != null) obj = root;
#if UNITY_6000_3_OR_NEWER
                PWBCore.DestroyTempCollider(obj.GetEntityId());
#else
                PWBCore.DestroyTempCollider(obj.GetInstanceID());
#endif
                UnityEditor.Undo.DestroyObjectImmediate(obj);
            }
            foreach (var obj in toBeDeleted) EraseObject(obj);
        }

    }
}
#pragma warning restore UDR0001
