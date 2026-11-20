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

        private static bool _showMirrorHandles = true;
        private static GameObject _mirrorObject = null;
        private static Transform _mirroredTransform = null;

        public static void ResetMirrorState(bool askIfWantToSave = true)
        {
            if (askIfWantToSave && _paintStroke.Count > 0) DisplaySaveDialog(CreateMirroredObjects);
        }

        public static void InitializeMirrorPose()
        {
            if (_sceneViewCamera == null) return;
            MirrorManager.settings.mirrorPosition = Vector3.zero;
            var camRay = new Ray(_sceneViewCamera.transform.position, _sceneViewCamera.transform.forward);
            if (Raycast(camRay, out RaycastHit hit, float.MaxValue, -1, QueryTriggerInteraction.Ignore))
                MirrorManager.settings.mirrorPosition = hit.point;
            if (GridRaycast(camRay, out RaycastHit gridHit))
                MirrorManager.settings.mirrorPosition = gridHit.point;
            MirrorManager.settings.mirrorPosition = SnapToBounds(MirrorManager.settings.mirrorPosition);
            MirrorManager.settings.mirrorPosition = SnapAndUpdateGridOrigin(MirrorManager.settings.mirrorPosition,
                GridManager.settings.snappingEnabled, paintOnPalettePrefabs: true, paintOnMeshesWithoutCollider: true,
                ignoresceneColliders: true, paintOnTheGrid: false, Vector3.down);
            MirrorManager.settings.mirrorRotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
            _showMirrorHandles = true;
        }

        private static void MirrorDuringSceneGUI(UnityEditor.SceneView sceneView)
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                if (_showMirrorHandles) _showMirrorHandles = false;
                else
                {
                    ResetMirrorState(false);
                    ToolController.DeselectTool(false);
                }
            }
            DrawMirror();
            if (SelectionManager.topLevelSelection.Length == 0) return;
            PreviewMirror();
            MirrorInput();
        }

        private static void CreateMirroredObjects()
        {
            if (MirrorManager.settings.action == MirrorSettings.MirrorAction.CREATE)
                Paint(MirrorManager.settings, "Mirror");
            else
            {
                foreach (var item in _paintStroke)
                {
                    UnityEditor.Undo.RecordObject(item.prefab.transform, "Mirror");
                    item.prefab.transform.position = item.position;
                    item.prefab.transform.rotation = item.rotation;
                }
            }
            ToolController.DeselectTool();
        }

        private static void MirrorInput()
        {
            void Rotate90(Vector3 axis) => MirrorManager.settings.mirrorRotation *= Quaternion.AngleAxis(90, axis);
            if (Event.current.type == EventType.KeyDown && Event.current.control
               && Event.current.keyCode == KeyCode.UpArrow) Rotate90(Vector3.right);
            else if (Event.current.type == EventType.KeyDown && Event.current.control
                && Event.current.keyCode == KeyCode.DownArrow) Rotate90(Vector3.left);
            else if (Event.current.type == EventType.KeyDown && Event.current.control
                && Event.current.keyCode == KeyCode.RightArrow) Rotate90(Vector3.up);
            else if (Event.current.type == EventType.KeyDown && Event.current.control
               && Event.current.keyCode == KeyCode.LeftArrow) Rotate90(Vector3.down);
            else if (Event.current.type == EventType.KeyDown && Event.current.control
                && Event.current.keyCode == KeyCode.PageUp) Rotate90(Vector3.forward);
            else if (Event.current.type == EventType.KeyDown && Event.current.control
                && Event.current.keyCode == KeyCode.PageDown) Rotate90(Vector3.back);

            var confirm = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return;
            if (!confirm) return;
            CreateMirroredObjects();
        }

        private static void DrawMirror()
        {
            UnityEditor.Handles.color = Color.yellow;
            var handleSize = UnityEditor.HandleUtility.GetHandleSize(Vector3.zero);
            UnityEditor.Handles.RectangleHandleCap(0, MirrorManager.settings.mirrorPosition
                - MirrorManager.settings.mirrorPose.forward * handleSize * 0.02f,
                MirrorManager.settings.mirrorRotation, handleSize, EventType.Repaint);
            UnityEditor.Handles.RectangleHandleCap(0, MirrorManager.settings.mirrorPosition
                + MirrorManager.settings.mirrorPose.forward * handleSize * 0.02f,
                MirrorManager.settings.mirrorRotation, handleSize, EventType.Repaint);
            UnityEditor.Handles.color = Color.black;
            UnityEditor.Handles.RectangleHandleCap(0, MirrorManager.settings.mirrorPosition,
                MirrorManager.settings.mirrorPose.rotation, handleSize * 1.2f, EventType.Repaint);
            if (_showMirrorHandles)
            {
                var prevPose = MirrorManager.settings.mirrorPose;
                MirrorManager.settings.mirrorPosition = PWBPositionHandle(TOOL_HANDLE_ID,
                    MirrorManager.settings.mirrorPosition, MirrorManager.settings.mirrorRotation);
                MirrorManager.settings.mirrorPosition = SnapAndUpdateGridOrigin(MirrorManager.settings.mirrorPosition,
                GridManager.settings.snappingEnabled, paintOnPalettePrefabs: true, paintOnMeshesWithoutCollider: true,
                 ignoresceneColliders: true, paintOnTheGrid: false, Vector3.down);
                MirrorManager.settings.mirrorPosition = SnapToBounds(MirrorManager.settings.mirrorPosition);
                MirrorManager.settings.mirrorRotation
                    = UnityEditor.Handles.RotationHandle(MirrorManager.settings.mirrorRotation,
                    MirrorManager.settings.mirrorPosition);
                if (prevPose != MirrorManager.settings.mirrorPose) ToolProperties.RepainWindow();
            }
            else
            {
                DrawDotHandleCap(MirrorManager.settings.mirrorPosition);
                var controlId = GUIUtility.GetControlID(FocusType.Passive);
                var distFromMouse = UnityEditor.HandleUtility.DistanceToRectangle(MirrorManager.settings.mirrorPosition,
                    Quaternion.identity, 0f);
                UnityEditor.HandleUtility.AddControl(controlId, distFromMouse);
                if (UnityEditor.HandleUtility.nearestControl == controlId && Event.current.button == 0
                    && Event.current.type == EventType.MouseDown) _showMirrorHandles = true;
            }
        }

        private static void PreviewMirror()
        {
            _paintStroke.Clear();
            if (_mirrorObject == null)
            {
                _mirrorObject = new GameObject("PluginMasterMirror");
                _mirrorObject.hideFlags = HideFlags.HideAndDontSave;
                _mirroredTransform = new GameObject("PluginMasterMirrorTempTransform").transform;
                _mirroredTransform.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
            var settings = MirrorManager.settings;
            _mirrorObject.transform.position = settings.mirrorPosition;
            _mirrorObject.transform.rotation = settings.mirrorRotation;
            foreach (var obj in SelectionManager.topLevelSelection)
            {
                if (obj == null) continue;
                _mirrorObject.transform.localScale = Vector3.one;
                _mirroredTransform.position = obj.transform.position;
                _mirroredTransform.rotation = obj.transform.rotation;
                _mirroredTransform.localScale = obj.transform.lossyScale;
                _mirroredTransform.SetParent(_mirrorObject.transform, true);
                _mirrorObject.transform.localScale = new Vector3(1f, 1f, -1f);
                if (!MirrorManager.settings.reflectRotation) _mirroredTransform.rotation = obj.transform.rotation;

                var previewScale = Vector3.one;
                var scale = _mirroredTransform.localScale;
                if (settings.invertScale)
                {
                    scale *= -1;
                    previewScale *= -1;
                    var angle = new Vector3(180 - _mirroredTransform.rotation.eulerAngles.x,
                        settings.reflectRotation
                        ? _mirroredTransform.rotation.eulerAngles.y + 180
                        : 180 - _mirroredTransform.rotation.eulerAngles.y,
                        _mirroredTransform.rotation.eulerAngles.z);
                    _mirroredTransform.rotation = Quaternion.Euler(angle);
                }
                Transform surface = null;
                if (settings.embedInSurface)
                {
                    var TRS = Matrix4x4.TRS(_mirroredTransform.position, _mirroredTransform.rotation, previewScale);
                    var bottomVertices = BoundsUtils.GetBottomVertices(obj.transform);
                    var height = BoundsUtils.GetMagnitude(obj.transform) * 3;
                    var surfceDistance = settings.embedAtPivotHeight
                    ? GetPivotDistanceToSurfaceSigned(_mirroredTransform.position, height, paintOnPalettePrefabs: true,
                    castOnMeshesWithoutCollider: true, ignoreSceneColliders: true, out Transform surf)
                    : GetBottomDistanceToSurfaceSigned(bottomVertices, TRS, height, paintOnPalettePrefabs: true,
                    castOnMeshesWithoutCollider: true, ignoreSceneColliders: true);
                    surfceDistance -= settings.surfaceDistance;
                    _mirroredTransform.position += new Vector3(0f, -surfceDistance, 0f);
                    if (settings.rotateToTheSurface)
                    {
                        var down = _mirroredTransform.rotation * Vector3.down;
                        var ray = new Ray(_mirroredTransform.position - down * height, down);
                        if (PWBToolRaycast(ray, out RaycastHit hitInfo, out GameObject collider,
                        float.MaxValue, -1, paintOnPalettePrefabs: true, castOnMeshesWithoutCollider: true,
                        ignoreSceneColliders: true))
                        {
                            var tangent = Vector3.Cross(hitInfo.normal, Vector3.left);
                            if (tangent.sqrMagnitude < 0.000001) tangent = Vector3.Cross(hitInfo.normal, Vector3.back);
                            tangent = tangent.normalized;
                            _mirroredTransform.rotation = Quaternion.LookRotation(tangent, hitInfo.normal);
                            var colObj = PWBCore.GetGameObjectFromTempCollider(collider);
                            if (colObj != null) surface = colObj.transform;
                        }
                    }
                }
                var matrix = Matrix4x4.TRS(_mirroredTransform.position, _mirroredTransform.rotation, previewScale)
                    * Matrix4x4.Rotate(Quaternion.Inverse(obj.transform.rotation))
                    * Matrix4x4.Translate(-obj.transform.position);
                var layer = settings.overwritePrefabLayer ? settings.layer : obj.layer;
                var reverseTriagles = scale.x * scale.y * scale.z < 0;
                PreviewBrushItem(obj, matrix, layer, _sceneViewCamera, false, reverseTriagles, false, false);
                var parent = settings.sameParentAsSource
                    ? obj.transform.parent : GetParent(settings, obj.name, false, null);

                _paintStroke.Add(new PaintStrokeItem(obj, guid: "2", _mirroredTransform.position,
                    _mirroredTransform.rotation, scale, layer, parent, null, false, false));
            }
        }
    }
}
#pragma warning restore UDR0001
