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
    public static partial class PWBIO
    {

        #region HANDLERS
        private static void TilingInitializeOnLoad()
        {
            TilingManager.settings.OnDataChanged -= OnTilingSettingsChanged;
            TilingManager.settings.OnDataChanged += OnTilingSettingsChanged;

            BrushSettings.OnBrushSettingsChanged -= PreviewSelectedPersistentTilings;
            BrushSettings.OnBrushSettingsChanged += PreviewSelectedPersistentTilings;
        }
        private static void OnUndoTiling() => ClearTilingStroke();
        private static void OnTilingToolModeChanged()
        {
            DeselectPersistentItems(TilingManager.instance);
            if (!ToolController.editMode)
            {
                ToolProperties.RepainWindow();
                return;
            }
            if (_tilingData != null || _selectedPersistentTilingData != null)
                ResetTilingState();
            ResetSelectedPersistentObject(TilingManager.instance, ref _editingPersistentTiling, _initialPersistentTilingData);
        }
        private static void OnTilingSettingsChanged()
        {
            repaint = true;
            if (!ToolController.editMode)
            {
                _tilingData.settings = TilingManager.settings;
                updateStroke = true;
                return;
            }
            if (_selectedPersistentTilingData == null) return;
            _selectedPersistentTilingData.settings.Copy(TilingManager.settings);
            PreviewPersistentTiling(_selectedPersistentTilingData);
        }
        #endregion

        #region COMMON
        private static TilingData _tilingData = TilingData.instance;
        private static void ClearTilingStroke()
        {
            _paintStroke.Clear();
            BrushstrokeManager.ClearBrushstroke();
            updateStroke = true;
            if (ToolController.editMode)
            {
                if (!_editingPersistentLine) return;
                _selectedPersistentTilingData.UpdatePoses();
                PreviewPersistentTiling(_selectedPersistentTilingData);
                UnityEditor.SceneView.RepaintAll();
            }
            else
            {
                UpdateCellCenters(_tilingData, false);
                TilingStrokePreview(UnityEditor.SceneView.lastActiveSceneView.camera, TilingData.nextHexId, true);
            }
        }
        private static void TilingDuringSceneGUI(UnityEditor.SceneView sceneView)
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                if (_tilingData.state == ToolController.ToolState.EDIT && _tilingData.selectedPointIdx > 0)
                    _tilingData.selectedPointIdx = -1;
                else if (_tilingData.state == ToolController.ToolState.NONE) ToolController.DeselectTool();
                else ResetTilingState(false);
            }
            if (ToolController.editMode || TilingManager.instance.showPreexistingElements) TilingToolEditMode(sceneView);
            if (ToolController.editMode) return;
            switch (_tilingData.state)
            {
                case ToolController.ToolState.NONE:
                    TilingStateNone(sceneView.in2DMode);
                    break;
                case ToolController.ToolState.PREVIEW:
                    TilingStateRectangle(sceneView);
                    break;
                case ToolController.ToolState.EDIT:
                    TilingStateEdit(sceneView.camera);
                    break;
            }
        }
        private static void DrawTilingRectangle(TilingData data)
        {
            var settings = data.settings;
            var cornerPoints = new Vector3[] { data.GetPoint(0), data.GetPoint(1),
                data.GetPoint(2), data.GetPoint(3), data.GetPoint(0) };
            UnityEditor.Handles.color = new Color(0f, 0f, 0f, 0.7f);
            UnityEditor.Handles.DrawAAPolyLine(8, cornerPoints);
            UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.7f);
            UnityEditor.Handles.DrawAAPolyLine(4, cornerPoints);
        }
        private static void UpdateMidpoints(TilingData data)
        {
            for (int i = 0; i < 4; ++i)
            {
                var nextI = (i + 1) % 4;
                var point = data.GetPoint(i);
                var nextPoint = data.GetPoint(nextI);
                data.SetPoint(i + 4, point + (nextPoint - point) / 2, registerUndo: false, selectAll: false);
            }
            data.SetPoint(8, data.GetPoint(0)
                + (data.GetPoint(2) - data.GetPoint(0)) / 2, registerUndo: false, selectAll: false);
        }
        private static void DrawCells(TilingData data) => UpdateCellCenters(data, true);
        private static void DrawTilingGrid(TilingData data)
        {
            DrawCells(data);
            DrawTilingRectangle(data);
        }
        public static TilingData tilingData => ToolController.editMode ? _selectedPersistentTilingData : _tilingData;
        private static void ApplyTilingHandlePosition(TilingData data) => SetTilingSelectedPoint(data, _handlePosition);
        private static bool SetTilingSelectedPoint(TilingData data, Vector3 position)
        {
            if (data.selectedPointIdx < 0) return false;
            _handlePosition = position;
            var prevPosition = data.selectedPoint;
            var snappedPoint = SnapToBounds(_handlePosition);
            snappedPoint = SnapAndUpdateGridOrigin(snappedPoint, GridManager.settings.snappingEnabled,
               data.settings.paintOnPalettePrefabs, data.settings.paintOnMeshesWithoutCollider,
               data.settings.ignoreSceneColliders, paintOnTheGrid: false, Vector3.down);
            data.SetPoint(data.selectedPointIdx, snappedPoint, registerUndo: true, selectAll: false);
            _handlePosition = data.selectedPoint;
            if (prevPosition == data.selectedPoint) return false;

            updateStroke = true;
            var delta = data.selectedPoint - prevPosition;
            if (data.selectedPointIdx < 4)
            {
                var nextCornerIdx = (data.selectedPointIdx + 1) % 4;
                var oppositeCornerIdx = (data.selectedPointIdx + 2) % 4;
                var prevCornerIdx = (data.selectedPointIdx + 3) % 4;

                var nextVector = data.GetPoint(nextCornerIdx) - prevPosition;
                var prevVector = data.GetPoint(prevCornerIdx) - prevPosition;
                var deltaNext = Vector3.Project(delta, nextVector);
                var deltaPrev = Vector3.Project(delta, prevVector);
                var deltaNormal = delta - deltaNext - deltaPrev;
                data.AddValue(nextCornerIdx, deltaPrev + deltaNormal);
                data.AddValue(prevCornerIdx, deltaNext + deltaNormal);
                data.AddValue(oppositeCornerIdx, deltaNormal);
            }
            else if (data.selectedPointIdx < 8)
            {
                var prevCornerIdx = data.selectedPointIdx - 4;
                var nextCornerIdx = (data.selectedPointIdx - 3) % 4;
                var oppositeSideIdx = (data.selectedPointIdx - 2) % 4 + 4;
                var parallel = data.GetPoint(nextCornerIdx) - data.GetPoint(prevCornerIdx);
                var perpendicular = data.GetPoint(oppositeSideIdx) - prevPosition;
                var deltaParallel = Vector3.Project(delta, parallel);
                var deltaPerpendicular = Vector3.Project(delta, perpendicular);
                var deltaNormal = delta - deltaParallel - deltaPerpendicular;
                for (int i = 0; i < 4; ++i) data.AddValue(i, deltaParallel + deltaNormal);
                data.AddValue(prevCornerIdx, deltaPerpendicular);
                data.AddValue(nextCornerIdx, deltaPerpendicular);
            }
            else for (int i = 0; i < 4; ++i) data.AddValue(i, delta);
            UpdateMidpoints(data);
            UpdateCellCenters(data, false);
            return true;
        }
        private static bool SetTilingRotation(TilingData data, Quaternion rotation)
        {
            var prevRotation = data.settings.rotation;
            data.settings.rotation = rotation;
            if (data.settings.rotation == prevRotation) return false;

            ToolProperties.RegisterUndo(TilingData.COMMAND_NAME);
            updateStroke = true;
            var delta = rotation * Quaternion.Inverse(prevRotation);
            for (int i = 0; i < 8; ++i)
            {
                var centerToPoint = data.GetPoint(i) - data.GetPoint(8);
                var rotatedPos = (delta * centerToPoint) + data.GetPoint(8);
                data.SetPoint(i, rotatedPos, registerUndo: false, selectAll: false);
            }
            DrawCells(data);
            ToolProperties.RepainWindow();
            UpdateCellCenters(data, false);
            return true;
        }
        public static void UpdateCellSize()
        {
            if (ToolController.editMode)
            {
                if (_selectedPersistentTilingData == null) return;
                _selectedPersistentTilingData.settings.UpdateCellSize();
                UpdateCellCenters(_selectedPersistentTilingData, true);
            }
            _tilingData.settings.UpdateCellSize();
            UpdateCellCenters(_tilingData, true);
        }
        private static Vector2Int _tilingSize = Vector2Int.zero;
        private static void UpdateCellCenters(TilingData data, bool DrawCells)
        {
            if (!ToolController.editMode && data.state == ToolController.ToolState.NONE) return;
            data.tilingCenters.Clear();
            var settings = data.settings;
            var tangentDir = data.GetPoint(1) - data.GetPoint(0);
            var tangentSize = tangentDir.magnitude;
            tangentDir.Normalize();
            var bitangentDir = data.GetPoint(3) - data.GetPoint(0);
            var bitangentSize = bitangentDir.magnitude;
            bitangentDir.Normalize();
            var cellTangent = tangentDir * Mathf.Abs(settings.cellSize.x);
            var cellBitangent = bitangentDir * Mathf.Abs(settings.cellSize.y);
            var vertices = new Vector3[] { Vector3.zero, cellTangent, cellTangent + cellBitangent, cellBitangent };
            var offset = data.GetPoint(0);
            void SetTileCenter()
            {
                var linePoints = new Vector3[5];
                for (int i = 0; i <= 4; ++i) linePoints[i] = vertices[i % 4] + offset;
                var cellCenter = linePoints[0] + (linePoints[2] - linePoints[0]) / 2;
                data.tilingCenters.Add(cellCenter);
                if (!DrawCells) return;
                UnityEditor.Handles.color = new Color(0f, 0f, 0f, 0.3f);

                UnityEditor.Handles.DrawAAPolyLine(6, linePoints);
                UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.3f);
                UnityEditor.Handles.DrawAAPolyLine(2, linePoints);

            }
            var minCellSize = settings.cellSize + settings.spacing;
            minCellSize = Vector2.Max(minCellSize, Vector2.one * 0.001f);
            var cellSize = minCellSize - settings.spacing;
            float tangentOffset = 0;
            _tilingSize = Vector2Int.zero;
            while (Mathf.Abs(tangentOffset) + Mathf.Abs(cellSize.x) <= tangentSize)
            {
                float bitangentOffset = 0;
                ++_tilingSize.x;
                var sizeY = 0;
                while (Mathf.Abs(bitangentOffset) + Mathf.Abs(cellSize.y) <= bitangentSize)
                {
                    SetTileCenter();
                    bitangentOffset += minCellSize.y;
                    offset = data.GetPoint(0) + tangentDir * Mathf.Abs(tangentOffset)
                        + bitangentDir * Mathf.Abs(bitangentOffset);
                    ++sizeY;
                }
                _tilingSize.y = Mathf.Max(_tilingSize.y, sizeY);
                tangentOffset += minCellSize.x;
                offset = data.GetPoint(0) + tangentDir * Mathf.Abs(tangentOffset);
            }
        }

        private static Vector3 _rotateTilingAxis = Vector3.zero;

        private static bool _rotateTiling90 = false;
        public static void ShowTilingContextMenu(TilingData data, Vector2 mousePosition)
        {
            if (!ToolController.editMode) return;
            void Rotate90(Vector3 axis)
            {
                if (ToolController.editMode) SelectTiling(data);
                _rotateTiling90 = true;
                _rotateTilingAxis = axis;
            }
            var menu = new UnityEditor.GenericMenu();
            menu.AddItem(new GUIContent("Rotate 90� around Y ... "
                + PWBSettings.shortcuts.selectionRotate90YCW.combination.ToString()), on: false,
                () => Rotate90(Vector3.down));
            menu.AddItem(new GUIContent("Rotate -90� around Y ... "
                + PWBSettings.shortcuts.selectionRotate90YCCW.combination.ToString()), on: false,
                () => Rotate90(Vector3.up));
            menu.AddItem(new GUIContent("Rotate 90� around X ... "
                + PWBSettings.shortcuts.selectionRotate90XCW.combination.ToString()), on: false,
                () => Rotate90(Vector3.left));
            menu.AddItem(new GUIContent("Rotate -90� around X ... "
                + PWBSettings.shortcuts.selectionRotate90XCCW.combination.ToString()), on: false,
                () => Rotate90(Vector3.right));
            menu.AddItem(new GUIContent("Rotate 90� around Z ... "
                + PWBSettings.shortcuts.selectionRotate90ZCW.combination.ToString()), on: false,
                () => Rotate90(Vector3.back));
            menu.AddItem(new GUIContent("Rotate -90� around Z ... "
                + PWBSettings.shortcuts.selectionRotate90ZCCW.combination.ToString()), on: false,
                () => Rotate90(Vector3.forward));
            menu.AddSeparator(string.Empty);
            PersistentItemContextMenu(menu, data, mousePosition);
            menu.ShowAsContext();
        }
        private static bool DrawTilingControlPoints(TilingData data,
            out bool clickOnPoint, out bool wasEdited, out Vector3 delta)
        {
            delta = Vector3.zero;
            clickOnPoint = false;
            wasEdited = false;

            for (int i = 0; i < 9; ++i)
            {
                var controlId = GUIUtility.GetControlID(FocusType.Passive);
                if (!clickOnPoint)
                {
                    float distFromMouse
                        = UnityEditor.HandleUtility.DistanceToRectangle(data.GetPoint(i), Quaternion.identity, 0f);
                    UnityEditor.HandleUtility.AddControl(controlId, distFromMouse);
                    if (Event.current.button == 0 && Event.current.type == EventType.MouseDown
                        && UnityEditor.HandleUtility.nearestControl == controlId)
                    {
                        data.selectedPointIdx = i;
                        clickOnPoint = true;
                        Event.current.Use();
                    }
                    if (Event.current.button == 1 && Event.current.type == EventType.MouseDown
                      && !Event.current.control && !Event.current.shift && !Event.current.alt
                          && UnityEditor.HandleUtility.nearestControl == controlId)
                    {
                        ShowTilingContextMenu(data,
                            UnityEditor.EditorGUIUtility.GUIToScreenPoint(Event.current.mousePosition));
                        Event.current.Use();
                    }
                }
                if (Event.current.type != EventType.Repaint) continue;
                DrawDotHandleCap(data.GetPoint(i));
            }
            if (clickOnPoint) ToolProperties.RepainWindow();
            if (data.selectedPointIdx < 0) return false;
            var prevPoint = data.selectedPoint;
            wasEdited = SetTilingSelectedPoint(data,
                PWBPositionHandle(CONTROL_POINT_HANDLE_ID, data.selectedPoint, data.settings.rotation));
            if (prevPoint != data.selectedPoint) ToolProperties.RepainWindow();
            if (data.selectedPointIdx == 8)
            {
                var prevRotation = data.settings.rotation;
                wasEdited = wasEdited || SetTilingRotation(data,
                    UnityEditor.Handles.RotationHandle(data.settings.rotation, data.GetPoint(8)));
                if (prevRotation != data.settings.rotation) ToolProperties.RepainWindow();
            }
            return clickOnPoint || wasEdited;
        }
        private static bool TilingShortcuts(TilingData data)
        {
            if (data == null) return false;
            var keyCode = Event.current.keyCode;

            var spacing1 = PWBSettings.shortcuts.tilingEditSpacing1.Check();
            var spacing2 = PWBSettings.shortcuts.tilingEditSpacing2.Check();
            if (spacing1 || spacing2)
            {
                var delta = spacing1 ? PWBSettings.shortcuts.tilingEditSpacing1.combination.delta
                    : -PWBSettings.shortcuts.tilingEditSpacing2.combination.delta;
                var deltaSign = -Mathf.Sign(delta);
                var otherAxes = AxesUtils.GetOtherAxes(AxesUtils.Axis.Y);
                var spacing = Vector3.zero;
                AxesUtils.SetAxisValue(ref spacing, otherAxes[0], data.settings.spacing.x);
                AxesUtils.SetAxisValue(ref spacing, otherAxes[1], data.settings.spacing.y);
                var axisIdx = spacing1 ? 1 : 0;
                var size = data.GetPoint(2) - data.GetPoint(axisIdx);
                var axisSize = AxesUtils.GetAxisValue(size, otherAxes[axisIdx]);
                AxesUtils.AddValueToAxis(ref spacing, otherAxes[axisIdx], axisSize * deltaSign * 0.005f);
                data.settings.spacing = new Vector2(AxesUtils.GetAxisValue(spacing, otherAxes[0]),
                    AxesUtils.GetAxisValue(spacing, otherAxes[1]));
                ToolProperties.RepainWindow();
                Event.current.Use();
                return true;
            }
            void Rotate90(Vector3 axis)
            {
                _rotateTiling90 = true;
                _rotateTilingAxis = axis;
            }
            if (PWBSettings.shortcuts.selectionRotate90XCCW.Check())
            {
                Rotate90(Vector3.right);
                return true;
            }
            if (PWBSettings.shortcuts.selectionRotate90XCW.Check())
            {
                Rotate90(Vector3.left);
                return true;
            }
            if (PWBSettings.shortcuts.selectionRotate90YCCW.Check())
            {
                Rotate90(Vector3.up);
                return true;
            }
            if (PWBSettings.shortcuts.selectionRotate90YCW.Check())
            {
                Rotate90(Vector3.down);
                return true;
            }
            if (PWBSettings.shortcuts.selectionRotate90ZCCW.Check())
            {
                Rotate90(Vector3.forward);
                return true;
            }
            if (PWBSettings.shortcuts.selectionRotate90ZCW.Check())
            {
                Rotate90(Vector3.back);
                return true;
            }
            return false;
        }
        public static void UpdateTilingRotation(Quaternion rotation)
        {
            if (tilingData == null) return;
            updateStroke = true;
            SetTilingRotation(tilingData, rotation);
        }
        #endregion

    }
}
#pragma warning restore UDR0001
