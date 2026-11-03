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
        public static void InitializeShapeTool()
        {
            ShapeManager.instance.DeselectAllItems();
            ResetShapeState(false);
            if (ShapeManager.settings.paintOnMeshesWithoutCollider)
            {
                if (ShapeManager.settings.ignoreSceneColliders) UpdateSceneColliderSet();
                UpdateOctree();
            }
        }
        public static void ResetShapeState(bool askIfWantToSave = true)
        {
            if (askIfWantToSave && _shapeData != null)
            {
                void Save()
                {
                    if (UnityEditor.SceneView.lastActiveSceneView != null)
                        ShapeStrokePreview(UnityEditor.SceneView.lastActiveSceneView,
                            ShapeData.nextHexId, forceUpdate: true, _shapeData);
                    CreateShape();
                }
                AskIfWantToSave(_shapeData.state, Save);
            }
            if (_shapeData == null) return;
            _snappedToVertex = false;
            _shapeData.Reset();
        }

        private static void ShapeStateNone(bool in2DMode)
        {
            if (Event.current.button == 0 && Event.current.type == EventType.MouseDown && !Event.current.alt)
                _shapeData.state = ToolController.ToolState.PREVIEW;
            if (TryGetMouseWorldHit(out Vector3 point, out Vector3 normal, ShapeManager.settings.mode, in2DMode,
                ShapeManager.settings.paintOnPalettePrefabs, ShapeManager.settings.paintOnMeshesWithoutCollider, false,
                ignoreSceneColliders: ShapeManager.settings.ignoreSceneColliders))
            {
                var snappedPoint = SnapToBounds(point);
                snappedPoint = SnapAndUpdateGridOrigin(snappedPoint, GridManager.settings.snappingEnabled,
                   ShapeManager.settings.paintOnPalettePrefabs, ShapeManager.settings.paintOnMeshesWithoutCollider,
                   ShapeManager.settings.ignoreSceneColliders, paintOnTheGrid: false, Vector3.down);

                if (snappedPoint != point && ShapeManager.settings.mode == PaintOnSurfaceToolSettingsBase.PaintMode.ON_SURFACE
                    && ShapeManager.settings.initialPlaneNormalDirection == ShapeSettings.NormalDirection.SURFACE_NORMAL
                    && _sceneViewCamera != null)
                {
                    var direction = (snappedPoint - _sceneViewCamera.transform.position).normalized;
                    var ray = new Ray(snappedPoint - direction, direction);
                    if (PWBToolRaycast(ray, out RaycastHit surfaceHit, out GameObject collider,
                        float.MaxValue, -1, ShapeManager.settings.paintOnPalettePrefabs,
                        ShapeManager.settings.paintOnMeshesWithoutCollider,
                        ignoreSceneColliders: ShapeManager.settings.ignoreSceneColliders))
                        normal = surfaceHit.normal;
                }
                point = snappedPoint;
                var planeNormal =
                    ShapeManager.settings.initialPlaneNormalDirection == ShapeSettings.NormalDirection.SURFACE_NORMAL
                    ? normal : ShapeManager.settings.initialPlaneNormal;
                _shapeData.SetCenter(point, planeNormal);
            }
            if (_shapeData.pointsCount > 0) DrawDotHandleCap(_shapeData.GetPoint(0));
        }
        private static void ShapeStateRadius(bool in2DMode, ShapeData shapeData)
        {
            if (Event.current.button == 0 && Event.current.type == EventType.MouseDown && !Event.current.alt)
            {
                shapeData.SetHandlePoints(GetPolygonVertices());
                shapeData.state = ToolController.ToolState.EDIT;
                updateStroke = true;
                return;
            }
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (_snapToVertex)
            {
                if (SnapToVertex(mouseRay, out RaycastHit closestVertexInfo, in2DMode))
                    mouseRay.origin = closestVertexInfo.point - mouseRay.direction;
            }
            var radiusPoint = shapeData.center;
            if (shapeData.plane.Raycast(mouseRay, out float distance))
                radiusPoint = mouseRay.GetPoint(distance);
            radiusPoint = SnapToBounds(radiusPoint);
            radiusPoint = SnapAndUpdateGridOrigin(radiusPoint, GridManager.settings.snappingEnabled,
                   shapeData.settings.paintOnPalettePrefabs, shapeData.settings.paintOnMeshesWithoutCollider,
                   shapeData.settings.ignoreSceneColliders, paintOnTheGrid: false, Vector3.down);
            radiusPoint = ClosestPointOnPlane(radiusPoint, shapeData);
            shapeData.SetRadius(radiusPoint);
            DrawShapeLines(shapeData);
            DrawDotHandleCap(shapeData.center);
            DrawDotHandleCap(shapeData.radiusPoint);
        }
        private static void ShapeStateEdit(UnityEditor.SceneView sceneView)
        {
            var evt = Event.current;
            var isCircle = ShapeManager.settings.shapeType == ShapeSettings.ShapeType.CIRCLE;
            var isPolygon = ShapeManager.settings.shapeType == ShapeSettings.ShapeType.POLYGON;
            var forceUpdate = updateStroke;

            if (updateStroke)
            {
                updateStroke = false;
                BrushstrokeManager.UpdateShapeBrushstroke();
            }

            ShapeStrokePreview(sceneView, ShapeData.nextHexId, forceUpdate, _shapeData);

            DrawShapeLines(_shapeData);
            DrawDotHandleCap(_shapeData.center);

            if (isPolygon)
                foreach (var vertex in _shapeData.vertices) DrawDotHandleCap(vertex);
            else DrawDotHandleCap(_shapeData.radiusPoint);

            if (_shapeData.selectedPointIdx >= 0 && _shapeData.selectedPointIdx < _shapeData.pointsCount)
                DrawDotHandleCap(_shapeData.selectedPoint, 1f, 1.2f);

            DrawDotHandleCap(_shapeData.GetPoint(-1));
            DrawDotHandleCap(_shapeData.GetPoint(-2));

            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Return)
            {
                CreateShape();
                ResetShapeState(false);
                return;
            }

            if (evt.button == 1 && evt.type == EventType.MouseDrag && evt.shift && evt.control)
            {
                var deltaSign = Mathf.Sign(evt.delta.x + evt.delta.y);
                ShapeManager.settings.gapSize += Mathf.PI * _shapeData.radius * deltaSign * 0.001f;
                ToolProperties.RepainWindow();
                evt.Use();
            }

            // Disable PWB interaction during RMB / Alt navigation,
            // but keep allocating control IDs to keep IMGUI in sync.
            var navigating = evt.alt || evt.button == 1;

            bool clickOnPoint = false;
            for (int i = 0; i < _shapeData.pointsCount; ++i)
            {
                if (isCircle && i == 2) i = _shapeData.pointsCount - 2;

                // Always allocate the control id, even while navigating.
                var controlId = GUIUtility.GetControlID(FocusType.Passive);

                if (navigating || clickOnPoint) continue;

                var point = _shapeData.GetPoint(i);
                var handleSize = UnityEditor.HandleUtility.GetHandleSize(point);
                var guiPoint = UnityEditor.HandleUtility.WorldToGUIPoint(point);
                var edgeWorld = point
                    + UnityEngine.Camera.current.transform.right
                    * handleSize * 0.0325f * PWBCore.staticData.controPointSize;
                var guiEdge = UnityEditor.HandleUtility.WorldToGUIPoint(edgeWorld);

                float radiusPx = Vector2.Distance(guiPoint, guiEdge);
                float clickRadiusPx = Mathf.Max(radiusPx, 8f);
                float distFromMouse = Vector2.Distance(guiPoint, evt.mousePosition);
                bool mouseNearPoint = distFromMouse <= clickRadiusPx;

                if (!mouseNearPoint) continue;

                if (isPolygon) DrawDotHandleCap(point);

                if (evt.button == 0 && evt.type == EventType.MouseDown)
                {
                    _shapeData.selectedPointIdx = i;
                    clickOnPoint = true;
                    GUIUtility.hotControl = 0;
                    GUIUtility.keyboardControl = 0;
                    ToolProperties.RepainWindow();
                    evt.Use();
                }
            }

            if (!navigating && _shapeData.selectedPointIdx >= 0)
            {
                var selectedPoint = _shapeData.selectedPoint;
                if (_updateHandlePosition)
                {
                    selectedPoint = _handlePosition;
                    _updateHandlePosition = false;
                }

                var prevPosition = _shapeData.selectedPoint;
                var snappedPoint = PWBPositionHandle(CONTROL_POINT_HANDLE_ID, selectedPoint, _shapeData.planeRotation);
                snappedPoint = SnapToBounds(snappedPoint);
                snappedPoint = SnapAndUpdateGridOrigin(snappedPoint, GridManager.settings.snappingEnabled,
                   ShapeManager.settings.paintOnPalettePrefabs, ShapeManager.settings.paintOnMeshesWithoutCollider,
                   ShapeManager.settings.ignoreSceneColliders, paintOnTheGrid: false, Vector3.down);

                if (prevPosition != snappedPoint)
                {
                    _shapeData.MovePoint(_shapeData.selectedPointIdx, snappedPoint);
                    updateStroke = true;
                    ToolProperties.RepainWindow();
                }

                _handlePosition = _shapeData.selectedPoint;

                if (_shapeData.selectedPointIdx == 0)
                {
                    var selectedRotation = _shapeData.rotation;
                    if (_updateHandleRotation)
                    {
                        selectedRotation = _handleRotation;
                        _updateHandleRotation = false;
                    }

                    var prevRotation = _shapeData.rotation;
                    var rotation = UnityEditor.Handles.RotationHandle(selectedRotation, _shapeData.center);

                    if (prevRotation != rotation)
                    {
                        _shapeData.rotation = rotation;
                        updateStroke = true;
                        ToolProperties.RepainWindow();
                    }

                    _handleRotation = _shapeData.rotation;
                }
            }
        }
        private static void CreateShape()
        {
            var nextShapeId = ShapeData.nextHexId;
            var objDic = Paint(ShapeManager.settings, PAINT_CMD, true, false, nextShapeId);
            var objs = objDic[nextShapeId].ToArray();
            var scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            var sceneGUID = UnityEditor.AssetDatabase.AssetPathToGUID(scenePath);
            if (isInPrefabMode)
                sceneGUID = UnityEditor.AssetDatabase.AssetPathToGUID(prefabStage.assetPath);
            var initialBrushId = PaletteManager.selectedBrush != null ? PaletteManager.selectedBrush.id : -1;
            var persistentData = new ShapeData(objs, initialBrushId, _shapeData);
            ShapeManager.instance.AddPersistentItem(sceneGUID, persistentData);
            PWBItemsWindow.RepainWindow();
        }
        private static void ShapeStrokePreview(UnityEditor.SceneView sceneView, string hexId,
            bool forceUpdate, ShapeData shapeData)
        {
            BrushstrokeItem[] brushstroke;
            if (PreviewIfBrushtrokestaysTheSame(out brushstroke, sceneView.camera, forceUpdate)) return;
            _paintStroke.Clear();
            if (shapeData == null) return;
            var settings = ShapeManager.settings;
            float maxSurfaceHeight = 0f;
            for (int i = 0; i < brushstroke.Length; ++i)
            {
                var strokeItem = brushstroke[i];

                var prefab = strokeItem.settings.prefab;
                if (prefab == null) continue;
                var bounds = BoundsUtils.GetBoundsRecursive(prefab.transform,
                    prefab.transform.rotation, strokeItem.scaleMultiplier);
                var pivotToCenter = prefab.transform.InverseTransformDirection(bounds.center - prefab.transform.position);

                var size = bounds.size;

                var itemRotation = Quaternion.Euler(strokeItem.additionalAngle);
                var itemPosition = strokeItem.tangentPosition;

                var projectionDirection = settings.projectionDirection;
                if (settings.projectionDirectionType == ShapeSettings.ShapeProjectionDirection.PLANE_NORMAL)
                    projectionDirection = -shapeData.normal;
                else if (settings.projectionDirectionType == ShapeSettings.ShapeProjectionDirection.FROM_CENTER)
                    projectionDirection = itemPosition - shapeData.center;
                else if (settings.projectionDirectionType == ShapeSettings.ShapeProjectionDirection.TO_CENTER)
                    projectionDirection = shapeData.center - itemPosition;
                projectionDirection.Normalize();

                var height = size.x + size.y + size.z
                    + maxSurfaceHeight + Vector3.Distance(itemPosition, shapeData.center) + shapeData.radius;

                var ray = new Ray(itemPosition - projectionDirection * height, projectionDirection);
                Transform surface = null;
                Vector3 surfaceNormal = -projectionDirection;
                if (settings.mode != PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE)
                {
                    if (PWBToolRaycast(ray, out RaycastHit itemHit, out GameObject collider,
                        maxDistance: float.MaxValue, layerMask: -1,
                        settings.paintOnPalettePrefabs, settings.paintOnMeshesWithoutCollider,
                        sameOriginAsRay: false, origin: itemPosition,
                        createTempColliders: settings.paintOnMeshesWithoutCollider,
                        ignoreSceneColliders: settings.ignoreSceneColliders))
                    {
                        itemPosition = itemHit.point;
                        surfaceNormal = itemHit.normal;
                        surface = collider.transform;
                        var surfSize = BoundsUtils.GetBounds(surface).size;
                        var h = surfSize.x + surfSize.y + surfSize.z;
                        maxSurfaceHeight = Mathf.Max(h, maxSurfaceHeight);
                    }
                    else if (settings.mode == PaintOnSurfaceToolSettingsBase.PaintMode.ON_SURFACE) continue;
                }

                BrushSettings brushSettings = strokeItem.settings;
                if (settings.overwriteBrushProperties) brushSettings = settings.brushSettings;

                var perpendicularToTheSurface = settings.perpendicularToTheSurface
                    || (brushSettings.rotateToTheSurface && !brushSettings.alwaysOrientUp);

                if (settings.mode != PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE)
                {
                    if (perpendicularToTheSurface)
                    {
                        var itemForward = itemRotation * Vector3.forward;
                        var plane = new Plane(surfaceNormal, itemPosition);
                        itemForward = plane.ClosestPointOnPlane(itemForward + itemPosition) - itemPosition;
                        if (itemForward != Vector3.zero) itemRotation = Quaternion.LookRotation(itemForward, surfaceNormal);
                        projectionDirection = -surfaceNormal;
                    }
                }

                if (!settings.perpendicularToTheSurface && brushSettings.rotateToTheSurface && brushSettings.alwaysOrientUp)
                {
                    var fw = itemRotation * Vector3.forward;
                    const float minMag = 1e-6f;
                    fw.y = 0;
                    if (Mathf.Abs(fw.x) < minMag && Mathf.Abs(fw.z) < minMag) fw = Quaternion.Euler(0, 90, 0) * surfaceNormal;
                    itemRotation = Quaternion.LookRotation(fw, Vector3.up);
                }

                itemPosition += itemRotation * Quaternion.Inverse(prefab.transform.rotation)
                    * (-pivotToCenter + Vector3.up * (size.y / 2));
                if (brushSettings.embedInSurface
                    && settings.mode != PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE)
                {
                    if (brushSettings.embedAtPivotHeight)
                        itemPosition += itemRotation * new Vector3(0f, strokeItem.settings.bottomMagnitude, 0f);
                    else
                    {
                        var TRS = Matrix4x4.TRS(itemPosition, itemRotation,
                            Vector3.Scale(prefab.transform.localScale, strokeItem.scaleMultiplier));
                        var bottomDistanceToSurfce = GetBottomDistanceToSurface(strokeItem.settings.bottomVertices,
                            TRS, Mathf.Abs(strokeItem.settings.bottomMagnitude), settings.paintOnPalettePrefabs,
                            settings.paintOnMeshesWithoutCollider, settings.ignoreSceneColliders,
                            out Transform surfaceTransform);
                        itemPosition += itemRotation * new Vector3(0f, -bottomDistanceToSurfce, 0f);
                    }
                }

                itemPosition += itemRotation * brushSettings.localPositionOffset;
                itemPosition += itemRotation * (Vector3.up * brushSettings.surfaceDistance);

                var rootToWorld = Matrix4x4.TRS(itemPosition, itemRotation, strokeItem.scaleMultiplier)
                    * Matrix4x4.Translate(-prefab.transform.position);
                var itemScale = Vector3.Scale(prefab.transform.localScale, strokeItem.scaleMultiplier);
                var layer = settings.overwritePrefabLayer ? settings.layer : prefab.layer;
                Transform parentTransform = settings.parent;

                var paintItem = new PaintStrokeItem(prefab, strokeItem.settings.guid, itemPosition,
                    itemRotation * prefab.transform.rotation,
                    itemScale, layer, parentTransform, surface, strokeItem.flipX, strokeItem.flipY);
                paintItem.persistentParentId = hexId;

                _paintStroke.Add(paintItem);

                PreviewBrushItem(prefab, rootToWorld, layer, sceneView.camera,
                    redMaterial: false, reverseTriangles: false, strokeItem.flipX, strokeItem.flipY);
                _previewData.Add(new PreviewData(prefab, rootToWorld, layer, strokeItem.flipX, strokeItem.flipY));
            }
        }
    }
}
#pragma warning restore UDR0001
