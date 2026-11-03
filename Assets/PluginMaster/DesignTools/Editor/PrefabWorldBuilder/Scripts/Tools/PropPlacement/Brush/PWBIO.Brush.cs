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
        
        private static float _brushAngle = 0f;
        private static bool BrushRaycast(Ray ray, out RaycastHit hit, float maxDistance,
            LayerMask layerMask, BrushToolSettings settings, TerrainLayer[] terrainLayers, out GameObject collider)
        {
            hit = new RaycastHit();
            bool result = false;
            if (PWBToolRaycast(ray, out RaycastHit hitInfo, out collider, maxDistance,
                layerMask, settings.paintOnPalettePrefabs, settings.paintOnMeshesWithoutCollider,
                settings.tagFilter.ToArray(), terrainLayers, createTempColliders: true,
                ignoreSceneColliders: settings.ignoreSceneColliders))
            {
                bool selectedOnlyFilter = !settings.paintOnSelectedOnly;
                if (settings.paintOnSelectedOnly) selectedOnlyFilter = SelectionManager.selection.Contains(collider);
                bool paletteFilter = true;
                if (!settings.paintOnPalettePrefabs)
                    paletteFilter = !PaletteManager.selectedPalette.ContainsSceneObject(collider);
                result = selectedOnlyFilter && paletteFilter;
                if (result) hit = hitInfo;
            }
            return result;
        }

        private static void BrushDuringSceneGUI(UnityEditor.SceneView sceneView)
        {
            BrushstrokeMouseEvents(BrushManager.settings);
            var mousePos = Event.current.mousePosition;
            if (_pinned) mousePos = _pinMouse;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePos);
            bool snappedToVertex = false;
            var closestVertexInfo = new RaycastHit();
            if (_snapToVertex) snappedToVertex = SnapToVertex(mouseRay, out closestVertexInfo, sceneView.in2DMode);
            if (snappedToVertex) mouseRay.origin = closestVertexInfo.point - mouseRay.direction;

            var in2DMode = (PaletteManager.selectedBrush != null && PaletteManager.selectedBrush.isAsset2D)
                && sceneView.in2DMode;
            if (BrushRaycast(mouseRay, out RaycastHit hit, float.MaxValue, -1,
                BrushManager.settings, null, out GameObject collider) || in2DMode)
            {
                if (in2DMode)
                {
                    hit.point = new Vector3(mouseRay.origin.x, mouseRay.origin.y, 0f);
                    hit.normal = Vector3.back;
                }
                DrawBrush(sceneView, ref hit, BrushManager.settings.showPreview);
            }
            else _paintStroke.Clear();

            if (Event.current.button == 0 && !Event.current.alt
                && (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag))
            {
                if (!BrushManager.settings.showPreview) DrawBrush(sceneView, ref hit, true);
                Paint(BrushManager.settings);
                RepaintInspectorAfterScenePaint();
                Event.current.Use();
            }
        }

        private static Vector3 GetTangent(Vector3 normal)
        {
            var rotation = Quaternion.AngleAxis(_brushAngle, Vector3.up);
            var tangent = Vector3.Cross(normal, rotation * Vector3.right);
            if (tangent.sqrMagnitude < 0.000001) tangent = Vector3.Cross(normal, rotation * Vector3.forward);
            tangent.Normalize();
            return tangent;
        }

        private static void DrawBrush(UnityEditor.SceneView sceneView, ref RaycastHit hit, bool preview)
        {
            var settings = BrushManager.settings;
            UpdateStrokeDirection(hit.point);
            if (PaletteManager.selectedBrush == null) return;
            hit.point = SnapToBounds(hit.point);
            hit.point = SnapAndUpdateGridOrigin(hit.point, GridManager.settings.snappingEnabled,
                settings.paintOnPalettePrefabs, settings.paintOnMeshesWithoutCollider, settings.ignoreSceneColliders,
                paintOnTheGrid: false, Vector3.down);
            var tangent = GetTangent(hit.normal);
            var bitangent = Vector3.Cross(hit.normal, tangent);

            if (settings.brushShape == BrushToolSettings.BrushShape.POINT)
            {
                DrawCircleIndicator(hit.point, hit.normal, 0.1f, settings.maxHeightFromCenter,
                tangent, bitangent, hit.normal, settings.paintOnPalettePrefabs, true,
                settings.layerFilter, settings.tagFilter.ToArray());
            }
            else
            {
                UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                UnityEditor.Handles.color = Color.green;
                UnityEditor.Handles.DrawAAPolyLine(3, hit.point, hit.point + hit.normal * settings.maxHeightFromCenter);
                if (settings.brushShape == BrushToolSettings.BrushShape.CIRCLE)
                {
                    DrawCircleIndicator(hit.point, hit.normal, settings.radius, settings.maxHeightFromCenter, tangent,
                        bitangent, hit.normal, settings.paintOnPalettePrefabs, true,
                        settings.layerFilter, settings.tagFilter.ToArray());
                }
                else if (settings.brushShape == BrushToolSettings.BrushShape.SQUARE)
                {
                    DrawSquareIndicator(hit.point, settings.radius, settings.maxHeightFromCenter, tangent,
                        bitangent, hit.normal);
                }
            }
            if (preview) BrushstrokePreview(hit.point, hit.normal, tangent, bitangent, sceneView);

        }
        private static readonly System.Collections.Generic.List<GameObject> _nearbyObjectsAtDensitySpacing
            = new System.Collections.Generic.List<GameObject>(128);
        private static void BrushstrokePreview(Vector3 hitPoint, Vector3 normal,
            Vector3 tangent, Vector3 bitangent, UnityEditor.SceneView sceneView)
        {
            var camera = sceneView.camera;
            var settings = BrushManager.settings;
            _paintStroke.Clear();

            BrushToolSettings.AvoidOverlappingType avoidMode = settings.avoidOverlapping;
            float densitySpacing = 0f;
            if (avoidMode != BrushToolSettings.AvoidOverlappingType.DISABLED)
            {
                float minSpacing = settings.minSpacing;
                float densityFactor = settings.density * 0.01f;
                densitySpacing = Mathf.Sqrt((minSpacing * minSpacing) / densityFactor) * 0.99999f;
            }

            _nearbyObjectsAtDensitySpacing.Clear();

            foreach (var strokeItem in BrushstrokeManager.brushstroke)
            {
                var worldPos = hitPoint + TangentSpaceToWorld(tangent, bitangent,
                    new Vector2(strokeItem.tangentPosition.x, strokeItem.tangentPosition.y));
                var height = settings.heightType == BrushToolSettings.HeightType.CUSTOM
                    ? settings.maxHeightFromCenter : settings.radius;
                BrushSettings brushSettings = strokeItem.settings;
                if (!brushSettings.rotateToTheSurface) normal = Vector3.up;

                var ray = new Ray(worldPos + normal * height, -normal);
                var in2DMode = strokeItem.settings.isAsset2D && sceneView.in2DMode;

                if (BrushRaycast(ray, out RaycastHit itemHit, height * 2f, settings.layerFilter,
                    settings, settings.terrainLayerFilter, out GameObject collider) || in2DMode)
                {
                    if (in2DMode)
                    {
                        itemHit.point = new Vector3(worldPos.x, worldPos.y, 0f);
                        itemHit.normal = Vector3.forward;
                    }
                    else
                    {
                        var slope = Mathf.Abs(Vector3.Angle(Vector3.up, itemHit.normal));
                        if (slope > 90f) slope = 180f - slope;
                        if (slope < settings.slopeFilter.min || slope > settings.slopeFilter.max) continue;
                    }
                    var prefab = strokeItem.settings.prefab;
                    if (prefab == null) continue;
                    if (settings.overwriteBrushProperties)
                    {
                        brushSettings = settings.brushSettings;
                    }
                    var itemRotation = Quaternion.AngleAxis(_brushAngle, Vector3.up);
                    var itemPosition = itemHit.point;
                    if (brushSettings.rotateToTheSurface)
                    {
                        var itemTangent = GetTangent(itemHit.normal);
                        itemRotation = Quaternion.LookRotation(itemTangent, itemHit.normal);
                        itemPosition += itemHit.normal * brushSettings.surfaceDistance;
                    }
                    else itemPosition += normal * brushSettings.surfaceDistance;

                    if (settings.orientAlongBrushstroke)
                    {
                        itemRotation = Quaternion.Euler(settings.additionalOrientationAngle)
                            * Quaternion.LookRotation(_strokeDirection, itemRotation * Vector3.up);
                        itemPosition = hitPoint + itemRotation * (itemPosition - hitPoint);
                    }
                    itemRotation *= Quaternion.Euler(strokeItem.additionalAngle);
                    if (brushSettings.alwaysOrientUp)
                    {
                        var fw = Quaternion.Euler(strokeItem.additionalAngle) * itemHit.normal;
                        fw.y = 0;
                        const float minMag = 1e-6f;
                        if (Mathf.Abs(fw.x) > minMag || Mathf.Abs(fw.z) > minMag)
                            itemRotation = Quaternion.LookRotation(fw, Vector3.up);
                    }

                    itemPosition += itemRotation * brushSettings.localPositionOffset;

                    if (brushSettings.embedInSurface && !brushSettings.embedAtPivotHeight)
                    {
                        var TRS = Matrix4x4.TRS(itemPosition, itemRotation,
                            Vector3.Scale(prefab.transform.localScale, strokeItem.scaleMultiplier));
                        var localDirection = Quaternion.Inverse(itemRotation) * -normal;
                        float magnitudeInDirection;
                        var furthestVertices = strokeItem.settings.GetFurthestVerticesInDirection(localDirection,
                        out magnitudeInDirection);
                        var distanceTosurface = GetDistanceToSurface(furthestVertices, TRS, -normal,
                        Mathf.Abs(magnitudeInDirection), BrushManager.settings.paintOnPalettePrefabs,
                        BrushManager.settings.paintOnMeshesWithoutCollider, BrushManager.settings.ignoreSceneColliders,
                        out Transform surfaceTransform, prefab);
                        itemPosition -= normal * distanceTosurface;
                    }

                    var itemScale = Vector3.Scale(prefab.transform.localScale, strokeItem.scaleMultiplier);
                    Transform surface = null;

                    GameObject colObj = null;
                    if (collider != null)
                        colObj = PWBCore.GetGameObjectFromTempCollider(collider.gameObject);
                    if (colObj != null) surface = colObj.transform;

                    if (avoidMode != BrushToolSettings.AvoidOverlappingType.DISABLED)
                    {

                        var itemBounds = BoundsUtils.GetBoundsRecursive(prefab.transform, Quaternion.identity);
                        var pivotToCenter = itemBounds.center - prefab.transform.position;
                        pivotToCenter = Vector3.Scale(pivotToCenter, strokeItem.scaleMultiplier);
                        pivotToCenter = itemRotation * pivotToCenter;
                        var itemCenter = itemPosition + pivotToCenter;
                        var itemSize = Vector3.Scale(itemBounds.size, strokeItem.scaleMultiplier);
                        itemSize.x += settings.overlapPadding * 2;
                        itemSize.y += settings.overlapPadding * 2;
                        itemSize.z += settings.overlapPadding * 2;
                        var collidingWith = new System.Collections.Generic.List<GameObject>();
                        boundsOctree.GetColliding(collidingWith, new Bounds(itemCenter, itemSize));

                        var isOverlapped = false;
                        if (collidingWith.Count > 0)
                        {
                            System.Collections.Generic.HashSet<GameObject> brushPrefabs = null;
                            System.Collections.Generic.HashSet<GameObject> palettePrefabs = null;
                            if (avoidMode == BrushToolSettings.AvoidOverlappingType.WITH_BRUSH_PREFABS)
                                brushPrefabs = strokeItem.settings.parentSettings.prefabs;
                            else if (avoidMode == BrushToolSettings.AvoidOverlappingType.WITH_PALETTE_PREFABS)
                                palettePrefabs = strokeItem.settings.parentSettings.palette.prefabs;

                            foreach (var sceneObj in collidingWith)
                            {
                                if (avoidMode == BrushToolSettings.AvoidOverlappingType.WITH_ALL_OBJECTS)
                                {
                                    if (HierarchyUtils.IsInHierarchy(surface, sceneObj.transform)) continue;
                                    else
                                    {
                                        isOverlapped = true;
                                        break;
                                    }
                                }
                                if (!UnityEditor.PrefabUtility.IsPartOfAnyPrefab(sceneObj)) continue;
                                GameObject nearestRoot = sceneObj;
                                var go = sceneObj;
                                bool isBrushPrefab = false;
                                do
                                {
                                    go = nearestRoot;
                                    nearestRoot = UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(go);
                                    if (nearestRoot == null) break;
                                    var collidingPrefab = UnityEditor.PrefabUtility
                                        .GetCorrespondingObjectFromSource(nearestRoot);
                                    if (avoidMode == BrushToolSettings.AvoidOverlappingType.WITH_BRUSH_PREFABS
                                        && !brushPrefabs.Contains(collidingPrefab)) continue;
                                    else if (avoidMode == BrushToolSettings.AvoidOverlappingType.WITH_PALETTE_PREFABS
                                        && !palettePrefabs.Contains(collidingPrefab)) continue;
                                    else if (avoidMode == BrushToolSettings.AvoidOverlappingType.WITH_SAME_PREFABS
                                        && collidingPrefab != prefab) continue;
                                    isBrushPrefab = true;
                                    break;
                                } while (nearestRoot != go);
                                if (!isBrushPrefab) continue;
                                isOverlapped = true;
                                break;
                            }
                        }
                        if (isOverlapped) continue;
                    }


                    var layer = settings.overwritePrefabLayer ? settings.layer : prefab.layer;
                    Transform parentTransform = GetParent(settings, prefab.name, false, surface);
                    _paintStroke.Add(new PaintStrokeItem(prefab, strokeItem.settings.guid, itemPosition,
                        itemRotation * Quaternion.Euler(prefab.transform.eulerAngles),
                        itemScale, layer, parentTransform, surface, strokeItem.flipX, strokeItem.flipY));
                    if (settings.showPreview)
                    {
                        var rootToWorld = Matrix4x4.TRS(itemPosition, itemRotation, strokeItem.scaleMultiplier)
                        * Matrix4x4.Translate(-prefab.transform.position);
                        PreviewBrushItem(prefab, rootToWorld, layer, camera, false, false, strokeItem.flipX, strokeItem.flipY);
                    }
                }
            }
        }
    }
}
#pragma warning restore UDR0001
