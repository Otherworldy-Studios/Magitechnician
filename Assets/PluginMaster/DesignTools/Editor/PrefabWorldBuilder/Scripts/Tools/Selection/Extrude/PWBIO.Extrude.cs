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
using System.Linq;
using UnityEngine;

namespace PluginMaster
{
    public static partial class PWBIO
    {

        private static Vector3 _extrudeHandlePosition;
        private static Vector3Int _extrudeDirection;
        private static Vector3 _initialExtrudePosition;
        private static Vector3 _selectionSize;
        private static Vector3 _deltaSnapped;
        private static Vector3 _extrudeSpacing;
        private static int _extrudegPreviewObjectCount = 0;
        private static bool _draggingExtrudeHandle = false;
        private static bool _editingExtrudeHandle = false;
        private static bool _extrudeSelectionStarted = false;
        private static bool _extrudeRectSelecting = false;
        private static Vector2 _extrudeSelectionStartMousePosition;

        private const float EXTRUDE_RECT_SELECTION_THRESHOLD = 5f;

        public static void ResetExtrudeState(bool askIfWantToSave = true)
        {
            if (askIfWantToSave && _extrudegPreviewObjectCount > 0) DisplaySaveDialog(CreateExtrudedObjects);
            _extrudegPreviewObjectCount = 0;
            ClearExtrudeAngles();
            _draggingExtrudeHandle = false;
            _editingExtrudeHandle = false;
            _extrudeSelectionStarted = false;
            _extrudeRectSelecting = false;
            _extrudeNeedsInteractionReset = true;
            UnityEditor.Tools.hidden = false;
        }

        public static void ClearExtrudeAngles() => _extrudeAngles.Clear();
        private static void ExtrudeDuringSceneGUI(UnityEditor.SceneView sceneView)
        {

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                ResetUnityCurrentTool();
                ResetExtrudeState(false);
                ToolController.DeselectTool();
                return;
            }
            if (_extrudeNeedsInteractionReset)
            {
                GUIUtility.hotControl = 0;
                GUIUtility.keyboardControl = 0;
                _draggingExtrudeHandle = false;
                _editingExtrudeHandle = false;
                _extrudeSelectionStarted = false;
                _extrudeRectSelecting = false;
                _extrudeNeedsInteractionReset = false;
            }
            if (SelectionManager.topLevelSelection.Length == 0)
            {
                ExtrudeSelectionInput(sceneView);
                return;
            }

            ExtrudeInput();

            if (SelectionManager.topLevelSelection.Length == 0) return;

            var settings = ExtrudeManager.settings;
            if (UnityEditor.Tools.current != UnityEditor.Tool.View && UnityEditor.Tools.current != UnityEditor.Tool.None)
                SaveUnityCurrentTool();
            if (UnityEditor.Tools.current == UnityEditor.Tool.None)
                UnityEditor.Tools.current = _unityCurrentTool != UnityEditor.Tool.None
                    ? _unityCurrentTool
                    : UnityEditor.Tool.Move;

            UnityEditor.Tools.hidden = true;
            var anchor = settings.rotationAccordingTo == ExtrudeSettings.RotationAccordingTo.FRIST_SELECTED
                ? SelectionManager.topLevelSelection.First().transform
                : SelectionManager.topLevelSelection.Last().transform;

            var prevZTest = UnityEditor.Handles.zTest;
            UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            var hotControlBefore = GUIUtility.hotControl;

            UnityEditor.EditorGUI.BeginChangeCheck();
            var handlePosition = PWBPositionHandle(TOOL_HANDLE_ID, _extrudeHandlePosition,
                settings.space == Space.World ? Quaternion.identity : anchor.rotation, showPlanes: false);
            var changed = UnityEditor.EditorGUI.EndChangeCheck();

            var hotControlAfter = GUIUtility.hotControl;

            UnityEditor.Handles.zTest = prevZTest;

            if (hotControlAfter != 0 && hotControlAfter != hotControlBefore)
            {
                _editingExtrudeHandle = true;
                _extrudeSelectionStarted = false;
                _extrudeRectSelecting = false;
            }

