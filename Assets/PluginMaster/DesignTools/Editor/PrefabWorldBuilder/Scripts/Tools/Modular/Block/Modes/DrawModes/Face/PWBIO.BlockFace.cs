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
        
        private static System.Collections.Generic.HashSet<GameObject> _blockFaceCurrentStrokePlacedObjects
            = new System.Collections.Generic.HashSet<GameObject>();

        #region PREVIEW
        private static void PreviewBlockFace(Camera camera,
            out Vector3 mousePos3D, out Vector3 localMousePos3D)
        {
            var mousePos2D = Event.current.mousePosition;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePos2D);
            mousePos3D = Vector3.zero;
            localMousePos3D = Vector3.zero;

            var toolSettings = BlockManager.settings;
            var cellSize = toolSettings.moduleSize;
            var cellRotation = GridManager.settings.rotation;
            if (BlockManager.quarterTurns > 0)
                cellRotation *= Quaternion.AngleAxis(BlockManager.quarterTurns * 90, toolSettings.upwardAxis);

            Vector3 hitNormal = Vector3.up;
            Vector3 baseHitPoint = Vector3.zero;

            if (!PWBToolRaycast(mouseRay, out RaycastHit raycastHit, out GameObject collider, maxDistance: float.MaxValue,
                layerMask: -1, paintOnPalettePrefabs: true, castOnMeshesWithoutCollider: true,
                createTempColliders: true, exceptions: _blockFaceCurrentStrokePlacedObjects, ignoreSceneColliders: true))
            {
                BrushstrokeManager.ClearBrushstroke();
                _paintStroke.Clear();
                return;
            }

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
            baseHitPoint = SnapBlockPosition(adjustedHitPoint, out localMousePos3D, snapToGridY: true);

            BlockToolModes.faceNormalDirection = GetFaceNormalDirectionFromHitNormal(hitNormal, cellRotation);

            var inverseNormalOffset = -Vector3.Scale(GetFaceNormalVector(BlockToolModes.faceNormalDirection,
                cellRotation), cellSize);
            BlockToolModes.faceTargetCellCenter = baseHitPoint + inverseNormalOffset;

            mousePos3D = baseHitPoint;

            BrushstrokeManager.UpdateBlockFaceBrushstroke();

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

        private static BlockToolModes.FaceNormalDirection GetFaceNormalDirectionFromHitNormal(Vector3 hitNormal,
            Quaternion cellRotation)
        {
            var invRotation = Quaternion.Inverse(cellRotation);
            var localNormal = invRotation * hitNormal;

            var absX = Mathf.Abs(localNormal.x);
            var absY = Mathf.Abs(localNormal.y);
            var absZ = Mathf.Abs(localNormal.z);

            if (absY >= absX && absY >= absZ)
                return localNormal.y >= 0
                    ? BlockToolModes.FaceNormalDirection.UP
                    : BlockToolModes.FaceNormalDirection.DOWN;

            if (absX >= absY && absX >= absZ)
                return localNormal.x >= 0
                    ? BlockToolModes.FaceNormalDirection.RIGHT
                    : BlockToolModes.FaceNormalDirection.LEFT;

            return localNormal.z >= 0
                ? BlockToolModes.FaceNormalDirection.FORWARD
                : BlockToolModes.FaceNormalDirection.BACK;
        }

        private static Vector3 GetFaceNormalVector(BlockToolModes.FaceNormalDirection direction,
            Quaternion cellRotation)
        {
            switch (direction)
            {
                case BlockToolModes.FaceNormalDirection.UP:
                    return cellRotation * Vector3.up;
                case BlockToolModes.FaceNormalDirection.DOWN:
                    return cellRotation * Vector3.down;
                case BlockToolModes.FaceNormalDirection.LEFT:
                    return cellRotation * Vector3.left;
                case BlockToolModes.FaceNormalDirection.RIGHT:
                    return cellRotation * Vector3.right;
                case BlockToolModes.FaceNormalDirection.FORWARD:
                    return cellRotation * Vector3.forward;
                case BlockToolModes.FaceNormalDirection.BACK:
                    return cellRotation * Vector3.back;
                default:
                    return cellRotation * Vector3.up;
            }
        }
        #endregion
        #region INPUT
        private static void AttachBlockFaceInput(Vector3 mousePos3D)
        {
            if (Event.current.button == 0 && Event.current.type == EventType.MouseDown)
            {
                _blockFaceCurrentStrokePlacedObjects.Clear();

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
                            _blockFaceCurrentStrokePlacedObjects.Add(objAndIndex.Item1);
                            var objCenter = BoundsUtils.GetBoundsRecursive(objAndIndex.Item1.transform).center;
                            var cellCenter = SnapPositionToBlockCellCenter(objCenter);
                            BlockManager.AddOccupiedCell(cellCenter, cellSize, cellRotation, brushId);
                        }
                    }
                }

                _blockFaceCurrentStrokePlacedObjects.Clear();
            }
        }
        #endregion
    }
}
#pragma warning restore UDR0001
