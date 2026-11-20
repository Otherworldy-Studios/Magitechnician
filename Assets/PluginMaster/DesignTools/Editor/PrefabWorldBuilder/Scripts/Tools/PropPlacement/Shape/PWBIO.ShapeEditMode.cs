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

        private static bool _editingPersistentShape = true;
        private static ShapeData _initialPersistentShapeData = null;
        private static ShapeData _selectedPersistentShapeData = null;

        private static void DeselectPersistentShapes()
        {
            var persistentShapes = ShapeManager.instance.GetPersistentItems();
            foreach (var s in persistentShapes) s.ClearSelection();
        }

        private static void ResetSelectedPersistentShape()
        {
            _editingPersistentShape = false;
            if (_initialPersistentShapeData == null) return;
            var selectedShape = ShapeManager.instance.GetItem(_initialPersistentShapeData.id);
            if (selectedShape == null) return;
            selectedShape.ResetPoses(_initialPersistentShapeData);
            selectedShape.ClearSelection();
        }

        public static void SelectShape(ShapeData data)
        {
            ApplySelectedPersistentShape(true);
            _editingPersistentShape = true;
            data.ClearSelection();
            data.selectedPointIdx = 0;
            _selectedPersistentShapeData = data;
            if (_initialPersistentShapeData == null) _initialPersistentShapeData = data.Clone();
            ShapeManager.instance.CopyToolSettings(data.settings);
        }

        private static void ShapeToolEditMode(UnityEditor.SceneView sceneView)
        {
            var persistentItems = ShapeManager.instance.GetPersistentItems();
            var selectedItemId = _initialPersistentShapeData == null ? -1 : _initialPersistentShapeData.id;
            bool skipPreview = _selectedPersistentShapeData != null
                && _selectedPersistentShapeData.objectCount > PWBCore.staticData.maxPreviewCountInEditMode;
            foreach (var shapeData in persistentItems)
            {
                DrawShapeLines(shapeData);
                if (ShapeControlPoints(shapeData, out bool clickOnPoint, out bool wasEditted,
                     ToolController.editMode, out Vector3 delta))
                {
                    if (clickOnPoint)
                    {
                        _editingPersistentShape = true;
                        if (selectedItemId != shapeData.id)
                        {
                            ApplySelectedPersistentShape(false);
                            if (selectedItemId == -1)
                                _createProfileName = ShapeManager.instance.selectedProfileName;
                            ShapeManager.instance.CopyToolSettings(shapeData.settings);
                            ToolProperties.RepainWindow();
                        }
                        _selectedPersistentShapeData = shapeData;
                        if (_initialPersistentShapeData == null) _initialPersistentShapeData = shapeData.Clone();
                        else if (_initialPersistentShapeData.id != shapeData.id)
                            _initialPersistentShapeData = shapeData.Clone();

                        foreach (var i in persistentItems)
                        {
                            if (i == shapeData) continue;
                            i.selectedPointIdx = -1;
                        }
                    }
                    if (wasEditted)
                    {
                        _editingPersistentShape = true;
                        if (!skipPreview) PreviewPersistentShape(shapeData);
                        PWBCore.SetSavePending();
                        _persistentItemWasEdited = true;
                    }
                }
            }

            if (!ToolController.editMode) return;

            if (!skipPreview)
            {
                if (_editingPersistentShape && _selectedPersistentShapeData != null)
                {
                    var forceStrokeUpdate = updateStroke;
                    if (updateStroke)
                    {
                        PreviewPersistentShape(_selectedPersistentShapeData);
                        updateStroke = false;
                        PWBCore.SetSavePending();
                    }
                    ShapeStrokePreview(sceneView, _selectedPersistentShapeData.hexId,
                        forceStrokeUpdate, _selectedPersistentShapeData);
                }
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                if (skipPreview)
                {
                    PreviewPersistentShape(_selectedPersistentShapeData);
                    ShapeStrokePreview(sceneView, _selectedPersistentShapeData.hexId,
                        forceUpdate: true, _selectedPersistentShapeData);
                }
                DeleteDisabledObjects();
                _persistentItemWasEdited = true;
                ApplySelectedPersistentShape(true);
                DeleteDisabledObjects();
                ToolProperties.RepainWindow();
            }
            else if (PWBSettings.shortcuts.editModeSelectParent.Check() && _selectedPersistentShapeData != null)
            {
                var parent = _selectedPersistentShapeData.GetParent();
                if (parent != null) UnityEditor.Selection.activeGameObject = parent;
            }
            else if (PWBSettings.shortcuts.editModeDeleteItemButNotItsChildren.Check())
                ShapeManager.instance.DeletePersistentItem(_selectedPersistentShapeData.id, false);
            else if (PWBSettings.shortcuts.editModeDeleteItemAndItsChildren.Check())
                ShapeManager.instance.DeletePersistentItem(_selectedPersistentShapeData.id, true);
            else if (PWBSettings.shortcuts.editModeDuplicate.Check()) DuplicateItem(_selectedPersistentShapeData.id);

        }

        public static void PreviewSelectedPersistentShapes()
        {
            if (ToolController.current != ToolController.Tool.SHAPE) return;
            if (_selectedPersistentShapeData != null) PreviewPersistentShape(_selectedPersistentShapeData);
            var persistentShapes = ShapeManager.instance.GetPersistentItems();
            foreach (var shapeData in persistentShapes)
            {
                if (!shapeData.isSelected) continue;
                if (_shapeData == _selectedPersistentShapeData) continue;
                PreviewPersistentShape(shapeData);
            }
        }

        private static void PreviewPersistentShape(ShapeData shapeData)
        {
            BrushstrokeObject[] objPoses = null;
            var objList = shapeData.objectList;
            BrushstrokeManager.UpdatePersistentShapeBrushstroke(shapeData, objList, out objPoses);
            _disabledObjects = new System.Collections.Generic.HashSet<GameObject>(objList);
            var settings = shapeData.settings;
            BrushSettings brushSettings = ShapeManager.instance.applyBrushToExisting ?
                 PaletteManager.selectedBrush : PaletteManager.GetBrushById(shapeData.initialBrushId);
            if (brushSettings == null && PaletteManager.selectedBrush != null)
            {
                brushSettings = PaletteManager.selectedBrush;
                shapeData.SetInitialBrushId(brushSettings.id);
            }
            if (settings.overwriteBrushProperties) brushSettings = settings.brushSettings;
            if (brushSettings == null) brushSettings = new BrushSettings();
            var objSet = shapeData.objectSet;
            float maxSurfaceHeight = 0f;
            for (int i = 0; i < objPoses.Length; ++i)
            {
                var objIdx = objPoses[i].objIdx;
                var obj = objList[objIdx];
                if (obj == null)
                {
                    shapeData.RemovePose(objIdx);
                    continue;
                }
                obj.SetActive(true);

                var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(obj);
                var bounds = BoundsUtils.GetBoundsRecursive(prefab.transform, prefab.transform.rotation, ignoreDissabled: true,
                    BoundsUtils.ObjectProperty.BOUNDING_BOX, recursive: true, useDictionary: true);

                var size = Vector3.Scale(bounds.size, objPoses[i].objScale);


                var itemRotation = prefab.transform.rotation * objPoses[objIdx].objRotation;
                var itemPosition = objPoses[objIdx].objPosition;

                var projectionDirection = shapeData.settings.projectionDirection;
                if (settings.projectionDirectionType == ShapeSettings.ShapeProjectionDirection.PLANE_NORMAL)
                    projectionDirection = -shapeData.normal;
                else if (settings.projectionDirectionType == ShapeSettings.ShapeProjectionDirection.FROM_CENTER)
                    projectionDirection = itemPosition - shapeData.center;
                else if (settings.projectionDirectionType == ShapeSettings.ShapeProjectionDirection.TO_CENTER)
                    projectionDirection = shapeData.center - itemPosition;
                projectionDirection.Normalize();

                var height = size.x + size.y + size.z + maxSurfaceHeight
                    + Vector3.Distance(itemPosition, shapeData.center) + shapeData.radius;

                var ray = new Ray(itemPosition - projectionDirection * height, projectionDirection);

                Vector3 surfaceNormal = -projectionDirection;
                if (settings.mode != PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE)
                {
                    if (PWBToolRaycast(ray, out RaycastHit itemHit, out GameObject collider, height * 2, -1,
                        settings.paintOnPalettePrefabs, settings.paintOnMeshesWithoutCollider, tags: null,
                        terrainLayers: null, exceptions: objSet, createTempColliders: settings.paintOnMeshesWithoutCollider,
                        ignoreSceneColliders: settings.ignoreSceneColliders))
                    {
                        itemPosition = itemHit.point;
                        surfaceNormal = itemHit.normal;
                        var surface = collider.transform;
                        var surfSize = BoundsUtils.GetBounds(surface).size;
                        var h = surfSize.x + surfSize.y + surfSize.z;
                        maxSurfaceHeight = Mathf.Max(h, maxSurfaceHeight);
                    }
                    else if (settings.mode == PaintOnSurfaceToolSettingsBase.PaintMode.ON_SURFACE) continue;
                }

                var perpendicularToTheSurface = settings.perpendicularToTheSurface
                    || (brushSettings.rotateToTheSurface && !brushSettings.alwaysOrientUp);

                if (settings.mode != PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE)
                {
                    if (perpendicularToTheSurface)
                    {
                        var itemForward = itemRotation * Vector3.forward;
                        itemForward = Vector3.ProjectOnPlane(itemForward, surfaceNormal);
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

                var pivotToCenter = prefab.transform.InverseTransformDirection(bounds.center - prefab.transform.position);
                pivotToCenter = itemRotation * Vector3.Scale(pivotToCenter, objPoses[i].objScale);

                UnityEditor.Undo.RecordObject(obj.transform, ShapeData.COMMAND_NAME);
                obj.transform.rotation = Quaternion.identity;
                obj.transform.position = Vector3.zero;

                obj.transform.rotation = itemRotation;
                var sceneView = UnityEditor.SceneView.currentDrawingSceneView ?? UnityEditor.SceneView.lastActiveSceneView;
                if (Utils2D.Is2DAsset(obj) && sceneView != null && sceneView.in2DMode)
                    obj.transform.rotation *= Quaternion.AngleAxis(90, Vector3.right);

                itemPosition += (-pivotToCenter - projectionDirection * (size.y / 2));
                if (ShapeManager.instance.applyBrushToExisting)
                {
                    if (brushSettings.embedInSurface
                        && settings.mode != PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE)
                    {
                        var bottomMagnitude = BoundsUtils.GetBottomMagnitude(obj.transform);
                        if (brushSettings.embedAtPivotHeight)
                            itemPosition += itemRotation * (Vector3.up * bottomMagnitude);
                        else
                        {
                            var TRS = Matrix4x4.TRS(itemPosition, itemRotation, objPoses[i].objScale);
                            var bottomVertices = BoundsUtils.GetBottomVertices(obj.transform);
                            var bottomDistanceToSurfce = GetBottomDistanceToSurface(bottomVertices, TRS,
                                Mathf.Abs(bottomMagnitude), settings.paintOnPalettePrefabs,
                                settings.paintOnMeshesWithoutCollider, settings.ignoreSceneColliders,
                                out Transform surfaceTransform, exceptions: shapeData.objectSet);
                            itemPosition += itemRotation * (Vector3.up * -bottomDistanceToSurfce);
                        }
                    }

                    itemPosition += itemRotation * brushSettings.localPositionOffset;
                    itemPosition += itemRotation * (Vector3.up * objPoses[i].surfaceDistance);

                    var additionalAngle = brushSettings.GetAdditionalAngle();
                    if (additionalAngle != Vector3.zero) obj.transform.rotation *= Quaternion.Euler(additionalAngle);
                    var flipX = brushSettings.GetFlipX();
                    var flipY = brushSettings.GetFlipY();
                    if (flipX || flipY)
                    {
                        var spriteRenderers = obj.GetComponentsInChildren<SpriteRenderer>();
                        foreach (var spriteRenderer in spriteRenderers)
                        {
                            UnityEditor.Undo.RecordObject(spriteRenderer, ShapeData.COMMAND_NAME);
                            spriteRenderer.flipX = flipX;
                            spriteRenderer.flipY = flipY;
                        }
                    }
                }
                obj.transform.position = itemPosition;
                obj.transform.localScale = objPoses[i].objScale;
                _disabledObjects.Remove(obj);
            }
            foreach (var obj in _disabledObjects) if (obj != null) obj.SetActive(false);
        }
        private static void ApplySelectedPersistentShape(bool deselectPoint)
        {
            if (!_persistentItemWasEdited) return;
            _persistentItemWasEdited = false;
            if (!ApplySelectedPersistentObject(deselectPoint, ref _editingPersistentShape, ref _initialPersistentShapeData,
               ref _selectedPersistentShapeData, ShapeManager.instance)) return;
            if (_initialPersistentShapeData == null) return;
            var selected = ShapeManager.instance.GetItem(_initialPersistentShapeData.id);
            _initialPersistentShapeData = selected.Clone();
        }
    }
}
#pragma warning restore UDR0001
