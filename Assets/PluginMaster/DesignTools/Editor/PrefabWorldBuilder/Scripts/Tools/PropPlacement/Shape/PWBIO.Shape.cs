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
        private static void ShapeInitializeOnLoad()
        {
            ShapeManager.settings.OnDataChanged -= OnShapeSettingsChanged;
            ShapeManager.settings.OnDataChanged += OnShapeSettingsChanged;
            BrushSettings.OnBrushSettingsChanged -= PreviewSelectedPersistentShapes;
            BrushSettings.OnBrushSettingsChanged += PreviewSelectedPersistentShapes;
        }
        private static void OnShapeToolModeChanged()
        {
            DeselectPersistentShapes();
            if (!ToolController.editMode)
            {
                ToolProperties.RepainWindow();
                return;
            }
            if (_shapeData != null || _selectedPersistentShapeData != null)
                ResetShapeState();
            ResetSelectedPersistentShape();
        }

        public static void OnShapeSettingsChanged()
        {
            repaint = true;
            if (!ToolController.editMode)
            {
                _shapeData.settings = ShapeManager.settings;
                updateStroke = true;
                return;
            }
            if (_selectedPersistentShapeData == null) return;
            _selectedPersistentShapeData.settings.Copy(ShapeManager.settings);
            _selectedPersistentShapeData.Update(false);
            PreviewPersistentShape(_selectedPersistentShapeData);
        }
        private static void OnUndoShape() => ClearShapeStroke();
        #endregion

        #region COMMON
        private static ShapeData _shapeData = ShapeData.instance;
        private static void ClearShapeStroke()
        {
            _paintStroke.Clear();
            BrushstrokeManager.ClearBrushstroke();
            if (ToolController.editMode)
            {
                PreviewPersistentShape(_selectedPersistentShapeData);
                UnityEditor.SceneView.RepaintAll();
                repaint = true;
            }
        }

        public static Vector3 GetShapePlaneNormal()
        {
            if (!ToolController.editMode) return -ShapeData.instance.normal;
            if (_selectedPersistentShapeData == null) return Vector3.up;
            return -_selectedPersistentShapeData.normal;
        }
        private static void ShapeDuringSceneGUI(UnityEditor.SceneView sceneView)
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                if (_shapeData.state == ToolController.ToolState.EDIT && _shapeData.selectedPointIdx > 0)
                    _shapeData.selectedPointIdx = -1;
                else if (_shapeData.state == ToolController.ToolState.NONE) ToolController.DeselectTool();
                else ResetShapeState(false);
                OnUndoShape();
                UpdateStroke();
                BrushstrokeManager.ClearBrushstroke();
            }
            if (ToolController.editMode || ShapeManager.instance.showPreexistingElements) ShapeToolEditMode(sceneView);
            if (ToolController.editMode) return;
            switch (_shapeData.state)
            {
                case ToolController.ToolState.NONE:
                    ShapeStateNone(sceneView.in2DMode);
                    break;
                case ToolController.ToolState.PREVIEW:
                    ShapeStateRadius(sceneView.in2DMode, _shapeData);
                    break;
                case ToolController.ToolState.EDIT:
                    ShapeStateEdit(sceneView);
                    break;
            }
        }
        private static Vector3 ClosestPointOnPlane(Vector3 point, ShapeData shapeData)
        {
            var plane = new Plane(shapeData.planeRotation * Vector3.up, shapeData.center);
            return plane.ClosestPointOnPlane(point);
        }

        public static void ShowShapeContextMenu(ShapeData data, Vector2 mousePosition)
        {
            if (!ToolController.editMode) return;
            var menu = new UnityEditor.GenericMenu();
            PersistentItemContextMenu(menu, data, mousePosition);
            menu.ShowAsContext();
        }
        private static bool ShapeControlPoints(ShapeData shapeData, out bool clickOnPoint,
            out bool wasEdited, bool showHandles, out Vector3 delta)
        {
            delta = Vector3.zero;
            clickOnPoint = false;
            wasEdited = false;
            var isCircle = shapeData.settings.shapeType == ShapeSettings.ShapeType.CIRCLE;
            var isPolygon = shapeData.settings.shapeType == ShapeSettings.ShapeType.POLYGON;
            bool leftMouseDown = Event.current.button == 0 && Event.current.type == EventType.MouseDown;

            DrawDotHandleCap(shapeData.center);
            if (isPolygon) foreach (var vertex in shapeData.vertices) DrawDotHandleCap(vertex);
            else DrawDotHandleCap(shapeData.radiusPoint);
            if (shapeData.selectedPointIdx >= 0 && shapeData.selectedPointIdx < shapeData.pointsCount)
                DrawDotHandleCap(shapeData.selectedPoint, 1f, 1.2f);
            DrawDotHandleCap(shapeData.GetPoint(-1));
            DrawDotHandleCap(shapeData.GetPoint(-2));

            for (int i = 0; i < shapeData.pointsCount; ++i)
            {
                var controlId = GUIUtility.GetControlID(FocusType.Passive);
                if (clickOnPoint) ToolProperties.RepainWindow();
                else
                {
                    if (showHandles)
                    {
                        var handleSize = UnityEditor.HandleUtility.GetHandleSize(shapeData.GetPoint(i));
                        var guiPoint = UnityEditor.HandleUtility.WorldToGUIPoint(shapeData.GetPoint(i));
                        var edgeWorld = shapeData.GetPoint(i)
                            + UnityEngine.Camera.current.transform.right
                            * handleSize * 0.0325f * PWBCore.staticData.controPointSize;
                        var guiEdge = UnityEditor.HandleUtility.WorldToGUIPoint(edgeWorld);
                        float radiusPx = Vector2.Distance(guiPoint, guiEdge);
                        float clickRadiusPx = Mathf.Max(radiusPx, 8f);
                        float distFromMouse = Vector2.Distance(guiPoint, Event.current.mousePosition);
                        UnityEditor.HandleUtility.AddControl(controlId, distFromMouse);
                        bool mouseNearPoint = distFromMouse <= clickRadiusPx;
                        if (UnityEditor.HandleUtility.nearestControl != controlId || !mouseNearPoint) continue;
                        if (isPolygon) DrawDotHandleCap(shapeData.GetPoint(i));
                        if (Event.current.button == 0 && Event.current.type == EventType.MouseDown)
                        {
                            shapeData.selectedPointIdx = i;
                            clickOnPoint = true;
                            Event.current.Use();
                        }
                    }
                }
                if (Event.current.button == 1 && Event.current.type == EventType.MouseDown
                       && !Event.current.control && !Event.current.shift && !Event.current.alt
                           && UnityEditor.HandleUtility.nearestControl == controlId)
                {
                    ShowShapeContextMenu(shapeData,
                        UnityEditor.EditorGUIUtility.GUIToScreenPoint(Event.current.mousePosition));
                    Event.current.Use();
                }
            }
            if (showHandles && shapeData.selectedPointIdx >= 0 && shapeData.selectedPointIdx < shapeData.pointsCount)
            {
                var selectedPoint = shapeData.selectedPoint;
                if (_updateHandlePosition)
                {
                    selectedPoint = _handlePosition;
                    _updateHandlePosition = false;
                }
                var prevPosition = shapeData.selectedPoint;
                var snappedPoint = PWBPositionHandle(CONTROL_POINT_HANDLE_ID, selectedPoint, shapeData.planeRotation);
                snappedPoint = SnapToBounds(snappedPoint);
                snappedPoint = SnapAndUpdateGridOrigin(snappedPoint, GridManager.settings.snappingEnabled,
                   shapeData.settings.paintOnPalettePrefabs, shapeData.settings.paintOnMeshesWithoutCollider,
                   shapeData.settings.ignoreSceneColliders, paintOnTheGrid: false, Vector3.down);
                if (prevPosition != snappedPoint)
                {
                    shapeData.MovePoint(shapeData.selectedPointIdx, snappedPoint);
                    wasEdited = true;
                    ToolProperties.RepainWindow();
                }

                _handlePosition = shapeData.selectedPoint;
                if (shapeData.selectedPointIdx == 0)
                {
                    var selectedRotation = shapeData.rotation;
                    if (_updateHandleRotation)
                    {
                        selectedRotation = _handleRotation;
                        _updateHandleRotation = false;
                    }
                    var prevRotation = shapeData.rotation;
                    var rotation = UnityEditor.Handles.RotationHandle(selectedRotation, shapeData.center);
                    if (prevRotation != rotation)
                    {
                        shapeData.rotation = rotation;
                        wasEdited = true;
                        ToolProperties.RepainWindow();
                    }
                    _handleRotation = shapeData.rotation;
                }
            }
            if (!showHandles) return false;
            return clickOnPoint || wasEdited;
        }
        public static Vector3[] GetPolygonVertices(ShapeData shapeData)
        {
            var tangent = Vector3.Cross(Vector3.left, shapeData.normal);
            if (tangent.sqrMagnitude < 0.000001) tangent = Vector3.Cross(Vector3.forward, shapeData.normal);
            var bitangent = Vector3.Cross(shapeData.normal, tangent);

            var polygonSides = shapeData.settings.shapeType == ShapeSettings.ShapeType.CIRCLE
                ? shapeData.circleSideCount : shapeData.settings.sidesCount;

            var periPoints = new System.Collections.Generic.List<Vector3>();
            var centerToRadius = shapeData.radiusPoint - shapeData.center;
            var sign = Vector3.Dot(Vector3.Cross(tangent, centerToRadius), shapeData.normal) > 0 ? 1f : -1f;
            float mouseAngle = Vector3.Angle(tangent, centerToRadius) * Mathf.Deg2Rad * sign;

            for (int i = 0; i < polygonSides; ++i)
            {
                var radians = TAU * i / polygonSides + mouseAngle;
                var tangentDir = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                var worldDir = TangentSpaceToWorld(tangent, bitangent, tangentDir).normalized;
                periPoints.Add(shapeData.center + (worldDir * shapeData.radius));
            }
            return periPoints.ToArray();
        }

        public static Vector3[] GetPolygonVertices() => GetPolygonVertices(_shapeData);

        public static Vector3[] GetArcVertices(float radius, ShapeData shapeData)
        {
            var tangent = Vector3.Cross(Vector3.left, shapeData.normal);
            if (tangent.sqrMagnitude < 0.000001) tangent = Vector3.Cross(Vector3.forward, shapeData.normal);
            var bitangent = Vector3.Cross(shapeData.normal, tangent);

            const float polygonSideSize = 0.3f;
            const int minPolygonSides = 8;
            const int maxPolygonSides = 60;
            var polygonSides = Mathf.Clamp((int)(TAU * radius / polygonSideSize), minPolygonSides, maxPolygonSides);

            var periPoints = new System.Collections.Generic.List<Vector3>();
            var centerToRadius = shapeData.GetPoint(-1) - shapeData.center;
            var sign = Vector3.Dot(Vector3.Cross(tangent, centerToRadius), shapeData.normal) > 0 ? 1 : -1;

            float firstAngle = Vector3.Angle(tangent, centerToRadius) * Mathf.Deg2Rad * sign;
            var sideDelta = TAU / polygonSides * Mathf.Sign(shapeData.arcAngle);

            for (int i = 0; i <= polygonSides; ++i)
            {
                var delta = sideDelta * i;
                bool arcComplete = false;
                if (Mathf.Abs(delta * Mathf.Rad2Deg) > Mathf.Abs(shapeData.arcAngle))
                {
                    delta = shapeData.arcAngle * Mathf.Deg2Rad;
                    arcComplete = true;
                }
                var radians = delta + firstAngle;
                var tangentDir = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                var worldDir = TangentSpaceToWorld(tangent, bitangent, tangentDir).normalized;
                periPoints.Add(shapeData.center + (worldDir * radius));
                if (arcComplete) break;
            }
            return periPoints.ToArray();
        }

        private static void DrawShapeLines(ShapeData shapeData)
        {
            if (shapeData.radius < 0.0001f) return;
            var points = new System.Collections.Generic.List<Vector3>(shapeData.state == ToolController.ToolState.PREVIEW
                ? GetPolygonVertices(shapeData) : shapeData.vertices);
            points.Add(points[0]);
            UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            var pointsArray = points.ToArray();
            UnityEditor.Handles.color = new Color(0f, 0f, 0f, 0.7f);
            UnityEditor.Handles.DrawAAPolyLine(8, pointsArray);
            UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.7f);
            UnityEditor.Handles.DrawAAPolyLine(4, pointsArray);
            if (shapeData.state < ToolController.ToolState.EDIT) return;

            var arcLines = new Vector3[] { shapeData.GetPoint(-1), shapeData.center, shapeData.GetPoint(-2) };
            UnityEditor.Handles.color = new Color(0f, 0f, 0.7f);
            UnityEditor.Handles.DrawAAPolyLine(4, arcLines);
            UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.7f);
            UnityEditor.Handles.DrawAAPolyLine(2, arcLines);
            var arcPoints = GetArcVertices(shapeData.radius * 1.5f, shapeData);
            UnityEditor.Handles.color = new Color(0f, 0f, 0.7f);
            UnityEditor.Handles.DrawAAPolyLine(4, arcPoints);
            UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.7f);
            UnityEditor.Handles.DrawAAPolyLine(2, arcPoints);
        }
        #endregion

    }
}
#pragma warning restore UDR0001
