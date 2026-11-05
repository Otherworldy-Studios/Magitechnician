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
        
        private static System.Collections.Generic.HashSet<GameObject> _blockBoxCurrentStrokePlacedObjects
            = new System.Collections.Generic.HashSet<GameObject>();

        #region BOX PREVIEW
        private static void PreviewBlockBox(Camera camera,
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
                createTempColliders: true, exceptions: _blockBoxCurrentStrokePlacedObjects, ignoreSceneColliders: true))
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
                SnapBlockPosition(adjustedHitPoint, out localMousePos3D, out Vector3 cellCenter, snapToGridY: true);
                baseHitPoint = cellCenter;
                hasHit = true;
            }
            else if (GridRaycast(mouseRay, out RaycastHit gridHit))
            {
                hitNormal = gridHit.normal;
                SnapBlockPosition(gridHit.point, out localMousePos3D, out Vector3 cellCenter, snapToGridY: false);
                baseHitPoint = cellCenter;
                hasHit = true;
            }

            if (!hasHit) return;

            mousePos3D = baseHitPoint;

            if (!BlockToolModes.blockBoxFirstPointSet)
            {
                BlockToolModes.boxFirstPoint = baseHitPoint;
                BlockToolModes.boxSecondPoint = baseHitPoint;
            }
            else
            {
                BlockToolModes.boxSecondPoint = baseHitPoint;
            }
            BrushstrokeManager.UpdateBlockBoxBrushstroke();
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
                var cellCenter = SnapPositionToBlockCellCenter(strokeItem.tangentPosition);

                if (IsBlockCellOccupied(cellCenter, cellSize, cellRotation)) continue;

                PreviewBlockStrokeItem(camera, strokeItem);
            }
        }
        #endregion

        #region BOX INPUT
        private static void AttachBlockBoxInput(Vector3 mousePos3D)
        {
            if (Event.current.button == 0 && Event.current.type == EventType.MouseDown)
            {
                if (!BlockToolModes.blockBoxFirstPointSet)
                {
                    _blockBoxCurrentStrokePlacedObjects.Clear();
                    BlockToolModes.boxFirstPoint = mousePos3D;
                    BlockToolModes.boxSecondPoint = mousePos3D;
                    BlockToolModes.blockBoxFirstPointSet = true;
                }
            }

            if (BlockToolModes.blockBoxFirstPointSet && Event.current.button == 0
                && Event.current.type == EventType.MouseUp)
            {
                var toolSettings = BlockManager.settings;
                var cellSize = toolSettings.moduleSize;
                var cellRotation = GridManager.settings.rotation;
                if (BlockManager.quarterTurns > 0)
                    cellRotation *= Quaternion.AngleAxis(BlockManager.quarterTurns * 90, toolSettings.upwardAxis);

                var brush = PaletteManager.selectedBrush;
                var brushId = brush?.id ?? -1;

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

                BlockToolModes.blockBoxFirstPointSet = false;
                _blockBoxCurrentStrokePlacedObjects.Clear();
            }
        }
        #endregion
    }
}
#pragma warning restore UDR0001
