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

        private static bool _draggingSymmetryOriginHandle = false;
        private static bool _editingSymmetryOriginHandle = false;
        private static Vector3 _symmetryOriginHandlePosition = Vector3.zero;

        private static bool _pickingSymmetryOrigin = false;
        private static Vector3 _pickingSymmetryOriginPreview = Vector3.zero;
        private static bool _mirrorModesWereActive = false;

        private static void MoveSymmetryOriginToMousePosInput(UnityEditor.SceneView sceneView)
        {
            var shortcut = PWBSettings.shortcuts.toolModesMoveSymmetryOriginToMousePos;
            if (shortcut.holdKeysAndClickCombination.holdingChanged)
                _pickingSymmetryOrigin = shortcut.holdKeysAndClickCombination.holdingKeys;

            var pickShortcutOn = shortcut.Check();
            var pickOrigin = _pickingSymmetryOrigin && Event.current.button == 0
                && Event.current.type == EventType.MouseDown;

            if (pickShortcutOn || pickOrigin)
            {
                var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                if (GridRaycast(mouseRay, out RaycastHit gridHit))
                {
                    var snappedPoint = SnapPosition(gridHit.point, onGrid: true, applySettings: false,
                        snapStepFactor: Vector3.one, ignoreMidpoints: true);
                    ModularToolModes.symmetryOrigin = snappedPoint;
                    UnityEditor.SceneView.RepaintAll();
                }
                Event.current.Use();
                _pickingSymmetryOrigin = false;
                sceneView.Repaint();
            }

            if (_pickingSymmetryOrigin
                && Event.current.type == EventType.KeyDown
                && Event.current.keyCode == KeyCode.Escape)
            {
                _pickingSymmetryOrigin = false;
                Event.current.Use();
            }

            if (_pickingSymmetryOrigin)
            {
                UnityEditor.HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                if (GridRaycast(mouseRay, out RaycastHit gridHit))
                {
                    _pickingSymmetryOriginPreview = SnapPosition(gridHit.point, onGrid: true, applySettings: false,
                        snapStepFactor: Vector3.one, ignoreMidpoints: true);
                }
                var positionText = "(" + _pickingSymmetryOriginPreview.x.ToString("F2") + ", "
                    + _pickingSymmetryOriginPreview.y.ToString("F2") + ", "
                    + _pickingSymmetryOriginPreview.z.ToString("F2") + ")";
                var labelTexts = new string[] { "Click to set symmetry origin", positionText };
                InfoText.Draw(sceneView, labelTexts);
                sceneView.Repaint();
            }
        }
        private static void DrawSymmetryOriginPlanesAndHandle()
        {
            UpdateSymmetryOriginOnMirrorActivation();

            bool showXPlane = ModularToolModes.mirrorX || BlockToolModes.axisX;
            bool showYPlane = ModularToolModes.mirrorY || BlockToolModes.axisY;
            bool showZPlane = ModularToolModes.mirrorZ || BlockToolModes.axisZ;

            if (!showXPlane && !showYPlane && !showZPlane)
            {
                _draggingSymmetryOriginHandle = false;
                _editingSymmetryOriginHandle = false;
                return;
            }

            var origin = ModularToolModes.symmetryOrigin;
            var rotation = GridManager.settings.rotation;
            var step = GridManager.settings.step;
            var handleSize = UnityEditor.HandleUtility.GetHandleSize(origin);
            var planeExtent = Mathf.Max(
                handleSize * 0.75f,
                Mathf.Max(step.x, Mathf.Max(step.y, step.z)) * 2f);

            var prevZTest = UnityEditor.Handles.zTest;
            var prevColor = UnityEditor.Handles.color;
            UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            void DrawPlane(AxesUtils.Axis normalAxis, Color color)
            {
                Vector3 localNormal, localAxisA, localAxisB;

                switch (normalAxis)
                {
                    case AxesUtils.Axis.X:
                        localNormal = Vector3.right;
                        localAxisA = Vector3.up;
                        localAxisB = Vector3.forward;
                        break;
                    case AxesUtils.Axis.Y:
                        localNormal = Vector3.up;
                        localAxisA = Vector3.right;
                        localAxisB = Vector3.forward;
                        break;
                    default:
                        localNormal = Vector3.forward;
                        localAxisA = Vector3.right;
                        localAxisB = Vector3.up;
                        break;
                }

                var worldNormal = rotation * localNormal;
                var worldAxisA = rotation * localAxisA * planeExtent;
                var worldAxisB = rotation * localAxisB * planeExtent;

                var verts = new Vector3[]
                {
                    origin - worldAxisA - worldAxisB,
                    origin - worldAxisA + worldAxisB,
                    origin + worldAxisA + worldAxisB,
                    origin + worldAxisA - worldAxisB
                };

                var fill = new Color(color.r, color.g, color.b, 0.10f);
                var outline = new Color(color.r, color.g, color.b, 0.95f);
                UnityEditor.Handles.DrawSolidRectangleWithOutline(verts, fill, outline);
            }

            if (showXPlane) DrawPlane(AxesUtils.Axis.X, UnityEditor.Handles.xAxisColor);
            if (showYPlane) DrawPlane(AxesUtils.Axis.Y, UnityEditor.Handles.yAxisColor);
            if (showZPlane) DrawPlane(AxesUtils.Axis.Z, UnityEditor.Handles.zAxisColor);

            _editingSymmetryOriginHandle = _draggingSymmetryOriginHandle;

            if (!_draggingSymmetryOriginHandle)
            {
                _symmetryOriginHandlePosition = ModularToolModes.symmetryOrigin;
            }

            var hotControlBefore = GUIUtility.hotControl;

            UnityEditor.EditorGUI.BeginChangeCheck();
            var newOrigin = PWBPositionHandle(SYMMETRY_HANDLE_ID, _symmetryOriginHandlePosition, rotation);
            var changed = UnityEditor.EditorGUI.EndChangeCheck();

            var hotControlAfter = GUIUtility.hotControl;

            if (changed)
            {
                _draggingSymmetryOriginHandle = true;
                _symmetryOriginHandlePosition = newOrigin;
                if (GridManager.settings.snappingEnabled)
                {
                    ModularToolModes.symmetryOrigin = SnapPosition(
                    _symmetryOriginHandlePosition, false, false, Vector3.one);
                }
                else
                {
                    ModularToolModes.symmetryOrigin = _symmetryOriginHandlePosition;
                }
                UnityEditor.SceneView.RepaintAll();
            }

            if (_draggingSymmetryOriginHandle && GUIUtility.hotControl == 0 && !changed)
            {
                _draggingSymmetryOriginHandle = false;
                _symmetryOriginHandlePosition = ModularToolModes.symmetryOrigin;
            }

            UnityEditor.Handles.color = prevColor;
            UnityEditor.Handles.zTest = prevZTest;
        }

        private static void UpdateSymmetryOriginOnMirrorActivation()
        {
            var mirrorModesActive = ModularToolModes.mirrorX
                || ModularToolModes.mirrorY
                || ModularToolModes.mirrorZ;

            if (mirrorModesActive && !_mirrorModesWereActive)
            {
                MoveSymmetryOriginToSceneViewCenter();
            }

            _mirrorModesWereActive = mirrorModesActive;
        }

        private static void MoveSymmetryOriginToSceneViewCenter()
        {
            var sceneView = UnityEditor.SceneView.currentDrawingSceneView
                ?? UnityEditor.SceneView.lastActiveSceneView;

            if (sceneView == null || sceneView.camera == null) return;

            var centerRay = sceneView.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!GridRaycast(centerRay, out RaycastHit gridHit)) return;

            var snappedPoint = SnapPosition(
                gridHit.point,
                onGrid: true,
                applySettings: false,
                snapStepFactor: Vector3.one,
                ignoreMidpoints: true);

            ModularToolModes.symmetryOrigin = snappedPoint;
            _symmetryOriginHandlePosition = snappedPoint;
            _draggingSymmetryOriginHandle = false;
            _editingSymmetryOriginHandle = false;
        }
    }
}
#pragma warning restore UDR0001
