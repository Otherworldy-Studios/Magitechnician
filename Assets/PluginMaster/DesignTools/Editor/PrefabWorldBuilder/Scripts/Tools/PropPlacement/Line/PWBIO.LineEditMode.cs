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

        private static System.Collections.Generic.HashSet<GameObject> _disabledObjects
            = new System.Collections.Generic.HashSet<GameObject>();
        private static bool _editingPersistentLine = false;
        private static LineData _initialPersistentLineData = null;
        private static LineData _selectedPersistentLineData = null;

        private static System.Collections.Generic.Dictionary<long, IPersistentData.Visibility> _prevDataVisibility
            = new System.Collections.Generic.Dictionary<long, IPersistentData.Visibility>();

        private static void UpdateDataPrevVisibility(IPersistentData data)
        {
            if (data.visibility == IPersistentData.Visibility.HIDE_ALL)
                UnityEditor.SceneVisibilityManager.instance.Hide(data.objects, true);
            else UnityEditor.SceneVisibilityManager.instance.Show(data.objects, true);
            if (_prevDataVisibility.ContainsKey(data.id)) _prevDataVisibility[data.id] = data.visibility;
            else _prevDataVisibility.Add(data.id, data.visibility);
        }

        public static void SelectLine(LineData data)
        {
            ApplySelectedPersistentLine(true);
            _editingPersistentLine = true;
            data.ClearSelection();
            data.selectedPointIdx = 0;
            data.showHandles = true;
            _selectedPersistentLineData = data;
            if (_initialPersistentLineData == null) _initialPersistentLineData = data.Clone();
            LineManager.instance.CopyToolSettings(data.settings);
        }
        private static void LineToolEditMode(UnityEditor.SceneView sceneView)
        {
            var persistentLines = LineManager.instance.GetPersistentItems();
            var selectedLineId = _initialPersistentLineData == null ? -1 : _initialPersistentLineData.id;
            bool clickOnAnyPoint = false;
            bool someLinesWereEdited = false;
            var delta = Vector3.zero;
            var editedData = _selectedPersistentLineData;
            DrawSelectionRectangle();
            foreach (var lineData in persistentLines)
            {
                if (lineData.pointsCount <= 2)
                {
                    void DeleteItem()
                    {
                        LineManager.instance.DeletePersistentItem(lineData.id, deleteObjects: true, registerUndo: false);
                        PWBItemsWindow.RepainWindow();
                    }
                    if (lineData.pointsCount <= 1)
                    {
                        DeleteItem();
                        continue;
                    }
                    var points = lineData.points;
                    if (points[0] == points[1] && points[0] == Vector3.zero)
                    {
                        DeleteItem();
                        continue;
                    }
                }
                if (!_prevDataVisibility.ContainsKey(lineData.id) || lineData.visibility != _prevDataVisibility[lineData.id])
                {
                    if (lineData.visibility == IPersistentData.Visibility.HIDE_ALL)
                        UnityEditor.SceneVisibilityManager.instance.Hide(lineData.objects, true);
                    else UnityEditor.SceneVisibilityManager.instance.Show(lineData.objects, true);
                    UpdateDataPrevVisibility(lineData);
                }
                if (lineData.visibility != IPersistentData.Visibility.SHOW_ALL) continue;
                DrawLine(lineData, drawSurfacePath: lineData.selectionCount > 0);

                if (DrawLineControlPoints(lineData, isPersistent: true, ToolController.editMode,
                    out bool clickOnPoint, out bool multiSelection, out bool addToselection,
                    out bool removedFromSelection, out bool wasEdited, out Vector3 localDelta))
                {
                    if (clickOnPoint)
                    {
                        clickOnAnyPoint = true;
                        _editingPersistentLine = true;
                        if (selectedLineId != lineData.id)
                        {
                            ApplySelectedPersistentLine(false);
                            if (selectedLineId == -1) _createProfileName = LineManager.instance.selectedProfileName;
                            else if (!addToselection && !removedFromSelection)
                            {
                                var selectedLines
                                    = persistentLines.Where(i => i != lineData && i.selectionCount > 0).ToArray();
                                foreach (var selected in selectedLines)
                                {
                                    PWBCore.SetActiveTempColliders(selected.objects, true);
                                    selected.showHandles = false;
                                    selected.ClearSelection();
                                }
                            }
                            LineManager.instance.CopyToolSettings(lineData.settings);
                            ToolProperties.RepainWindow();
                            PWBCore.SetActiveTempColliders(lineData.objects, false);
                        }
                        _selectedPersistentLineData = lineData;
                        if (_initialPersistentLineData == null) _initialPersistentLineData = lineData.Clone();
                        else if (_initialPersistentLineData.id != lineData.id) _initialPersistentLineData = lineData.Clone();
                        if (!removedFromSelection) foreach (var l in persistentLines) l.showHandles = (l == lineData);
                    }
                    if (addToselection) lineData.showHandles = true;
                    if (wasEdited)
                    {
                        _editingPersistentLine = true;
                        someLinesWereEdited = true;
                        delta = localDelta;
                        editedData = lineData;
                        _persistentItemWasEdited = true;
                    }
                }
            }

            var repaintItemsWindow = false;
            foreach (var lineData in persistentLines)
            {
                var isSelected = lineData.selectionCount > 0;
                if (lineData.isSelected != isSelected) repaintItemsWindow = true;
                lineData.isSelected = lineData.selectionCount > 0;
            }
            if (repaintItemsWindow) PWBItemsWindow.RepainWindow();

            var linesEdited = persistentLines.Where(i => i.selectionCount > 0).ToArray();

            if (someLinesWereEdited)
            {
                if (linesEdited.Length > 0) _disabledObjects.Clear();
                if (linesEdited.Length > 1)
                {
                    _paintStroke.Clear();
                    foreach (var lineData in linesEdited)
                    {
                        if (lineData != editedData) lineData.AddDeltaToSelection(delta);
                        lineData.UpdatePath(forceUpdate: false, updateOnSurfacePoints: true);
                        PreviewPersistentLine(lineData);
                        LineStrokePreview(sceneView, lineData, persistent: true, forceUpdate: true, _firstNewObjIdx);
                    }
                    PWBCore.SetSavePending();
                    return;
                }
            }
            if (linesEdited.Length > 1) PreviewPersistent(sceneView.camera);

            if (!ToolController.editMode) return;

            if (LineManager.editModeType == LineManager.EditModeType.NODES) SelectionRectangleInput(clickOnAnyPoint);

            bool skipPreview = _selectedPersistentLineData != null
                && _selectedPersistentLineData.objectCount > PWBCore.staticData.maxPreviewCountInEditMode;
            if (!skipPreview)
            {
                if ((!someLinesWereEdited && linesEdited.Length <= 1)
                    && _editingPersistentLine && _selectedPersistentLineData != null)
                {
                    var forceStrokeUpdate = updateStroke;
                    if (updateStroke)
                    {
                        _selectedPersistentLineData.UpdatePath(forceUpdate: false, updateOnSurfacePoints: true);
                        PreviewPersistentLine(_selectedPersistentLineData);
                        updateStroke = false;
                        PWBCore.SetSavePending();
                    }
                    if (_brushstroke != null
                        && !BrushstrokeManager.BrushstrokeEqual(BrushstrokeManager.brushstroke, _brushstroke))
                        _paintStroke.Clear();

                    LineStrokePreview(sceneView, _selectedPersistentLineData,
                        persistent: true, forceStrokeUpdate, _firstNewObjIdx);
                }
            }
            LineInput(true, sceneView, skipPreview);
        }

        private static int _firstNewObjIdx = 0;

        public static void PreviewSelectedPersistentLines()
        {
            if (ToolController.current != ToolController.Tool.LINE) return;
            var persistentLines = LineManager.instance.GetPersistentItems();
            foreach (var lineData in persistentLines)
            {
                if (!lineData.isSelected) continue;
                PreviewPersistentLine(lineData);
            }
        }
        public static void PreviewPersistentLine(LineData lineData)
        {
            BrushstrokeObject[] objPos = null;
            var objList = lineData.objectList;
            Vector3[] strokePos = null;
            var toolSettings = lineData.settings;
            BrushstrokeManager.UpdatePersistentLineBrushstroke(lineData.pathPoints,
                toolSettings, objList, out objPos, out strokePos, out _firstNewObjIdx);
            _disabledObjects.UnionWith(lineData.objects);
            float pathLength = 0;
            var prevSegmentDir = Vector3.zero;

            BrushSettings brushSettings = LineManager.instance.applyBrushToExisting ?
                PaletteManager.selectedBrush : PaletteManager.GetBrushById(lineData.initialBrushId);
            if (brushSettings == null && PaletteManager.selectedBrush != null)
            {
                brushSettings = PaletteManager.selectedBrush;
                lineData.SetInitialBrushId(brushSettings.id);
            }
            if (toolSettings.overwriteBrushProperties) brushSettings = toolSettings.brushSettings;
            if (brushSettings == null) brushSettings = new BrushSettings();
            var objSet = lineData.objectSet;
            float maxSurfaceHeight = 0f;
            for (int i = 0; i < objPos.Length; ++i)
            {
                var objIdx = objPos[i].objIdx;
                var obj = objList[objIdx];
                if (obj == null)
                {
                    lineData.RemovePose(objIdx);
                    continue;
                }
                obj.SetActive(true);
                var objScale = objPos[i].objScale;
                if (i > 0) pathLength += (objPos[i].objPosition - objPos[i - 1].objPosition).magnitude;

                var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(obj);
                if (prefab == null) prefab = obj;
                var bounds = BoundsUtils.GetBoundsRecursive(prefab.transform, prefab.transform.rotation,
                    ignoreDissabled: true, BoundsUtils.ObjectProperty.BOUNDING_BOX, recursive: true, useDictionary: false);

                var size = Vector3.Scale(bounds.size, objScale);

                var height = size.x + size.y + size.z + maxSurfaceHeight + pathLength;
                Vector3 segmentDir = Vector3.zero;
                var objOnLineSize = AxesUtils.GetAxisValue(size, toolSettings.axisOrientedAlongTheLine);

                segmentDir = objPos[i].brushstrokeDirection;

                var perpendicularToTheSurface = toolSettings.perpendicularToTheSurface
                    || (brushSettings.rotateToTheSurface && !brushSettings.alwaysOrientUp);
                if (toolSettings.objectsOrientedAlongTheLine && !perpendicularToTheSurface)
                {
                    var projectionAxis = ((AxesUtils.SignedAxis)(toolSettings.projectionDirection)).axis;
                    segmentDir -= AxesUtils.GetVector(AxesUtils.GetAxisValue(segmentDir, projectionAxis), projectionAxis);
                }
                var normal = -toolSettings.projectionDirection;
                var otherAxes = AxesUtils.GetOtherAxes((AxesUtils.SignedAxis)(-toolSettings.projectionDirection));
                var tangetAxis = otherAxes[toolSettings.objectsOrientedAlongTheLine ? 0 : 1];
                Vector3 itemTangent = (AxesUtils.SignedAxis)(tangetAxis);
                var itemRotation = Quaternion.LookRotation(itemTangent, normal);
                var lookAt = Quaternion.LookRotation((Vector3)(AxesUtils.SignedAxis)
                    (toolSettings.axisOrientedAlongTheLine), Vector3.up);
                if (segmentDir != Vector3.zero) itemRotation = Quaternion.LookRotation(segmentDir, normal) * lookAt;
                var itemPosition = objPos[i].objPosition;
                var ray = new Ray(itemPosition + normal * height, -normal);
                if (toolSettings.mode != PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE)
                {
                    if (PWBToolRaycast(ray, out RaycastHit itemHit, out GameObject collider, maxDistance: float.MaxValue,
                        layerMask: -1, toolSettings.paintOnPalettePrefabs, toolSettings.paintOnMeshesWithoutCollider,
                        tags: null, terrainLayers: null, exceptions: objSet, sameOriginAsRay: false, origin: itemPosition,
                        createTempColliders: toolSettings.paintOnMeshesWithoutCollider,
                        ignoreSceneColliders: toolSettings.ignoreSceneColliders))
                    {
                        itemPosition = itemHit.point;
                        if (perpendicularToTheSurface) normal = itemHit.normal;
                        var surfObj = PWBCore.GetGameObjectFromTempCollider(collider);
                        var surfSize = BoundsUtils.GetBounds(surfObj.transform).size;
                        var h = surfSize.x + surfSize.y + surfSize.z;
                        maxSurfaceHeight = Mathf.Max(h, maxSurfaceHeight);
                    }
                    else if (toolSettings.mode == PaintOnSurfaceToolSettingsBase.PaintMode.ON_SURFACE) continue;
                }

                if (perpendicularToTheSurface && segmentDir != Vector3.zero)
                {
                    if (toolSettings.mode == PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE)
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

                    if (!toolSettings.perpendicularToTheSurface
                            && brushSettings.rotateToTheSurface && brushSettings.alwaysOrientUp)
                    {
                        var fw = itemRotation * Vector3.forward;
                        const float minMag = 1e-6f;
                        fw.y = 0;
                        if (Mathf.Abs(fw.x) < minMag && Mathf.Abs(fw.z) < minMag) fw = Quaternion.Euler(0, 90, 0) * normal;
                        itemRotation = Quaternion.LookRotation(fw, Vector3.up);
                    }

                var pivotToCenter = prefab.transform.InverseTransformDirection(bounds.center - prefab.transform.position);
                pivotToCenter = itemRotation * Vector3.Scale(pivotToCenter, objScale);

                itemPosition += normal * (size.y / 2) - pivotToCenter;
                if (LineManager.instance.applyBrushToExisting)
                {
                    if (brushSettings.embedInSurface
                    && toolSettings.mode != PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE)
                    {
                        var bottomMagnitude = BoundsUtils.GetBottomMagnitude(obj.transform);
                        if (brushSettings.embedAtPivotHeight)
                            itemPosition += itemRotation * (normal * bottomMagnitude);
                        else
                        {
                            var TRS = Matrix4x4.TRS(itemPosition, itemRotation, objScale);
                            var bottomVertices = BoundsUtils.GetBottomVertices(obj.transform);
                            var bottomDistanceToSurfce = GetBottomDistanceToSurface(bottomVertices, TRS,
                                Mathf.Abs(bottomMagnitude), toolSettings.paintOnPalettePrefabs,
                                toolSettings.paintOnMeshesWithoutCollider, toolSettings.ignoreSceneColliders,
                                out Transform surfaceTransform, exceptions: objSet);
                            itemPosition += itemRotation * (normal * -bottomDistanceToSurfce);
                        }
                    }

                    itemPosition += normal * objPos[i].surfaceDistance;
                    itemPosition += itemRotation * brushSettings.localPositionOffset;

                    var additionalAngle = brushSettings.GetAdditionalAngle();
                    if (additionalAngle != Vector3.zero) itemRotation *= Quaternion.Euler(additionalAngle);
                    var flipX = brushSettings.GetFlipX();
                    var flipY = brushSettings.GetFlipY();
                    if (flipX || flipY)
                    {
                        var spriteRenderers = obj.GetComponentsInChildren<SpriteRenderer>();
                        foreach (var spriteRenderer in spriteRenderers)
                        {
                            UnityEditor.Undo.RecordObject(spriteRenderer, LineData.COMMAND_NAME);
                            spriteRenderer.flipX = flipX;
                            spriteRenderer.flipY = flipY;
                        }
                    }
                }
                UnityEditor.Undo.RecordObject(obj.transform, LineData.COMMAND_NAME);
                obj.transform.SetPositionAndRotation(itemPosition, itemRotation);
                obj.transform.localScale = objScale;
                _disabledObjects.Remove(obj);
                lineData.lastTangentPos = objPos[i].objPosition;
            }
            foreach (var obj in _disabledObjects) if (obj != null) obj.SetActive(false);
        }

        private static void ResetSelectedPersistentLine()
        {
            _editingPersistentLine = false;
            if (_initialPersistentLineData == null) return;
            var selectedLine = LineManager.instance.GetItem(_initialPersistentLineData.id);
            if (selectedLine == null) return;
            selectedLine.ResetPoses(_initialPersistentLineData);
            selectedLine.ClearSelection();
        }

        private static void ApplySelectedPersistentLine(bool deselectPoint)
        {
            if (!_persistentItemWasEdited) return;
            _persistentItemWasEdited = false;
            if (!ApplySelectedPersistentObject(deselectPoint, ref _editingPersistentLine, ref _initialPersistentLineData,
                ref _selectedPersistentLineData, LineManager.instance)) return;
            if (_initialPersistentLineData == null) return;
            var selected = LineManager.instance.GetItem(_initialPersistentLineData.id);
            _initialPersistentLineData = selected.Clone();
        }

        private static void DeselectPersistentLines()
        {
            var persistentLines = LineManager.instance.GetPersistentItems();
            foreach (var l in persistentLines) l.ClearSelection();
        }

    }
}
#pragma warning restore UDR0001