            if (changed) _draggingExtrudeHandle = _editingExtrudeHandle = true;
            else if (_draggingExtrudeHandle && GUIUtility.hotControl == 0)
                _draggingExtrudeHandle = false;

            if (_editingExtrudeHandle && GUIUtility.hotControl == 0 && !changed)
                _editingExtrudeHandle = false;

            ExtrudeSelectionInput(sceneView);

            var handleDelta = handlePosition - _extrudeHandlePosition;
            _extrudeHandlePosition = handlePosition;
            var delta = _extrudeHandlePosition - _initialExtrudePosition;
            if (settings.space == Space.Self)
            {
                handleDelta = anchor.InverseTransformVector(handleDelta);
                delta = anchor.InverseTransformVector(delta);
            }

            if (delta.sqrMagnitude > 0.01)
            {
                var direction = Vector3Int.one;
                var absDelta = new Vector3(Mathf.Abs(handleDelta.x),
                    Mathf.Abs(handleDelta.y), Mathf.Abs(handleDelta.z));
                direction.x = (absDelta.x <= absDelta.y || absDelta.x <= absDelta.z) ? 0 : (int)Mathf.Sign(delta.x);
                direction.y = (absDelta.y <= absDelta.x || absDelta.y <= absDelta.z) ? 0 : (int)Mathf.Sign(delta.y);
                direction.z = (absDelta.z <= absDelta.x || absDelta.z <= absDelta.y) ? 0 : (int)Mathf.Sign(delta.z);
                var directionChanged = direction != Vector3Int.zero && _extrudeDirection != direction;
                if (handleDelta != Vector3.zero && directionChanged && _extrudeDirection != Vector3.zero
                    && _extrudeDirection != (direction * -1)) CreateExtrudedObjects(anchor);

                if (directionChanged) _extrudeDirection = direction;
                _extrudeSpacing = _selectionSize + (settings.spacingType == ExtrudeSettings.SpacingType.BOX_SIZE
                    ? Vector3.Scale(_selectionSize, settings.multiplier - Vector3.one)
                    : settings.spacing);
                _deltaSnapped = new Vector3(
                    Mathf.Floor((Mathf.Abs(delta.x) + _selectionSize.x / 2f) / _extrudeSpacing.x)
                    * _extrudeSpacing.x * Mathf.Sign(delta.x),
                    Mathf.Floor((Mathf.Abs(delta.y) + _selectionSize.y / 2f) / _extrudeSpacing.y)
                    * _extrudeSpacing.y * Mathf.Sign(delta.y),
                    Mathf.Floor((Mathf.Abs(delta.z) + _selectionSize.z / 2f) / _extrudeSpacing.z)
                    * _extrudeSpacing.z * Mathf.Sign(delta.z));
                if (_deltaSnapped != Vector3.zero) PreviewExtrudedObjects(sceneView.camera, anchor);
            }
        }

        private static Vector3 GetExtrudeStep(Transform anchor)
        {
            var step = Vector3.Scale(_extrudeSpacing, _extrudeDirection);
            if (ExtrudeManager.settings.space == Space.Self)
            {
                if (anchor.lossyScale.x != 0) step.x /= anchor.lossyScale.x;
                if (anchor.lossyScale.y != 0) step.y /= anchor.lossyScale.y;
                if (anchor.lossyScale.z != 0) step.z /= anchor.lossyScale.z;
            }
            return step;
        }
#if UNITY_6000_3_OR_NEWER
        private static System.Collections.Generic.Dictionary<EntityId, System.Collections.Generic.List<Vector3>>
            _extrudeAngles = new System.Collections.Generic.Dictionary<EntityId, System.Collections.Generic.List<Vector3>>();
        private static System.Collections.Generic.Dictionary<EntityId, System.Collections.Generic.List<Pose>>
            _extrudePoses = new System.Collections.Generic.Dictionary<EntityId, System.Collections.Generic.List<Pose>>();
#else
        private static System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<Vector3>>
            _extrudeAngles = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<Vector3>>();
        private static System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<Pose>>
            _extrudePoses = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<Pose>>();
#endif
        private static bool _extrudeNeedsInteractionReset = false;

