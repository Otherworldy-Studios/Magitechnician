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
        
        private static System.Collections.Generic.HashSet<GameObject> _blockLineCurrentStrokePlacedObjects
            = new System.Collections.Generic.HashSet<GameObject>();

        #region LINE PREVIEW
        private static void PreviewBlockLine(Camera camera, out Vector3 mousePos3D, out Vector3 localMousePos3D)
        {
            var mousePos2D = Event.current.mousePosition;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePos2D);
            mousePos3D = Vector3.zero;
            localMousePos3D = Vector3.zero;

            Vector3 hitNormal = Vector3.up;
            Vector3 baseHitPoint = Vector3.zero;
            bool hasHit = false;

            if (PWBToolRaycast(mouseRay, out RaycastHit raycastHit, out GameObject collider, maxDistance: float.MaxValue,
                layerMask: -1, paintOnPalettePrefabs: true, castOnMeshesWithoutCollider: true,
                createTempColliders: true, exceptions: _blockLineCurrentStrokePlacedObjects, ignoreSceneColliders: true))
            {
                hitNormal = raycastHit.normal;
                var absNormal = new Vector3(Mathf.Abs(hitNormal.x), Mathf.Abs(hitNormal.y), Mathf.Abs(hitNormal.z));
                var step = GridManager.settings.step;
                var offsetDirection = Vector3.zero;
                if (absNormal.x > absNormal.y && absNormal.x > absNormal.z)
                    offsetDirection = new Vector3(Mathf.Sign(hitNormal.x) * step.x * 0.5f, 0f, 0f);
                else if (absNormal.z > absNormal.x && absNormal.z > absNormal.y)
                    offsetDirection = new Vector3(0f, 0f, Mathf.Sign(hitNormal.z) * step.z * 0.5f);
                else offsetDirection = Vector3.zero;

                var adjustedHitPoint = raycastHit.point + GridManager.settings.rotation * offsetDirection;
                baseHitPoint = SnapBlockPosition(adjustedHitPoint, out localMousePos3D, snapToGridY: true);
                hasHit = true;
            }
            else if (GridRaycast(mouseRay, out RaycastHit gridHit))
            {
                hitNormal = gridHit.normal;
                baseHitPoint = SnapBlockPosition(gridHit.point, out localMousePos3D, snapToGridY: false);
                hasHit = true;
            }

            if (!hasHit) return;

            mousePos3D = baseHitPoint;

            BlockToolModes.lineSecondPoint = baseHitPoint;

            BrushstrokeManager.UpdateBlockLineBrushstroke(setNextIdx: false);

            var toolSettings = BlockManager.settings;
            var cellSize = toolSettings.moduleSize;

            BrushstrokeItem[] brushstroke = BrushstrokeManager.brushstroke;
            _paintStroke.Clear();

            for (int i = 0; i < brushstroke.Length; ++i)
            {
                var strokeItem = brushstroke[i];
                if (strokeItem.settings == null) continue;
                var cellCenter = strokeItem.tangentPosition;

                if (BlockToolModes.projectionAxis != BlockToolModes.ProjectionAxis.NONE)
                {
                    var projectedPosition = ProjectBrushstrokePosition(cellCenter);
                    if (!projectedPosition.HasValue) continue;
                    cellCenter = projectedPosition.Value;
                    strokeItem = strokeItem.Clone();
                    strokeItem.tangentPosition = cellCenter;
                }
                var snapedCellCenter = SnapPositionToBlockCellCenter(cellCenter);
                if (IsBlockCellOccupied(snapedCellCenter, cellSize, GridManager.settings.rotation)) continue;

                PreviewBlockStrokeItem(camera, strokeItem);
            }
        }

        private static Vector3? ProjectBrushstrokePosition(Vector3 position)
        {
            var projectionDirection = GetProjectionDirection();
            var maxRayDistance = float.MaxValue;
            var rayOrigin = position - projectionDirection * (GridManager.settings.step.magnitude * 100);
            var ray = new Ray(rayOrigin, projectionDirection);

            if (PWBToolRaycast(ray, out RaycastHit raycastHit, out GameObject collider,
                maxDistance: maxRayDistance, layerMask: -1, paintOnPalettePrefabs: true,
                castOnMeshesWithoutCollider: true, createTempColliders: true,
                exceptions: _blockLineCurrentStrokePlacedObjects, ignoreSceneColliders: true))
            {

                var hitNormal = raycastHit.normal;

                var offset = GetProjectionDirection() * GetGridSizeAlongProyectionDirection() * 0.5f;
                if (BlockToolModes.selectedEditMode == BlockToolModes.EditMode.ATTACH) offset *= -1f;
                var snapedHitPoint = SnapPositionToCellFaceCenter(raycastHit.point);
                var adjustedHitPoint = snapedHitPoint + offset;

                var snappedPoint = SnapPositionToBlockCellCenter(adjustedHitPoint);

                return snappedPoint;
            }

            if (BlockToolModes.projectionAxis == BlockToolModes.ProjectionAxis.DOWN)
            {
                var gridPlane = new Plane(GridManager.settings.rotation * Vector3.up, GridManager.settings.origin);
                if (gridPlane.Raycast(ray, out float distance))
                {
                    var gridHitPoint = ray.GetPoint(distance);
                    return SnapPositionToBlockCellCenter(gridHitPoint);
                }
            }

            return null;
        }

        private static float GetGridSizeAlongProyectionDirection()
        {
            switch (BlockToolModes.projectionAxis)
            {
                case BlockToolModes.ProjectionAxis.CAMERA:
                    var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                    var cameraDirection = sceneView != null ? sceneView.camera.transform.forward : Vector3.forward;
                    var localDirection = Quaternion.Inverse(GridManager.settings.rotation) * cameraDirection;
                    var absDirection = new Vector3(Mathf.Abs(localDirection.x),
                        Mathf.Abs(localDirection.y), Mathf.Abs(localDirection.z));
                    if (absDirection.x > absDirection.y && absDirection.x > absDirection.z)
                        return GridManager.settings.step.x;
                    else if (absDirection.z > absDirection.x && absDirection.z > absDirection.y)
                        return GridManager.settings.step.z;
                    else
                        return GridManager.settings.step.y;
                case BlockToolModes.ProjectionAxis.DOWN:
                case BlockToolModes.ProjectionAxis.UP:
                    return GridManager.settings.step.y;
                case BlockToolModes.ProjectionAxis.BACK:
                case BlockToolModes.ProjectionAxis.FORWARD:
                    return GridManager.settings.step.z;
                case BlockToolModes.ProjectionAxis.LEFT:
                case BlockToolModes.ProjectionAxis.RIGHT:
                    return GridManager.settings.step.x;
                default:
                    return GridManager.settings.step.y;
            }
        }
        private static Vector3 GetProjectionDirection()
        {
            switch (BlockToolModes.projectionAxis)
            {
                case BlockToolModes.ProjectionAxis.CAMERA:
                    var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                    var cameraDirection = sceneView != null ? sceneView.camera.transform.forward : Vector3.forward;

                    var localDirection = Quaternion.Inverse(GridManager.settings.rotation) * cameraDirection;
                    var absDirection = new Vector3(Mathf.Abs(localDirection.x),
                        Mathf.Abs(localDirection.y), Mathf.Abs(localDirection.z));
                    if (absDirection.x > absDirection.y && absDirection.x > absDirection.z)
                        return GridManager.settings.rotation * (localDirection.x> 0 ? Vector3.right : Vector3.left);
                    else if (absDirection.z > absDirection.x && absDirection.z > absDirection.y)
                        return GridManager.settings.rotation * (localDirection.z > 0 ? Vector3.forward : Vector3.back);
                    else
                        return GridManager.settings.rotation * (localDirection.y > 0 ? Vector3.up : Vector3.down);
                case BlockToolModes.ProjectionAxis.DOWN:
                    return GridManager.settings.rotation * Vector3.down;
                case BlockToolModes.ProjectionAxis.UP:
                    return GridManager.settings.rotation * Vector3.up;
                case BlockToolModes.ProjectionAxis.BACK:
                    return GridManager.settings.rotation * Vector3.back;
                case BlockToolModes.ProjectionAxis.FORWARD:
                    return GridManager.settings.rotation * Vector3.forward;
                case BlockToolModes.ProjectionAxis.LEFT:
                    return GridManager.settings.rotation * Vector3.left;
                case BlockToolModes.ProjectionAxis.RIGHT:
                    return GridManager.settings.rotation * Vector3.right;
                default:
                    return Vector3.down;
            }
        }
        #endregion

        #region LINE INPUT
        private static void AttachBlockLineInput(Vector3 mousePos3D)
        {
            if (Event.current.button == 0 && Event.current.type == EventType.MouseDown)
            {
                _blockLineCurrentStrokePlacedObjects.Clear();
                BlockToolModes.lineFirstPoint = mousePos3D;
                BlockToolModes.lineSecondPoint = mousePos3D;
                BlockToolModes.lineState = BlockToolModes.LineState.SECOND_POINT;
            }

            if (BlockToolModes.lineState == BlockToolModes.LineState.SECOND_POINT
                && Event.current.button == 0 && Event.current.type == EventType.MouseUp)
            {
                var toolSettings = BlockManager.settings;
                var cellSize = toolSettings.moduleSize;
                var cellRotation = GridManager.settings.rotation;
                if (BlockManager.quarterTurns > 0)
                    cellRotation *= Quaternion.AngleAxis(BlockManager.quarterTurns * 90, toolSettings.upwardAxis);

                var brush = PaletteManager.selectedBrush;
                var brushId = brush?.id ?? -1;

                if (BlockToolModes.projectionAxis != BlockToolModes.ProjectionAxis.NONE)
                {
                    ApplyProjectionToBrushstroke(cellRotation);
                }

                var paintedObjects = Paint(BlockManager.settings);

                foreach (var pair in paintedObjects)
                {
                    foreach (var objAndIndex in pair.Value)
                    {
                        if (objAndIndex.Item1 != null)
                        {
                            var objCenter = BoundsUtils.GetBoundsRecursive(objAndIndex.Item1.transform).center;
                            var cellCenter = SnapPositionToBlockCellCenter(objCenter);
                            BlockManager.AddOccupiedCell(cellCenter, cellSize, cellRotation, brushId);
                        }
                    }
                }

                BlockToolModes.lineState = BlockToolModes.LineState.FIRST_POINT;
                _blockLineCurrentStrokePlacedObjects.Clear();
            }
        }

        private static void ApplyProjectionToBrushstroke(Quaternion cellRotation)
        {
            var brushstroke = BrushstrokeManager.brushstroke;
            var toolSettings = BlockManager.settings;
            var cellSize = toolSettings.moduleSize;
            var validItems = new System.Collections.Generic.List<BrushstrokeItem>();

            for (int i = 0; i < brushstroke.Length; ++i)
            {
                var strokeItem = brushstroke[i];
                if (strokeItem.settings == null) continue;

                var projectedPosition = ProjectBrushstrokePosition(strokeItem.tangentPosition);
                if (!projectedPosition.HasValue) continue;

                if (IsBlockCellOccupied(projectedPosition.Value, cellSize, cellRotation)) continue;

                strokeItem.tangentPosition = projectedPosition.Value;
                validItems.Add(strokeItem);
            }

            _paintStroke.Clear();
            foreach (var item in validItems)
            {
                var prefab = item.settings.prefab;
                if (prefab == null) continue;

                var itemRotation = Quaternion.Euler(item.additionalAngle);
                BrushSettings brush = item.settings;
                if (toolSettings.overwriteBrushProperties) brush = toolSettings.brushSettings;
                var brushOffset = toolSettings.subtractBrushOffset ? brush.localPositionOffset : Vector3.zero;
                var cellCenter = item.tangentPosition + itemRotation * brushOffset;
                var scaleMult = item.scaleMultiplier;
                var centerToPivot = GetCenterToPivot(prefab, scaleMult, itemRotation);
                var itemPosition = cellCenter + centerToPivot;
                var itemScale = Vector3.Scale(prefab.transform.localScale, scaleMult);
                var layer = toolSettings.overwritePrefabLayer ? toolSettings.layer : prefab.layer;
                Transform parentTransform = toolSettings.parent;

                var paintItem = new PaintStrokeItem(prefab, item.settings.guid, itemPosition,
                    itemRotation * prefab.transform.rotation, itemScale, layer, parentTransform,
                    surface: null, flipX: false, flipY: false);
                _paintStroke.Add(paintItem);
            }
        }
        #endregion
    }
}
#pragma warning restore UDR0001
