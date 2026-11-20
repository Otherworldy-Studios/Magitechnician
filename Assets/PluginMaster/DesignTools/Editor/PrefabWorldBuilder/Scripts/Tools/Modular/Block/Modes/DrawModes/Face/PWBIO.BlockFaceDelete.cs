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
        #region BLOCK FACE DELETE PREVIEW
        private static bool PreviewBlockFaceDelete(Camera camera, out Vector3 mousePos3D,
            out Vector3 localMousePos3D)
        {
            mousePos3D = Vector3.zero;
            localMousePos3D = Vector3.zero;
            _blockDeleteTargets.Clear();

            var mousePos2D = Event.current.mousePosition;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePos2D);

            if (!PWBToolRaycast(mouseRay, out RaycastHit raycastHit, out _,
                maxDistance: float.MaxValue, layerMask: -1, paintOnPalettePrefabs: true,
                castOnMeshesWithoutCollider: true, createTempColliders: true,
                exceptions: null, ignoreSceneColliders: true))
            {
                return false;
            }

            var toolSettings = BlockManager.settings;
            var cellSize = toolSettings.moduleSize;
            var cellRotation = GridManager.settings.rotation;
            if (BlockManager.quarterTurns > 0)
                cellRotation *= Quaternion.AngleAxis(BlockManager.quarterTurns * 90, toolSettings.upwardAxis);

            var hitNormal = raycastHit.normal;
            var invRotation = Quaternion.Inverse(cellRotation);
            var localNormal = invRotation * hitNormal;

            var absNormal = new Vector3(Mathf.Abs(localNormal.x), Mathf.Abs(localNormal.y), Mathf.Abs(localNormal.z));
            var step = GridManager.settings.step;
            var offsetDirection = Vector3.zero;

            if (absNormal.y >= absNormal.x && absNormal.y >= absNormal.z)
                offsetDirection = new Vector3(0f, -Mathf.Sign(localNormal.y) * step.y * 0.5f, 0f);
            else if (absNormal.x >= absNormal.y && absNormal.x >= absNormal.z)
                offsetDirection = new Vector3(-Mathf.Sign(localNormal.x) * step.x * 0.5f, 0f, 0f);
            else
                offsetDirection = new Vector3(0f, 0f, -Mathf.Sign(localNormal.z) * step.z * 0.5f);

            var adjustedHitPoint = raycastHit.point + cellRotation * offsetDirection;
            var baseHitPoint = SnapPositionToBlockCellCenter(adjustedHitPoint, out localMousePos3D);
            mousePos3D = baseHitPoint;

            BlockToolModes.faceNormalDirection = GetFaceNormalDirectionFromHitNormal(hitNormal, cellRotation);
            BlockToolModes.faceTargetCellCenter = baseHitPoint;

            BrushstrokeManager.UpdateDeleteBlockFaceBrushstroke();

            var brushstroke = BrushstrokeManager.brushstroke;
            var halfStep = Mathf.Min(cellSize.x, cellSize.y, cellSize.z) * 0.4999f;
            foreach (var strokeItem in brushstroke)
            {
                var cellCenter = strokeItem.tangentPosition;
                var mirroredPositions = GetMirrorAndAxisModesPositions(cellCenter);
                mirroredPositions.Add(cellCenter);
                foreach (var mirroredCenter in mirroredPositions)
                {
                    var targetObj = FindObjectAtCellCenter(mirroredCenter, halfStep);
                    if (targetObj != null && !_blockDeleteTargets.Contains(targetObj))
                    {
                        _blockDeleteTargets.Add(targetObj);
                    }

                    if (Event.current.type == EventType.Repaint)
                    {
                        var TRS = Matrix4x4.TRS(mirroredCenter, cellRotation, cellSize);
                        Graphics.DrawMesh(cubeMesh, TRS, transparentRedMaterial2, layer: 0, camera);
                    }
                }
            }

            return _blockDeleteTargets.Count > 0;
        }
        #endregion
        #region BLOCK FACE DELETE INPUT
        private static void DeleteBlockFaceInput(Vector3 mousePos3D)
        {
            if (_blockDeleteTargets == null || _blockDeleteTargets.Count == 0) return;
            if (Event.current.button == 0 && Event.current.type == EventType.MouseDown)
            {
                foreach (var target in _blockDeleteTargets)
                {
                    if (target == null) continue;
                    EraseBlockObject(target);
                }
            }
        }
        #endregion

    }
}
#pragma warning restore UDR0001
