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

        private static TilingData _initialPersistentTilingData = null;
        private static TilingData _selectedPersistentTilingData = null;
        private static bool _editingPersistentTiling = false;

        public static TilingData selectedPersistentTilingData
        {
            get
            {
                if (!_editingPersistentTiling) return null;
                return _selectedPersistentTilingData;
            }
        }

        public static void SelectTiling(TilingData data)
        {
            ApplySelectedPersistentTiling(true);
            _editingPersistentTiling = true;
            data.ClearSelection();
            data.selectedPointIdx = 8;
            data.isSelected = true;
            _selectedPersistentTilingData = data;
            if (_initialPersistentTilingData == null) _initialPersistentTilingData = data.Clone();
            TilingManager.instance.CopyToolSettings(data.settings);
        }

        private static void TilingToolEditMode(UnityEditor.SceneView sceneView)
        {
            var persistentItems = TilingManager.instance.GetPersistentItems();
            var deselectedItems = new System.Collections.Generic.List<TilingData>(persistentItems);
            bool clickOnAnyPoint = false;
            bool selectedItemWasEdited = false;
            foreach (var itemData in persistentItems)
            {
                DrawCells(itemData);
                if (!ToolController.editMode) continue;
                DrawTilingRectangle(itemData);

                var selectedTilingId = _initialPersistentTilingData == null ? -1 : _initialPersistentTilingData.id;
                if (DrawTilingControlPoints(itemData, out bool clickOnPoint, out bool wasEdited, out Vector3 delta))
                {
                    if (clickOnPoint)
                    {
                        clickOnAnyPoint = true;
                        _editingPersistentTiling = true;
                        if (selectedTilingId != itemData.id)
                        {
                            ApplySelectedPersistentTiling(false);
                            if (selectedTilingId == -1)
                                _createProfileName = TilingManager.instance.selectedProfileName;
                            TilingManager.instance.CopyToolSettings(itemData.settings);
                            itemData.isSelected = true;
                            _selectedPersistentTilingData = itemData;

                            _editingPersistentTiling = true;
                            UpdateCellSize();
                        }
                        if (_initialPersistentTilingData == null) _initialPersistentTilingData = itemData.Clone();
                        else if (_initialPersistentTilingData.id != itemData.id)
                            _initialPersistentTilingData = itemData.Clone();
                        deselectedItems.Remove(itemData);
                    }
                    if (wasEdited)
                    {
                        _editingPersistentTiling = true;
                        selectedItemWasEdited = true;
                        _persistentItemWasEdited = true;
                    }
                }
            }
            if (clickOnAnyPoint)
                foreach (var itemData in deselectedItems) itemData.ClearSelection();
            if (!ToolController.editMode) return;
            bool skipPreview = _selectedPersistentTilingData != null
                && _selectedPersistentTilingData.objectCount > PWBCore.staticData.maxPreviewCountInEditMode;
            if (!skipPreview)
            {
                if (selectedItemWasEdited) PreviewPersistentTiling(_selectedPersistentTilingData);
                else if (_editingPersistentTiling && _selectedPersistentTilingData != null)
                {
                    var forceStrokeUpdate = updateStroke;
                    if (updateStroke)
                    {
                        PreviewPersistentTiling(_selectedPersistentTilingData);
                        updateStroke = false;
                        PWBCore.SetSavePending();
                    }
                    if (_brushstroke != null
                        && !BrushstrokeManager.BrushstrokeEqual(BrushstrokeManager.brushstroke, _brushstroke))
                        _paintStroke.Clear();
                    TilingStrokePreview(sceneView.camera, _selectedPersistentTilingData.hexId, forceStrokeUpdate);
                }
            }
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                if (skipPreview)
                {
                    PreviewPersistentTiling(_selectedPersistentTilingData);
                    TilingStrokePreview(sceneView.camera, _selectedPersistentTilingData.hexId, forceUpdate: true);
                }
                _persistentItemWasEdited = true;
                ApplySelectedPersistentTiling(deselectPoint: true);

                ToolProperties.RepainWindow();
            }
            else if (PWBSettings.shortcuts.editModeSelectParent.Check()
                && _selectedPersistentTilingData != null)
            {
                var parent = _selectedPersistentTilingData.GetParent();
                if (parent != null) UnityEditor.Selection.activeGameObject = parent;
            }
            else if (PWBSettings.shortcuts.editModeDeleteItemButNotItsChildren.Check())
                TilingManager.instance.DeletePersistentItem(_selectedPersistentTilingData.id, false);
            else if (PWBSettings.shortcuts.editModeDeleteItemAndItsChildren.Check())
                TilingManager.instance.DeletePersistentItem(_selectedPersistentTilingData.id, true);
            else if (PWBSettings.shortcuts.editModeDuplicate.Check()) DuplicateItem(_selectedPersistentTilingData.id);
            if (TilingShortcuts(_selectedPersistentTilingData))
            {
                DrawCells(_selectedPersistentTilingData);
                PreviewPersistentTiling(_selectedPersistentTilingData);
                repaint = true;
            }
            if (_rotateTiling90)
            {
                var rotation = _selectedPersistentTilingData.settings.rotation * Quaternion.AngleAxis(90, _rotateTilingAxis);
                SetTilingRotation(_selectedPersistentTilingData, rotation);
                PreviewPersistentTiling(_selectedPersistentTilingData);
                repaint = true;
                _rotateTiling90 = false;
            }
        }

        public static void PreviewSelectedPersistentTilings()
        {
            if (ToolController.current != ToolController.Tool.TILING) return;
            var persistentTilings = TilingManager.instance.GetPersistentItems();
            foreach (var tilingData in persistentTilings)
            {
                if (!tilingData.isSelected) continue;
                PreviewPersistentTiling(tilingData);
            }
        }

        private static void PreviewPersistentTiling(TilingData data)
        {
            Vector3[] objPos = null;
            var objList = data.objectList;
            var toolSettings = data.settings;
            BrushstrokeManager.UpdatePersistentTilingBrushstroke(data.tilingCenters.ToArray(),
                toolSettings, objList, out objPos, out Vector3[] strokePos);
            _disabledObjects.Clear();
            _disabledObjects.UnionWith(objList);
            var objSet = data.objectSet;
            float maxSurfaceHeight = 0f;
            for (int objIdx = 0; objIdx < objPos.Length; ++objIdx)
            {
                var obj = objList[objIdx];
                if (obj == null)
                {
                    data.RemovePose(objIdx);
                    continue;
                }
                obj.SetActive(true);
                var itemPosition = objPos[objIdx];

                BrushSettings brushSettings = TilingManager.instance.applyBrushToExisting
                    ? (toolSettings.overwriteBrushProperties ? toolSettings.brushSettings : PaletteManager.selectedBrush)
                    : PaletteManager.GetBrushById(data.initialBrushId);
                if (brushSettings == null) brushSettings = new BrushSettings();

                var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(obj);

                var bounds = BoundsUtils.GetBoundsRecursive(prefab.transform, prefab.transform.rotation, ignoreDissabled: true,
                    BoundsUtils.ObjectProperty.BOUNDING_BOX, recursive: true, useDictionary: true);

                var scaleMult = brushSettings.GetScaleMultiplier();
                var size = Vector3.Scale(bounds.size, scaleMult);

                var height = size.x + size.y + size.z + maxSurfaceHeight
                    + Vector3.Distance(itemPosition, data.GetCenter()) + Vector3.Distance(data.GetPoint(0), data.GetPoint(2));
                var normal = toolSettings.rotation * Vector3.up;

                var ray = new Ray(itemPosition + normal * height, -normal);
                if (toolSettings.mode != PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE)
                {
                    if (PWBToolRaycast(ray, out RaycastHit itemHit, out GameObject collider, height * 2f, -1,
                        toolSettings.paintOnPalettePrefabs, toolSettings.paintOnMeshesWithoutCollider,
                        tags: null, terrainLayers: null, exceptions: objSet,
                        createTempColliders: toolSettings.paintOnMeshesWithoutCollider,
                        ignoreSceneColliders: toolSettings.ignoreSceneColliders))
                    {
                        itemPosition = itemHit.point;
                        if (brushSettings.rotateToTheSurface) normal = itemHit.normal;
                        var surfObj = PWBCore.GetGameObjectFromTempCollider(collider);
                        var surfSize = BoundsUtils.GetBounds(surfObj.transform).size;
                        var h = surfSize.x + surfSize.y + surfSize.z;
                        maxSurfaceHeight = Mathf.Max(h, maxSurfaceHeight);
                    }
                    else if (toolSettings.mode == PaintOnSurfaceToolSettingsBase.PaintMode.ON_SURFACE) continue;
                }
                var itemRotation = toolSettings.rotation;
                Vector3 itemTangent = itemRotation * Vector3.forward;

                if (brushSettings.rotateToTheSurface
                    && toolSettings.mode != PaintOnSurfaceToolSettings.PaintMode.ON_SHAPE)
                {
                    itemRotation = Quaternion.LookRotation(itemTangent, normal);
                    itemPosition += normal * brushSettings.surfaceDistance;
                }
                else itemPosition += normal * brushSettings.surfaceDistance;
                var axisAlignedWithNormal = (Vector3)toolSettings.axisAlignedWithNormal;

                itemRotation *= Quaternion.FromToRotation(Vector3.up, axisAlignedWithNormal);

                if (brushSettings.rotateToTheSurface && brushSettings.alwaysOrientUp)
                {
                    var fw = itemRotation * Vector3.forward;
                    const float minMag = 1e-6f;
                    fw.y = 0;
                    if (Mathf.Abs(fw.x) < minMag && Mathf.Abs(fw.z) < minMag) fw = Quaternion.Euler(0, 90, 0) * normal;
                    itemRotation = Quaternion.LookRotation(fw, Vector3.up);
                }

                itemPosition += itemRotation * brushSettings.localPositionOffset;

                UnityEditor.Undo.RecordObject(obj.transform, TilingData.COMMAND_NAME);
                obj.transform.rotation = Quaternion.identity;
                obj.transform.position = Vector3.zero;
                obj.transform.rotation = itemRotation;


                var pivotToCenter = prefab.transform.InverseTransformDirection(bounds.center - prefab.transform.position);
                pivotToCenter = Vector3.Scale(pivotToCenter, scaleMult);
                pivotToCenter = itemRotation * pivotToCenter;


                itemPosition -= pivotToCenter;
                if (brushSettings.embedInSurface)
                {
                    if (brushSettings.embedAtPivotHeight)
                        itemPosition += normal * AxesUtils.GetAxisValue(pivotToCenter, toolSettings.axisAlignedWithNormal);
                    else
                        itemPosition += normal * (AxesUtils.GetAxisValue(size, toolSettings.axisAlignedWithNormal) / 2);
                }

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

                if (brushSettings.embedInSurface
                    && toolSettings.mode != PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE)
                {
                    var bottomMagnitude = BoundsUtils.GetBottomMagnitude(obj.transform);
                    if (brushSettings.embedAtPivotHeight)
                        itemPosition += itemRotation * (axisDirection * bottomMagnitude);
                    else
                    {
                        var TRS = Matrix4x4.TRS(itemPosition, itemRotation, obj.transform.lossyScale);
                        var bottomVertices = BoundsUtils.GetBottomVertices(obj.transform);
                        var bottomDistanceToSurfce = GetBottomDistanceToSurface(bottomVertices, TRS,
                            Mathf.Abs(bottomMagnitude), toolSettings.paintOnPalettePrefabs,
                            toolSettings.paintOnMeshesWithoutCollider, toolSettings.ignoreSceneColliders,
                            out Transform surfaceTransform);
                        itemPosition += itemRotation * (axisDirection * -bottomDistanceToSurfce);
                    }
                }
                obj.transform.position = itemPosition;

                if (TilingManager.instance.applyBrushToExisting)
                {
                    brushSettings = TilingManager.instance.applyBrushToExisting
                    ? (toolSettings.overwriteBrushProperties ? toolSettings.brushSettings : PaletteManager.selectedBrush)
                    : PaletteManager.GetBrushById(data.initialBrushId);
                    if (brushSettings == null) brushSettings = new BrushSettings();

                    obj.transform.localScale = Vector3.Scale(prefab.transform.localScale, scaleMult);
                    obj.transform.localRotation *= Quaternion.Euler(brushSettings.GetAdditionalAngle());
                    obj.transform.position += itemRotation * (axisDirection * brushSettings.GetSurfaceDistance());
                    var flipX = brushSettings.GetFlipX();
                    var flipY = brushSettings.GetFlipY();
                    if (flipX || flipY)
                    {
                        var spriteRenderers = obj.GetComponentsInChildren<SpriteRenderer>();
                        foreach (var spriteRenderer in spriteRenderers)
                        {
                            UnityEditor.Undo.RecordObject(spriteRenderer, TilingData.COMMAND_NAME);
                            spriteRenderer.flipX = flipX;
                            spriteRenderer.flipY = flipY;
                        }
                    }
                }
                _disabledObjects.Remove(obj);
            }
            foreach (var obj in _disabledObjects) if (obj != null) obj.SetActive(false);
        }
        private static void ApplySelectedPersistentTiling(bool deselectPoint)
        {
            if (!_persistentItemWasEdited) return;
            _persistentItemWasEdited = false;
            if (!ApplySelectedPersistentObject(deselectPoint, ref _editingPersistentTiling, ref _initialPersistentTilingData,
                ref _selectedPersistentTilingData, TilingManager.instance)) return;
            if (_initialPersistentTilingData == null) return;
            var selectedTiling = TilingManager.instance.GetItem(_initialPersistentTilingData.id);
            _initialPersistentTilingData = selectedTiling.Clone();

        }
    }
}
#pragma warning restore UDR0001
