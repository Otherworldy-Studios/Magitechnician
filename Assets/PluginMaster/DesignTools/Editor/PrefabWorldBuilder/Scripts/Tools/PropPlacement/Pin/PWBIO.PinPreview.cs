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

        private static Vector3 _prevPinHitNormal = Vector3.zero;
        private static bool snapPinRotationToGrid = false;

        private static void DrawPin(UnityEditor.SceneView sceneView, RaycastHit hit,
            bool snapToGrid)
        {
            if (PaletteManager.selectedBrush == null) return;
            if (!_pinned)
            {
                hit.point = SnapToBounds(hit.point);
                hit.point = SnapAndUpdateGridOrigin(hit.point, snapToGrid,
                   PinManager.settings.paintOnPalettePrefabs, PinManager.settings.paintOnMeshesWithoutCollider,
                   PinManager.settings.ignoreSceneColliders,
                   paintOnTheGrid: PinManager.settings.mode == PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE,
                   -hit.normal);
                _pinHit = hit;
                if(PinManager.settings.flattenTerrain) _pinHit.normal = Vector3.up;
            }
            PinPreview(sceneView.camera);
        }
        
        private static Vector3 GetPinAngleSnappedToGrid(Vector3 position, Quaternion rotation)
        {
            var gridRotation = Quaternion.identity;
            if (GridManager.settings.radialGridEnabled)
            {
                var gridLocalNormal = (Vector3)(AxesUtils.SignedAxis)GridManager.settings.gridAxis;
                var gridNormal = GridManager.settings.rotation * gridLocalNormal;
                var posOnPlane = Vector3.ProjectOnPlane(position, gridNormal) - GridManager.settings.origin;
                gridRotation = Quaternion.Inverse(rotation) * Quaternion.LookRotation(posOnPlane, gridNormal);
            }
            else gridRotation = Quaternion.Inverse(rotation) * GridManager.settings.rotation;
            Vector3 GetSnappedToGrid(Vector3 v)
            {
                var xProj = Vector3.Project(v, gridRotation * Vector3.right);
                var yProj = Vector3.Project(v, gridRotation * Vector3.up);
                var zProj = Vector3.Project(v, gridRotation * Vector3.forward);
                var xMag = xProj.magnitude;
                var yMag = yProj.magnitude;
                var zMag = zProj.magnitude;
                if (xMag >= yMag && xMag >= zMag) return xProj;
                else if (yMag >= xMag && yMag >= zMag) return yProj;
                else return zProj;
            }
            var pinRotation = Quaternion.Euler(_pinAngle);
            var snappedUp = GetSnappedToGrid(pinRotation * Vector3.up);
            var snappedFw = GetSnappedToGrid(pinRotation * Vector3.forward);
            return Quaternion.LookRotation(snappedFw, snappedUp).eulerAngles;
        }
        private static void PinPreview(Camera camera)
        {
            _paintStroke.Clear();
            if (BrushstrokeManager.brushstroke.Length == 0) return;
            var strokeItem = BrushstrokeManager.brushstroke[0].Clone();
            var prefab = strokeItem.settings.prefab;
            if (prefab == null) return;
            BrushSettings brushSettings = strokeItem.settings;
            if (PinManager.settings.overwriteBrushProperties) brushSettings = PinManager.settings.brushSettings;

            var itemRotation = Quaternion.identity;
            var itemPosition = _pinHit.point;
            if (brushSettings.rotateToTheSurface && !PinManager.settings.flattenTerrain)
            {
                if (_pinHit.normal == Vector3.zero) _pinHit.normal = Vector3.up;
                var normal = _pinHit.normal.normalized;

                bool GetYOnPlane(out float y)
                {
                    y = 0;
                    if (Mathf.Approximately(normal.y, 0f)) return false;
                    y = -normal.x / normal.y;
                    return true;
                }
                bool GetZOnPlane(out float z)
                {
                    z = 0f;
                    if (Mathf.Approximately(normal.z, 0f)) return false;
                    z = -normal.x / normal.z;
                    return true;
                }
                bool GetXOnPlane(out float x)
                {
                    x = 0f;
                    if (Mathf.Approximately(normal.x, 0f)) return false;
                    x = -normal.z / normal.x;
                    return true;
                }
                var right = Vector3.right;
                if (GetYOnPlane(out float y)) right = new Vector3(1, y, 0);
                else if (GetZOnPlane(out float z)) right = new Vector3(1, 0, z);
                else if (GetXOnPlane(out float x)) right = new Vector3(x, 0, 1);
                var forward = Vector3.Cross(normal, right);
                itemRotation = Quaternion.LookRotation(forward, normal);
                if (strokeItem.settings.isAsset2D) itemRotation *= Quaternion.Euler(90, 0, 0);
            }

            GameObject objUnderMouse = null;
            if (_pinHit.collider != null)
            {
                var parentUnderMouse = _pinHit.collider.transform.parent;
                if (parentUnderMouse != null
#if UNITY_6000_3_OR_NEWER
                    && parentUnderMouse.gameObject.GetEntityId() == PWBCore.parentColliderId)
                    objUnderMouse = PWBCore.GetGameObjectFromTempColliderId(
                        _pinHit.collider.gameObject.GetEntityId());
#else
                    && parentUnderMouse.gameObject.GetInstanceID() == PWBCore.parentColliderId)
                    objUnderMouse = PWBCore.GetGameObjectFromTempColliderId(
                        _pinHit.collider.gameObject.GetInstanceID());
#endif
                else objUnderMouse = _pinHit.collider.gameObject;
            }
            if (PinManager.settings.paintOnSelectedOnly && objUnderMouse != null
                && !SelectionManager.selection.Contains(objUnderMouse)) return;
            itemRotation *= Quaternion.Euler(strokeItem.additionalAngle);

            var pinAngle = _pinAngle;
            if (PinManager.settings.snapRotationToGrid || snapPinRotationToGrid)
            {
                pinAngle = GetPinAngleSnappedToGrid(itemPosition, itemRotation);
                if (snapPinRotationToGrid)
                {
                    _pinAngle = pinAngle;
                    snapPinRotationToGrid = false;
                }
            }
            itemRotation *= Quaternion.Euler(pinAngle);

            if (brushSettings.rotateToTheSurface && brushSettings.alwaysOrientUp && !strokeItem.settings.isAsset2D)
            {
                var fw = (Quaternion.Euler(strokeItem.additionalAngle) * Quaternion.Euler(_pinAngle)) * _pinHit.normal;
                fw.y = 0;
                const float minMag = 1e-6f;
                if (Mathf.Abs(fw.x) > minMag || Mathf.Abs(fw.z) > minMag)
                    itemRotation = Quaternion.LookRotation(fw, Vector3.up);
            }
            itemPosition += itemRotation * brushSettings.localPositionOffset;

            var scaleMult = strokeItem.scaleMultiplier * _pinScale;
            var itemScale = Vector3.Scale(prefab.transform.localScale, scaleMult);

            UpdatePinValues(prefab, itemRotation * prefab.transform.rotation);
            var invScaleMult = new Vector3(1 / scaleMult.x, 1 / scaleMult.y, 1 / scaleMult.z);
            var previewPinOffset = Vector3.Scale(_pinOffset, invScaleMult);
            var strokePinOffset = _pinOffset;
            if (brushSettings.embedInSurface && PinManager.settings.mode != PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE)
            {

                if (brushSettings.embedAtPivotHeight)
                {
                    var embedOffset = _pinBoundPoints[pinBoundLayerIdx][0] - _pinOffset;
                    embedOffset = Vector3.Project(embedOffset, _pinProjectionDirection);
                    itemPosition += embedOffset;
                }
                else
                {
                    var TRS = Matrix4x4.TRS(itemPosition + _pinOffset, itemRotation,
                        Vector3.Scale(prefab.transform.localScale, scaleMult));
                    float magnitudeInDirection;
                    var localDirection = Quaternion.Inverse(itemRotation) * _pinProjectionDirection;
                    var furthestVertices = strokeItem.settings.GetFurthestVerticesInDirection(localDirection,
                        out magnitudeInDirection);
                    var distanceTosurface = GetDistanceToSurface(furthestVertices, TRS, _pinProjectionDirection,
                        Mathf.Abs(magnitudeInDirection), PinManager.settings.paintOnPalettePrefabs,
                        PinManager.settings.paintOnMeshesWithoutCollider, PinManager.settings.ignoreSceneColliders,
                        out Transform surfaceTransform, prefab,
                        createTemColliders: false);
                    itemPosition += _pinProjectionDirection * distanceTosurface;
                    if (PinManager.settings.flattenTerrain)
                    {
                        itemPosition.y = _pinHit.point.y;
                    }
                }
            }

            itemPosition -= _pinProjectionDirection * (strokeItem.surfaceDistance + _pinDistanceFromSurface);

            var layer = PinManager.settings.overwritePrefabLayer ? PinManager.settings.layer : prefab.layer;
            Transform parentTransform = GetParent(PinManager.settings, prefab.name, false, _pinSurface);

            if (PinManager.settings.avoidOverlapping)
            {
                var itemBounds = BoundsUtils.GetBoundsRecursive(prefab.transform, Quaternion.identity);
                var pivotToCenter = itemBounds.center - prefab.transform.position;
                pivotToCenter = Vector3.Scale(pivotToCenter, scaleMult);
                pivotToCenter = itemRotation * pivotToCenter;
                var itemCenter = itemPosition + pivotToCenter;
                var itemSize = Vector3.Scale(itemBounds.size, strokeItem.scaleMultiplier);

                var collidingWith = new System.Collections.Generic.List<GameObject>();
                boundsOctree.GetColliding(collidingWith, new Bounds(itemCenter, itemSize));
                var isOverlapped = false;
                if (collidingWith.Count > 0)
                {
                    var brushPrefabs = strokeItem.settings.parentSettings.prefabs;
                    foreach (var sceneObj in collidingWith)
                    {
                        if (!UnityEditor.PrefabUtility.IsPartOfAnyPrefab(sceneObj)) continue;
                        GameObject nearestRoot = sceneObj;
                        var go = sceneObj;
                        bool isBrushPrefab = false;
                        do
                        {
                            go = nearestRoot;
                            nearestRoot = UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(go);
                            if (nearestRoot == null) break;
                            var collidingPrefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(nearestRoot);
                            if (!brushPrefabs.Contains(collidingPrefab)) continue;
                            isBrushPrefab = true;
                            break;
                        } while (nearestRoot != go);
                        if (!isBrushPrefab) continue;
                        isOverlapped = true;
                        break;
                    }
                }
                if (isOverlapped)
                {
                    DrawPinHandles(new Color(1f, 0f, 0f, 0.7f));
                    return;
                }
            }

            var flipX = strokeItem.flipX ^ _pinFlipX;
            _paintStroke.Add(new PaintStrokeItem(prefab, strokeItem.settings.guid, itemPosition + strokePinOffset,
                itemRotation * prefab.transform.rotation,
                itemScale, layer, parentTransform, _pinSurface, flipX, strokeItem.flipY));

            var translateMatrix = Matrix4x4.Translate(Quaternion.Inverse(itemRotation) * previewPinOffset
               - prefab.transform.position);
            var rootToWorld = Matrix4x4.TRS(itemPosition, itemRotation, scaleMult) * translateMatrix;
            PreviewBrushItem(prefab, rootToWorld, layer, camera, false, false, flipX, strokeItem.flipY);

            if (!brushSettings.isAsset2D && _prevPinHitNormal != _pinHit.normal) _prevPinHitNormal = _pinHit.normal;


            DrawPinHandles(new Color(1f, 1f, 1f, 0.7f));

            _pinSurface = null;
        }
        private static void DrawPinHandles(Color color)
        {
            if (BrushstrokeManager.brushstroke.Length == 0) return;
            var strokeItem = BrushstrokeManager.brushstroke[0];
            var prefab = strokeItem.settings.prefab;
            if (prefab == null) return;
            var pos = Vector3.zero;
            var prevPos = Vector3.zero;
            var pos0 = Vector3.zero;
            var handlePoints = new System.Collections.Generic.List<Vector3>();
            UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            if (_pinBoundPoints.Count == 0) ResetPinValues();
            var flatteningPoints = new System.Collections.Generic.List<Vector3>();
            var layerIdx = Mathf.Clamp(pinBoundLayerIdx, 0, _pinBoundPoints.Count - 1);
            var pivotPos = Vector3.zero;
            for (int i = 0; i < _pinBoundPoints[layerIdx].Count; ++i)
            {
                prevPos = pos;
                pos = _pinOffset - _pinBoundPoints[layerIdx][i] + _pinHit.point;
                if (i > _pinBoundPoints[layerIdx].Count - 5)
                {
                    if (i == _pinBoundPoints[layerIdx].Count - 4) pos0 = pos;
                    else if (i < _pinBoundPoints[layerIdx].Count)
                    {
                        UnityEditor.Handles.color = new Color(0f, 0f, 0f, 0.7f);
                        UnityEditor.Handles.DrawAAPolyLine(6, new Vector3[] { prevPos, pos });
                        UnityEditor.Handles.color = color;
                        UnityEditor.Handles.DrawAAPolyLine(2, new Vector3[] { prevPos, pos });
                    }
                }
                flatteningPoints.Add(pos);
                if (i == 0) pivotPos = pos;
                if (pinBoundPointIdx == i) continue;
                handlePoints.Add(pos);
            }
            UnityEditor.Handles.color = new Color(0f, 0f, 0f, 0.7f);
            UnityEditor.Handles.DrawAAPolyLine(6, new Vector3[] { pos, pos0 });
            UnityEditor.Handles.color = color;
            UnityEditor.Handles.DrawAAPolyLine(2, new Vector3[] { pos, pos0 });

            if (PinManager.settings.flattenTerrain && _pinHit.collider != null
                && _pinHit.collider.GetComponent<Terrain>() != null)
            {
                Vector3 p0, p1, p2, p3;
                var n = flatteningPoints.Count;


                var side1_2 = flatteningPoints[n - 3] - flatteningPoints[n - 4];
                var side2_3 = flatteningPoints[n - 2] - flatteningPoints[n - 3];
                var dir1_2 = side1_2.normalized;
                var dir2_3 = side2_3.normalized;
                p0 = flatteningPoints[n - 4] + (-dir1_2 - dir2_3) * PinManager.settings.flatteningSettings.padding;
                p1 = flatteningPoints[n - 3] + (dir1_2 - dir2_3) * PinManager.settings.flatteningSettings.padding;
                p2 = flatteningPoints[n - 2] + (dir1_2 + dir2_3) * PinManager.settings.flatteningSettings.padding;
                p3 = flatteningPoints[n - 1] + (-dir1_2 + dir2_3) * PinManager.settings.flatteningSettings.padding;

                p0.y = p1.y = p2.y = p3.y = _pinHit.point.y;
                _flatteningCenter = (p2 - p0) / 2 + p0;

                UnityEditor.Handles.color = new Color(0.5f, 0f, 1f, 0.7f);
                UnityEditor.Handles.DrawAAPolyLine(6, new Vector3[] { p0, p1, p2, p3, p0 });
                UnityEditor.Handles.color = new Color(0f, 0.5f, 1f, 0.7f);
                UnityEditor.Handles.DrawAAPolyLine(2, new Vector3[] { p0, p1, p2, p3, p0 });
            }

            foreach (var handlePoint in handlePoints)
                DrawDotHandleCap(handlePoint);

            var pinHitPoint = _pinHit.point;
            var isHitSameAsPivot = pinHitPoint == pivotPos;
            DrawDotHandleCap(pinHitPoint, selected: !isHitSameAsPivot, scale: isHitSameAsPivot ? 1f : 1.3f);
            DrawDotHandleCap(pivotPos, isPivot: true, scale: isHitSameAsPivot ? 1f : 0.615f);
        }
    }
}
#pragma warning restore UDR0001