        private static void PreviewExtrudedObjects(Camera camera, Transform anchor)
        {
            var step = GetExtrudeStep(anchor);
            var settings = ExtrudeManager.settings;
            _extrudegPreviewObjectCount = 0;
            _extrudePoses.Clear();
            foreach (var obj in SelectionManager.topLevelSelection)
            {
                var objPose = new Pose(obj.transform.position, obj.transform.rotation);

                var delta = step;
#if UNITY_6000_3_OR_NEWER
                var objId = obj.GetEntityId();
#else
                var objId = obj.GetInstanceID();
#endif
                _extrudePoses.Add(objId, new System.Collections.Generic.List<Pose>());
                System.Collections.Generic.List<Vector3> rotationList = null;
                if (_extrudeAngles.ContainsKey(objId))
                {
                    rotationList = _extrudeAngles[objId];
                }
                else
                {
                    rotationList = new System.Collections.Generic.List<Vector3>();
                    _extrudeAngles.Add(objId, rotationList);
                }
                int rotationIdx = 0;

                do
                {
                    var deltaPos = settings.space == Space.World ? delta : anchor.TransformVector(delta);
                    var localToWorld = Matrix4x4.Translate(deltaPos);

                    var additonalAngle = Vector3.zero;
                    if (settings.space == Space.World)
                    {
                        if (rotationIdx < rotationList.Count - 1)
                        {
                            additonalAngle = rotationList[rotationIdx];
                        }
                        else
                        {
                            if (settings.addRandomRotation)
                            {
                                var randomAngle = settings.randomEulerOffset.randomVector;
                                if (settings.rotateInMultiples)
                                {
                                    randomAngle = new Vector3(
                                        Mathf.Round(randomAngle.x / settings.rotationFactor) * settings.rotationFactor,
                                        Mathf.Round(randomAngle.y / settings.rotationFactor) * settings.rotationFactor,
                                        Mathf.Round(randomAngle.z / settings.rotationFactor) * settings.rotationFactor);
                                }
                                additonalAngle += randomAngle;
                            }
                            else additonalAngle += settings.eulerOffset;
                            rotationList.Add(additonalAngle);
                        }

                        if (additonalAngle != Vector3.zero)
                        {
                            var aditionalRotation = Quaternion.Euler(additonalAngle);
                            Vector3 additionalRotationAxis;
                            float additionalRotationAngle;
                            aditionalRotation.ToAngleAxis(out additionalRotationAngle, out additionalRotationAxis);
                            obj.transform.rotation = objPose.rotation;
                            obj.transform.position = objPose.position;
                            var center = BoundsUtils.GetBoundsRecursive(obj.transform).center;
                            obj.transform.RotateAround(center, additionalRotationAxis, additionalRotationAngle);
                        }
                    }
                    var surfaceDelta = Vector3.zero;
                    if (settings.embedInSurface)
                    {
                        var bottomVertices = BoundsUtils.GetBottomVertices(obj.transform);
                        var height = BoundsUtils.GetMagnitude(obj.transform) * 3;
                        Vector3 position = anchor.position + deltaPos;
                        var rotation = anchor.rotation;
                        var TRS = Matrix4x4.TRS(position, rotation, obj.transform.lossyScale);
                        var surfceDistance = settings.embedAtPivotHeight
                        ? GetPivotDistanceToSurfaceSigned(position, height, paintOnPalettePrefabs: true,
                        castOnMeshesWithoutCollider: true, ignoreSceneColliders: true, out Transform surface)
                        : GetBottomDistanceToSurfaceSigned(bottomVertices, TRS, height, paintOnPalettePrefabs: true,
                        castOnMeshesWithoutCollider: true, ignoreSceneColliders: true);
                        surfceDistance -= settings.surfaceDistance;
                        position += new Vector3(0f, -surfceDistance, 0f);
                        deltaPos += new Vector3(0f, -surfceDistance, 0f);
                        surfaceDelta = new Vector3(0f, -surfceDistance, 0f);
                        localToWorld = Matrix4x4.Translate(deltaPos);
                    }
                    ++_extrudegPreviewObjectCount;
                    PreviewBrushItem(obj, localToWorld, obj.layer, camera, false, false, false, false);
                    var posePosition = obj.transform.position + surfaceDelta;
                    posePosition += settings.space == Space.World ? delta : obj.transform.rotation * delta;
                    _extrudePoses[objId].Add(new Pose(posePosition, obj.transform.rotation));
                    delta += step;
                    ++rotationIdx;
                } while (Mathf.Abs(delta.x) <= Mathf.Abs(_deltaSnapped.x)
                && Mathf.Abs(delta.y) <= Mathf.Abs(_deltaSnapped.y)
                && Mathf.Abs(delta.z) <= Mathf.Abs(_deltaSnapped.z));
                obj.transform.rotation = objPose.rotation;
                obj.transform.position = objPose.position;
            }
        }

