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

        #region STATE
        static bool _persistentItemWasEdited = false;
        #endregion

        #region UNDO & TOOL STATE

        public static void OnUndoPerformed()
        {
            if (ToolController.current == ToolController.Tool.NONE) return;
            _boundsOctree = null;
            if (tool == ToolController.Tool.LINE && UnityEditor.Undo.GetCurrentGroupName() == LineData.COMMAND_NAME)
            {
                OnUndoLine();
                UpdateStroke();
            }
            else if (tool == ToolController.Tool.SHAPE && UnityEditor.Undo.GetCurrentGroupName() == ShapeData.COMMAND_NAME)
            {
                OnUndoShape();
                UpdateStroke();
            }
            else if (tool == ToolController.Tool.TILING && UnityEditor.Undo.GetCurrentGroupName() == TilingData.COMMAND_NAME)
            {
                OnUndoTiling();
                UpdateStroke();
            }
            if (ToolController.current == ToolController.Tool.LINE
                || ToolController.current == ToolController.Tool.SHAPE
                || ToolController.current == ToolController.Tool.TILING)
                PWBCore.staticData.SaveAndUpdateVersion();
            else
            {
                if (ToolController.current == ToolController.Tool.REPLACER) BrushstrokeManager.ClearReplacerDictionary();
                BrushstrokeManager.UpdateBrushstroke();
            }
            UnityEditor.SceneView.RepaintAll();
        }

        public static void OnToolChange(ToolController.Tool prevTool)
        {
            switch (prevTool)
            {
                case ToolController.Tool.LINE:
                    ResetLineState();
                    break;
                case ToolController.Tool.SHAPE:
                    ResetShapeState();
                    break;
                case ToolController.Tool.TILING:
                    ResetTilingState();
                    break;
                case ToolController.Tool.EXTRUDE:
                    ResetExtrudeState();
                    break;
                case ToolController.Tool.MIRROR:
                    ResetMirrorState();
                    break;
                default: break;
            }
            _meshesAndRenderers.Clear();
            UnityEditor.SceneView.RepaintAll();
        }

        private static void OnEditModeChanged()
        {
            switch (tool)
            {
                case ToolController.Tool.LINE:
                    OnLineToolModeChanged();
                    break;
                case ToolController.Tool.SHAPE:
                    OnShapeToolModeChanged();
                    break;
                case ToolController.Tool.TILING:
                    OnTilingToolModeChanged();
                    break;
                default: break;
            }
        }

        #endregion

        #region STROKE & UPDATE
        private static bool _updateStroke = false;
        public static bool updateStroke { get => _updateStroke; set => _updateStroke = value; }


        public static void UpdateStroke() => updateStroke = true;

        public static void UpdateSelectedPersistentObject()
        {
            BrushstrokeManager.UpdateBrushstroke(false);
            switch (tool)
            {
                case ToolController.Tool.LINE:
                    if (_selectedPersistentLineData != null) _editingPersistentLine = true;
                    break;
                case ToolController.Tool.SHAPE:
                    if (_selectedPersistentShapeData != null) _editingPersistentShape = true;
                    break;
                case ToolController.Tool.TILING:
                    if (_selectedPersistentTilingData != null) _editingPersistentTiling = true;
                    break;
            }
            repaint = true;
        }
        #endregion

        #region SELECTION & EDITING
        public static int selectedPointIdx
        {
            get
            {
                switch (ToolController.current)
                {
                    case ToolController.Tool.TILING:
                        if (ToolController.editMode)
                        {
                            if (_selectedPersistentTilingData == null) return -1;
                            return _selectedPersistentTilingData.selectedPointIdx;
                        }
                        else if (_tilingData.state == ToolController.ToolState.EDIT) return _tilingData.selectedPointIdx;
                        break;
                    case ToolController.Tool.LINE:
                        if (ToolController.editMode)
                        {
                            if (_selectedPersistentLineData == null) return -1;
                            return _selectedPersistentLineData.selectedPointIdx;
                        }
                        else if (_lineData.state == ToolController.ToolState.EDIT) return _lineData.selectedPointIdx;
                        break;
                    case ToolController.Tool.SHAPE:
                        if (ToolController.editMode)
                        {
                            if (_selectedPersistentShapeData == null) return -1;
                            return _selectedPersistentShapeData.selectedPointIdx;
                        }
                        else if (_shapeData.state == ToolController.ToolState.EDIT) return _shapeData.selectedPointIdx;
                        break;
                }
                return -1;
            }
        }

        private static void ResetSelectedPersistentObject<TOOL_NAME, TOOL_SETTINGS,
            CONTROL_POINT, TOOL_DATA, SCENE_DATA, TOOL_MANAGER>
            (PersistentToolControllerBase<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT,
                TOOL_DATA, SCENE_DATA, TOOL_MANAGER> manager,
            ref bool editingPersistentObject, TOOL_DATA initialPersistentData)
            where TOOL_NAME : IToolName, new()
            where TOOL_SETTINGS : IToolSettings, new()
            where CONTROL_POINT : ControlPoint, new()
            where TOOL_DATA : PersistentData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT>, new()
            where SCENE_DATA : SceneData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT, TOOL_DATA>, new()
            where TOOL_MANAGER : PersistentToolControllerBase<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT,
                TOOL_DATA, SCENE_DATA, TOOL_MANAGER>, new()
        {
            editingPersistentObject = false;
            if (initialPersistentData == null) return;
            var selectedItem = manager.GetItem(initialPersistentData.id);
            if (selectedItem == null) return;
            selectedItem.ResetPoses(initialPersistentData);
            selectedItem.ClearSelection();
        }

        private static void DeselectPersistentItems<TOOL_NAME, TOOL_SETTINGS,
            CONTROL_POINT, TOOL_DATA, SCENE_DATA, TOOL_MANAGER>
            (PersistentToolControllerBase<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT,
                TOOL_DATA, SCENE_DATA, TOOL_MANAGER> manager)
            where TOOL_NAME : IToolName, new()
            where TOOL_SETTINGS : IToolSettings, new()
            where CONTROL_POINT : ControlPoint, new()
            where TOOL_DATA : PersistentData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT>, new()
            where SCENE_DATA : SceneData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT, TOOL_DATA>, new()
            where TOOL_MANAGER : PersistentToolControllerBase<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT,
                TOOL_DATA, SCENE_DATA, TOOL_MANAGER>, new()
        {
            var persitentTilings = manager.GetPersistentItems();
            foreach (var i in persitentTilings) i.ClearSelection();
        }

        private static bool ApplySelectedPersistentObject<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT,
            TOOL_DATA, SCENE_DATA, TOOL_MANAGER>
            (bool deselectPoint, ref bool editingPersistentObject, ref TOOL_DATA initialPersistentData,
            ref TOOL_DATA selectedPersistentData,
            PersistentToolControllerBase<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT,
                TOOL_DATA, SCENE_DATA, TOOL_MANAGER> manager)
            where TOOL_NAME : IToolName, new()
            where TOOL_SETTINGS : IToolSettings, new()
            where CONTROL_POINT : ControlPoint, new()
            where TOOL_DATA : PersistentData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT>, new()
            where SCENE_DATA : SceneData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT, TOOL_DATA>, new()
            where TOOL_MANAGER : PersistentToolControllerBase<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT,
                TOOL_DATA, SCENE_DATA, TOOL_MANAGER>, new()
        {
            editingPersistentObject = false;
            if (initialPersistentData == null) return false;
            var selected = manager.GetItem(initialPersistentData.id);
            if (selected == null)
            {
                initialPersistentData = null;
                selectedPersistentData = null;
                return false;
            }
            selected.UpdatePoses();
            if (_paintStroke.Count > 0)
            {
                var objDic = Paint(selected.settings as IPaintToolSettings, PAINT_CMD, true, true);
                foreach (var paintedItem in objDic)
                {
                    var persistentItem = manager.GetItem(paintedItem.Key);
                    if (persistentItem == null) continue;
                    persistentItem.AddObjects(paintedItem.Value.ToArray());
                }
            }
            if (deselectPoint)
            {
                DeselectPersistentItems(manager);
            }
            DeleteDisabledObjects();

            _persistentPreviewData.Clear();
            PWBCore.staticData.SaveAndUpdateVersion();
            if (!deselectPoint) return true;
            var persistentObjects = manager.GetPersistentItems();
            foreach (var item in persistentObjects) item.ClearSelection();
            return true;
        }

        private static void DeleteDisabledObjects()
        {
            if (_disabledObjects == null) return;
            foreach (var obj in _disabledObjects)
            {
                if (obj == null) continue;
                obj.SetActive(true);
                UnityEditor.Undo.DestroyObjectImmediate(obj);
            }
        }
        #endregion

        #region ITEM OPERATIONS
        public static void DuplicateItem(long itemId)
        {
            var toolMan = ToolController.GetCurrentPersistentToolController();
            var clone = toolMan.Duplicate(itemId);
            ToolController.editMode = true;
            clone.isSelected = true;
            var allItems = toolMan.GetItems();
            foreach (var item in allItems)
            {
                if (item == clone) continue;
                item.isSelected = false;
                item.ClearSelection();
            }
            var bounds = clone.GetBounds(1.1f);
            UnityEditor.SceneView.lastActiveSceneView.Frame(bounds, false);

            if (ToolController.current == ToolController.Tool.LINE)
            {
                LineManager.editModeType = LineManager.EditModeType.LINE_POSE;
                PWBIO.SelectLine(clone as LineData);
            }
            else if (ToolController.current == ToolController.Tool.SHAPE) PWBIO.SelectShape(clone as ShapeData);
            else if (ToolController.current == ToolController.Tool.TILING) PWBIO.SelectTiling(clone as TilingData);
        }

        public static void PersistentItemContextMenu(UnityEditor.GenericMenu menu,
            IPersistentData data, Vector2 mousePosition)
        {
            void DeleteItem(bool deleteObjects)
            {
                var toolMan = ToolController.GetCurrentPersistentToolController();
                toolMan.DeletePersistentItem(data.id, deleteObjects);
                UnityEditor.SceneView.RepaintAll();
            }
            menu.AddItem(new GUIContent("Select parent object ... "
               + PWBSettings.shortcuts.editModeSelectParent.combination.ToString()), on: false, () =>
               {
                   var parent = data.GetParent();
                   if (parent != null) UnityEditor.Selection.activeGameObject = parent;
               });
            menu.AddItem(new GUIContent("Duplicate ... "
                + PWBSettings.shortcuts.editModeDuplicate.combination.ToString()), on: false, () => DuplicateItem(data.id));
            menu.AddItem(new GUIContent("Delete item and its children ... "
                + PWBSettings.shortcuts.editModeDeleteItemAndItsChildren.combination.ToString()),
                on: false, () => DeleteItem(deleteObjects: true));
            menu.AddItem(new GUIContent("Delete item but not its children ... "
                + PWBSettings.shortcuts.editModeDeleteItemButNotItsChildren.combination.ToString()), on: false,
                () => DeleteItem(deleteObjects: false));
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent(data.toolName + " properties..."), on: false,
                           () => ItemPropertiesWindow.ShowItemProperties(data, mousePosition));
        }
        #endregion

        #region MOUSE HIT
        private static bool TryGetMouseWorldHit(out Vector3 point, out Vector3 normal,
            PaintOnSurfaceToolSettingsBase.PaintMode mode, bool in2DMode,
            bool paintOnPalettePrefabs, bool castOnMeshesWithoutCollider, bool snapOnGrid, bool ignoreSceneColliders)
        {
            point = Vector3.zero;
            normal = Vector3.up;
            var mousePos = Event.current.mousePosition;
            if (mousePos.x < 0 || mousePos.x >= Screen.width || mousePos.y < 0 || mousePos.y >= Screen.height) return false;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePos);
            Vector3 SnapPoint(Vector3 hitPoint, ref Vector3 snapNormal)
            {
                if (_snapToVertex)
                {
                    if (SnapToVertex(mouseRay, out RaycastHit snappedHit, in2DMode))
                    {
                        _snappedToVertex = true;
                        hitPoint = snappedHit.point;
                        snapNormal = snappedHit.normal;
                    }
                }
                return hitPoint;
            }

            RaycastHit surfaceHit;
            bool surfaceFound = PWBToolRaycast(mouseRay, out surfaceHit, out GameObject collider,
                float.MaxValue, -1, paintOnPalettePrefabs, castOnMeshesWithoutCollider,
                ignoreSceneColliders: ignoreSceneColliders);
            if (mode != PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE && surfaceFound)
            {
                normal = surfaceHit.normal;
                point = SnapPoint(surfaceHit.point, ref normal);
                return true;
            }
            if (mode != PaintOnSurfaceToolSettingsBase.PaintMode.ON_SURFACE)
            {
                if (surfaceFound)
                {
                    point = SnapPoint(surfaceHit.point, ref normal);
                    var direction = GridManager.settings.rotation * Vector3.down;
                    var ray = new Ray(point - direction, direction);
                    if (PWBToolRaycast(ray, out RaycastHit hitInfo, out collider, float.MaxValue, -1,
                        paintOnPalettePrefabs, castOnMeshesWithoutCollider, ignoreSceneColliders: ignoreSceneColliders))
                        point = hitInfo.point;
                    UpdateGridOrigin(point);
                    return true;
                }
                if (GridRaycast(mouseRay, out RaycastHit gridHit))
                {
                    point = SnapPoint(gridHit.point, ref normal);
                    return true;
                }
            }
            return false;
        }
        #endregion
    }
}
#pragma warning restore UDR0001
