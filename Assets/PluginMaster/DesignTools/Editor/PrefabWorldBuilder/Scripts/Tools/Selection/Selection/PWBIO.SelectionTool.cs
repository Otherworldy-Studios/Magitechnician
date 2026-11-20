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
        #region SELECTION STATE & DATA

        private static int _selectedBoxPointIdx = -1;
        private static Quaternion _selectionRotation = Quaternion.identity;
        private static Vector3 _selectionScale = Vector3.one;
        private static Vector3 _snappedPoint;
        private static bool _snappedPointIsVisible = false;
        private static bool _snappedPointIsSelected = false;
        private static (Vector3 position, GameObject[] selection) _selectionMoveFrom;
        private static bool _selectionMoving = false;
        private static bool _editingSelectionHandlePosition = false;
        private static Vector3 _tempSelectionHandle = Vector3.zero;
        private static bool _selectionChanged = false;
        private static Bounds _selectionBounds;
        private static bool _setSelectionOriginPosition = false;
        private static int _previousHotControl;
        private static Vector3 _selectionHandlePosition = Vector3.zero;
        private static bool _movingSelectionHandle = false;
        private static Quaternion _selectionHnadleRotation = Quaternion.identity;
        private static bool _moveSelectionToMousePositionEnabled = false;
        private static System.Collections.Generic.Dictionary<int, Quaternion> _initialRotations
            = new System.Collections.Generic.Dictionary<int, Quaternion>();
#if UNITY_6000_3_OR_NEWER
        private static System.Collections.Generic.Dictionary<EntityId, float> _selectionSurfaceMagnitudeDic
            = new System.Collections.Generic.Dictionary<EntityId, float>();
#else
        private static System.Collections.Generic.Dictionary<int, float> _selectionSurfaceMagnitudeDic
            = new System.Collections.Generic.Dictionary<int, float>();
#endif
        private static float _selectionSurfaceMagnitude;
        private static bool _editingSelectionRotation = false;

        public static void InitializeSelectionTool()
        {
            SetSelectionOriginPosition();
            SelectionManager.UpdateSelection();
            ResetUnityCurrentTool();
            UpdateOctree();
            _movingSelectionHandle = false;
            _selectionMoving = false;
            _previousHotControl = 0;
            _editingSelectionHandlePosition = false;
            _moveSelectionToMousePositionEnabled = false;
            _editingSelectionRotation = false;
        }

        public static void InitializeSelectionToolOnBrushChanged()
        {
            ApplySelectionFilters();
            _selectionSurfaceMagnitude = 0f;
            _selectionSurfaceMagnitudeDic.Clear();
        }

        public static void SetSelectionOriginPosition() => _setSelectionOriginPosition = true;

        public static void ResetSelectionRotation()
        {
            _selectionRotation = Quaternion.identity;
            UpdateSelection();
        }

        private static Quaternion GetSelectionRotation()
        {
            var rotation = _selectionRotation;
            if (SelectionManager.topLevelSelection.Length == 1)
            {
                if (SelectionManager.topLevelSelection[0] == null) SelectionManager.UpdateSelection();
                else if (SelectionToolController.settings.boxSpace == Space.Self)
                    rotation = SelectionManager.topLevelSelection[0].transform.rotation;
            }
            else if (SelectionToolController.settings.handleSpace == Space.Self)
            {
                var count = 0;
                var avgForward = Vector3.forward;
                var avgUp = Vector3.up;
                if (SelectionManager.topLevelSelection.Length > 0)
                {
                    avgForward = Vector3.zero;
                    avgUp = Vector3.zero;
                }
                foreach (var obj in SelectionManager.topLevelSelection)
                {
                    if (obj == null) continue;
                    ++count;
                    avgForward += obj.transform.rotation * Vector3.forward;
                    avgUp += obj.transform.rotation * Vector3.up;
                }
                avgForward /= count;
                avgUp /= count;
                rotation = Quaternion.LookRotation(avgForward, avgUp);
            }
            return rotation;
        }
