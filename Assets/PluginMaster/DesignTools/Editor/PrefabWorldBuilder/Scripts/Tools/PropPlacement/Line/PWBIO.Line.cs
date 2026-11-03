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
        private static void LineInitializeOnLoad()
        {
            LineManager.settings.OnDataChanged += OnLineSettingsChanged;
            BrushSettings.OnBrushSettingsChanged += PreviewSelectedPersistentLines;
        }
        private static void OnLineToolModeChanged()
        {
            DeselectPersistentLines();
            if (!ToolController.editMode)
            {
                ToolProperties.RepainWindow();
                return;
            }
            if (_lineData != null || _selectedPersistentLineData != null)
                ResetLineState();
            ResetSelectedPersistentLine();
            LineManager.editModeType = LineManager.EditModeType.NODES;
        }
        private static void OnLineSettingsChanged()
        {
            repaint = true;
            if (!ToolController.editMode)
            {
                _lineData.settings = LineManager.settings;
                updateStroke = true;
                return;
            }
            if (_selectedPersistentLineData == null) return;
            _selectedPersistentLineData.settings.Copy(LineManager.settings);
            PreviewPersistentLine(_selectedPersistentLineData);
        }
        private static void OnUndoLine() => ClearLineStroke();
        #endregion
        #region LINE DATA & SCENE GUI
        private static LineData _lineData = LineData.instance;
        private static bool _selectingLinePoints = false;
        private static Rect _selectionRect = new Rect();
        private static string _createProfileName = ToolProfile.DEFAULT;
        private static Quaternion _lineRotation = Quaternion.identity;

        public static LineData lineData
            => (ToolController.editMode && _selectedPersistentLineData != null) ? _selectedPersistentLineData : _lineData;
        public static bool selectingLinePoints
        {
            get => _selectingLinePoints;
            set
            {
                if (value == _selectingLinePoints) return;
                _selectingLinePoints = value;
            }
        }

        private static void ClearLineStroke()
        {
            _paintStroke.Clear();
            BrushstrokeManager.ClearBrushstroke();
            if (ToolController.editMode && _selectedPersistentLineData != null)
            {
                _selectedPersistentLineData.UpdatePath(forceUpdate: true, updateOnSurfacePoints: false);
                PreviewPersistentLine(_selectedPersistentLineData);
                UnityEditor.SceneView.RepaintAll();
                repaint = true;
            }
        }

        private static void LineDuringSceneGUI(UnityEditor.SceneView sceneView)
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                if (_lineData.state == ToolController.ToolState.EDIT && _lineData.selectedPointIdx > 0)
                    _lineData.ClearSelection();
                else if (_lineData.state == ToolController.ToolState.NONE && !ToolController.editMode)
                    ToolController.DeselectTool();
                else if (ToolController.editMode)
                {
                    if (_editingPersistentLine) ResetSelectedPersistentLine();
                    else ToolController.DeselectTool();
                    DeselectPersistentLines();
                    _initialPersistentLineData = null;
                    _selectedPersistentLineData = null;
                    ToolProperties.RepainWindow();
                    ToolController.editMode = false;
                }
                else ResetLineState(false);
                OnUndoLine();
                UpdateStroke();
                BrushstrokeManager.ClearBrushstroke();
            }

            LineToolEditMode(sceneView);
            if (ToolController.editMode) return;

            switch (_lineData.state)
            {
                case ToolController.ToolState.NONE:
                    LineStateNone(sceneView.in2DMode);
                    break;
                case ToolController.ToolState.PREVIEW:
                    LineStateStraightLine(sceneView.in2DMode);
                    break;
                case ToolController.ToolState.EDIT:
                    LineStateBezier(sceneView);
                    break;
            }
        }

        private static void RotateLineAround(int idx, Quaternion rotation, LineData lineData)
        {
            var pivotPosition = lineData.GetPoint(idx);
            for (int i = 0; i < lineData.pointsCount; ++i)
            {
                if (i == idx) continue;
                var localPositionUnrotated = Quaternion.Inverse(_lineRotation) * (lineData.GetPoint(i) - pivotPosition);
                var localPosition = rotation * localPositionUnrotated;
                lineData.SetRotatedPoint(i, pivotPosition + localPosition, true);
            }
            _lineRotation = rotation;
            lineData.UpdatePath(forceUpdate: false, updateOnSurfacePoints: true);
        }

        public static void ResetLineRotation() => _lineRotation = Quaternion.identity;

        public static void UpdateLinePathAndStroke(LineData data)
        {
            data.UpdatePath(forceUpdate: true, updateOnSurfacePoints: true);
            PWBIO.PreviewPersistentLine(data);
        }

        private static Vector3 LinePointSnapping(Vector3 point)
        {
            const float snapSqrDistance = 400f;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            var persistentLines = LineManager.instance.GetPersistentItems();
            var result = point;
            var minSqrDistance = snapSqrDistance;
            foreach (var lineData in persistentLines)
            {
                var controlPoints = lineData.points;
                foreach (var controlPoint in controlPoints)
                {
                    var intersection = mouseRay.origin + Vector3.Project(controlPoint - mouseRay.origin, mouseRay.direction);
                    var GUIControlPoint = UnityEditor.HandleUtility.WorldToGUIPoint(controlPoint);
                    var intersectionGUIPoint = UnityEditor.HandleUtility.WorldToGUIPoint(intersection);
                    var sqrDistance = (GUIControlPoint - intersectionGUIPoint).sqrMagnitude;
                    if (sqrDistance > 0 && sqrDistance < snapSqrDistance && sqrDistance < minSqrDistance)
                    {
                        minSqrDistance = sqrDistance;
                        result = controlPoint;
                    }
                }
            }
            return result;
        }

        private static void DrawLine(LineData lineData, bool drawSurfacePath)
        {
            var pathPoints = lineData.pathPoints;
            var surfacePathPoints = lineData.onSurfacePathPoints;
            if (pathPoints.Length == 0 || (drawSurfacePath && surfacePathPoints.Length == 0))
                lineData.UpdatePath(forceUpdate: true, updateOnSurfacePoints: drawSurfacePath);
            UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            if (drawSurfacePath)
            {
                UnityEditor.Handles.color = new Color(0f, 0f, 0f, 0.7f);
                UnityEditor.Handles.DrawAAPolyLine(8, surfacePathPoints);
                UnityEditor.Handles.color = new Color(0f, 1f, 1f, 0.5f);
                UnityEditor.Handles.DrawAAPolyLine(4, surfacePathPoints);
            }

            UnityEditor.Handles.color = new Color(0f, 0f, 0f, 0.7f);
            UnityEditor.Handles.DrawAAPolyLine(8, pathPoints);
            UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.7f);
            UnityEditor.Handles.DrawAAPolyLine(4, pathPoints);
        }

        private static void DrawSelectionRectangle()
        {
            if (!selectingLinePoints) return;
            var rays = new Ray[]
            {
                UnityEditor.HandleUtility.GUIPointToWorldRay(_selectionRect.min),
                UnityEditor.HandleUtility.GUIPointToWorldRay(new Vector2(_selectionRect.xMax, _selectionRect.yMin)),
                UnityEditor.HandleUtility.GUIPointToWorldRay(_selectionRect.max),
                UnityEditor.HandleUtility.GUIPointToWorldRay(new Vector2(_selectionRect.xMin, _selectionRect.yMax))
            };
            var verts = new Vector3[4];
            for (int i = 0; i < 4; ++i) verts[i] = rays[i].origin + rays[i].direction;
            UnityEditor.Handles.DrawSolidRectangleWithOutline(verts,
            new Color(0f, 0.5f, 0.5f, 0.3f), new Color(0f, 0.5f, 0.5f, 1f));
        }

        public static void ApplyPersistentLine(LineData data)
        {
            data.UpdatePoses();
            DeleteDisabledObjects();
            PWBCore.staticData.SetSavePending();
            AutoSave.QuickSave();
        }
        #endregion

    }
}
#pragma warning restore UDR0001
