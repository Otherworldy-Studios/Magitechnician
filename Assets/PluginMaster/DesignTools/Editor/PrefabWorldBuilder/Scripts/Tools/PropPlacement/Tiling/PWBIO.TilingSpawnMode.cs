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
        public static void InitializeTilingTool()
        {
            TilingManager.instance.DeselectAllItems();
            ResetTilingState(false);
            if (TilingManager.settings.paintOnMeshesWithoutCollider)
            {
                if (TilingManager.settings.ignoreSceneColliders) UpdateSceneColliderSet();
                UpdateOctree();
            }
        }
        public static void ResetTilingState(bool askIfWantToSave = true)
        {
            _initialPersistentTilingData = null;
            _selectedPersistentTilingData = null;
            _editingPersistentTiling = false;
            if (askIfWantToSave && _tilingData != null)
            {
                void Save()
                {
                    if (UnityEditor.SceneView.lastActiveSceneView != null)
                        TilingStrokePreview(UnityEditor.SceneView.lastActiveSceneView.camera, TilingData.nextHexId, true);
                    CreateTiling();
                }
                AskIfWantToSave(_tilingData.state, Save);
            }
            if (_tilingData == null) return;
            _snappedToVertex = false;
            _tilingData.Reset();
            _paintStroke.Clear();
        }
        private static void TilingStateNone(bool in2DMode)
        {
            if (Event.current.button == 0 && Event.current.type == EventType.MouseDown && !Event.current.alt)
            {
                _tilingData.state = ToolController.ToolState.PREVIEW;
                TilingManager.settings.UpdateCellSize();
            }
            if (TryGetMouseWorldHit(out Vector3 point, out Vector3 normal, TilingManager.settings.mode, in2DMode,
                TilingManager.settings.paintOnPalettePrefabs, TilingManager.settings.paintOnMeshesWithoutCollider, false,
                ignoreSceneColliders: TilingManager.settings.ignoreSceneColliders))
            {
                point = SnapToBounds(point);
                point = SnapAndUpdateGridOrigin(point, GridManager.settings.snappingEnabled,
                   TilingManager.settings.paintOnPalettePrefabs, TilingManager.settings.paintOnMeshesWithoutCollider,
                   TilingManager.settings.ignoreSceneColliders, paintOnTheGrid: false,
                   TilingManager.settings.rotation * Vector3.down);
                _tilingData.SetPoint(2, point, registerUndo: false, selectAll: false);
                _tilingData.SetPoint(0, point, registerUndo: false, selectAll: false);
            }
            if (_tilingData.pointsCount > 0) DrawDotHandleCap(_tilingData.GetPoint(0));
        }

        private static void TilingStateRectangle(UnityEditor.SceneView sceneView)
        {
            var settings = TilingManager.settings;
            if (Event.current.button == 0 && Event.current.type == EventType.MouseDown && !Event.current.alt)
            {
                UpdateMidpoints(_tilingData);
                _tilingData.state = ToolController.ToolState.EDIT;
                updateStroke = true;
            }

            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            var plane = new Plane(settings.rotation * Vector3.up, _tilingData.GetPoint(0));

            if (plane.Raycast(mouseRay, out float distance))
            {
                var point = mouseRay.GetPoint(distance);
                point = SnapToBounds(point);
                point = SnapAndUpdateGridOrigin(point, GridManager.settings.snappingEnabled,
                   TilingManager.settings.paintOnPalettePrefabs, TilingManager.settings.paintOnMeshesWithoutCollider,
                   TilingManager.settings.ignoreSceneColliders, paintOnTheGrid: false,
                   TilingManager.settings.rotation * Vector3.down);
                _tilingData.SetPoint(2, point, registerUndo: false, selectAll: false);
                var diagonal = point - _tilingData.GetPoint(0);
                var tangent = Vector3.Project(diagonal, settings.rotation * Vector3.right);
                var bitangent = Vector3.Project(diagonal, settings.rotation * Vector3.forward);
                _tilingData.SetPoint(1, _tilingData.GetPoint(0) + tangent, registerUndo: false, selectAll: false);
                _tilingData.SetPoint(3, _tilingData.GetPoint(0) + bitangent, registerUndo: false, selectAll: false);
                DrawTilingGrid(_tilingData);
                TilingInfoText(sceneView);
                for (int i = 0; i < 4; ++i) DrawDotHandleCap(_tilingData.GetPoint(i));
                return;
            }
            DrawDotHandleCap(_tilingData.GetPoint(0));

        }
        private static void TilingInfoText(UnityEditor.SceneView sceneView)
        {
            if (!PWBCore.staticData.showInfoText) return;
            if (_tilingSize == Vector2Int.zero) return;
            var labelTexts = new string[]
            { $"{_tilingSize.x} x {_tilingSize.y}" };
            InfoText.Draw(sceneView, labelTexts);
        }
        private static void TilingStateEdit(Camera camera)
        {
            bool mouseDown = Event.current.button == 0 && Event.current.type == EventType.MouseDown;
            TilingShortcuts(_tilingData);
            if (_rotateTiling90)
            {
                var rotation = _tilingData.settings.rotation * Quaternion.AngleAxis(90, _rotateTilingAxis);
                SetTilingRotation(_tilingData, rotation);
                _rotateTiling90 = false;
            }
            var forceStrokeUpdate = updateStroke;
            if (updateStroke)
            {
                BrushstrokeManager.UpdateTilingBrushstroke(_tilingData.tilingCenters.ToArray());
                updateStroke = false;
            }
            if (TilingManager.settings.showPreview) TilingStrokePreview(camera, TilingData.nextHexId, forceStrokeUpdate);

            DrawTilingGrid(_tilingData);
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                if (!TilingManager.settings.showPreview) TilingStrokePreview(camera, TilingData.nextHexId, forceStrokeUpdate);
                CreateTiling();
                ResetTilingState(false);
            }
            DrawTilingControlPoints(_tilingData, out bool clickOnPoint, out bool wasEdited, out Vector3 delta);
        }
        private static void CreateTiling()
        {
            var nextTilingId = TilingData.nextHexId;
            var objDic = Paint(TilingManager.settings, PAINT_CMD, true, false, nextTilingId);
            if (objDic.Count != 1) return;
            var scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            var sceneGUID = UnityEditor.AssetDatabase.AssetPathToGUID(scenePath);
            if (isInPrefabMode)
                sceneGUID = UnityEditor.AssetDatabase.AssetPathToGUID(prefabStage.assetPath);
            var initialBrushId = PaletteManager.selectedBrush != null ? PaletteManager.selectedBrush.id : -1;
            var objs = objDic[nextTilingId].ToArray();
            var persistentData = new TilingData(objs, initialBrushId, _tilingData);
            TilingManager.instance.AddPersistentItem(sceneGUID, persistentData);
            PWBItemsWindow.RepainWindow();
        }
        private static void TilingStrokePreview(Camera camera, string hexId, bool forceUpdate)
        {
            BrushstrokeItem[] brushstroke;
            if (PreviewIfBrushtrokestaysTheSame(out brushstroke, camera, forceUpdate)) return;
            _paintStroke.Clear();
            if (tilingData == null) return;
            var toolSettings = TilingManager.settings;
            float maxSurfaceHeight = 0f;
            for (int i = 0; i < brushstroke.Length; ++i)
            {
                var strokeItem = brushstroke[i];

                var prefab = strokeItem.settings.prefab;
                if (prefab == null) continue;
                Bounds bounds = BoundsUtils.GetBoundsRecursive(prefab.transform, prefab.transform.rotation);

                BrushSettings brushSettings = strokeItem.settings;
                if (toolSettings.overwriteBrushProperties) brushSettings = toolSettings.brushSettings;
                if (brushSettings == null) brushSettings = new BrushSettings();

                var additionalRotation = Quaternion.Euler(strokeItem.additionalAngle);
                var scaleMult = brushSettings.GetScaleMultiplier();

                var size = additionalRotation * Vector3.Scale(bounds.size, scaleMult);
                size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
                var pivotToCenter = prefab.transform.InverseTransformDirection(bounds.center - prefab.transform.position);
                pivotToCenter = Vector3.Scale(pivotToCenter, scaleMult);
                pivotToCenter = additionalRotation * pivotToCenter;
                var itemPosition = strokeItem.tangentPosition;

                var height = size.x + size.y + size.z + maxSurfaceHeight
                    + Vector3.Distance(itemPosition, tilingData.GetCenter())
                    + Vector3.Distance(tilingData.GetPoint(0), tilingData.GetPoint(2));

                var normal = toolSettings.rotation * Vector3.up;
                var axisDirection = Vector3.up;
                if (toolSettings.axisAlignedWithNormal == AxesUtils.Axis.Z)
                {
                    size.x = bounds.size.y;
                    size.y = bounds.size.z;
                    size.z = bounds.size.x;
                    axisDirection = Vector3.forward;
                }
                else if (toolSettings.axisAlignedWithNormal == AxesUtils.Axis.X)
                {
                    size.x = bounds.size.z;
                    size.y = bounds.size.x;
                    size.z = bounds.size.y;
                    axisDirection = Vector3.right;
                }

                var ray = new Ray(itemPosition + normal * height, -normal);
                Transform surface = null;
                if (toolSettings.mode != TilingSettings.PaintMode.ON_SHAPE)
                {
                    if (PWBToolRaycast(ray, out RaycastHit itemHit, out GameObject collider, height * 2f, -1,
                        toolSettings.paintOnPalettePrefabs, toolSettings.paintOnMeshesWithoutCollider,
                        createTempColliders: toolSettings.paintOnMeshesWithoutCollider,
                        ignoreSceneColliders: toolSettings.ignoreSceneColliders))

                    {
                        itemPosition = itemHit.point;
                        if (brushSettings.rotateToTheSurface) normal = itemHit.normal;
                        var colObj = PWBCore.GetGameObjectFromTempCollider(collider);
                        if (colObj != null) surface = colObj.transform;
                        var surfObj = PWBCore.GetGameObjectFromTempCollider(collider);
                        var surfSize = BoundsUtils.GetBounds(surfObj.transform).size;
                        var h = surfSize.x + surfSize.y + surfSize.z;
                        maxSurfaceHeight = Mathf.Max(h, maxSurfaceHeight);
                    }
                    else if (toolSettings.mode == TilingSettings.PaintMode.ON_SURFACE) continue;
                }
                var itemRotation = toolSettings.rotation;
                Vector3 itemTangent = itemRotation * Vector3.forward;

                if (brushSettings.rotateToTheSurface
                    && toolSettings.mode != PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE)
                {
                    itemRotation = Quaternion.LookRotation(itemTangent, normal);
                    itemPosition += normal * strokeItem.surfaceDistance;
                }
                else itemPosition += normal * strokeItem.surfaceDistance;
                var axisAlignedWithNormal = (Vector3)toolSettings.axisAlignedWithNormal;

                itemRotation *= Quaternion.FromToRotation(Vector3.up, axisAlignedWithNormal);

                itemRotation *= additionalRotation;

                if (brushSettings.rotateToTheSurface && brushSettings.alwaysOrientUp)
                {
                    var fw = itemRotation * Vector3.forward;
                    const float minMag = 1e-6f;
                    fw.y = 0;
                    if (Mathf.Abs(fw.x) < minMag && Mathf.Abs(fw.z) < minMag) fw = Quaternion.Euler(0, 90, 0) * normal;
                    itemRotation = Quaternion.LookRotation(fw, Vector3.up);
                }

                itemPosition += itemRotation * (brushSettings.localPositionOffset);

                itemPosition -= itemRotation * pivotToCenter;
                if (brushSettings.embedInSurface)
                {
                    if (brushSettings.embedAtPivotHeight)
                        itemPosition += normal * AxesUtils.GetAxisValue(pivotToCenter, toolSettings.axisAlignedWithNormal);
                    else
                        itemPosition += normal * (AxesUtils.GetAxisValue(size, toolSettings.axisAlignedWithNormal) / 2);
                }
                if (brushSettings.embedInSurface
                && toolSettings.mode != PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE)
                {
                    if (!brushSettings.embedAtPivotHeight)
                    {
                        var TRS = Matrix4x4.TRS(itemPosition, itemRotation,
                            Vector3.Scale(prefab.transform.localScale, scaleMult));
                        var bottomDistanceToSurfce = GetBottomDistanceToSurface(strokeItem.settings.bottomVertices,
                            TRS, Mathf.Abs(strokeItem.settings.bottomMagnitude), toolSettings.paintOnPalettePrefabs,
                            toolSettings.paintOnMeshesWithoutCollider, toolSettings.ignoreSceneColliders,
                            out Transform surfaceTransform);
                        itemPosition += itemRotation * (axisDirection * -bottomDistanceToSurfce);
                    }
                }

                var itemScale = Vector3.Scale(prefab.transform.localScale, scaleMult);
                var layer = toolSettings.overwritePrefabLayer ? toolSettings.layer : prefab.layer;
                Transform parentTransform = toolSettings.parent;

                var paintItem = new PaintStrokeItem(prefab, strokeItem.settings.guid, itemPosition, itemRotation,
                    itemScale, layer, parentTransform, surface, strokeItem.flipX, strokeItem.flipY);
                paintItem.persistentParentId = hexId;

                _paintStroke.Add(paintItem);
                var previewRootToWorld = Matrix4x4.TRS(itemPosition, itemRotation, scaleMult)
                    * Matrix4x4.Rotate(Quaternion.Inverse(prefab.transform.rotation))
                    * Matrix4x4.Translate(-prefab.transform.position);
                PreviewBrushItem(prefab, previewRootToWorld, layer, camera, false, false, strokeItem.flipX, strokeItem.flipY);
                _previewData.Add(new PreviewData(prefab, previewRootToWorld, layer, strokeItem.flipX, strokeItem.flipY));
            }
        }
    }
}
#pragma warning restore UDR0001
