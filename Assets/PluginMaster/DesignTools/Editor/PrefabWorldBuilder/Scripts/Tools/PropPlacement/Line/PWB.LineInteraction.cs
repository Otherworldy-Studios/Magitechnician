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
        public static void ApplyPersistentLineAndReset(LineData data)
        {
            data.UpdatePath(forceUpdate: true, updateOnSurfacePoints: true);
            PreviewPersistentLine(data);
            DeleteDisabledObjects();
            ApplyPersistentLine(data);
            _initialPersistentLineData = null;
            _selectedPersistentLineData = null;
            UnityEditor.SceneView.RepaintAll();
        }

        public static void DeleteLinePoints(LineData data, int[] indexes, bool isPersistent)
        {
            if (isPersistent && data.pointsCount - indexes.Length <= 1)
            {
                LineManager.instance.DeletePersistentItem(data.id, deleteObjects: true);
                UnityEditor.SceneView.RepaintAll();
                return;
            }
            data.RemovePoints(indexes);
            if (isPersistent) ApplyPersistentLineAndReset(data);
            if (data.pointsCount >= 2) updateStroke = true;
        }

        public static void ShowLineContextMenu(LineData data, bool isPersistent, Vector2 mousePosition, int pointIdx)
        {
            if (isPersistent && !ToolController.editMode) return;
            var menu = new UnityEditor.GenericMenu();

            menu.AddItem(new GUIContent("Delete point ... Delete"), on: false, () =>
            {
                if (isPersistent && data.pointsCount <= 2)
                {
                    LineManager.instance.DeletePersistentItem(data.id, deleteObjects: true);
                    UnityEditor.SceneView.RepaintAll();
                    return;
                }
                data.RemovePoint(pointIdx);
                if (isPersistent)
                {
                    data.UpdatePath(forceUpdate: true, updateOnSurfacePoints: true);
                    PreviewPersistentLine(data);
                    DeleteDisabledObjects();
                    ApplyPersistentLine(data);
                    _initialPersistentLineData = null;
                    _selectedPersistentLineData = null;
                }
                if (data.pointsCount >= 2) updateStroke = true;
            });
            menu.AddItem(new GUIContent("Delete selected points ... Delete"), on: false, () =>
            {
                if (isPersistent && data.pointsCount - data.selectionCount <= 1)
                {
                    LineManager.instance.DeletePersistentItem(data.id, deleteObjects: true);
                    UnityEditor.SceneView.RepaintAll();
                    return;
                }
                data.RemoveSelectedPoints();
                if (isPersistent)
                {
                    data.UpdatePath(forceUpdate: true, updateOnSurfacePoints: true);
                    PreviewPersistentLine(data);
                    DeleteDisabledObjects();
                    ApplyPersistentLine(data);
                    _initialPersistentLineData = null;
                    _selectedPersistentLineData = null;
                }
                if (data.pointsCount >= 2) updateStroke = true;
            });
            menu.AddItem(new GUIContent("Select all points ... "
                + PWBSettings.shortcuts.lineSelectAllPoints.combination.ToString()), on: false, () => data.SelectAll());
            menu.AddItem(new GUIContent("Deselect all points ... "
                + PWBSettings.shortcuts.lineDeselectAllPoints.combination.ToString()), on: false,
                () => data.ClearSelection());
            menu.AddItem(new GUIContent("Set prev segment as straight or curved ... "
                + PWBSettings.shortcuts.lineToggleCurve.combination.ToString()), on: false, () =>
                {
                    data.ToggleSegmentType();
                    updateStroke = true;
                });
            menu.AddItem(new GUIContent("Close or open the path ... "
                + PWBSettings.shortcuts.lineToggleClosed.combination.ToString()), on: false, () =>
                {
                    data.ToggleClosed();
                    updateStroke = true;
                });

            menu.AddSeparator(string.Empty);
            PersistentItemContextMenu(menu, data, mousePosition);
            menu.ShowAsContext();
        }

        private static bool DrawLineControlPoints(LineData lineData, bool isPersistent, bool showHandles,
            out bool clickOnPoint, out bool multiSelection, out bool addToSelection,
            out bool removedFromSelection, out bool wasEdited, out Vector3 delta)
        {
            delta = Vector3.zero;
            clickOnPoint = false;
            wasEdited = false;
            multiSelection = false;
            addToSelection = false;
            removedFromSelection = false;
            bool leftMouseDown = Event.current.button == 0 && Event.current.type == EventType.MouseDown;
            bool selectAll = ToolController.editMode && LineManager.editModeType == LineManager.EditModeType.LINE_POSE;
            bool selectionChanged = false;
            for (int i = 0; i < lineData.pointsCount; ++i)
            {
                if (selectingLinePoints)
                {
                    var GUIPos = UnityEditor.HandleUtility.WorldToGUIPoint(lineData.GetPoint(i));
                    var rect = _selectionRect;
                    if (_selectionRect.size.x < 0 || _selectionRect.size.y < 0)
                    {
                        var max = Vector2.Max(_selectionRect.min, _selectionRect.max);
                        var min = Vector2.Min(_selectionRect.min, _selectionRect.max);
                        var size = max - min;
                        rect = new Rect(min, size);
                    }
                    if (rect.Contains(GUIPos))
                    {
                        if (!Event.current.control && lineData.selectedPointIdx < 0) lineData.selectedPointIdx = i;
                        lineData.AddToSelection(i);
                        clickOnPoint = true;
                        multiSelection = true;
                        selectionChanged = true;
                    }
                }
                else
                {
                    var controlId = GUIUtility.GetControlID(FocusType.Passive);
                    float distFromMouse = UnityEditor.HandleUtility.DistanceToRectangle(lineData.GetPoint(i),
                        Quaternion.identity, 0f);
                    UnityEditor.HandleUtility.AddControl(controlId, distFromMouse);

                    var handleSize = UnityEditor.HandleUtility.GetHandleSize(lineData.GetPoint(i));
                    var guiPoint = UnityEditor.HandleUtility.WorldToGUIPoint(lineData.GetPoint(i));
                    var edgeWorld = lineData.GetPoint(i)
                        + UnityEngine.Camera.current.transform.right
                        * handleSize * 0.0325f * PWBCore.staticData.controPointSize;
                    var guiEdge = UnityEditor.HandleUtility.WorldToGUIPoint(edgeWorld);
                    float radiusPx = Vector2.Distance(guiPoint, guiEdge);
                    float clickRadiusPx = Mathf.Max(radiusPx, 8f);
                    bool mouseNearPoint = Vector2.Distance(guiPoint, Event.current.mousePosition) <= clickRadiusPx;

                    if (!clickOnPoint && showHandles && leftMouseDown
                    && UnityEditor.HandleUtility.nearestControl == controlId && mouseNearPoint)
                    {
                        if (!Event.current.control)
                        {
                            lineData.ClearSelection();
                            lineData.selectedPointIdx = i;
                            selectionChanged = true;
                        }
                        if ((!ToolController.editMode
                            || (ToolController.editMode && LineManager.editModeType == LineManager.EditModeType.NODES))
                            && (Event.current.control || lineData.selectionCount == 0))
                        {
                            if (lineData.ControlPointIsSelected(i))
                            {
                                lineData.RemoveFromSelection(i);
                                lineData.selectedPointIdx = -1;
                                removedFromSelection = true;
                            }
                            else
                            {
                                lineData.AddToSelection(i);
                                lineData.showHandles = true;
                                lineData.selectedPointIdx = i;
                                if (Event.current.control) addToSelection = true;
                            }
                            selectionChanged = true;
                        }
                        clickOnPoint = true;
                        Event.current.Use();
                    }
                    if (Event.current.button == 1 && Event.current.type == EventType.MouseDown
                        && !Event.current.control && !Event.current.shift && !Event.current.alt
                            && UnityEditor.HandleUtility.nearestControl == controlId && mouseNearPoint)
                    {
                        ShowLineContextMenu(lineData, isPersistent,
                            UnityEditor.EditorGUIUtility.GUIToScreenPoint(Event.current.mousePosition), pointIdx: i);
                        Event.current.Use();
                    }
                }
                if (Event.current.type != EventType.Repaint) continue;
                DrawDotHandleCap(lineData.GetPoint(i), 1, 1, lineData.ControlPointIsSelected(i));
            }
            if (selectionChanged) ResetLineRotation();
            var midpoints = lineData.midpoints;
            for (int i = 0; i < midpoints.Length; ++i)
            {
                var point = midpoints[i];

                var controlId = GUIUtility.GetControlID(FocusType.Passive);
                if (showHandles)
                {
                    float distFromMouse
                           = UnityEditor.HandleUtility.DistanceToRectangle(point, Quaternion.identity, 0f);
                    UnityEditor.HandleUtility.AddControl(controlId, distFromMouse);
                }
                DrawDotHandleCap(point, 0.4f);
                if (showHandles && UnityEditor.HandleUtility.nearestControl == controlId)
                {
                    DrawDotHandleCap(point);
                    if (leftMouseDown)
                    {
                        lineData.InsertPoint(i + 1, new LinePoint(point));
                        lineData.ClearSelection();
                        lineData.selectedPointIdx = i + 1;
                        updateStroke = true;
                        clickOnPoint = true;
                        Event.current.Use();
                    }
                }
            }
            if (showHandles && lineData.showHandles && lineData.selectedPointIdx >= 0)
            {
                var selectedPoint = lineData.selectedPoint;
                if (_updateHandlePosition)
                {
                    selectedPoint = _handlePosition;
                    _updateHandlePosition = false;
                }
                var prevPosition = lineData.selectedPoint;
                lineData.SetPoint(lineData.selectedPointIdx,
                    PWBPositionHandle(CONTROL_POINT_HANDLE_ID, selectedPoint, Quaternion.identity),
                    registerUndo: true, selectAll);
                var point = SnapToBounds(lineData.selectedPoint);
                point = _snapToVertex ? LinePointSnapping(point)
                    : SnapAndUpdateGridOrigin(point, GridManager.settings.snappingEnabled,
                        LineManager.settings.paintOnPalettePrefabs, LineManager.settings.paintOnMeshesWithoutCollider,
                        LineManager.settings.ignoreSceneColliders, paintOnTheGrid: false, Vector3.down);
                lineData.SetPoint(lineData.selectedPointIdx, point, registerUndo: false, selectAll);
                _handlePosition = lineData.selectedPoint;
                if (prevPosition != lineData.selectedPoint)
                {
                    wasEdited = true;
                    updateStroke = true;
                    delta = lineData.selectedPoint - prevPosition;
                    ToolProperties.RepainWindow();
                }
                if (LineManager.editModeType == LineManager.EditModeType.LINE_POSE)
                {
                    var prevRotation = _lineRotation;
                    var handleRotation = UnityEditor.Handles.RotationHandle(_lineRotation, lineData.selectedPoint);
                    if (prevRotation != handleRotation)
                    {
                        RotateLineAround(lineData.selectedPointIdx, handleRotation, lineData);
                        wasEdited = true;
                        updateStroke = true;
                        ToolProperties.RepainWindow();
                    }
                }
            }
            if (!showHandles) return false;
            return clickOnPoint || wasEdited;
        }

        private static void SelectionRectangleInput(bool clickOnPoint)
        {
            bool leftMouseDown = Event.current.button == 1 && Event.current.type == EventType.MouseDown;
            if (!selectingLinePoints && Event.current.shift && leftMouseDown && !clickOnPoint)
            {
                selectingLinePoints = true;
                _selectionRect = new Rect(Event.current.mousePosition, Vector2.zero);
                Event.current.Use();
            }
            if ((Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseMove)
                && selectingLinePoints)
            {
                _selectionRect.size = Event.current.mousePosition - _selectionRect.position;
            }
            if (Event.current.button == 0 && (Event.current.type == EventType.MouseUp
                || Event.current.type == EventType.Ignore || Event.current.type == EventType.KeyUp))
                selectingLinePoints = false;
        }

        private static void LineInput(bool persistent, UnityEditor.SceneView sceneView, bool skipPreview)
        {
            var lineData = persistent ? _selectedPersistentLineData : _lineData;
            if (lineData == null) return;
            if (Event.current.keyCode == KeyCode.Return && Event.current.type == EventType.KeyDown)
            {
                if (persistent)
                {
                    if (skipPreview)
                    {
                        PreviewPersistentLine(lineData);
                        LineStrokePreview(sceneView, lineData, persistent: true, forceUpdate: true, _firstNewObjIdx);
                    }
                    DeleteDisabledObjects();
                    _persistentItemWasEdited = true;
                    ApplySelectedPersistentLine(true);
                    DeleteDisabledObjects();
                    ToolProperties.RepainWindow();
                }
                else
                {
                    CreateLine();
                    ResetLineState(false);
                }
            }
            else if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete
                && !Event.current.control && !Event.current.alt && !Event.current.shift)
            {
                if (persistent && lineData.pointsCount <= 2)
                {
                    LineManager.instance.DeletePersistentItem(lineData.id, deleteObjects: true);
                    UnityEditor.SceneView.RepaintAll();
                }
                else
                {
                    lineData.RemoveSelectedPoints();
                    if (persistent)
                    {
                        lineData.UpdatePath(forceUpdate: true, updateOnSurfacePoints: true);
                        PreviewPersistentLine(lineData);
                        LineStrokePreview(sceneView, lineData, persistent: true, forceUpdate: true, _firstNewObjIdx);
                        DeleteDisabledObjects();
                        ApplySelectedPersistentLine(true);
                        _initialPersistentLineData = null;
                        _selectedPersistentLineData = null;
                    }
                    if (lineData.pointsCount >= 2) updateStroke = true;
                }
            }
            else if (Event.current.type == EventType.MouseDown && Event.current.button == 1
                && Event.current.control && !Event.current.alt && !Event.current.shift
                && LineManager.editModeType == LineManager.EditModeType.NODES)
            {
                if (TryGetMouseWorldHit(out Vector3 point, out Vector3 normal, lineData.settings.mode, sceneView.in2DMode,
                lineData.settings.paintOnPalettePrefabs, lineData.settings.paintOnMeshesWithoutCollider, false,
                ignoreSceneColliders: lineData.settings.ignoreSceneColliders))
                {
                    point = SnapToBounds(point);
                    point = _snapToVertex ? LinePointSnapping(point)
                        : SnapAndUpdateGridOrigin(point, GridManager.settings.snappingEnabled,
                        lineData.settings.paintOnPalettePrefabs, lineData.settings.paintOnMeshesWithoutCollider,
                        lineData.settings.ignoreSceneColliders, paintOnTheGrid: false, Vector3.down);
                    lineData.AddPoint(point, false);
                    if (persistent)
                    {
                        PreviewPersistentLine(_selectedPersistentLineData);
                        LineStrokePreview(sceneView, lineData, persistent: true, forceUpdate: true, _firstNewObjIdx);
                    }
                    else updateStroke = true;
                }
            }
            else if (PWBSettings.shortcuts.lineSelectAllPoints.Check()
                && LineManager.editModeType == LineManager.EditModeType.NODES)
                lineData.SelectAll();
            else if (PWBSettings.shortcuts.lineDeselectAllPoints.Check()) lineData.ClearSelection();
            else if (PWBSettings.shortcuts.lineToggleCurve.Check())
            {
                lineData.ToggleSegmentType();
                updateStroke = true;
            }
            else if (PWBSettings.shortcuts.lineToggleClosed.Check())
            {
                lineData.ToggleClosed();
                updateStroke = true;
            }
            else if (PWBSettings.shortcuts.lineEditGap.Check())
            {
                var deltaSign = Mathf.Sign(PWBSettings.shortcuts.lineEditGap.combination.delta);
                lineData.settings.gapSize += lineData.lenght * deltaSign * 0.001f;
                ToolProperties.RepainWindow();
            }
            if (!persistent) return;
            if (PWBSettings.shortcuts.editModeSelectParent.Check() && lineData != null)
            {
                var parent = lineData.GetParent();
                if (parent != null) UnityEditor.Selection.activeGameObject = parent;
            }
            else if (PWBSettings.shortcuts.editModeDeleteItemButNotItsChildren.Check())
                LineManager.instance.DeletePersistentItem(lineData.id, false);
            else if (PWBSettings.shortcuts.editModeDeleteItemAndItsChildren.Check())
                LineManager.instance.DeletePersistentItem(lineData.id, true);
            else if (PWBSettings.shortcuts.editModeDuplicate.Check()) DuplicateItem(lineData.id);
            else if (PWBSettings.shortcuts.lineEditModeTypeToggle.Check())
                LineManager.ToggleEditModeType();
        }
    }
}
#pragma warning restore UDR0001