#endregion

        #region SELECTION SCENE GUI & INPUT
        private static void SelectionDuringSceneGUI(UnityEditor.SceneView sceneView)
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                _snappedPointIsSelected = false;
                _selectionMoving = false;
                if (_selectedBoxPointIdx >= 0 && _selectedBoxPointIdx != 10) _selectedBoxPointIdx = 10;
                else
                {
                    ResetUnityCurrentTool();
                    ToolController.DeselectTool();
                    return;
                }
            }
            if (UnityEditor.Tools.current != UnityEditor.Tool.View && UnityEditor.Tools.current != UnityEditor.Tool.None)
            {
                _unityCurrentTool = UnityEditor.Tools.current;
                UnityEditor.Tools.current = UnityEditor.Tool.None;
            }
            if (SelectionManager.topLevelSelection.Length == 0) return;

            var points = SelectionPoints(sceneView.camera);

            if (_setSelectionOriginPosition && GridManager.settings.snappingEnabled && !GridManager.settings.lockedGrid)
            {
                _setSelectionOriginPosition = false;
                GridManager.settings.SetOriginHeight(points[_selectedBoxPointIdx], GridManager.settings.gridAxis);
            }

            SelectionInput(points, sceneView.in2DMode);
            if (_selectionMoving)
            {
                UnityEditor.Handles.CircleHandleCap(0, _selectionMoveFrom.position, sceneView.camera.transform.rotation,
                    UnityEditor.HandleUtility.GetHandleSize(_selectionMoveFrom.position) * 0.06f, EventType.Repaint);
                if (_selectedBoxPointIdx >= 0)
                    UnityEditor.Handles.DrawLine(_selectionMoveFrom.position, points[_selectedBoxPointIdx]);
            }

            bool mouseDown = Event.current.button == 0 && Event.current.type == EventType.MouseDown;
            bool clickOnPoint = false;

            bool SelectPoint(Vector3 point, int i)
            {
                if (_editingSelectionHandlePosition) return false;
                if (clickOnPoint) return false;
                var controlId = GUIUtility.GetControlID(FocusType.Passive);
                var distFromMouse = UnityEditor.HandleUtility.DistanceToRectangle(point, Quaternion.identity, 0f);
                UnityEditor.HandleUtility.AddControl(controlId, distFromMouse);
                if (UnityEditor.HandleUtility.nearestControl != controlId) return false;
                DrawDotHandleCap(point, 1f, 1.2f);
                if (!mouseDown) return false;
                _selectedBoxPointIdx = i;
                clickOnPoint = true;
                Event.current.Use();
                return true;
            }

            for (int i = 0; i < points.Count; ++i)
            {
                if (SelectPoint(points[i], i)) _snappedPointIsSelected = false;
                if (clickOnPoint) break;
            }

            if (_snappedPointIsVisible || _snappedPointIsSelected)
            {
                points.Add(_snappedPoint);
                if (SelectPoint(_snappedPoint, points.Count - 1)) _snappedPointIsSelected = true;
            }
            if (_selectionChanged)
            {
                _tempSelectionHandle = Vector3.zero;
                _selectionChanged = false;
                ApplySelectionFilters();
            }

            if (_editingSelectionHandlePosition)
            {
                _selectedBoxPointIdx = 11;
                UnityEditor.Handles.CircleHandleCap(0, points[11], sceneView.camera.transform.rotation,
                    UnityEditor.HandleUtility.GetHandleSize(points[11]) * 0.06f, EventType.Repaint);
            }
            if (_selectedBoxPointIdx >= 0)
            {
                var rotation = GetSelectionRotation();
                if (_editingSelectionHandlePosition)
                {
                    var delta = points[_selectedBoxPointIdx];
                    points[_selectedBoxPointIdx] = PWBPositionHandle(TOOL_HANDLE_ID, points[_selectedBoxPointIdx], rotation);
                    delta = points[_selectedBoxPointIdx] - delta;
                    _tempSelectionHandle += delta;
                }
                else
                {
                    bool handleWasUsed = MoveSelectionToHandlePosition(rotation, points, sceneView);
                    handleWasUsed |= MoveSelectionToMousePosition(points, sceneView);
                    if (RotateSelection(rotation, points, sceneView))
                    {
                        handleWasUsed = true;
                    }
                    handleWasUsed |= ScaleSelection(rotation, points);
                    if (handleWasUsed)
                    {
                        if (SelectionToolController.settings.embedInSurface) EmbedSelectionInSurface(_selectionRotation);
                        PWBCore.UpdateTempCollidersTransforms(SelectionManager.topLevelSelection);
                    }
                }
            }
            else _editingSelectionHandlePosition = false;
        }

        private static void SelectionInput(System.Collections.Generic.List<Vector3> points, bool in2DMode)
        {
            if (UnityEditor.Tools.current == UnityEditor.Tool.Move) return;
            var keyCode = Event.current.keyCode;
            if (PWBSettings.shortcuts.selectionTogglePositionHandle.Check())
            {
                SelectionToolController.settings.move = !SelectionToolController.settings.move;
                PWBToolbar.RepaintWindow();
            }
            else if (PWBSettings.shortcuts.selectionToggleRotationHandle.Check())
            {
                SelectionToolController.settings.rotate = !SelectionToolController.settings.rotate;
                PWBToolbar.RepaintWindow();
            }
            else if (PWBSettings.shortcuts.selectionToggleScaleHandle.Check())
            {
                SelectionToolController.settings.scale = !SelectionToolController.settings.scale;
                PWBToolbar.RepaintWindow();
            }
            else if (Event.current.type == EventType.KeyDown
                && (PWBSettings.shortcuts.selectionEditCustomHandle.Check()
                || (_editingSelectionHandlePosition && (Event.current.keyCode == KeyCode.Escape
                || Event.current.keyCode == KeyCode.Return))))
            {
                _editingSelectionHandlePosition = !_editingSelectionHandlePosition;
            }
            else if (_snappedToVertex && _selectedBoxPointIdx < 0)
            {
                _snappedPointIsVisible = false;
                var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                if (SnapToVertex(mouseRay, out RaycastHit snappedHit, in2DMode, SelectionManager.topLevelSelection))
                {
                    _snappedPoint = snappedHit.point;
                    _snappedPointIsVisible = true;
                }
            }
            else if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return
                && _selectedBoxPointIdx >= 0)
            {
                _editingSelectionHandlePosition = false;
                if (_selectionMoving)
                {
                    var delta = points[_selectedBoxPointIdx] - _selectionMoveFrom.position;
                    foreach (var obj in _selectionMoveFrom.selection)
                    {
                        if (obj == null) continue;
                        UnityEditor.Undo.RecordObject(obj.transform, "Move Selection");
                        obj.transform.position += delta;
                    }
                    _selectionMoving = false;
                    SelectionManager.UpdateSelection();
                    _selectedBoxPointIdx = -1;
                }
                else
                {
                    _selectionMoveFrom = (points[_selectedBoxPointIdx], SelectionManager.topLevelSelection);
                    _selectionMoving = true;
                }
            }
            else if (PWBSettings.shortcuts.selectionRotate90XCCW.Check())
                RotateSelection90Deg(Vector3.left, points);
            else if (PWBSettings.shortcuts.selectionRotate90XCW.Check())
                RotateSelection90Deg(Vector3.right, points);
            else if (PWBSettings.shortcuts.selectionRotate90YCCW.Check())
                RotateSelection90Deg(Vector3.down, points);
            else if (PWBSettings.shortcuts.selectionRotate90YCW.Check())
                RotateSelection90Deg(Vector3.up, points);
            else if (PWBSettings.shortcuts.selectionRotate90ZCCW.Check())
                RotateSelection90Deg(Vector3.back, points);
            else if (PWBSettings.shortcuts.selectionRotate90ZCW.Check())
                RotateSelection90Deg(Vector3.forward, points);
            else if (PWBSettings.shortcuts.selectionToggleSpace.Check())
            {
                SelectionToolController.settings.handleSpace = SelectionToolController.settings.handleSpace == Space.Self
                    ? Space.World : Space.Self;
                if (SelectionToolController.settings.handleSpace == Space.World) ResetSelectionRotation();
                UnityEditor.SceneView.RepaintAll();
                ToolProperties.RepainWindow();
                Event.current.Use();
            }
        }

        public static void ApplySelectionFilters()
        {
            var selection = SelectionManager.topLevelSelection;
            if (selection == null)
            {
                SelectionManager.UpdateSelection();
                selection = SelectionManager.topLevelSelection;
            }

            var filtered = new System.Collections.Generic.List<GameObject>(selection.Length);

            for (int i = 0; i < selection.Length; ++i)
            {
                var obj = selection[i];
                if (obj == null) continue;

                if (SelectionToolController.settings.paletteFilter)
                {
                    if (PaletteManager.selectedPalette == null) continue;
                    if (!PaletteManager.selectedPalette.ContainsSceneObject(obj)) continue;
                }

                if (SelectionToolController.settings.brushFilter)
                {
                    if (PaletteManager.selectedBrush == null) continue;
                    if (!PaletteManager.selectedBrush.ContainsSceneObject(obj)) continue;
                }

                if (SelectionToolController.settings.layerFilter != -1)
                {
                    var layerMask = SelectionToolController.settings.layerFilter;
                    if ((layerMask & (1 << obj.layer)) == 0) continue;
                }

                if (SelectionToolController.settings.tagFilter != null
                    && SelectionToolController.settings.tagFilter.Count > 0)
                {
                    bool tagFound = false;
                    for (int t = 0; t < SelectionToolController.settings.tagFilter.Count; ++t)
                    {
                        if (obj.tag == SelectionToolController.settings.tagFilter[t])
                        {
                            tagFound = true;
                            break;
                        }
                    }
                    if (!tagFound) continue;
                }
                else continue;
                filtered.Add(obj);
            }

            UnityEditor.Selection.objects = filtered.ToArray();
        }
        #endregion
    }
}
#pragma warning restore UDR0001
