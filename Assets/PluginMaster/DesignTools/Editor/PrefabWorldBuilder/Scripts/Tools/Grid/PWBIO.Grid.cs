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

        private static bool _gridShorcutEnabled = false;
        public static bool gridShorcutEnabled => _gridShorcutEnabled;
        private static SceneViewCameraOrientation.Orientation _cameraOrientation
            = SceneViewCameraOrientation.Orientation.NotAligned;
        private static AxesUtils.Axis _nonAlignedAxis = AxesUtils.Axis.Y;
        public static void SetAxis(AxesUtils.Axis axis) => _nonAlignedAxis = axis;
        private static void GridDuringSceneGui(UnityEditor.SceneView sceneView)
        {
            if (PWBSettings.shortcuts.gridEnableShortcuts.Check())
            {
                if (!_gridShorcutEnabled)
                {
                    _gridShorcutEnabled = true;
                    Event.current.Use();
                }
            }
            void MoveGridOrigin(AxesUtils.SignedAxis forwardAxis)
            {
                var fw = GridManager.settings.rotation * forwardAxis;
                var stepSize = GridManager.settings.radialGridEnabled ? GridManager.settings.radialStep
                : AxesUtils.GetAxisValue(GridManager.settings.step, forwardAxis);
                GridManager.settings.origin += fw * stepSize;
                _gridShorcutEnabled = false;
            }
            if (PWBSettings.shortcuts.gridToggle.Check())
            {
                GridManager.settings.visibleGrid = !GridManager.settings.visibleGrid;
                _gridShorcutEnabled = false;
            }
            else if (PWBSettings.shortcuts.gridToggleSnaping.Check()
                && (!PWBSettings.shortcuts.gridToggleSnaping.firstStepEnabled || _gridShorcutEnabled))
            {
                GridManager.settings.snappingEnabled = !GridManager.settings.snappingEnabled;
                _gridShorcutEnabled = false;
            }
            else if (PWBSettings.shortcuts.gridToggleLock.Check()
                && (!PWBSettings.shortcuts.gridToggleLock.firstStepEnabled || _gridShorcutEnabled))
            {
                GridManager.settings.lockedGrid = !GridManager.settings.lockedGrid;
                _gridShorcutEnabled = false;
            }
            else if (PWBSettings.shortcuts.gridSetOriginPosition.Check() && UnityEditor.Selection.activeTransform != null
                && (!PWBSettings.shortcuts.gridSetOriginPosition.firstStepEnabled || _gridShorcutEnabled))
            {
                GridManager.settings.origin = UnityEditor.Selection.activeTransform.position;
                GridManager.settings.showPositionHandle = true;
                _gridShorcutEnabled = false;
            }
            else if (PWBSettings.shortcuts.gridSetOriginRotation.Check() && UnityEditor.Selection.activeTransform != null
                && (!PWBSettings.shortcuts.gridSetOriginRotation.firstStepEnabled || _gridShorcutEnabled))
            {
                GridManager.settings.rotation = UnityEditor.Selection.activeTransform.rotation;
                GridManager.settings.showRotationHandle = true;
                _gridShorcutEnabled = false;
            }
            else if (PWBSettings.shortcuts.gridSetSize.Check() && UnityEditor.Selection.activeTransform != null
                && (!PWBSettings.shortcuts.gridSetSize.firstStepEnabled || _gridShorcutEnabled))
            {
                GridManager.settings.step = BoundsUtils.GetBounds(UnityEditor.Selection.activeTransform,
                    UnityEditor.Selection.activeTransform.rotation).size;
                GridManager.settings.showScaleHandle = true;
                _gridShorcutEnabled = false;
            }
            else if (PWBSettings.shortcuts.gridFrameOrigin.Check()
                && (!PWBSettings.shortcuts.gridFrameOrigin.firstStepEnabled || _gridShorcutEnabled))
            {
                GridManager.FrameGridOrigin();
                _gridShorcutEnabled = false;
            }
            else if (PWBSettings.shortcuts.gridTogglePositionHandle.Check()
                && (!PWBSettings.shortcuts.gridTogglePositionHandle.firstStepEnabled || _gridShorcutEnabled))
            {
                GridManager.ToggleGridPositionHandle();
                _gridShorcutEnabled = false;
            }
            else if (PWBSettings.shortcuts.gridToggleRotationHandle.Check()
                && (!PWBSettings.shortcuts.gridToggleRotationHandle.firstStepEnabled || _gridShorcutEnabled))
            {
                GridManager.ToggleGridRotationHandle();
                _gridShorcutEnabled = false;
            }
            else if (PWBSettings.shortcuts.gridToggleSpacingHandle.Check()
                && (!PWBSettings.shortcuts.gridToggleSpacingHandle.firstStepEnabled || _gridShorcutEnabled))
            {
                GridManager.ToggleGridScaleHandle();
                _gridShorcutEnabled = false;
            }
            else if (PWBSettings.shortcuts.gridMoveOriginUp.Check()
                && (!PWBSettings.shortcuts.gridMoveOriginUp.firstStepEnabled || _gridShorcutEnabled))
            {
                MoveGridOrigin(AxesUtils.SignedAxis.UP);
            }
            else if (PWBSettings.shortcuts.gridMoveOriginDown.Check()
                && (!PWBSettings.shortcuts.gridMoveOriginDown.firstStepEnabled || _gridShorcutEnabled))
            {
                MoveGridOrigin(AxesUtils.SignedAxis.DOWN);
            }
            else if (PWBSettings.shortcuts.gridNextOrigin.Check()
                && (!PWBSettings.shortcuts.gridNextOrigin.firstStepEnabled || _gridShorcutEnabled))
            {
                GridManager.settings.SetNextOrigin();
                SnapSettingsWindow.RepaintWindow();
            }
            else if (PWBSettings.shortcuts.snapToggleBoundsSnapping.Check())
            {
                GridManager.settings.boundsSnapping = !GridManager.settings.boundsSnapping;
            }
            if (!GridManager.settings.visibleGrid)
            {
                return;
            }
            GridMoveOriginToMousePosInput(sceneView);

            var originOffset = GridManager.settings.origin;
            var rotation = GridManager.settings.rotation;

            var camOrientation = SceneViewCameraOrientation.GetSceneViewOrientation();

            if (GridManager.settings.autoCameraAlignment)
            {
                if (camOrientation == SceneViewCameraOrientation.Orientation.NotAligned)
                {
                    switch (_nonAlignedAxis)
                    {
                        case AxesUtils.Axis.Y:
                            GridManager.settings.gridOnY = true;
                            break;
                        case AxesUtils.Axis.X:
                            GridManager.settings.gridOnX = true;
                            break;
                        case AxesUtils.Axis.Z:
                            GridManager.settings.gridOnZ = true;
                            break;
                    }
                }
                else
                {
                    if (_cameraOrientation != camOrientation)
                        _nonAlignedAxis = GridManager.settings.gridOnX ? AxesUtils.Axis.X
                        : GridManager.settings.gridOnY ? AxesUtils.Axis.Y : AxesUtils.Axis.Z;
                    switch (camOrientation)
                    {
                        case SceneViewCameraOrientation.Orientation.Top:
                        case SceneViewCameraOrientation.Orientation.Bottom:
                            GridManager.settings.gridOnY = true;
                            break;
                        case SceneViewCameraOrientation.Orientation.Left:
                        case SceneViewCameraOrientation.Orientation.Right:
                            GridManager.settings.gridOnX = true;
                            break;
                        case SceneViewCameraOrientation.Orientation.Front:
                        case SceneViewCameraOrientation.Orientation.Back:
                            GridManager.settings.gridOnZ = true;
                            break;
                    }
                }
            }
            _cameraOrientation = camOrientation;
            var axis = GridManager.settings.gridOnX ? AxesUtils.Axis.X
                : GridManager.settings.gridOnY ? AxesUtils.Axis.Y : AxesUtils.Axis.Z;


            var camRay = new Ray(sceneView.camera.transform.position, sceneView.camera.transform.forward);
            var plane = new Plane(rotation * (axis == AxesUtils.Axis.X ? Vector3.right
                : axis == AxesUtils.Axis.Y ? Vector3.up : Vector3.forward), originOffset);
            Vector3 focusPoint;
            if (plane.Raycast(camRay, out float distance)) focusPoint = camRay.GetPoint(distance);
            else
            {
                return;
            }

            if (!GridManager.settings.radialGridEnabled && GridManager.settings.drawGridAsTexture)
                DrawGridQuad(axis, sceneView);
            else
            {
                var snapSize = GridManager.settings.step;
                var maxCells = GetMaxCells(axis, focusPoint, sceneView, out snapSize);
                var snapStepFactor = new Vector3(
                    snapSize.x / GridManager.settings.step.x,
                    snapSize.y / GridManager.settings.step.y,
                    snapSize.z / GridManager.settings.step.z);
                focusPoint = SnapPosition(focusPoint, GridManager.settings.snappingEnabled, false, snapStepFactor, true);

                if (GridManager.settings.radialGridEnabled) DrawRadialGrid(axis, sceneView, maxCells, snapSize.x);
                else DrawGrid(axis, focusPoint, maxCells, snapSize);
            }
            GridHandles();

            if (GridManager.settings.visibleGrid)
            {
                if (Event.current.type == EventType.MouseMove ||
                    Event.current.type == EventType.MouseDrag ||
                    Event.current.type == EventType.MouseDown ||
                    Event.current.type == EventType.MouseUp ||
                    Event.current.type == EventType.DragUpdated ||
                    Event.current.type == EventType.DragPerform)
                {
                    PWBIO.MarkGridForRepaint();
                    sceneView.Repaint();
                }
            }
        }

        private static bool _gridNeedsRepaint = true;
        private static int _lastGridRepaintFrame = -1;
        public static void MarkGridForRepaint() => _gridNeedsRepaint = true;

        private static bool _pickingGridOrigin = false;
        private static Vector3 _pickingGridOriginPreview = Vector3.zero;
        private static void GridMoveOriginToMousePosInput(UnityEditor.SceneView sceneView)
        {
            if (!GridManager.settings.visibleGrid) return;
            var shortcut = PWBSettings.shortcuts.gridMoveOriginToMousePos;
            if (shortcut.holdKeysAndClickCombination.holdingChanged)
                _pickingGridOrigin = shortcut.holdKeysAndClickCombination.holdingKeys;
            var pickShortcutOn = shortcut.Check();
            var pickOrigin = _pickingGridOrigin && Event.current.button == 0
                && Event.current.type == EventType.MouseDown;
            if (pickShortcutOn || pickOrigin)
            {
                var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                if (GridRaycast(mouseRay, out RaycastHit gridHit))
                {
                    var snappedPoint = SnapPosition(gridHit.point, onGrid: true, applySettings: false,
                        snapStepFactor: Vector3.one, ignoreMidpoints: true);
                    GridManager.settings.origin = snappedPoint;
                }
                Event.current.Use();
                if (pickShortcutOn) _pickingGridOrigin = false;
                else if (pickOrigin) _pickingGridOrigin = false;
                sceneView.Repaint();
                return;
            }
            if (_pickingGridOrigin
                && Event.current.type == EventType.KeyDown
                && Event.current.keyCode == KeyCode.Escape)
            {
                _pickingGridOrigin = false;
                Event.current.Use();
            }
            if (_pickingGridOrigin)
            {
                UnityEditor.HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                if (GridRaycast(mouseRay, out RaycastHit gridHit))
                {
                    _pickingGridOriginPreview = SnapPosition(gridHit.point, onGrid: true, applySettings: false,
                        snapStepFactor: Vector3.one, ignoreMidpoints: true);
                }
                var positionText = "(" + _pickingGridOriginPreview.x.ToString("F2") + ", "
                    + _pickingGridOriginPreview.y.ToString("F2") + ", "
                    + _pickingGridOriginPreview.z.ToString("F2") + ")";
                var labelTexts = new string[] { "Click to set origin", positionText };
                InfoText.Draw(sceneView, labelTexts);
                sceneView.Repaint();
            }
        }

        private static int GetMaxCells(AxesUtils.Axis axis, Vector3 focusPoint, UnityEditor.SceneView sceneView,
            out Vector3 snapSize)
        {
            snapSize = GridManager.settings.radialGridEnabled ? Vector3.one * GridManager.settings.radialStep
                : GridManager.settings.step;
            var rotation = GridManager.settings.rotation;

            var guiDistance = (UnityEditor.HandleUtility.WorldToGUIPoint(focusPoint)
                - UnityEditor.HandleUtility.WorldToGUIPoint(focusPoint + rotation * snapSize)).magnitude;

            const int minGuidistance = 30;
            if (guiDistance < minGuidistance) snapSize *= Mathf.Round(minGuidistance / guiDistance);
            int maxCells = 10;

            var halfSize = new Vector3(
                axis == AxesUtils.Axis.X ? 0f : maxCells * snapSize.x,
                axis == AxesUtils.Axis.Y ? 0f : maxCells * snapSize.y,
                axis == AxesUtils.Axis.Z ? 0f : maxCells * snapSize.z);

            var axis1Vector = rotation * (axis == AxesUtils.Axis.X ? Vector3.forward
                : axis == AxesUtils.Axis.Y ? Vector3.right : Vector3.up);
            var axis2Vector = rotation * (axis == AxesUtils.Axis.X ? Vector3.up
                : axis == AxesUtils.Axis.Y ? Vector3.forward : Vector3.right);

            var gridAxes = new Vector2[]
            {
                UnityEditor.HandleUtility.WorldToGUIPoint(focusPoint - Vector3.Scale(halfSize, axis1Vector)),
                UnityEditor.HandleUtility.WorldToGUIPoint(focusPoint + Vector3.Scale(halfSize, axis1Vector)),
                UnityEditor.HandleUtility.WorldToGUIPoint(focusPoint - Vector3.Scale(halfSize, axis2Vector)),
                UnityEditor.HandleUtility.WorldToGUIPoint(focusPoint + Vector3.Scale(halfSize, axis2Vector))
            };

            var gridMax = new Vector2(
                Mathf.Max(gridAxes[0].x, gridAxes[1].x, gridAxes[2].x, gridAxes[3].x),
                Mathf.Max(gridAxes[0].y, gridAxes[1].y, gridAxes[2].y, gridAxes[3].y));
            var gridMin = new Vector2(
                Mathf.Min(gridAxes[0].x, gridAxes[1].x, gridAxes[2].x, gridAxes[3].x),
                Mathf.Min(gridAxes[0].y, gridAxes[1].y, gridAxes[2].y, gridAxes[3].y));

            var gridSizeOnGUI = gridMax - gridMin;
            var diff = sceneView.position.size - gridSizeOnGUI;

            if (diff.x > 0 || diff.y > 0)
            {
                float maxRatio = float.MinValue;
                if (diff.x > 0) maxRatio = sceneView.position.size.x / gridSizeOnGUI.x;
                if (diff.y > 0)
                {
                    float ratio = sceneView.position.size.y / gridSizeOnGUI.y;
                    if (ratio > maxRatio) maxRatio = ratio;
                }
                maxCells = Mathf.CeilToInt((float)maxCells * maxRatio);
                if (maxCells > 30)
                {
                    var maxCellsRatio = Mathf.CeilToInt((float)maxCells / 30f);
                    snapSize = snapSize * maxCellsRatio;
                    maxCells = 30;
                }
            }
            return maxCells;
        }
        private static bool GridRaycast(Ray ray, out RaycastHit hitInfo)
        {
            hitInfo = new RaycastHit();
            var plane = new Plane(GridManager.settings.rotation * (GridManager.settings.gridOnX ? Vector3.right
                : GridManager.settings.gridOnY ? Vector3.up : Vector3.forward), GridManager.settings.origin);
            if (Vector3.Cross(ray.direction, plane.normal).magnitude < 0.000001)
                plane = new Plane(ray.direction, GridManager.settings.origin);
            if (plane.Raycast(ray, out float distance))
            {
                hitInfo.normal = plane.normal;
                hitInfo.point = ray.GetPoint(distance);
                return true;
            }
            return false;
        }

    }
}
#pragma warning restore UDR0001
