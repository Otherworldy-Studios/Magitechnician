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
        
        private static System.Collections.Generic.HashSet<GameObject> _blockByBlockCurrentStrokePlacedObjects
            = new System.Collections.Generic.HashSet<GameObject>();
        #region PREVIEW
        private static void PreviewBlockByBlock(Camera camera,
            out Vector3 mousePos3D, out Vector3 localMousePos3D)
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
                createTempColliders: true, exceptions: _blockByBlockCurrentStrokePlacedObjects, ignoreSceneColliders: true))
            {
                var ts = BlockManager.settings;
                var cSize = ts.moduleSize;
                var cRotation = GridManager.settings.rotation;
                if (BlockManager.quarterTurns > 0)
                    cRotation *= Quaternion.AngleAxis(BlockManager.quarterTurns * 90, ts.upwardAxis);

                var boundsCenter = BoundsUtils.GetBoundsRecursive(collider.transform).center;
                var cellCenter = SnapPositionToBlockCellCenter(boundsCenter);

                if (CellRayCast(cellCenter, cSize, cRotation, mouseRay, out RaycastHit cellHit))
                {
                    hitNormal = cellHit.normal;
                    var absNormal = new Vector3(Mathf.Abs(hitNormal.x), Mathf.Abs(hitNormal.y), Mathf.Abs(hitNormal.z));
                    var step = GridManager.settings.step;
                    var offsetDirection = Vector3.zero;
                    if (absNormal.y > absNormal.x && absNormal.y > absNormal.z)
                        offsetDirection = new Vector3(0f, Mathf.Sign(hitNormal.y) * step.y * 0.5f, 0f);
                    else if(absNormal.x > absNormal.y && absNormal.x > absNormal.z)
                        offsetDirection = new Vector3(Mathf.Sign(hitNormal.x) * step.x * 0.5f, 0f, 0f);
                    else if (absNormal.z > absNormal.x && absNormal.z > absNormal.y)
                        offsetDirection = new Vector3(0f, 0f, Mathf.Sign(hitNormal.z) * step.z * 0.5f);

                    var adjustedHitPoint = cellHit.point + GridManager.settings.rotation * offsetDirection;

                    var baseCellCenter = SnapPositionToBlockCellCenter(adjustedHitPoint);
                    baseHitPoint = SnapBlockPosition(baseCellCenter, out localMousePos3D);
                    hasHit = true;
                }
            }
            if (!hasHit && GridRaycast(mouseRay, out RaycastHit gridHit))
            {
                hitNormal = gridHit.normal;
                baseHitPoint = SnapBlockPosition(gridHit.point, out localMousePos3D, snapToGridY: false);
                hasHit = true;
            }

            if (!hasHit) return;

            mousePos3D = baseHitPoint;

            BrushstrokeManager.SetBlockBrushParameters(baseHitPoint, hitNormal);
            BrushstrokeManager.UpdateBlockByBlockBrushstroke(setNextIdx: false);

            var toolSettings = BlockManager.settings;
            var cellSize = toolSettings.moduleSize;
            var cellRotation = GridManager.settings.rotation;
            if (BlockManager.quarterTurns > 0)
                cellRotation *= Quaternion.AngleAxis(BlockManager.quarterTurns * 90, toolSettings.upwardAxis);

            BrushstrokeItem[] brushstroke = BrushstrokeManager.brushstroke;
            _paintStroke.Clear();

            for (int i = 0; i < brushstroke.Length; ++i)
            {
                var strokeItem = brushstroke[i];
                if (strokeItem.settings == null) continue;
                var baseCellCenter = SnapPositionToBlockCellCenter(strokeItem.tangentPosition);

                if (IsBlockCellOccupied(baseCellCenter, cellSize, cellRotation))
                    continue;

                PreviewBlockStrokeItem(camera, strokeItem);
            }
        }

        private static void PreviewBlockStrokeItem(Camera camera, BrushstrokeItem strokeItem)
        {
            var prefab = strokeItem.settings.prefab;
            if (prefab == null) return;

            var toolSettings = BlockManager.settings;
            var itemRotation = Quaternion.Euler(strokeItem.additionalAngle);
            var previewRotation = itemRotation;
            previewRotation *= Quaternion.Inverse(prefab.transform.rotation);

            var baseCellCenter = strokeItem.tangentPosition;
            BrushSettings brush = strokeItem.settings;
            if (toolSettings.overwriteBrushProperties) brush = toolSettings.brushSettings;
            var brushOffset = toolSettings.subtractBrushOffset ? brush.localPositionOffset : Vector3.zero;
            var cellCenter = baseCellCenter + itemRotation * brushOffset;

            var scaleMult = strokeItem.scaleMultiplier;
            var centerToPivot = GetCenterToPivot(prefab, scaleMult, itemRotation);
            var translateMatrix = Matrix4x4.Translate(-prefab.transform.position);
            var layer = toolSettings.overwritePrefabLayer ? toolSettings.layer : prefab.layer;
            var itemScale = Vector3.Scale(prefab.transform.localScale, scaleMult);
            Transform parentTransform = toolSettings.parent;

            var cellSize = toolSettings.moduleSize;
            var cellRotation = GridManager.settings.rotation;
            if (BlockManager.quarterTurns > 0)
                cellRotation *= Quaternion.AngleAxis(BlockManager.quarterTurns * 90, toolSettings.upwardAxis);

            var itemPosition = cellCenter + centerToPivot;
            var rootToWorld = Matrix4x4.TRS(itemPosition, previewRotation, scaleMult) * translateMatrix;

            PreviewBrushItem(prefab, rootToWorld, layer, camera,
                redMaterial: false, reverseTriangles: false, flipX: false, flipY: false);

            var paintItem = new PaintStrokeItem(prefab, strokeItem.settings.guid, itemPosition,
                itemRotation * prefab.transform.rotation, itemScale, layer, parentTransform,
                surface: null, flipX: false, flipY: false);
            _paintStroke.Add(paintItem);

            var rotatedBrushOffset = brushOffset;
            if (BlockManager.quarterTurns > 0)
                rotatedBrushOffset = Quaternion.AngleAxis(BlockManager.quarterTurns * 90,
                    toolSettings.upwardAxis) * rotatedBrushOffset;
            var pureCellCenter = baseCellCenter
                + GridManager.settings.rotation * (rotatedBrushOffset * 0.5f);

            var mirroredTransforms = GetMirrorAndAxisModesTransforms(pureCellCenter, brushOffset, itemRotation);
            foreach (var mt in mirroredTransforms)
            {
                var mtcellCenter = SnapPositionToBlockCellCenter(mt.position);
                if (IsBlockCellOccupied(mtcellCenter, cellSize, cellRotation)) continue;

                var mirroredItemRotation = mt.rotationOffset * itemRotation;
                var mirroredPreviewRotation = mirroredItemRotation * Quaternion.Inverse(prefab.transform.rotation);
                var mirroredScaleMult = Vector3.Scale(scaleMult, mt.scaleMultiplier);
                var mirroredCenterToPivot = GetCenterToPivot(prefab, mirroredScaleMult, mirroredItemRotation);
                var mirroredItemScale = Vector3.Scale(prefab.transform.localScale, mirroredScaleMult);
                var reverseTriangles = (mt.scaleMultiplier.x * mt.scaleMultiplier.y * mt.scaleMultiplier.z) < 0;
                var mirroredItemPosition = mt.position + mirroredCenterToPivot;

                var mirroredRootToWorld = Matrix4x4.TRS(mirroredItemPosition, mirroredPreviewRotation,
                    mirroredScaleMult) * translateMatrix;

                PreviewBrushItem(prefab, mirroredRootToWorld, layer, camera,
                    redMaterial: false, reverseTriangles: reverseTriangles, flipX: false, flipY: false);

                var mirroredPaintItem = new PaintStrokeItem(prefab, strokeItem.settings.guid, mirroredItemPosition,
                    mirroredItemRotation * prefab.transform.rotation, mirroredItemScale, layer, parentTransform,
                    surface: null, flipX: false, flipY: false);
                _paintStroke.Add(mirroredPaintItem);
            }
        }

        private static void PreviewBlockSingleTile(Camera camera, out Vector3 mousePos3D, out Vector3 localMousePos3D)
        {
            var mousePos2D = Event.current.mousePosition;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePos2D);
            mousePos3D = Vector3.zero;
            localMousePos3D = Vector3.zero;

            Vector3 hitNormal = Vector3.up;
            Vector3 baseHitPoint = Vector3.zero;
            Vector3 baseCellCenter = Vector3.zero;
            bool hasHit = false;

            if (PWBToolRaycast(mouseRay, out RaycastHit raycastHit, out GameObject collider, maxDistance: float.MaxValue,
                layerMask: -1, paintOnPalettePrefabs: true, castOnMeshesWithoutCollider: true,
                createTempColliders: true, exceptions: _blockByBlockCurrentStrokePlacedObjects, ignoreSceneColliders: true))
            {
                var ts = BlockManager.settings;
                var cSize = ts.moduleSize;
                var cRotation = GridManager.settings.rotation;
                if (BlockManager.quarterTurns > 0)
                    cRotation *= Quaternion.AngleAxis(BlockManager.quarterTurns * 90, ts.upwardAxis);

                var boundsCenter = BoundsUtils.GetBoundsRecursive(collider.transform).center;
                var cellCenter =  SnapPositionToBlockCellCenter(boundsCenter);

                if (CellRayCast(cellCenter, cSize, cRotation, mouseRay, out RaycastHit cellHit))
                {
                    hitNormal = cellHit.normal;
                    var absNormal = new Vector3(Mathf.Abs(hitNormal.x), Mathf.Abs(hitNormal.y), Mathf.Abs(hitNormal.z));
                    var step = GridManager.settings.step;
                    var offsetDirection = Vector3.zero;
                    if (absNormal.y > absNormal.x && absNormal.y > absNormal.z)
                        offsetDirection = new Vector3(0f, Mathf.Sign(hitNormal.y) * step.y * 0.5f, 0f);
                    else if (absNormal.x > absNormal.y && absNormal.x > absNormal.z)
                        offsetDirection = new Vector3(Mathf.Sign(hitNormal.x) * step.x * 0.5f, 0f, 0f);
                    else if (absNormal.z > absNormal.x && absNormal.z > absNormal.y)
                        offsetDirection = new Vector3(0f, 0f, Mathf.Sign(hitNormal.z) * step.z * 0.5f);

                    var adjustedHitPoint = cellHit.point + GridManager.settings.rotation * offsetDirection;
                    baseCellCenter = SnapPositionToBlockCellCenter(adjustedHitPoint);
                    baseHitPoint = SnapBlockPosition(baseCellCenter, out localMousePos3D);
                    hasHit = true;
                }
            }
            if (!hasHit && GridRaycast(mouseRay, out RaycastHit gridHit))
            {
                hitNormal = gridHit.normal;
                baseHitPoint = SnapBlockPosition(gridHit.point, out localMousePos3D, out baseCellCenter,
                    snapToGridY: false);
                hasHit = true;
            }

            if (!hasHit) return;

            mousePos3D = baseHitPoint;

            BrushstrokeItem[] brushstroke = BrushstrokeManager.brushstroke;
            if (brushstroke.Length == 0)
            {
                BrushstrokeManager.UpdateBlockByBlockBrushstroke(setNextIdx: false);
                brushstroke = BrushstrokeManager.brushstroke;
            }
            if (brushstroke.Length == 0) return;

            var strokeItem = brushstroke[0].Clone();
            if (strokeItem.settings == null)
            {
                BrushstrokeManager.UpdateBlockByBlockBrushstroke(setNextIdx: false);
                return;
            }

            var prefab = strokeItem.settings.prefab;
            if (prefab == null) return;

            var toolSettings = BlockManager.settings;
            var itemRotation = Quaternion.Euler(strokeItem.additionalAngle);
            var previewRotation = itemRotation;
            previewRotation *= Quaternion.Inverse(prefab.transform.rotation);

            BrushSettings brush = strokeItem.settings;
            if (toolSettings.overwriteBrushProperties) brush = toolSettings.brushSettings;
            var brushOffset = toolSettings.subtractBrushOffset ? brush.localPositionOffset : Vector3.zero;
            var cellCenter2 = mousePos3D + itemRotation * brushOffset;
            if (_modularDeleteMode)
            {
                if (toolSettings.subtractBrushOffset)
                {
                    var r = GridManager.settings.rotation;
                    if (BlockManager.quarterTurns > 0)
                        r *= Quaternion.AngleAxis(BlockManager.quarterTurns * 90, toolSettings.upwardAxis);
                    cellCenter2 -= r * (brush.localPositionOffset * 0.5f);
                }
                var TRS = Matrix4x4.TRS(cellCenter2, GridManager.settings.rotation, toolSettings.moduleSize);
                Graphics.DrawMesh(cubeMesh, TRS, transparentRedMaterial2, 0, camera);
                return;
            }

            var cellSize = toolSettings.moduleSize;
            var cellRotation = GridManager.settings.rotation;
            if (BlockManager.quarterTurns > 0)
                cellRotation *= Quaternion.AngleAxis(BlockManager.quarterTurns * 90, toolSettings.upwardAxis);

            var scaleMult = strokeItem.scaleMultiplier;
            var centerToPivot = GetCenterToPivot(prefab, scaleMult, itemRotation);
            var translateMatrix = Matrix4x4.Translate(-prefab.transform.position);
            var layer = toolSettings.overwritePrefabLayer ? toolSettings.layer : prefab.layer;
            var itemScale = Vector3.Scale(prefab.transform.localScale, scaleMult);
            Transform parentTransform = toolSettings.parent;

            _paintStroke.Clear();
            var snappedCellCenter = SnapPositionToBlockCellCenter(cellCenter2);
            var isCellOccupied = IsBlockCellOccupied(snappedCellCenter, cellSize, cellRotation);
            if (!isCellOccupied)
            {
                var itemPosition = cellCenter2 + centerToPivot;
                var rootToWorld = Matrix4x4.TRS(itemPosition, previewRotation, scaleMult) * translateMatrix;
                PreviewBrushItem(prefab, rootToWorld, layer, camera,
                    redMaterial: false, reverseTriangles: false, flipX: false, flipY: false);

                var paintItem = new PaintStrokeItem(prefab, strokeItem.settings.guid, itemPosition,
                    itemRotation * prefab.transform.rotation, itemScale, layer, parentTransform,
                    surface: null, flipX: false, flipY: false);
                _paintStroke.Add(paintItem);
            }

            var mirroredTransforms = GetMirrorAndAxisModesTransforms(baseCellCenter, brushOffset, itemRotation);
            foreach (var mt in mirroredTransforms)
            {
                var mtCellCenter = SnapPositionToBlockCellCenter(mt.position);
                var mirroredCellOccupied = IsBlockCellOccupied(mtCellCenter, cellSize, cellRotation);
                if (mirroredCellOccupied) continue;

                var mirroredItemRotation = mt.rotationOffset * itemRotation;
                var mirroredPreviewRotation = mirroredItemRotation * Quaternion.Inverse(prefab.transform.rotation);
                var mirroredScaleMult = Vector3.Scale(scaleMult, mt.scaleMultiplier);
                var mirroredCenterToPivot = GetCenterToPivot(prefab, mirroredScaleMult, mirroredItemRotation);
                var mirroredItemScale = Vector3.Scale(prefab.transform.localScale, mirroredScaleMult);
                var reverseTriangles = (mt.scaleMultiplier.x * mt.scaleMultiplier.y * mt.scaleMultiplier.z) < 0;
                var mirroredItemPosition = mt.position + mirroredCenterToPivot;
                
                var mirroredRootToWorld = Matrix4x4.TRS(mirroredItemPosition, mirroredPreviewRotation,
                    mirroredScaleMult) * translateMatrix;
                PreviewBrushItem(prefab, mirroredRootToWorld, layer, camera,
                    redMaterial: false, reverseTriangles: reverseTriangles, flipX: false, flipY: false);

                var mirroredPaintItem = new PaintStrokeItem(prefab, strokeItem.settings.guid, mirroredItemPosition,
                    mirroredItemRotation * prefab.transform.rotation, mirroredItemScale, layer, parentTransform,
                    surface: null, flipX: false, flipY: false);
                _paintStroke.Add(mirroredPaintItem);
            }
        }
        #endregion
        #region INPUT
        private static void AttachBlockByBlockInput(Vector3 mousePos3D)
        {
            if (Event.current.button == 0 && Event.current.type == EventType.MouseDown)
            {
                _blockByBlockCurrentStrokePlacedObjects.Clear();
            }

            if (Event.current.button == 0 &&
                (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag))
            {
                var toolSettings = BlockManager.settings;
                var cellSize = toolSettings.moduleSize;
                var cellRotation = GridManager.settings.rotation;
                if (BlockManager.quarterTurns > 0)
                    cellRotation *= Quaternion.AngleAxis(BlockManager.quarterTurns * 90, toolSettings.upwardAxis);

                var brush = PaletteManager.selectedBrush;
                var brushId = brush?.id ?? -1;

                var tokenIndexBeforePaint = brush != null ? brush.GetPatternTokenIndex() : 0;

                var paintedObjects = Paint(BlockManager.settings);

                var placedThisCall = 0;
                foreach (var pair in paintedObjects)
                {
                    foreach (var objAndIndex in pair.Value)
                    {
                        if (objAndIndex.Item1 != null)
                        {
                            ++placedThisCall;

                            _blockByBlockCurrentStrokePlacedObjects.Add(objAndIndex.Item1);
                            var objCenter = BoundsUtils.GetBoundsRecursive(objAndIndex.Item1.transform).center;
                            var cellCenter = SnapPositionToBlockCellCenter(objCenter);
                            BlockManager.AddOccupiedCell(cellCenter, cellSize, cellRotation, brushId);
                        }
                    }
                }

                if (brush != null)
                {
                    if (brush.restartPatternForEachStroke)
                    {
                        brush.ResetCurrentItemIndex();
                    }
                    else if (brush.frequencyMode == MultibrushSettings.FrequencyMode.PATTERN)
                    {
                        brush.SetPatternTokenIndex(tokenIndexBeforePaint);
                        for (int i = 0; i < placedThisCall; ++i) brush.SetNextItemIndex();
                    }
                    else
                    {
                        for (int i = 0; i < placedThisCall; ++i) brush.SetNextItemIndex();
                    }
                }

                return;
            }

            if (Event.current.button == 0 && Event.current.type == EventType.MouseUp)
            {
                _blockByBlockCurrentStrokePlacedObjects.Clear();
            }
        }
        #endregion
    }
}
#pragma warning restore UDR0001
