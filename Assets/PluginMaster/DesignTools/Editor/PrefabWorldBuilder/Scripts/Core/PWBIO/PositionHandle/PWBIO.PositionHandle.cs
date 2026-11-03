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

        #region State & Configuration

        private static Mesh _positionHandleAxisMesh;
        private static Mesh _positionHandleConeMesh;
        private static Mesh _positionHandlePlaneMesh;
        private static Material _positionHandleMeshMaterial;

        private static readonly int _colorPropertyId = Shader.PropertyToID("_Color");

        private enum PositionHandleDragMode
        {
            None,
            AxisX,
            AxisY,
            AxisZ,
            PlaneXY,
            PlaneXZ,
            PlaneYZ
        }

        private struct PositionHandleHit
        {
            public PositionHandleDragMode mode;
            public float score;
            public Vector3 axis;
            public Vector3 planeNormal;
            public Vector3 planePoint;
        }

        private static int _positionHandleControlId = 0;
        private static int _positionHandleActiveId = -1;
        private static PositionHandleDragMode _positionHandleDragMode = PositionHandleDragMode.None;
        private static Vector3 _positionHandleDragStartPosition = Vector3.zero;
        private static Vector3 _positionHandleDragAxis = Vector3.right;
        private static Vector3 _positionHandleDragPlaneNormal = Vector3.up;
        private static Vector3 _positionHandleDragPlanePoint = Vector3.zero;
        private static Vector3 _positionHandleDragStartPlaneHit = Vector3.zero;
        private static float _positionHandleDragStartAxisT = 0f;

        private const float POSITION_HANDLE_PICK_DISTANCE = 14f;
        private const int POSITION_HANDLE_CONTROL_HASH = 0x051D0A11;

        public static int draggingPositionHandleMeshesId
        {
            get
            {
                return _positionHandleDragMode == PositionHandleDragMode.None
                    ? -1
                    : _positionHandleActiveId;
            }
        }

        public static bool IsDraggingPositionHandleMeshes(int handleId)
        {
            return draggingPositionHandleMeshesId == handleId;
        }

        #endregion

        #region Input & Drag Logic
        private const int GRID_HANDLE_ID = 1;
        private const int SYMMETRY_HANDLE_ID = 2;
        private const int TOOL_HANDLE_ID = 3;
        private const int CONTROL_POINT_HANDLE_ID = 4;
        private static Vector3 PWBPositionHandle(int handleId, Vector3 position, Quaternion rotation, bool showPlanes = true)
        {
            var evt = Event.current;
            if (evt == null) return position;

            EnsurePositionHandleMeshResources();

            var size = GetMeshHandleSize(position);
            var axisSize = size * 0.85f;
            var planeOffset = size * 0.25f;
            var planeSize = size * 0.18f;

            var axisRadius = size * 0.012f;
            var coneLength = size * 0.24f;
            var coneRadius = size * 0.08f;
            var outlineRadius = size * 0.006f;

            var right = rotation * Vector3.right;
            var up = rotation * Vector3.up;
            var forward = rotation * Vector3.forward;

            var controlHint = unchecked((POSITION_HANDLE_CONTROL_HASH * 397) ^ handleId);
            var controlId = GUIUtility.GetControlID(controlHint, FocusType.Passive);

            var navigating = evt.alt || evt.button == 1;
            if (navigating && _positionHandleActiveId == handleId)
            {
                _positionHandleControlId = 0;
                _positionHandleActiveId = -1;
                _positionHandleDragMode = PositionHandleDragMode.None;
            }

            var isThisActiveHandle = GUIUtility.hotControl == controlId
                && _positionHandleControlId == controlId
                && _positionHandleActiveId == handleId;

            var hit = GetPositionHandleHit(
                position, right, up, forward,
                axisSize, coneLength, planeOffset, planeSize,
                showPlanes);

            if (!navigating && evt.type == EventType.Layout && hit.mode != PositionHandleDragMode.None)
            {
                UnityEditor.HandleUtility.AddControl(controlId, hit.score);
            }

            if (!navigating
                && evt.type == EventType.MouseDown
                && evt.button == 0
                && !evt.alt
                && hit.mode != PositionHandleDragMode.None
                && UnityEditor.HandleUtility.nearestControl == controlId)
            {
                _positionHandleControlId = controlId;
                _positionHandleActiveId = handleId;
                _positionHandleDragMode = hit.mode;
                _positionHandleDragStartPosition = position;
                _positionHandleDragAxis = hit.axis.normalized;
                _positionHandleDragPlaneNormal = hit.planeNormal.normalized;
                _positionHandleDragPlanePoint = hit.planePoint;

                var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(evt.mousePosition);

                if (IsAxisDragMode(hit.mode))
                {
                    TryGetClosestLineParameter(mouseRay, position,
                        _positionHandleDragAxis, out _positionHandleDragStartAxisT);
                }
                else if (TryRayPlane(mouseRay, _positionHandleDragPlanePoint,
                    _positionHandleDragPlaneNormal, out var planeHit))
                {
                    _positionHandleDragStartPlaneHit = planeHit;
                }

                GUIUtility.hotControl = controlId;
                GUIUtility.keyboardControl = 0;
                evt.Use();
            }

            if (evt.type == EventType.MouseDrag
                && isThisActiveHandle
                && _positionHandleDragMode != PositionHandleDragMode.None)
            {
                var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(evt.mousePosition);
                var newPosition = position;

                if (IsAxisDragMode(_positionHandleDragMode))
                {
                    if (TryGetClosestLineParameter(
                        mouseRay,
                        _positionHandleDragStartPosition,
                        _positionHandleDragAxis,
                        out var currentAxisT))
                    {
                        var delta = currentAxisT - _positionHandleDragStartAxisT;
                        newPosition = _positionHandleDragStartPosition + _positionHandleDragAxis * delta;
                    }
                }
                else if (TryRayPlane(
                    mouseRay,
                    _positionHandleDragPlanePoint,
                    _positionHandleDragPlaneNormal,
                    out var currentPlaneHit))
                {
                    newPosition = _positionHandleDragStartPosition + currentPlaneHit - _positionHandleDragStartPlaneHit;
                }

                if ((newPosition - position).sqrMagnitude > 0.0000001f)
                {
                    GUI.changed = true;
                    position = newPosition;
                }

                evt.Use();
            }

            if ((evt.type == EventType.MouseUp || evt.type == EventType.Ignore)
                && isThisActiveHandle)
            {
                GUIUtility.hotControl = 0;
                _positionHandleControlId = 0;
                _positionHandleActiveId = -1;
                _positionHandleDragMode = PositionHandleDragMode.None;
                evt.Use();
            }

            if (evt.type == EventType.Repaint)
            {
                var activeMode = isThisActiveHandle
                    ? _positionHandleDragMode
                    : PositionHandleDragMode.None;

                var anyPositionHandleActive = _positionHandleControlId != 0
                    && GUIUtility.hotControl == _positionHandleControlId
                    && _positionHandleDragMode != PositionHandleDragMode.None;

                var hoverMode = PositionHandleDragMode.None;
                if (activeMode != PositionHandleDragMode.None)
                {
                    hoverMode = activeMode;
                }
                else if (!anyPositionHandleActive && UnityEditor.HandleUtility.nearestControl == controlId)
                {
                    hoverMode = hit.mode;
                }

                var xBaseColor = new Color(0.95f, 0.25f, 0.25f, 0.85f);
                var yBaseColor = new Color(0.35f, 0.85f, 0.25f, 0.85f);
                var zBaseColor = new Color(0.25f, 0.45f, 1f, 0.85f);

                var xColor = GetHandlePartColor(xBaseColor, PositionHandleDragMode.AxisX, hoverMode, activeMode);
                var yColor = GetHandlePartColor(yBaseColor, PositionHandleDragMode.AxisY, hoverMode, activeMode);
                var zColor = GetHandlePartColor(zBaseColor, PositionHandleDragMode.AxisZ, hoverMode, activeMode);

                var xyFill = GetHandlePartColor(
                    new Color(zBaseColor.r, zBaseColor.g, zBaseColor.b, 0.18f),
                    PositionHandleDragMode.PlaneXY,
                    hoverMode,
                    activeMode);

                var xzFill = GetHandlePartColor(
                    new Color(yBaseColor.r, yBaseColor.g, yBaseColor.b, 0.18f),
                    PositionHandleDragMode.PlaneXZ,
                    hoverMode,
                    activeMode);

                var yzFill = GetHandlePartColor(
                    new Color(xBaseColor.r, xBaseColor.g, xBaseColor.b, 0.18f),
                    PositionHandleDragMode.PlaneYZ,
                    hoverMode,
                    activeMode);

                var xyOutline = GetHandlePartColor(
                    new Color(zBaseColor.r, zBaseColor.g, zBaseColor.b, 0.75f),
                    PositionHandleDragMode.PlaneXY,
                    hoverMode,
                    activeMode);

                var xzOutline = GetHandlePartColor(
                    new Color(yBaseColor.r, yBaseColor.g, yBaseColor.b, 0.75f),
                    PositionHandleDragMode.PlaneXZ,
                    hoverMode,
                    activeMode);

                var yzOutline = GetHandlePartColor(
                    new Color(xBaseColor.r, xBaseColor.g, xBaseColor.b, 0.75f),
                    PositionHandleDragMode.PlaneYZ,
                    hoverMode,
                    activeMode);

                DrawPositionHandleAxis(position, right, axisSize, axisRadius, coneLength, coneRadius, xColor);
                DrawPositionHandleAxis(position, up, axisSize, axisRadius, coneLength, coneRadius, yColor);
                DrawPositionHandleAxis(position, forward, axisSize, axisRadius, coneLength, coneRadius, zColor);

                if (showPlanes)
                {
                    DrawPositionHandlePlane(
                        position + right * planeOffset + up * planeOffset,
                        right,
                        up,
                        forward,
                        planeSize,
                        xyFill,
                        xyOutline,
                        outlineRadius);

                    DrawPositionHandlePlane(
                        position + right * planeOffset + forward * planeOffset,
                        right,
                        forward,
                        up,
                        planeSize,
                        xzFill,
                        xzOutline,
                        outlineRadius);

                    DrawPositionHandlePlane(
                        position + up * planeOffset + forward * planeOffset,
                        up,
                        forward,
                        right,
                        planeSize,
                        yzFill,
                        yzOutline,
                        outlineRadius);
                }
            }

            return position;
        }

        private static PositionHandleHit GetPositionHandleHit(
            Vector3 position,
            Vector3 right,
            Vector3 up,
            Vector3 forward,
            float axisSize,
            float coneLength,
            float planeOffset,
            float planeSize,
            bool includePlanes)
        {
            var hit = new PositionHandleHit
            {
                mode = PositionHandleDragMode.None,
                score = float.MaxValue
            };

            TrySetAxisHit(ref hit, PositionHandleDragMode.AxisX, position, right, axisSize + coneLength, right);
            TrySetAxisHit(ref hit, PositionHandleDragMode.AxisY, position, up, axisSize + coneLength, up);
            TrySetAxisHit(ref hit, PositionHandleDragMode.AxisZ, position, forward, axisSize + coneLength, forward);

            if (includePlanes)
            {
                TrySetPlaneHit(
                    ref hit,
                    PositionHandleDragMode.PlaneXY,
                    position + right * planeOffset + up * planeOffset,
                    right,
                    up,
                    forward);

                TrySetPlaneHit(
                    ref hit,
                    PositionHandleDragMode.PlaneXZ,
                    position + right * planeOffset + forward * planeOffset,
                    right,
                    forward,
                    up);

                TrySetPlaneHit(
                    ref hit,
                    PositionHandleDragMode.PlaneYZ,
                    position + up * planeOffset + forward * planeOffset,
                    up,
                    forward,
                    right);
            }

            return hit;

            void TrySetAxisHit(
                ref PositionHandleHit currentHit,
                PositionHandleDragMode mode,
                Vector3 origin,
                Vector3 direction,
                float length,
                Vector3 axis)
            {
                var end = origin + direction.normalized * length;
                var distance = UnityEditor.HandleUtility.DistanceToLine(origin, end);

                if (distance <= POSITION_HANDLE_PICK_DISTANCE && distance < currentHit.score)
                {
                    currentHit.mode = mode;
                    currentHit.score = distance;
                    currentHit.axis = axis.normalized;
                    currentHit.planeNormal = Vector3.zero;
                    currentHit.planePoint = origin;
                }
            }

            void TrySetPlaneHit(
                ref PositionHandleHit currentHit,
                PositionHandleDragMode mode,
                Vector3 center,
                Vector3 axisA,
                Vector3 axisB,
                Vector3 normal)
            {
                var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                if (!TryRayPlane(mouseRay, center, normal, out var planeHit)) return;

                var local = planeHit - center;
                var a = Vector3.Dot(local, axisA.normalized);
                var b = Vector3.Dot(local, axisB.normalized);

                if (Mathf.Abs(a) > planeSize || Mathf.Abs(b) > planeSize) return;
                var normalizedDist = Mathf.Max(Mathf.Abs(a), Mathf.Abs(b)) / planeSize;
                var planeScore = normalizedDist; 

                if (planeScore < currentHit.score)
                {
                    currentHit.mode = mode;
                    currentHit.score = planeScore;
                    currentHit.axis = Vector3.zero;
                    currentHit.planeNormal = normal.normalized;
                    currentHit.planePoint = center;
                }
            }
        }

        private static bool IsAxisDragMode(PositionHandleDragMode mode)
        {
            return mode == PositionHandleDragMode.AxisX
                || mode == PositionHandleDragMode.AxisY
                || mode == PositionHandleDragMode.AxisZ;
        }

        private static bool TryRayPlane(Ray ray, Vector3 planePoint, Vector3 planeNormal, out Vector3 hit)
        {
            hit = Vector3.zero;

            var plane = new Plane(planeNormal.normalized, planePoint);
            if (!plane.Raycast(ray, out var enter)) return false;
            if (enter < 0f) return false;

            hit = ray.GetPoint(enter);
            return true;
        }

        private static bool TryGetClosestLineParameter(
            Ray ray,
            Vector3 linePoint,
            Vector3 lineDirection,
            out float lineT)
        {
            lineT = 0f;

            var rayDirection = ray.direction.normalized;
            var dragDirection = lineDirection.normalized;

            var w0 = ray.origin - linePoint;
            var a = Vector3.Dot(rayDirection, rayDirection);
            var b = Vector3.Dot(rayDirection, dragDirection);
            var c = Vector3.Dot(dragDirection, dragDirection);
            var d = Vector3.Dot(rayDirection, w0);
            var e = Vector3.Dot(dragDirection, w0);
            var denominator = a * c - b * b;

            if (Mathf.Abs(denominator) < 0.000001f)
            {
                lineT = Vector3.Dot(ray.origin - linePoint, dragDirection);
                return true;
            }

            var rayT = (b * e - c * d) / denominator;
            lineT = (a * e - b * d) / denominator;

            if (rayT < 0f)
            {
                lineT = Vector3.Dot(ray.origin - linePoint, dragDirection);
            }

            return true;
        }

        private static float GetMeshHandleSize(Vector3 position)
        {
            var camera = Camera.current;
            if (camera == null) return 1f;

            const float targetPixelSize = 80f;
            if (camera.orthographic)
            {
                return camera.orthographicSize * 2f * targetPixelSize / Mathf.Max(1f, camera.pixelHeight);
            }

            var distance = Vector3.Distance(camera.transform.position, position);
            var frustumHeight = 2f * distance * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            return frustumHeight * targetPixelSize / Mathf.Max(1f, camera.pixelHeight);
        }
        #endregion

    }
}
#pragma warning restore UDR0001
