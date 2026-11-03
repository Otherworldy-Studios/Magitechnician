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
        private static void EmbedSelectionInSurface(Quaternion rotation)
        {
            PWBCore.SetActiveTempColliders(SelectionManager.topLevelSelection, false);
            var placeOnSurfaceData = new PlaceOnSurfaceUtils.PlaceOnSurfaceData();
            placeOnSurfaceData.projectionDirectionSpace = Space.World;
            placeOnSurfaceData.rotateToSurface = false;
            var objHeight = new float[SelectionManager.topLevelSelection.Length];
            for (int i = 0; i < SelectionManager.topLevelSelection.Length; ++i)
            {
                var obj = SelectionManager.topLevelSelection[i];
                objHeight[i] = BoundsUtils.GetMagnitude(obj.transform);
                obj.SetActive(false);
            }
            var maxSurfMag = 0f;
            for (int i = 0; i < SelectionManager.topLevelSelection.Length; ++i)
            {
                var obj = SelectionManager.topLevelSelection[i];
                obj.transform.rotation = Quaternion.Euler(0, obj.transform.rotation.eulerAngles.y, 0);
                var bottomVertices = BoundsUtils.GetBottomVertices(obj.transform);
                var TRS = obj.transform.localToWorldMatrix;
                maxSurfMag = Mathf.Max(maxSurfMag, _selectionSurfaceMagnitude);
                var magnitude = BoundsUtils.GetMagnitude(obj.transform) + maxSurfMag;
                Transform surfaceTransform;
                var exceptions = new System.Collections.Generic.HashSet<GameObject>() { obj };
                var surfceDistance = SelectionToolController.settings.embedAtPivotHeight
                    ? GetPivotDistanceToSurfaceSigned(obj.transform.position, magnitude, paintOnPalettePrefabs: true,
                    castOnMeshesWithoutCollider: true, ignoreSceneColliders: true, out surfaceTransform,
                    exceptions)
                    : GetBottomDistanceToSurface(bottomVertices, TRS, magnitude,
                     paintOnPalettePrefabs: true, castOnMeshesWithoutCollider: true, ignoreSceneColliders: true,
                     out surfaceTransform, exceptions);

                if (surfaceTransform != null)
                {
#if UNITY_6000_3_OR_NEWER
                    var surfId = surfaceTransform.GetEntityId();
#else
                    var surfId = surfaceTransform.GetInstanceID();
#endif
                    if (_selectionSurfaceMagnitudeDic.ContainsKey(surfId))
                        _selectionSurfaceMagnitude = _selectionSurfaceMagnitudeDic[surfId];
                    else
                    {
                        var m = BoundsUtils.GetMagnitude(surfaceTransform);
                        _selectionSurfaceMagnitude = m;
                        _selectionSurfaceMagnitudeDic.Add(surfId, m);
                    }
                }

                surfceDistance -= SelectionToolController.settings.surfaceDistance;
                if (surfceDistance != 0f)
                {
                    var euler = obj.transform.rotation.eulerAngles;
                    var delta = obj.transform.rotation * new Vector3(0f, -surfceDistance, 0f);
                    obj.transform.position += obj.transform.rotation * new Vector3(0f, -surfceDistance, 0f);
                }
                if (SelectionToolController.settings.rotateToTheSurface)
                {
                    var down = obj.transform.rotation * Vector3.down;
                    var ray = new Ray(obj.transform.position - down * objHeight[i], down);
                    if (PWBToolRaycast(ray, out RaycastHit hitInfo, out GameObject collider, float.MaxValue, layerMask: -1,
                        paintOnPalettePrefabs: true, castOnMeshesWithoutCollider: true, ignoreSceneColliders: true,
                        exceptions: new System.Collections.Generic.HashSet<GameObject>() { obj }, createTempColliders: true))
                    {
                        var initialRotation = obj.transform.rotation;
                        if (!_editingSelectionRotation)
                        {
                            initialRotation = rotation;
                            if (_initialRotations.ContainsKey(i)) initialRotation = _initialRotations[i];
                        }
                        obj.transform.rotation = GetRotationFromNormal(hitInfo.normal, initialRotation);

                    }
                }
            }
            foreach (var obj in SelectionManager.topLevelSelection) obj.SetActive(true);
            _selectionBounds = BoundsUtils.GetSelectionBounds(SelectionManager.topLevelSelection, rotation);
            PWBCore.SetActiveTempColliders(SelectionManager.topLevelSelection, true);
        }

        public static void EmbedSelectionInSurface()
            => EmbedSelectionInSurface(_selectionRotation);

        private static void RotateSelection90Deg(Vector3 axis, System.Collections.Generic.List<Vector3> points)
        {
            var rotation = _selectionRotation;
            foreach (var obj in SelectionManager.topLevelSelection)
            {
                if (obj == null)
                {
                    SelectionManager.UpdateSelection();
                    return;
                }
                UnityEditor.Undo.RecordObject(obj.transform, "Rotate Selection");
                obj.transform.RotateAround(points[_selectedBoxPointIdx < 0 ? 10 : _selectedBoxPointIdx],
                    rotation * axis, 90);
            }
            _selectionRotation = rotation * Quaternion.AngleAxis(90, axis);
            var localCenter = _selectionBounds.center - points[_selectedBoxPointIdx];
            _selectionBounds.center = (Quaternion.AngleAxis(90, axis) * localCenter) + points[_selectedBoxPointIdx];
            if (SelectionToolController.settings.embedInSurface) EmbedSelectionInSurface();
            PWBCore.UpdateTempCollidersTransforms(SelectionManager.topLevelSelection);
        }

        private static bool MoveSelection(
            System.Collections.Generic.List<Vector3> points, UnityEditor.SceneView sceneView, Vector3 position)
        {
            void SetSetectedPoint(Vector3 value) => points[_selectedBoxPointIdx] = value;
            var prevPosition = points[_selectedBoxPointIdx];
            if (_snapToVertex)
            {
                if (SnapToVertex(UnityEditor.HandleUtility.GUIPointToWorldRay(Event.current.mousePosition),
                    out RaycastHit closestVertexInfo, sceneView.in2DMode, null))
                    SetSetectedPoint(closestVertexInfo.point);
            }
            else
            {
                var snappedPoint = SnapToBounds(position);
                snappedPoint = SnapAndUpdateGridOrigin(snappedPoint,
                    GridManager.settings.snappingEnabled, paintOnPalettePrefabs: true, paintOnMeshesWithoutCollider: true,
                    ignoresceneColliders: true, paintOnTheGrid: true, Vector3.down);
                SetSetectedPoint(snappedPoint);
            }

            if (prevPosition == points[_selectedBoxPointIdx]) return false;
            if (_snappedPointIsSelected) _snappedPoint = points[_selectedBoxPointIdx];

            var delta = points[_selectedBoxPointIdx] - prevPosition;
            foreach (var obj in SelectionManager.topLevelSelection)
            {
                if (obj == null)
                {
                    SelectionManager.UpdateSelection();
                    return false;
                }
                UnityEditor.Undo.RecordObject(obj.transform, "Move Selection");
                obj.transform.position += delta;
            }
            _selectionBounds.center += delta;
            return true;
        }

        private static bool MoveSelectionToHandlePosition(Quaternion rotation,
            System.Collections.Generic.List<Vector3> points, UnityEditor.SceneView sceneView)
        {
            if (!SelectionToolController.settings.move) return false;

            if (SelectionToolController.settings.handleSpace == Space.World) rotation = Quaternion.identity;
            else if (SelectionManager.topLevelSelection.Length == 1)
                rotation = SelectionManager.topLevelSelection[0].transform.rotation;

            int hotControlBefore = GUIUtility.hotControl;

            var handlePosition = _movingSelectionHandle ? _selectionHandlePosition : points[_selectedBoxPointIdx];
            var r = _movingSelectionHandle ? _selectionHnadleRotation : rotation;
            handlePosition = PWBPositionHandle(TOOL_HANDLE_ID, handlePosition, r);
            if (_editingSelectionRotation) return false;
            _selectionHandlePosition = handlePosition;
            int hotControlAfter = GUIUtility.hotControl;
            bool wasMoving = _movingSelectionHandle;
            if (_previousHotControl != hotControlAfter)
            {
                if (hotControlBefore == 0 && hotControlAfter != 0)
                {
                    _initialRotations.Clear();
                    for (int i = 0; i < SelectionManager.topLevelSelection.Length; ++i)
                        _initialRotations.Add(i, SelectionManager.topLevelSelection[i].transform.rotation);
                    _movingSelectionHandle = true;
                    _selectionHandlePosition = points[_selectedBoxPointIdx];
                    _selectionHnadleRotation = rotation;
                }
                else if (hotControlBefore != 0 && hotControlAfter == 0)
                {
                    _movingSelectionHandle = false;
                }
            }
            _previousHotControl = hotControlAfter;

            if (!_movingSelectionHandle && !wasMoving) return false;
            return MoveSelection(points, sceneView, handlePosition);
        }

        private static bool MoveSelectionToMousePosition(
            System.Collections.Generic.List<Vector3> points, UnityEditor.SceneView sceneView)
        {
            var shortcutCheck = PWBSettings.shortcuts.selectionMoveToMousePosition
                .holdKeysAndMouseCombination.CheckIsHoldingKeys();
            if (shortcutCheck && !_moveSelectionToMousePositionEnabled)
            {
                _initialRotations.Clear();
                for (int i = 0; i < SelectionManager.topLevelSelection.Length; ++i)
                    _initialRotations.Add(i, SelectionManager.topLevelSelection[i].transform.rotation);
            }
            else if (!shortcutCheck && _moveSelectionToMousePositionEnabled) _initialRotations.Clear();

            _moveSelectionToMousePositionEnabled = shortcutCheck;
            if (!PWBSettings.shortcuts.selectionMoveToMousePosition.Check()) return false;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            var nearbyObjects = boundsOctree.GetColliding(mouseRay, float.MaxValue);


            if (!MeshUtils.Raycast(mouseRay, out RaycastHit hitInfo, out GameObject collider, nearbyObjects, float.MaxValue))
                return false;
            return MoveSelection(points, sceneView, hitInfo.point);
        }

        private static bool RotateSelection(Quaternion rotation,
            System.Collections.Generic.List<Vector3> points, UnityEditor.SceneView sceneView)
        {
            if (!SelectionToolController.settings.rotate) return false;
            if (SelectionToolController.settings.handleSpace == Space.Self && SelectionManager.topLevelSelection.Length == 1)
            {
                rotation = SelectionManager.topLevelSelection[0].transform.rotation;
            }
            int hotControlBefore = GUIUtility.hotControl;

            var prevRotation = rotation;
            var handlePosition = _movingSelectionHandle ? _selectionHandlePosition : points[_selectedBoxPointIdx];
            var newRotation = UnityEditor.Handles.RotationHandle(prevRotation, handlePosition);
            int hotControlAfter = GUIUtility.hotControl;
            if (_previousHotControl != hotControlAfter)
            {
                if (hotControlBefore == 0 && hotControlAfter != 0)
                {
                    _initialRotations.Clear();
                    for (int i = 0; i < SelectionManager.topLevelSelection.Length; ++i)
                        _initialRotations.Add(i, SelectionManager.topLevelSelection[i].transform.rotation);
                    _movingSelectionHandle = true;
                    _selectionHandlePosition = points[_selectedBoxPointIdx];
                    _editingSelectionRotation = true;
                }
                else if (hotControlBefore != 0 && hotControlAfter == 0)
                {
                    _movingSelectionHandle = false;
                    _editingSelectionRotation = false;
                }
            }

            _previousHotControl = hotControlAfter;
            if (!_movingSelectionHandle) return false;

            _selectionRotation = newRotation;
            var angle = Quaternion.Angle(prevRotation, newRotation);
            var axis = Vector3.Cross(prevRotation * Vector3.forward, newRotation * Vector3.forward);
            if (axis == Vector3.zero) axis = Vector3.Cross(prevRotation * Vector3.up, newRotation * Vector3.up);
            axis.Normalize();
            foreach (var obj in SelectionManager.topLevelSelection)
            {
                if (obj == null)
                {
                    SelectionManager.UpdateSelection();
                    return false;
                }
                UnityEditor.Undo.RecordObject(obj.transform, "Rotate Selection");
                obj.transform.RotateAround(points[_selectedBoxPointIdx], axis, angle);
            }
            var localCenter = _selectionBounds.center - points[_selectedBoxPointIdx];
            _selectionBounds.center = (Quaternion.AngleAxis(angle, axis) * localCenter)
                + points[_selectedBoxPointIdx];
            if (SelectionToolController.settings.rotateToTheSurface)
            {
                MoveSelection(points, sceneView, handlePosition);
            }
            return true;
        }

        private static bool ScaleSelection(Quaternion rotation, System.Collections.Generic.List<Vector3> points)
        {
            if (!SelectionToolController.settings.scale) return false;
            var prevScale = _selectionScale;
            var newScale = UnityEditor.Handles.ScaleHandle(prevScale, points[_selectedBoxPointIdx],
                rotation, UnityEditor.HandleUtility.GetHandleSize(points[_selectedBoxPointIdx]) * 1.4f);
            if (prevScale == newScale) return false;
            _selectionScale = newScale;
            var scaleFactor = new Vector3(
                prevScale.x == 0 ? newScale.x : newScale.x / prevScale.x,
                prevScale.y == 0 ? newScale.y : newScale.y / prevScale.y,
                prevScale.z == 0 ? newScale.z : newScale.z / prevScale.z);
            var pivot = new GameObject();
            pivot.hideFlags = HideFlags.HideAndDontSave;
            pivot.transform.position = points[_selectedBoxPointIdx];
            pivot.transform.rotation = rotation;
            foreach (var obj in SelectionManager.topLevelSelection)
            {
                if (obj == null)
                {
                    SelectionManager.UpdateSelection();
                    break;
                }
                UnityEditor.Undo.RecordObject(obj.transform, "Scale Selection");
                pivot.transform.localScale = Vector3.one;
                var localPosition = pivot.transform.InverseTransformPoint(obj.transform.position);
                pivot.transform.localScale = scaleFactor;
                obj.transform.position = pivot.transform.TransformPoint(localPosition);
                obj.transform.localScale = Vector3.Scale(obj.transform.localScale, scaleFactor);
            }
            GameObject.DestroyImmediate(pivot);
            var pivotToCenter = _selectionBounds.center - points[_selectedBoxPointIdx];
            _selectionBounds.center = points[_selectedBoxPointIdx] + Vector3.Scale(pivotToCenter, scaleFactor);
            _selectionBounds.size = Vector3.Scale(_selectionBounds.size, scaleFactor);
            return true;
        }
    }
}
#pragma warning restore UDR0001
