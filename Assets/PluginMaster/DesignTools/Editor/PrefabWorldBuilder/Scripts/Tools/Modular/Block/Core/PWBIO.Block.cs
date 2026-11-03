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

        #region HANDLERS
        private static void BlockInitializeOnLoad()
        {
            BlockManager.settings.OnDataChanged -= OnBlockSettingsChanged;
            BlockManager.settings.OnDataChanged += OnBlockSettingsChanged;


            BrushSettings.OnBrushSettingsChanged -= UpdateBlockSettingsOnBrushChanged;
            BrushSettings.OnBrushSettingsChanged += UpdateBlockSettingsOnBrushChanged;

            PaletteManager.OnBrushSelectionChanged -= UpdateBlockSettingsOnBrushChanged;
            PaletteManager.OnBrushSelectionChanged += UpdateBlockSettingsOnBrushChanged;

            GridManager.settings.OnGridOriginChange -= OnBlockGridOriginChange;
            GridManager.settings.OnGridOriginChange += OnBlockGridOriginChange;

            UnityEditor.Selection.selectionChanged -= OnBlockSelectionChanged;
            UnityEditor.Selection.selectionChanged += OnBlockSelectionChanged;

        }

        private static void SetSnapStepToBlockCellSize()
        {
            GridManager.settings.step = BlockManager.settings.moduleSize + BlockManager.settings.spacing;
            UnityEditor.SceneView.RepaintAll();
        }

        private static void OnBlockSettingsChanged()
        {
            repaint = true;
            SetSnapStepToBlockCellSize();
        }

        public static void UpdateBlockSettingsOnBrushChanged()
        {
            if (ToolController.current != ToolController.Tool.BLOCK) return;
            if (PaletteManager.selectedBrushIdx == -1) return;
            var previousCellSize = BlockManager.settings.moduleSize;
            PWBCore.LoadBlockManagerFromFile();
            BlockManager.quarterTurns = 0;
            BlockManager.settings.UpdateCellSize();
            SetSnapStepToBlockCellSize();
        }

        public static void OnBlockGridOriginChange()
        {
            if (ToolController.current != ToolController.Tool.BLOCK) return;
            repaint = true;
            SetSnapStepToBlockCellSize();
        }
        #endregion

        #region CORE
        private static System.Collections.Generic.List<GameObject> _blockDeleteTargets
            = new System.Collections.Generic.List<GameObject>();
        public static void OnBlockEnabled()
        {
            ModularToolModes.ResetEditMode();
            UpdateOctree();
            GridManager.settings.radialGridEnabled = false;
            GridManager.settings.gridOnY = true;
            GridManager.settings.visibleGrid = true;
            GridManager.settings.lockedGrid = true;
            GridManager.settings.snappingOnX = true;
            GridManager.settings.snappingOnY = true;
            GridManager.settings.snappingOnZ = true;
            GridManager.settings.snappingEnabled = true;
            UpdateBlockSettingsOnBrushChanged();
            GridManager.settings.DataChanged(repaint: true, forceSave: false);
            BlockToolModes.ResetDrawModeState();
            BlockManager.quarterTurns = 0;
            PWBIO.UpdateSceneColliderSet();
            _moveSelection.Clear();

        }
        private static void BlockToolDuringSceneGUI(UnityEditor.SceneView sceneView)
        {
            var mousePos3D = Vector3.zero;
            var localMousePos3D = Vector3.zero;
            BlockByBlockDeleteToggle();
            BlockShortcutsInput();
            if (_modularDeleteMode)
                PreviewBlockByBlockDelete(sceneView.camera, out localMousePos3D);
            else if (_blockRotateMode)
                PreviewBlockRotation(sceneView.camera);
            else
                BlockPreview(sceneView.camera, out mousePos3D, out localMousePos3D);

            if (!_blockRotateMode && !_editingSymmetryOriginHandle && !_pickingSymmetryOrigin)
                BlockInput(sceneView.camera, mousePos3D);

            BlockInfoText(sceneView, localMousePos3D);
        }
        private static void BlockPreview(Camera camera, out Vector3 mousePos3D, out Vector3 localMousePos3D)
        {
            mousePos3D = Vector3.zero;
            localMousePos3D = Vector3.zero;
            if (ModularToolModes.selectedEditMode == ModularToolModes.EditMode.ATTACH)
            {
                if (PaletteManager.selectedBrush == null) return;
                if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.BLOCK_BY_BLOCK)
                {
                    if (BlockToolModes.brushSize == 1)
                        PreviewBlockSingleTile(camera, out mousePos3D, out localMousePos3D);
                    else
                        PreviewBlockByBlock(camera, out mousePos3D, out localMousePos3D);
                }
                else if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.LINE)
                {
                    if (BlockToolModes.lineState == BlockToolModes.LineState.FIRST_POINT)
                        PreviewBlockSingleTile(camera, out mousePos3D, out localMousePos3D);
                    else
                        PreviewBlockLine(camera, out mousePos3D, out localMousePos3D);
                }
                else if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.FACE)
                    PreviewBlockFace(camera, out mousePos3D, out localMousePos3D);
                else if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.BOX)
                    PreviewBlockBox(camera, out mousePos3D, out localMousePos3D);
            }
            else if (ModularToolModes.selectedEditMode == ModularToolModes.EditMode.ERASE)
            {
                if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.BLOCK_BY_BLOCK)
                    PreviewBlockByBlockDelete(camera, out localMousePos3D);
                else if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.LINE)
                    PreviewBlockLineDelete(camera, out mousePos3D, out localMousePos3D);
                else if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.FACE)
                    PreviewBlockFaceDelete(camera, out mousePos3D, out localMousePos3D);
                else if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.BOX)
                    PreviewBlockBoxDelete(camera, out mousePos3D, out localMousePos3D);
            }
            else if (ModularToolModes.selectedEditMode == ModularToolModes.EditMode.MOVE)
                PreviewBlockMove(camera, out mousePos3D, out localMousePos3D);
            else if (ModularToolModes.selectedEditMode == ModularToolModes.EditMode.SELECT)
            {
                if (BlockToolModes.selectMode == BlockToolModes.SelecMode.RECT)
                    PreviewBlockRectSelect(camera, out mousePos3D, out localMousePos3D);
                else if (BlockToolModes.selectMode == BlockToolModes.SelecMode.BRUSH)
                {
                    if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.BLOCK_BY_BLOCK)
                        PreviewBlockBrushSelectBlockByBlock(camera, out mousePos3D, out localMousePos3D);
                    else if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.LINE)
                        PreviewBlockBrushSelectLine(camera, out mousePos3D, out localMousePos3D);
                    else if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.FACE)
                        PreviewBlockBrushSelectFace(camera, out mousePos3D, out localMousePos3D);
                    else if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.BOX)
                        PreviewBlockBrushSelectBox(camera, out mousePos3D, out localMousePos3D);
                }
                else if (BlockToolModes.selectMode == BlockToolModes.SelecMode.REGION)
                    PreviewBlockRegionSelect(camera, out mousePos3D, out localMousePos3D);
            }
            else if (ModularToolModes.selectedEditMode == ModularToolModes.EditMode.REPLACE)
                PreviewBlockReplace(camera, out mousePos3D, out localMousePos3D);
        }
        private static void BlockInput(Camera camera, Vector3 mousePos3D)
        {
            if (_modularDeleteMode)
                BlockByBlockDeleteInput();
            else if (ModularToolModes.selectedEditMode == ModularToolModes.EditMode.ATTACH)
            {
                if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.BLOCK_BY_BLOCK)
                    AttachBlockByBlockInput(mousePos3D);
                else if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.LINE)
                    AttachBlockLineInput(mousePos3D);
                else if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.FACE)
                    AttachBlockFaceInput(mousePos3D);
                else if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.BOX)
                    AttachBlockBoxInput(mousePos3D);
            }
            else if (ModularToolModes.selectedEditMode == ModularToolModes.EditMode.ERASE)
            {
                if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.BLOCK_BY_BLOCK)
                    BlockByBlockDeleteInput();
                else if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.LINE)
                    DeleteBlockLineInput(mousePos3D);
                else if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.FACE)
                    DeleteBlockFaceInput(mousePos3D);
                else if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.BOX)
                    DeleteBlockBoxInput(mousePos3D);
            }
            else if (ModularToolModes.selectedEditMode == ModularToolModes.EditMode.SELECT)
            {
                if (BlockToolModes.selectMode == BlockToolModes.SelecMode.RECT)
                    BlockRectSelectInput(camera, mousePos3D);
                else if (BlockToolModes.selectMode == BlockToolModes.SelecMode.BRUSH)
                {
                    if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.LINE)
                        BlockBrushSelectLineInput(mousePos3D);
                    else if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.FACE)
                        BlockBrushSelectFaceInput(mousePos3D);
                    else if (BlockToolModes.selectedDrawMode == BlockToolModes.DrawMode.BOX)
                        BlockBrushSelectBoxInput(mousePos3D);
                }
                else if (BlockToolModes.selectMode == BlockToolModes.SelecMode.REGION)
                    BlockRegionSelectInput(mousePos3D);
            }
            else if (ModularToolModes.selectedEditMode == ModularToolModes.EditMode.MOVE)
                BlockMoveInput(mousePos3D);
            else if (ModularToolModes.selectedEditMode == ModularToolModes.EditMode.REPLACE)
                BlockReplaceInput(mousePos3D);
            else if (ModularToolModes.selectedEditMode == ModularToolModes.EditMode.PICK)
                BlockPickInput();
        }
        #endregion

        #region UTILS
        private static bool IsBlockCellOccupied(Vector3 cellCenter, Vector3 cellSize, Quaternion rotation)
        {
            if (BlockManager.IsCellOccupied(cellCenter, cellSize))
                return true;

            var halfStep = Mathf.Min(cellSize.x, cellSize.y, cellSize.z) * 0.4999f;
            var halfCellSize = cellSize / 2;
            var nearbyObjects = new System.Collections.Generic.List<GameObject>();
            boundsOctree.GetColliding(cellCenter, halfCellSize, GridManager.settings.rotation,
                rotation, nearbyObjects);

            foreach (var obj in nearbyObjects)
            {
                if (obj == null) continue;
                if (!obj.activeInHierarchy) continue;
                if (_blockByBlockCurrentStrokePlacedObjects.Contains(obj)) continue;
                var objCenter = BoundsUtils.GetBoundsRecursive(obj.transform).center;
                var centerDistance = (objCenter - cellCenter).magnitude;
                if (centerDistance > halfStep) continue;
                if (PaletteManager.selectedPalette.ContainsSceneObject(obj))
                    return true;
            }
            return false;
        }
        public static Vector3 SnapPositionToBlockCellCenter(Vector3 position)
        {
            return SnapPositionToBlockCellCenter(position, out _);
        }

        private static Vector3 SnapPositionToBlockCellCenter(Vector3 position, out Vector3 localPosition)
        {
            var localOriginOffset = (GridManager.settings.step) * 0.5f;
            var origin = GridManager.settings.origin + GridManager.settings.rotation * localOriginOffset;
            var localPos = Quaternion.Inverse(GridManager.settings.rotation) * (position - origin);

            float Snap(float step, float value) => Mathf.Round(value / step) * step;
            var localSnappedPos = new Vector3(
                Snap(GridManager.settings.step.x, localPos.x),
                Snap(GridManager.settings.step.y, localPos.y),
                Snap(GridManager.settings.step.z, localPos.z));
            var result = GridManager.settings.rotation * localSnappedPos + origin;
            localPosition = localSnappedPos;
            return result;
        }

        private static Vector3 SnapBlockPosition(Vector3 hitPoint, out Vector3 localPosition,
            out Vector3 cellCenter, bool snapToGridY)
        {
            var toolSettings = BlockManager.settings;
            var brushOffset = Vector3.zero;
            if (toolSettings.subtractBrushOffset)
            {
                BrushSettings brush = PaletteManager.selectedBrush;
                if (toolSettings.overwriteBrushProperties) brush = toolSettings.brushSettings;
                if (brush != null) brushOffset = brush.localPositionOffset;
                if (BlockManager.quarterTurns > 0)
                    brushOffset = Quaternion.AngleAxis(BlockManager.quarterTurns * 90,
                        BlockManager.settings.upwardAxis) * brushOffset;
            }
            var localOriginOffset = (GridManager.settings.step - brushOffset) * 0.5f;
            var origin = GridManager.settings.origin + GridManager.settings.rotation * localOriginOffset;
            var localGridOrigin = GridManager.settings.origin
                + GridManager.settings.rotation * (GridManager.settings.step * 0.5f);
            var centerOriginOffset = GridManager.settings.step * 0.5f;
            var centerOrigin = GridManager.settings.origin + GridManager.settings.rotation * centerOriginOffset;
            float snappedLocalY = 0f;
            if (snapToGridY)
            {
                var localPos = Quaternion.Inverse(GridManager.settings.rotation) * (hitPoint - GridManager.settings.origin);
                float Snap(float step, float value) => Mathf.Round(value / step) * step;
                snappedLocalY = Snap(GridManager.settings.step.y, localPos.y);
                var yOffset = GridManager.settings.rotation * new Vector3(0f, snappedLocalY, 0f);
                origin += yOffset;
                centerOrigin += yOffset;
            }

            var localPos2 = Quaternion.Inverse(GridManager.settings.rotation) * (hitPoint - localGridOrigin);
            float Snap2(float step, float value) => Mathf.Round(value / step) * step;
            var localSnappedPos = new Vector3(
                Snap2(GridManager.settings.step.x, localPos2.x),
                0f,
                Snap2(GridManager.settings.step.z, localPos2.z));
            var result = GridManager.settings.rotation * localSnappedPos + origin;
            cellCenter = GridManager.settings.rotation * localSnappedPos + centerOrigin;
            localPosition = localSnappedPos;
            localPosition.y = snappedLocalY;
            return result;
        }

        private static Vector3 SnapBlockPosition(Vector3 hitPoint, out Vector3 localPosition, bool snapToGridY)
        {
            return SnapBlockPosition(hitPoint, out localPosition, out _, snapToGridY);
        }

        private static Vector3 SnapBlockPosition(Vector3 cellCenter, out Vector3 localPosition)
        {
            var toolSettings = BlockManager.settings;
            var brushOffset = Vector3.zero;
            if (toolSettings.subtractBrushOffset)
            {
                BrushSettings brush = PaletteManager.selectedBrush;
                if (toolSettings.overwriteBrushProperties) brush = toolSettings.brushSettings;
                if (brush != null) brushOffset = brush.localPositionOffset;
                if (BlockManager.quarterTurns > 0)
                    brushOffset = Quaternion.AngleAxis(BlockManager.quarterTurns * 90,
                        BlockManager.settings.upwardAxis) * brushOffset;
            }
            var localGridOrigin = GridManager.settings.origin
                + GridManager.settings.rotation * (GridManager.settings.step * 0.5f);
            localPosition = Quaternion.Inverse(GridManager.settings.rotation) * (cellCenter - localGridOrigin);
            var result = cellCenter - GridManager.settings.rotation * (brushOffset * 0.5f);
            return result;
        }

        private static bool CellRayCast(Vector3 cellCenter, Vector3 cellSize, Quaternion rotation,
            Ray ray, out RaycastHit hitInfo)
        {
            hitInfo = new RaycastHit();
            var halfSize = cellSize * 0.5f;

            var axes = new Vector3[]
            {
                rotation * Vector3.right,
                rotation * Vector3.up,
                rotation * Vector3.forward
            };

            float closestDist = float.MaxValue;
            bool hit = false;

            for (int i = 0; i < 3; i++)
            {
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    var normal = axes[i] * sign;
                    var faceCenter = cellCenter + axes[i] * (halfSize[i] * sign);
                    var plane = new Plane(normal, faceCenter);

                    float enter;
                    if (!plane.Raycast(ray, out enter) || enter >= closestDist) continue;

                    var point = ray.GetPoint(enter);
                    var offset = point - faceCenter;

                    int a1 = (i + 1) % 3;
                    int a2 = (i + 2) % 3;

                    if (Mathf.Abs(Vector3.Dot(offset, axes[a1])) > halfSize[a1] + 0.001f) continue;
                    if (Mathf.Abs(Vector3.Dot(offset, axes[a2])) > halfSize[a2] + 0.001f) continue;

                    closestDist = enter;
                    hitInfo.point = point;
                    hitInfo.normal = normal;
                    hitInfo.distance = enter;
                    hit = true;
                }
            }

            return hit;
        }

        private static void BlockInfoText(UnityEditor.SceneView sceneView, Vector3 localMousePos3D)
        {
            if (!PWBCore.staticData.showInfoText) return;

            if (BlockToolModes.selectedEditMode == BlockToolModes.EditMode.PICK)
            {
                var labelTexts = new string[]
                {
                    "Block Picker",
                    "Object: " + _blockPickObjectName,
                    "Brush: " + _blockPickBrushName
                };
                InfoText.Draw(sceneView, labelTexts);
                return;
            }

            var stepSize = GridManager.settings.step;
            var localX = Mathf.RoundToInt(localMousePos3D.x / stepSize.x);
            var localY = Mathf.RoundToInt(localMousePos3D.y / stepSize.y);
            var localZ = Mathf.RoundToInt(localMousePos3D.z / stepSize.z);

            var labelTexts2 = new string[] { $"Position: (X: {localX}, Y: {localY}, Z: {localZ})" };
            InfoText.Draw(sceneView, labelTexts2.ToArray());
        }
        #endregion

    }
}
#pragma warning restore UDR0001
