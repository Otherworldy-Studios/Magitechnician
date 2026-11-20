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
        public static void InitializeLineTool()
        {
            LineManager.instance.DeselectAllItems();
            ResetLineState(false);
            PWBCore.staticData.VersionUpdate();
            if (LineManager.settings.paintOnMeshesWithoutCollider)
            {
                if (LineManager.settings.ignoreSceneColliders) UpdateSceneColliderSet();
                UpdateOctree();
            }
        }
        public static void ResetLineState(bool askIfWantToSave = true)
        {
            if (_lineData.state == ToolController.ToolState.NONE) return;
            if (askIfWantToSave && _lineData != null)
            {
                void Save()
                {
                    if (UnityEditor.SceneView.lastActiveSceneView != null)
                        LineStrokePreview(UnityEditor.SceneView.lastActiveSceneView, _lineData,
                            persistent: false, forceUpdate: true, initialIdx: 0);
                    CreateLine();
                }
                AskIfWantToSave(_lineData.state, Save);
            }
            if (_lineData == null) return;
            _snappedToVertex = false;
            selectingLinePoints = false;
            _lineData.Reset();
            OnLineSettingsChanged();
        }

        private static void LineStateNone(bool in2DMode)
        {
            if (Event.current.button == 0 && Event.current.type == EventType.MouseDown && !Event.current.alt)
            {
                _lineData.name = LineData.nextHexId;
                _lineData.closed = false;
                _lineData.state = ToolController.ToolState.PREVIEW;
                Event.current.Use();
            }
            if (TryGetMouseWorldHit(out Vector3 point, out Vector3 normal, LineManager.settings.mode, in2DMode,
                LineManager.settings.paintOnPalettePrefabs, LineManager.settings.paintOnMeshesWithoutCollider, false,
                ignoreSceneColliders: LineManager.settings.ignoreSceneColliders))
            {
                point = SnapToBounds(point);
                point = _snapToVertex ? LinePointSnapping(point)
                    : SnapAndUpdateGridOrigin(point, GridManager.settings.snappingEnabled,
                    LineManager.settings.paintOnPalettePrefabs, LineManager.settings.paintOnMeshesWithoutCollider,
                    LineManager.settings.ignoreSceneColliders, paintOnTheGrid: false, Vector3.down);
                _lineData.SetPoint(0, point, registerUndo: false, selectAll: false);
                _lineData.SetPoint(1, point, registerUndo: false, selectAll: false);
            }
            DrawDotHandleCap(_lineData.GetPoint(0));
        }

        private static void LineStateStraightLine(bool in2DMode)
        {
            if (Event.current.button == 0 && Event.current.type == EventType.MouseDown && !Event.current.alt)
            {
                _lineData.state = ToolController.ToolState.EDIT;
                updateStroke = true;
            }
            if (TryGetMouseWorldHit(out Vector3 point, out Vector3 normal, LineManager.settings.mode, in2DMode,
                LineManager.settings.paintOnPalettePrefabs, LineManager.settings.paintOnMeshesWithoutCollider, false,
                ignoreSceneColliders: LineManager.settings.ignoreSceneColliders))
            {
                point = SnapToBounds(point);
                point = _snapToVertex ? LinePointSnapping(point)
                    : SnapAndUpdateGridOrigin(point, GridManager.settings.snappingEnabled,
                    LineManager.settings.paintOnPalettePrefabs, LineManager.settings.paintOnMeshesWithoutCollider,
                    LineManager.settings.ignoreSceneColliders, paintOnTheGrid: false, Vector3.down);
                _lineData.SetPoint(1, point, registerUndo: false, selectAll: false);
            }

            UnityEditor.Handles.color = new Color(0f, 0f, 0f, 0.7f);
            UnityEditor.Handles.DrawAAPolyLine(8, new Vector3[] { _lineData.GetPoint(0), _lineData.GetPoint(1) });
            UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.7f);
            UnityEditor.Handles.DrawAAPolyLine(4, new Vector3[] { _lineData.GetPoint(0), _lineData.GetPoint(1) });
            DrawDotHandleCap(_lineData.GetPoint(0));
            DrawDotHandleCap(_lineData.GetPoint(1));
        }

        private static void LineStateBezier(UnityEditor.SceneView sceneView)
        {
            var pathPoints = _lineData.pathPoints;
            var forceStrokeUpdate = updateStroke;
            if (updateStroke)
            {
                _lineData.UpdatePath(forceUpdate: false, updateOnSurfacePoints: false);
                pathPoints = _lineData.pathPoints;
                BrushstrokeManager.UpdateLineBrushstroke(pathPoints);
                updateStroke = false;
            }
            LineStrokePreview(sceneView, _lineData, persistent: false, forceStrokeUpdate, 0);
            DrawLine(_lineData, drawSurfacePath: true);
            DrawSelectionRectangle();
            LineInput(false, sceneView, false);

            if (selectingLinePoints && !Event.current.control) _lineData.ClearSelection();

            bool clickOnPoint, wasEdited;
            DrawLineControlPoints(_lineData, isPersistent: false, showHandles: true,
                out clickOnPoint, out bool multiSelection, out bool addToselection,
                out bool removeFromSelection, out wasEdited, out Vector3 delta);
            if (wasEdited) updateStroke = true;
            SelectionRectangleInput(clickOnPoint);
        }

        private static void CreateLine()
        {
            var nextLineId = LineData.nextHexId;
            var objDic = Paint(LineManager.settings, PAINT_CMD, addTempCollider: true,
                persistent: false, toolObjectId: nextLineId);
            if (objDic.Count != 1) return;

            var scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            var sceneGUID = UnityEditor.AssetDatabase.AssetPathToGUID(scenePath);
            if (isInPrefabMode)
                sceneGUID = UnityEditor.AssetDatabase.AssetPathToGUID(prefabStage.assetPath);
            var initialBrushId = PaletteManager.selectedBrush != null ? PaletteManager.selectedBrush.id : -1;
            var objs = objDic[nextLineId].ToArray();
            var persistentData = new LineData(objs, initialBrushId, _lineData);
            LineManager.instance.AddPersistentItem(sceneGUID, persistentData);
            PWBItemsWindow.RepainWindow();
        }

        private static void LineStrokePreview(UnityEditor.SceneView sceneView,
            LineData lineData, bool persistent, bool forceUpdate, int initialIdx)
        {
            if (lineData == null) return;
            var settings = lineData.settings;
            var lastPoint = lineData.lastPathPoint;
            var objectCount = lineData.objectCount;
            var lastObjectTangentPosition = lineData.lastTangentPos;

            BrushstrokeItem[] brushstroke = null;

            if (PreviewIfBrushtrokestaysTheSame(out brushstroke, sceneView.camera, forceUpdate)) return;

            if (!persistent) _paintStroke.Clear();
            
            var idx = initialIdx;
            float maxSurfaceHeight = 0f;
            for (int i = 0; i < brushstroke.Length; ++i)
            {
                var strokeItem = brushstroke[i];
                var prefab = strokeItem.settings.prefab;
                if (prefab == null) continue;
                var bounds = BoundsUtils.GetBoundsRecursive(prefab.transform, prefab.transform.rotation);
                BrushSettings brushSettings = strokeItem.settings;
                if (LineManager.settings.overwriteBrushProperties) brushSettings = LineManager.settings.brushSettings;

                var size = Vector3.Scale(bounds.size, strokeItem.scaleMultiplier);

                var pivotToCenter = Vector3.Scale(
                    prefab.transform.InverseTransformDirection(bounds.center - prefab.transform.position),
                    strokeItem.scaleMultiplier);
                var height = size.x + size.y + size.z + maxSurfaceHeight;
                Vector3 segmentDir = Vector3.zero;

                if (settings.objectsOrientedAlongTheLine && brushstroke.Length > 1)
                {
                    segmentDir = i < brushstroke.Length - 1
                        ? strokeItem.nextTangentPosition - strokeItem.tangentPosition
                        : lastPoint - strokeItem.tangentPosition;
                }
                if (brushstroke.Length == 1)
                {
                    segmentDir = lastPoint - brushstroke[0].tangentPosition;
                    if (persistent && objectCount > 0)
                        segmentDir = lastPoint - lastObjectTangentPosition;
                }
                if (i == brushstroke.Length - 1)
                {
                    var onLineSize = AxesUtils.GetAxisValue(size, settings.axisOrientedAlongTheLine)
                        + settings.gapSize;
                    var segmentSize = segmentDir.magnitude;
                    if (segmentSize > onLineSize) segmentDir = segmentDir.normalized
                            * (settings.spacingType == LineSettings.SpacingType.BOUNDS ? onLineSize : settings.spacing);
                }

                var perpendicularToTheSurface = settings.perpendicularToTheSurface
                    || (brushSettings.rotateToTheSurface && !brushSettings.alwaysOrientUp);

                if (settings.objectsOrientedAlongTheLine && !perpendicularToTheSurface)
                {
                    var projectionAxis = ((AxesUtils.SignedAxis)(settings.projectionDirection)).axis;
                    segmentDir -= AxesUtils.GetVector(AxesUtils.GetAxisValue(segmentDir, projectionAxis), projectionAxis);
                }
                var normal = -settings.projectionDirection;
                var otherAxes = AxesUtils.GetOtherAxes((AxesUtils.SignedAxis)(-settings.projectionDirection));
                var tangetAxis = otherAxes[settings.objectsOrientedAlongTheLine ? 0 : 1];
                Vector3 itemTangent = (AxesUtils.SignedAxis)(tangetAxis);
                var itemRotation = Quaternion.LookRotation(itemTangent, normal);
                var lookAt = Quaternion.LookRotation((Vector3)(AxesUtils.SignedAxis)
                    (settings.axisOrientedAlongTheLine), Vector3.up);

                var itemPosition = strokeItem.tangentPosition + segmentDir / 2;

                var ray = new Ray(itemPosition + normal * height, -normal);
                Transform surface = null;
                if (settings.mode != PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE)
                {
                    if (PWBToolRaycast(ray, out RaycastHit itemHit,
                        out GameObject collider, maxDistance: float.MaxValue, layerMask: -1,
                        settings.paintOnPalettePrefabs, settings.paintOnMeshesWithoutCollider,
                        sameOriginAsRay: false, origin: itemPosition,
                        createTempColliders: settings.paintOnMeshesWithoutCollider,
                        ignoreSceneColliders: settings.ignoreSceneColliders))
                    {
                        itemPosition = itemHit.point;
                        if (perpendicularToTheSurface) normal = itemHit.normal;
                        var colObj = PWBCore.GetGameObjectFromTempCollider(collider);
                        if (colObj != null) surface = colObj.transform;
                        var surfObj = PWBCore.GetGameObjectFromTempCollider(collider);
                        var surfSize = BoundsUtils.GetBounds(surfObj.transform).size;
                        var h = surfSize.x + surfSize.y + surfSize.z;
                        maxSurfaceHeight = Mathf.Max(h, maxSurfaceHeight);
                    }
                    else if (settings.mode == PaintOnSurfaceToolSettingsBase.PaintMode.ON_SURFACE) continue;
                }

                if (perpendicularToTheSurface && segmentDir != Vector3.zero)
                {
                    if (settings.mode == PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE)
                    {
                        var bitangent = Vector3.Cross(segmentDir, normal);
                        var lineNormal = Vector3.Cross(bitangent, segmentDir);
                        itemRotation = Quaternion.LookRotation(segmentDir, lineNormal) * lookAt;
                    }
                    else
                    {
                        var plane = new Plane(normal, itemPosition);
                        var tangent = plane.ClosestPointOnPlane(segmentDir + itemPosition) - itemPosition;
                        itemRotation = Quaternion.LookRotation(tangent, normal) * lookAt;
                    }
                }
                else if (!perpendicularToTheSurface && segmentDir != Vector3.zero)
                    itemRotation = Quaternion.LookRotation(segmentDir, normal) * lookAt;
                itemRotation *= Quaternion.Euler(strokeItem.additionalAngle);

                if (!settings.perpendicularToTheSurface && brushSettings.rotateToTheSurface && brushSettings.alwaysOrientUp)
                {
                    var fw = itemRotation * Vector3.forward;
                    const float minMag = 1e-6f;
                    fw.y = 0;
                    if (Mathf.Abs(fw.x) < minMag && Mathf.Abs(fw.z) < minMag) fw = Quaternion.Euler(0, 90, 0) * normal;
                    itemRotation = Quaternion.LookRotation(fw, Vector3.up);
                }

                itemPosition += normal * strokeItem.surfaceDistance;

                itemPosition += itemRotation * brushSettings.localPositionOffset;
                itemPosition -= itemRotation * (pivotToCenter - Vector3.up * (size.y / 2));


                if (brushSettings.embedInSurface
                    && settings.mode != PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE)
                {
                    if (brushSettings.embedAtPivotHeight)
                        itemPosition += itemRotation * new Vector3(0f, strokeItem.settings.bottomMagnitude, 0f);
                    else
                    {
                        var TRS = Matrix4x4.TRS(itemPosition, itemRotation,
                            Vector3.Scale(prefab.transform.localScale, strokeItem.scaleMultiplier));
                        float magnitudeInDirection;
                        var localDirection = Quaternion.Inverse(itemRotation) * -normal;
                        var furthestVertices = strokeItem.settings.GetFurthestVerticesInDirection(localDirection,
                            out magnitudeInDirection);
                        var distanceTosurface = GetDistanceToSurface(furthestVertices, TRS, -normal,
                            Mathf.Abs(magnitudeInDirection), LineManager.settings.paintOnPalettePrefabs,
                            LineManager.settings.paintOnMeshesWithoutCollider, LineManager.settings.ignoreSceneColliders,
                            out Transform surfaceTransform, prefab);
                        itemPosition -= normal * distanceTosurface;
                    }
                }

                var rootToWorld = Matrix4x4.TRS(itemPosition, itemRotation, strokeItem.scaleMultiplier)
                    * Matrix4x4.Rotate(Quaternion.Inverse(prefab.transform.rotation))
                    * Matrix4x4.Translate(-prefab.transform.position);
                var itemScale = Vector3.Scale(prefab.transform.localScale, strokeItem.scaleMultiplier);
                var layer = settings.overwritePrefabLayer ? settings.layer : prefab.layer;

                Transform parentTransform = settings.parent;
                var paintItem = new PaintStrokeItem(prefab, strokeItem.settings.guid, itemPosition, itemRotation,
                    itemScale, layer, parentTransform, surface, strokeItem.flipX, strokeItem.flipY, idx++);
                paintItem.persistentParentId = persistent ? lineData.hexId : LineData.nextHexId;
                _paintStroke.Add(paintItem);
                PreviewBrushItem(prefab, rootToWorld, layer, sceneView.camera,
                    false, false, strokeItem.flipX, strokeItem.flipY);
                var prevData = new PreviewData(prefab, rootToWorld, layer, strokeItem.flipX, strokeItem.flipY);
                _previewData.Add(prevData);
            }
            if (_persistentPreviewData.ContainsKey(lineData.id)) _persistentPreviewData[lineData.id] = _previewData.ToArray();
            else _persistentPreviewData.Add(lineData.id, _previewData.ToArray());
        }
    }
}
#pragma warning restore UDR0001
