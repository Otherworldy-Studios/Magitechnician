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
        private static void GridHandles()
        {
            if (!GridManager.settings.lockedGrid) return;
            var originOffset = GridManager.settings.origin;
            var rotation = GridManager.settings.rotation;
            var snapSize = GridManager.settings.step;
            UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            var handleSize = UnityEditor.HandleUtility.GetHandleSize(originOffset);

            void DrawSnapGizmos(AxesUtils.Axis forwardAxis, AxesUtils.Axis upwardAxis)
            {
                var fw = rotation * AxesUtils.GetVector(1, forwardAxis);
                var stepSize = GridManager.settings.radialGridEnabled
                    ? GridManager.settings.radialStep
                    : AxesUtils.GetAxisValue(snapSize, forwardAxis);

                var meshSize = GetMeshHandleSize(originOffset);
                var coneLength = meshSize * 0.24f;
                var coneRadius = meshSize * 0.08f;

                var pickRadius = coneRadius;

                var conePosFwCenter = originOffset + fw * (meshSize * 1.6f);
                var originScreenPos = _sceneViewCamera.WorldToScreenPoint(GridManager.settings.origin);
                var fwScreenPos = _sceneViewCamera.WorldToScreenPoint(conePosFwCenter);
                var alpha = Mathf.Clamp01((fwScreenPos - originScreenPos).magnitude / 90 - 0.5f);
                if (alpha <= 0f) return;

                EnsurePositionHandleMeshResources();

                var outBase = originOffset + fw * (meshSize * 1.6f);
                var outCenter = outBase + fw * (coneLength * 0.5f);

                var controlIdFw = GUIUtility.GetControlID(FocusType.Passive);
                var distFromMouseFw = UnityEditor.HandleUtility.DistanceToCircle(outCenter, pickRadius);
                UnityEditor.HandleUtility.AddControl(controlIdFw, distFromMouseFw);
                var mouseOverFw = UnityEditor.HandleUtility.nearestControl == controlIdFw
                    && distFromMouseFw <= pickRadius;

                if (Event.current.type == EventType.Repaint)
                {
                    var color = new Color(1f, 1f, mouseOverFw ? 1f : 0f, alpha);
                    DrawPositionHandleMesh(
                        _positionHandleConeMesh,
                        Matrix4x4.TRS(outBase, GetDirectionRotation(fw),
                            new Vector3(coneRadius, coneRadius, coneLength)),
                        color);
                }

                if (Event.current.button == 0
                    && Event.current.type == EventType.MouseDown
                    && mouseOverFw
                    && GUIUtility.hotControl == 0)
                {
                    GUIUtility.hotControl = controlIdFw;
                    GridManager.settings.origin += fw * stepSize;
                    Event.current.Use();
                }
                if (Event.current.type == EventType.MouseUp
                    && GUIUtility.hotControl == controlIdFw)
                {
                    GUIUtility.hotControl = 0;
                    Event.current.Use();
                }

                var inBase = originOffset + fw * (meshSize * 1.4f);
                var inCenter = inBase - fw * (coneLength * 0.5f);

                var controlIdBw = GUIUtility.GetControlID(FocusType.Passive);
                var distFromMouseBw = UnityEditor.HandleUtility.DistanceToCircle(inCenter, pickRadius);
                UnityEditor.HandleUtility.AddControl(controlIdBw, distFromMouseBw);
                var mouseOverBw = UnityEditor.HandleUtility.nearestControl == controlIdBw
                    && distFromMouseBw <= pickRadius;

                if (Event.current.type == EventType.Repaint)
                {
                    var color = new Color(1f, 1f, mouseOverBw ? 1f : 0f, alpha);
                    DrawPositionHandleMesh(
                        _positionHandleConeMesh,
                        Matrix4x4.TRS(inBase, GetDirectionRotation(-fw),
                            new Vector3(coneRadius, coneRadius, coneLength)),
                        color);
                }

                if (Event.current.button == 0
                    && Event.current.type == EventType.MouseDown
                    && mouseOverBw
                    && GUIUtility.hotControl == 0)
                {
                    GUIUtility.hotControl = controlIdBw;
                    GridManager.settings.origin -= fw * stepSize;
                    Event.current.Use();
                }
                if (Event.current.type == EventType.MouseUp
                    && GUIUtility.hotControl == controlIdBw)
                {
                    GUIUtility.hotControl = 0;
                    Event.current.Use();
                }
            }

            if (GridManager.settings.showPositionHandle)
            {
                GridManager.settings.origin = PWBPositionHandle(GRID_HANDLE_ID, originOffset, rotation);
                UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                DrawSnapGizmos(AxesUtils.Axis.X, AxesUtils.Axis.Y);
                DrawSnapGizmos(AxesUtils.Axis.Y, AxesUtils.Axis.Z);
                DrawSnapGizmos(AxesUtils.Axis.Z, AxesUtils.Axis.X);
            }
            else if (GridManager.settings.showRotationHandle)
                GridManager.settings.rotation = UnityEditor.Handles.RotationHandle(rotation, originOffset);
            else if (GridManager.settings.showScaleHandle)
            {
                if (GridManager.settings.radialGridEnabled)
                {
                    var step0 = Vector3.one * GridManager.settings.radialStep;
                    var step = UnityEditor.Handles.ScaleHandle(step0, originOffset,
                        rotation, handleSize);
                    if (step0 != step)
                    {
                        if (step0.x != step.x) GridManager.settings.radialStep = step.x;
                        else if (step0.y != step.y) GridManager.settings.radialStep = step.y;
                        else GridManager.settings.radialStep = step.z;
                    }
                }
                else
                {
                    GridManager.settings.step = UnityEditor.Handles.ScaleHandle(GridManager.settings.step,
                    originOffset, rotation, handleSize);
                }
            }
            if (GridManager.settings.origin != originOffset
                || GridManager.settings.rotation != rotation
                || GridManager.settings.step != snapSize)
                SnapSettingsWindow.RepaintWindow();
        }
    }
}
#pragma warning restore UDR0001