        private static void CreateExtrudedObjects(Transform anchor)
        {
            _extrudegPreviewObjectCount = 0;
            if (SelectionManager.topLevelSelection.Length == 0 || _extrudeDirection == Vector3Int.zero
                || _deltaSnapped == Vector3.zero) return;
            var newSelection = new System.Collections.Generic.List<GameObject>();
            _initialExtrudePosition += Vector3.Scale(_extrudeDirection, _deltaSnapped);
            _extrudeHandlePosition = _initialExtrudePosition;
            var step = GetExtrudeStep(anchor);
            foreach (var obj in SelectionManager.topLevelSelection)
            {
                GameObject extruded = null;
                var parent = GetParent(ExtrudeManager.settings, obj.name, true, null);
                if (ExtrudeManager.settings.sameParentAsSource) parent = obj.transform.parent;
#if UNITY_6000_3_OR_NEWER
                foreach (var pose in _extrudePoses[obj.GetEntityId()])
#else
                foreach (var pose in _extrudePoses[obj.GetInstanceID()])
#endif
                {
                    extruded = UnityEditor.PrefabUtility.IsOutermostPrefabInstanceRoot(obj)
                         ? (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(
                             UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(obj))
                         : GameObject.Instantiate(obj);
                    extruded.transform.position = pose.position;
                    extruded.transform.rotation = pose.rotation;
                    extruded.transform.localScale = obj.transform.lossyScale;
                    if (ExtrudeManager.settings.overwritePrefabLayer)
                        extruded.layer = ExtrudeManager.settings.layer;
                    const string COMMAND_NAME = "Extrude";
                    UnityEditor.Undo.RegisterCreatedObjectUndo(extruded, COMMAND_NAME);
                    UnityEditor.Undo.SetTransformParent(extruded.transform, parent, COMMAND_NAME);
                }
                newSelection.Add(extruded);
            }
            UnityEditor.Selection.objects = newSelection.ToArray();
        }

        private static void CreateExtrudedObjects()
        {
            var anchor = ExtrudeManager.settings.rotationAccordingTo == ExtrudeSettings.RotationAccordingTo.FRIST_SELECTED
               ? SelectionManager.topLevelSelection.First().transform
               : SelectionManager.topLevelSelection.Last().transform;
            CreateExtrudedObjects(anchor);
        }
        private static void ExtrudeInput()
        {
            if (SelectionManager.topLevelSelection.First() == null || SelectionManager.topLevelSelection.Last() == null)
                SelectionManager.UpdateSelection();
            if (SelectionManager.topLevelSelection.Length == 0) return;
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
                CreateExtrudedObjects();
        }

        private static void ExtrudeSelectionInput(UnityEditor.SceneView sceneView)
        {
            var currentEvent = Event.current;
            if (currentEvent.button != 0 || currentEvent.alt) return;
            if (GUIUtility.hotControl != 0 || _editingExtrudeHandle) return;

            if ((currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag)
                && IsMouseNearExtrudeHandle(currentEvent.mousePosition))
            {
                _extrudeSelectionStarted = false;
                _extrudeRectSelecting = false;
                return;
            }

            if (currentEvent.type == EventType.MouseDown)
            {
                _extrudeSelectionStarted = true;
                _extrudeRectSelecting = false;
                _extrudeSelectionStartMousePosition = currentEvent.mousePosition;
                return;
            }

            if (!_extrudeSelectionStarted) return;

            if (currentEvent.type == EventType.MouseDrag)
            {
                if ((currentEvent.mousePosition - _extrudeSelectionStartMousePosition).magnitude
                    >= EXTRUDE_RECT_SELECTION_THRESHOLD)
                {
                    _extrudeRectSelecting = true;
                    sceneView.Repaint();
                    currentEvent.Use();
                }
                return;
            }

            if (currentEvent.type == EventType.Repaint && _extrudeRectSelecting)
            {
                DrawExtrudeSelectionRect(currentEvent.mousePosition);
                return;
            }

            if (currentEvent.type != EventType.MouseUp) return;

            if (_extrudeRectSelecting)
                SelectExtrudeRectObjects(GetExtrudeSelectionRect(currentEvent.mousePosition), currentEvent);
            else
                SelectExtrudeSingleObject(currentEvent);

            _extrudeSelectionStarted = false;
            _extrudeRectSelecting = false;
            currentEvent.Use();
        }

        private static Rect GetExtrudeSelectionRect(Vector2 currentMousePosition)
        {
            return Rect.MinMaxRect(
                Mathf.Min(_extrudeSelectionStartMousePosition.x, currentMousePosition.x),
                Mathf.Min(_extrudeSelectionStartMousePosition.y, currentMousePosition.y),
                Mathf.Max(_extrudeSelectionStartMousePosition.x, currentMousePosition.x),
                Mathf.Max(_extrudeSelectionStartMousePosition.y, currentMousePosition.y));
        }

        private static void DrawExtrudeSelectionRect(Vector2 currentMousePosition)
        {
            var rect = GetExtrudeSelectionRect(currentMousePosition);

            UnityEditor.Handles.BeginGUI();

            var fillColor = new Color(0.3f, 0.55f, 1f, 0.12f);
            var outlineColor = new Color(0.3f, 0.55f, 1f, 0.9f);

            UnityEditor.EditorGUI.DrawRect(rect, fillColor);
            UnityEditor.EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, 1f), outlineColor);
            UnityEditor.EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMax - 1f, rect.width, 1f), outlineColor);
            UnityEditor.EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, 1f, rect.height), outlineColor);
            UnityEditor.EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.yMin, 1f, rect.height), outlineColor);

            UnityEditor.Handles.EndGUI();
        }

        private static void SelectExtrudeSingleObject(Event currentEvent)
        {
            var pickedObject = UnityEditor.HandleUtility.PickGameObject(currentEvent.mousePosition, false);

            if (pickedObject == null)
            {
                if (!currentEvent.shift && !currentEvent.control && !currentEvent.command)
                {
                    UnityEditor.Selection.objects = new Object[0];
                    SelectionManager.UpdateSelection();
                }
                return;
            }

            if (currentEvent.shift || currentEvent.control || currentEvent.command)
            {
                SelectionManager.ToggleSelection(pickedObject);
            }
            else
            {
                UnityEditor.Selection.activeGameObject = pickedObject;
                SelectionManager.UpdateSelection();
            }
        }

        private static void SelectExtrudeRectObjects(Rect rect, Event currentEvent)
        {
            var pickedObjects = UnityEditor.HandleUtility.PickRectObjects(rect, false);

#if UNITY_2021_1_OR_NEWER
            using (UnityEngine.Pool.HashSetPool<Object>
                .Get(out System.Collections.Generic.HashSet<Object> selectedObjectsSet))
#else
            var selectedObjectsSet = new System.Collections.Generic.HashSet<Object>();
#endif
            {
                if (currentEvent.shift || currentEvent.control || currentEvent.command)
                    selectedObjectsSet.UnionWith(UnityEditor.Selection.objects);

                if (currentEvent.control || currentEvent.command)
                    selectedObjectsSet.ExceptWith(pickedObjects);
                else
                    selectedObjectsSet.UnionWith(pickedObjects);

                UnityEditor.Selection.objects = selectedObjectsSet.ToArray();
                SelectionManager.UpdateSelection();
            }
        }

        private static bool IsMouseNearExtrudeHandle(Vector2 mousePosition)
        {
            var handleGuiPosition = UnityEditor.HandleUtility.WorldToGUIPoint(_extrudeHandlePosition);
            const float HANDLE_PICK_DISTANCE = 18f;
            return Vector2.Distance(mousePosition, handleGuiPosition) <= HANDLE_PICK_DISTANCE;
        }

    }
}
#pragma warning restore UDR0001
