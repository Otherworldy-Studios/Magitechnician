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
        private static void WallInitializeOnLoad()
        {
            WallManager.settings.OnDataChanged += OnWallSettingsChanged;
            BrushSettings.OnBrushSettingsChanged += UpdateWallSettingsOnBrushChanged;
            GridManager.settings.OnGridOriginChange += OnWallGridOriginChange;
        }

        private static void SetSnapStepToWallCellSize()
        {
            CalculateAxes(out Vector3 cellSize);
            GridManager.settings.step = new Vector3(WallManager.wallLength + WallManager.settings.spacing.x, cellSize.y,
                WallManager.wallLength + WallManager.settings.spacing.z);
            UnityEditor.SceneView.RepaintAll();
        }

        private static void OnWallSettingsChanged()
        {
            repaint = true;
            BrushstrokeManager.UpdateWallBrushstroke(WallManager.wallLenghtAxis, cellsCount: 1,
                setNextIdx: false, deleteMode: false);
            SetSnapStepToWallCellSize();
        }

        public static void CalculateAxes(out Vector3 cellSize)
        {
            cellSize = WallManager.settings.moduleSize;
            var multibrush = PaletteManager.selectedBrush;
            if (multibrush == null) return;
            var toolSettings = WallManager.settings;

            if (toolSettings.moduleSizeType != TilesUtils.SizeType.CUSTOM)
                cellSize = TilesUtils.GetCellSize(toolSettings.moduleSizeType, multibrush,
                WallManager.settings.moduleSize, toolSettings.subtractBrushOffset);

            toolSettings.SetUpwardAxis(AxesUtils.SignedAxis.UP);
            if (WallManager.settings.autoCalculateAxes)
            {
                if (cellSize.x >= cellSize.z)
                {
                    WallManager.wallLenghtAxis = AxesUtils.Axis.X;
                    WallManager.settings.SetForwardAxis(AxesUtils.SignedAxis.FORWARD);
                }
                else
                {
                    WallManager.wallLenghtAxis = AxesUtils.Axis.Z;
                    WallManager.settings.SetForwardAxis(AxesUtils.SignedAxis.RIGHT);
                }
                WallManager.wallThickness = Mathf.Min(cellSize.x, cellSize.z);
                WallManager.wallLength = Mathf.Max(cellSize.x, cellSize.z);
            }
            else
            {
                WallManager.wallLenghtAxis = AxesUtils.GetOtherAxis(WallManager.settings.forwardAxis,
                    WallManager.settings.upwardAxis);
                WallManager.wallThickness = AxesUtils.GetAxisValue(cellSize, WallManager.settings.forwardAxis);
                WallManager.wallLength = AxesUtils.GetAxisValue(cellSize, WallManager.wallLenghtAxis);
            }
        }

        public static void UpdateWallSettingsOnBrushChanged()
        {
            if (ToolController.current != ToolController.Tool.WALL) return;
            if (PaletteManager.selectedBrushIdx == -1) return;
            WallManager.halfTurn = false;
            CalculateAxes(out Vector3 cellSize);
            if (WallManager.settings.moduleSizeType != TilesUtils.SizeType.CUSTOM)
                WallManager.settings.SetCellSize(cellSize);
            SetSnapStepToWallCellSize();
            WallManager.state = WallManager.ToolState.FIRST_WALL_PREVIEW;
        }

        public static void OnWallGridOriginChange()
        {
            if (ToolController.current != ToolController.Tool.WALL) return;
            repaint = true;
            BrushstrokeManager.UpdateWallBrushstroke(WallManager.wallLenghtAxis, cellsCount: 1,
                setNextIdx: false, deleteMode: false);
            SetSnapStepToWallCellSize();
        }
        #endregion

        public static void OnWallEnabled()
        {
            GridManager.settings.radialGridEnabled = false;
            GridManager.settings.gridOnY = true;
            GridManager.settings.visibleGrid = true;
            GridManager.settings.lockedGrid = true;
            GridManager.settings.snappingOnX = true;
            GridManager.settings.snappingOnZ = true;
            GridManager.settings.snappingEnabled = true;
            UpdateWallSettingsOnBrushChanged();
            GridManager.settings.DataChanged(repaint: true);
            WallManager.state = WallManager.ToolState.FIRST_WALL_PREVIEW;
            WallManager.halfTurn = false;
        }

        private static Vector3 _wallEnd = Vector3.zero;
        private static void WallToolDuringSceneGUI(UnityEditor.SceneView sceneView)
        {
            if (PaletteManager.selectedBrush == null) return;
            var mousePos2D = Event.current.mousePosition;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePos2D);
            var mousePos3D = Vector3.zero;
            var localMousePos3D = Vector3.zero;
            AxesUtils.Axis axis;
            int cellsCount = 1;
            bool rotateHalfTurn;
            if (GridRaycast(mouseRay, out RaycastHit gridHit))
            {
                if (WallManager.state == WallManager.ToolState.FIRST_WALL_PREVIEW) WallManager.startPoint = gridHit.point;

                mousePos3D = (WallManager.state == WallManager.ToolState.FIRST_WALL_PREVIEW ||
                    (WallManager.state == WallManager.ToolState.EDITING && WallManager.startPoint == gridHit.point))
                    ? SnapWallPosition(gridHit.point, out axis, out rotateHalfTurn, out localMousePos3D)
                    : SnapWallPosition(WallManager.startPointSnapped, gridHit.point,
                        out axis, out cellsCount, out rotateHalfTurn, out localMousePos3D);
            }
            else return;

            if (WallInput(mousePos3D, axis, cellsCount)) return;

            switch (WallManager.state)
            {
                case WallManager.ToolState.FIRST_WALL_PREVIEW:
                    WallManager.startPointSnapped = mousePos3D;
                    PreviewFirstWall(sceneView.camera, mousePos3D, axis, rotateHalfTurn);
                    break;
                case WallManager.ToolState.EDITING:

                    PreviewWall(sceneView.camera, axis, rotateHalfTurn);
                    break;
            }
            WallInfoText(sceneView, localMousePos3D, cellsCount);
        }

        private static void WallInfoText(UnityEditor.SceneView sceneView, Vector3 localMousePos3D, int cellsCount)
        {
            if (!PWBCore.staticData.showInfoText) return;
            var localX = Mathf.RoundToInt(localMousePos3D.x / GridManager.settings.step.x);
            if (localX >= 0) ++localX;
            var localZ = Mathf.RoundToInt(localMousePos3D.z / GridManager.settings.step.z);
            if (localZ >= 0) ++localZ;
            var labelTexts = new string[] { $"Position: (X: {localX}, Z: {localZ})", $"Size: {cellsCount}" };
            InfoText.Draw(sceneView, labelTexts);

        }
        private static bool WallInput(Vector3 mousePos3D, AxesUtils.Axis axis, int cellsCount)
        {
            if ((Event.current.type == EventType.KeyUp || Event.current.type == EventType.KeyDown))
            {
                if (Event.current.control && !Event.current.alt && !Event.current.shift) _modularDeleteMode = true;
                else if (_modularDeleteMode && (!Event.current.control || Event.current.alt || Event.current.shift))
                {
                    _modularDeleteMode = false;
                    WallManager.state = WallManager.ToolState.FIRST_WALL_PREVIEW;
                    BrushstrokeManager.UpdateWallBrushstroke(axis, cellsCount: 1, setNextIdx: true, deleteMode: false);
                    return true;
                }
            }

            if (Event.current.button == 0)
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    WallManager.state = WallManager.ToolState.EDITING;
                    WallManager.endPointSnapped = WallManager.startPointSnapped = mousePos3D;
                    BrushstrokeManager.UpdateWallBrushstroke(axis, cellsCount: 1, setNextIdx: false, _modularDeleteMode);
                    return true;
                }
                if (WallManager.state == WallManager.ToolState.EDITING)
                {
                    if (Event.current.type == EventType.MouseDrag)
                    {
                        WallManager.endPointSnapped = mousePos3D;
                        if (_wallEnd != WallManager.endPointSnapped)
                            BrushstrokeManager.UpdateWallBrushstroke(axis, cellsCount, setNextIdx: true, _modularDeleteMode);
                    }
                    else if (Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseMove)
                    {
                        WallManager.endPointSnapped = mousePos3D;
                        if (_modularDeleteMode)
                            DeleteWall();
                        else Paint(WallManager.settings);
                        WallManager.state = WallManager.ToolState.FIRST_WALL_PREVIEW;
                        BrushstrokeManager.UpdateWallBrushstroke(axis, cellsCount: 1, setNextIdx: true, deleteMode: false);
                        return true;
                    }
                }
                _wallEnd = WallManager.endPointSnapped;
            }
            if (Event.current.isKey && Event.current.keyCode == KeyCode.Escape)
            {
                WallManager.state = WallManager.ToolState.FIRST_WALL_PREVIEW;
                BrushstrokeManager.UpdateWallBrushstroke(axis, cellsCount: 1, setNextIdx: true, deleteMode: false);
                return true;
            }

            if (PWBSettings.shortcuts.wallHalfTurn.Check())
            {
                WallManager.halfTurn = !WallManager.halfTurn;
                WallManager.settings.UpdateCellSize();
                SetSnapStepToWallCellSize();
                WallManager.state = WallManager.ToolState.FIRST_WALL_PREVIEW;
                BrushstrokeManager.UpdateWallBrushstroke(WallManager.wallLenghtAxis, cellsCount: 1,
                    setNextIdx: false, deleteMode: false);
                return true;
            }
            return false;
        }

        private static void PreviewFirstWall(Camera camera, Vector3 mousePos3D,
            AxesUtils.Axis axis, bool rotateHalfTurn)
        {
            if (BrushstrokeManager.brushstroke.Length == 0) return;
            var strokeItem = BrushstrokeManager.brushstroke[0].Clone();
            if (strokeItem.settings == null)
            {
                BrushstrokeManager.UpdateWallBrushstroke(axis, cellsCount: 1, setNextIdx: false, deleteMode: false);
                return;
            }
            var prefab = strokeItem.settings.prefab;
            if (prefab == null) return;
            _previewData.Clear();
            _paintStroke.Clear();
            var toolSettings = WallManager.settings;
            var itemRotation = Quaternion.Euler(strokeItem.additionalAngle);
            if (rotateHalfTurn) itemRotation *= Quaternion.AngleAxis(180, toolSettings.upwardAxis);

            if (axis == AxesUtils.Axis.Z) itemRotation *= Quaternion.AngleAxis(90, toolSettings.upwardAxis);
            var previewRotation = itemRotation;
            previewRotation *= Quaternion.Inverse(prefab.transform.rotation);

            var cellCenter = mousePos3D;
            BrushSettings brush = strokeItem.settings;
            if (toolSettings.overwriteBrushProperties) brush = toolSettings.brushSettings;
            cellCenter += itemRotation * brush.localPositionOffset;

            if (_modularDeleteMode)
            {
                var TRS = Matrix4x4.TRS(cellCenter, previewRotation, toolSettings.moduleSize);
                Graphics.DrawMesh(cubeMesh, TRS, transparentRedMaterial2, 0, camera);
                _wallDeleteStroke.Clear();
                _wallDeleteStroke.Add(new Pose(cellCenter, previewRotation));
                return;
            }

            var halfCellSize = toolSettings.moduleSize / 2;

            var nearbyObjects = new System.Collections.Generic.List<GameObject>();
            boundsOctree.GetColliding(cellCenter, halfCellSize, GridManager.settings.rotation,
                itemRotation, nearbyObjects);
            if (nearbyObjects.Count > 0)
            {
                bool checkNextItem = false;
                foreach (var obj in nearbyObjects)
                {
                    if (obj == null) continue;
                    if (!obj.activeInHierarchy) continue;
                    var objCenter = BoundsUtils.GetBoundsRecursive(obj.transform).center;
                    var centerDistance = (objCenter - cellCenter).magnitude;
                    if (centerDistance > WallManager.wallThickness * 0.9999) continue;
                    if (PaletteManager.selectedPalette.ContainsSceneObject(obj))
                    {
                        checkNextItem = true;
                        break;
                    }
                }
                if (checkNextItem) return;
            }

            var scaleMult = strokeItem.scaleMultiplier;

            var centerToPivot = GetCenterToPivot(prefab, scaleMult, itemRotation);
            var itemPosition = cellCenter + centerToPivot;
            var translateMatrix = Matrix4x4.Translate(-prefab.transform.position);
            var rootToWorld = Matrix4x4.TRS(itemPosition, previewRotation, scaleMult) * translateMatrix;
            var layer = toolSettings.overwritePrefabLayer ? toolSettings.layer : prefab.layer;

            PreviewBrushItem(prefab, rootToWorld, layer, camera,
                redMaterial: false, reverseTriangles: false, flipX: false, flipY: false);

            _previewData.Add(new PreviewData(prefab, rootToWorld, layer, flipX: false, flipY: false));
            var itemScale = Vector3.Scale(prefab.transform.localScale, scaleMult);
            Transform parentTransform = toolSettings.parent;
            var paintItem = new PaintStrokeItem(prefab, strokeItem.settings.guid, itemPosition, itemRotation,
                itemScale, layer, parentTransform, surface: null, flipX: false, flipY: false);
            _paintStroke.Add(paintItem);
        }

        private static void PreviewWall(Camera camera, AxesUtils.Axis axis, bool rotateHalfTurn)
        {
            BrushstrokeItem[] brushstroke = null;
            if (PreviewIfBrushtrokestaysTheSame(out brushstroke, camera, forceUpdate: _paintStroke.Count == 0))
                if (!_modularDeleteMode) return;
            if (BrushstrokeManager.brushstroke.Length == 0) return;
            _previewData.Clear();
            _paintStroke.Clear();
            var toolSettings = WallManager.settings;
            var halfCellSize = toolSettings.moduleSize / 2;
            if (_modularDeleteMode) _wallDeleteStroke.Clear();
            for (int i = 0; i < brushstroke.Length; ++i)
            {
                var strokeItem = brushstroke[i];
                if (strokeItem.settings == null) return;
                var prefab = strokeItem.settings.prefab;
                if (prefab == null) return;
                var scaleMult = strokeItem.scaleMultiplier;
                var itemRotation = Quaternion.Euler(strokeItem.additionalAngle);
                if (axis == AxesUtils.Axis.Z) itemRotation *= Quaternion.AngleAxis(90, toolSettings.upwardAxis);
                if (rotateHalfTurn) itemRotation *= Quaternion.AngleAxis(180, toolSettings.upwardAxis);

                if (WallManager.halfTurn)
                    itemRotation *= Quaternion.AngleAxis(180, toolSettings.upwardAxis);
                var cellCenter = strokeItem.tangentPosition;
                BrushSettings brush = strokeItem.settings;
                if (toolSettings.overwriteBrushProperties) brush = toolSettings.brushSettings;
                cellCenter += itemRotation * brush.localPositionOffset;

                if (_modularDeleteMode)
                {
                    var TRS = Matrix4x4.TRS(cellCenter, itemRotation, WallManager.settings.moduleSize);
                    Graphics.DrawMesh(cubeMesh, TRS, transparentRedMaterial2, layer: 0, camera);
                    _wallDeleteStroke.Add(new Pose(cellCenter, itemRotation));
                    continue;
                }
                var centerToPivot = GetCenterToPivot(prefab, scaleMult, itemRotation);
                var itemPosition = cellCenter + centerToPivot;
                var nearbyObjects = new System.Collections.Generic.List<GameObject>();
                boundsOctree.GetColliding(cellCenter, halfCellSize, GridManager.settings.rotation,
                    itemRotation, nearbyObjects);
                if (nearbyObjects.Count > 0)
                {
                    bool checkNextItem = false;
                    foreach (var obj in nearbyObjects)
                    {
                        if (obj == null) continue;
                        if (!obj.activeInHierarchy) continue;
                        var objCenter = BoundsUtils.GetBoundsRecursive(obj.transform).center;
                        var centerDistance = (objCenter - cellCenter).magnitude;
                        if (centerDistance > WallManager.wallThickness * 0.9999) continue;
                        if (PaletteManager.selectedPalette.ContainsSceneObject(obj))
                        {
                            checkNextItem = true;
                            break;
                        }
                    }
                    if (checkNextItem) continue;
                }
                var layer = toolSettings.overwritePrefabLayer ? toolSettings.layer : prefab.layer;

                var previewRotation = Quaternion.Inverse(prefab.transform.rotation) * itemRotation;
                var translateMatrix = Matrix4x4.Translate(-prefab.transform.position);
                var previeRootToWorld = Matrix4x4.TRS(itemPosition, previewRotation, scaleMult) * translateMatrix;
                PreviewBrushItem(prefab, previeRootToWorld, layer, camera,
                    redMaterial: false, reverseTriangles: false, flipX: false, flipY: false);
                _previewData.Add(new PreviewData(prefab, previeRootToWorld, layer, flipX: false, flipY: false));
                var itemScale = Vector3.Scale(prefab.transform.localScale, scaleMult);
                Transform parentTransform = toolSettings.parent;
                var paintItem = new PaintStrokeItem(prefab, strokeItem.settings.guid, itemPosition, itemRotation,
                    itemScale, layer, parentTransform, surface: null, flipX: false, flipY: false);
                _paintStroke.Add(paintItem);

            }
        }

        private static System.Collections.Generic.HashSet<Pose> _wallDeleteStroke
            = new System.Collections.Generic.HashSet<Pose>();

        private static void DeleteWall()
        {
            if (_wallDeleteStroke.Count == 0) return;
            var toolSettings = WallManager.settings;
            var toBeDeleted = new System.Collections.Generic.HashSet<GameObject>();
            var halfCellSize = toolSettings.moduleSize / 2;
            foreach (var cellPose in _wallDeleteStroke)
            {
                var nearbyObjects = new System.Collections.Generic.List<GameObject>();
                boundsOctree.GetColliding(cellPose.position, halfCellSize,
                    GridManager.settings.rotation, cellPose.rotation, nearbyObjects);
                if (nearbyObjects.Count == 0) continue;
                foreach (var obj in nearbyObjects)
                {
                    if (obj == null) continue;
                    if (!obj.activeInHierarchy) continue;
                    var objCenter = BoundsUtils.GetBoundsRecursive(obj.transform).center;
                    var centerDistance = (objCenter - cellPose.position).magnitude;
                    if (centerDistance > WallManager.wallThickness * 0.999) continue;
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
