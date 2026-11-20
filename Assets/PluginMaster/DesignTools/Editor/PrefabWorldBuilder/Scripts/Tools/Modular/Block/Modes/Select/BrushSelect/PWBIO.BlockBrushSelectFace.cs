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
        
        private static System.Collections.Generic.List<GameObject> _blockBrushSelectFaceTargets
            = new System.Collections.Generic.List<GameObject>();

        #region BLOCK BRUSH SELECT FACE PREVIEW
        private static void PreviewBlockBrushSelectFace(Camera camera, out Vector3 mousePos3D,
            out Vector3 localMousePos3D)
        {
            mousePos3D = Vector3.zero;
            localMousePos3D = Vector3.zero;
            _blockBrushSelectFaceTargets.Clear();

            var mousePos2D = Event.current.mousePosition;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePos2D);

            if (!PWBToolRaycast(mouseRay, out RaycastHit raycastHit, out _,
                maxDistance: float.MaxValue, layerMask: -1, paintOnPalettePrefabs: true,
                castOnMeshesWithoutCollider: true, createTempColliders: true,
                exceptions: null, ignoreSceneColliders: true))
            {
                return;
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
                    if (targetObj != null)
                    {
                        var root = UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(targetObj);
                        if (root != null) targetObj = root;
                        if (!_blockBrushSelectFaceTargets.Contains(targetObj))
                            _blockBrushSelectFaceTargets.Add(targetObj);
                    }

                    if (Event.current.type == EventType.Repaint)
                    {
                        var TRS = Matrix4x4.TRS(mirroredCenter, cellRotation, cellSize);
                        Graphics.DrawMesh(cubeMesh, TRS, snapBoxMaterial, layer: 0, camera);
                    }
                }
            }
        }
        #endregion
        #region BLOCK BRUSH SELECT FACE INPUT
        private static void BlockBrushSelectFaceInput(Vector3 mousePos3D)
        {
            if (_blockBrushSelectFaceTargets == null || _blockBrushSelectFaceTargets.Count == 0)
            {
                if (Event.current.button == 0 && Event.current.type == EventType.MouseDown
                    && !Event.current.shift)
                {
                    _brushSelectStrokeProcessed.Clear();
                    _brushSelectStrokeIsDeselecting = false;
                    UnityEditor.Selection.objects = new Object[0];
                }
                return;
            }

            if (Event.current.button == 0)
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    _brushSelectStrokeProcessed.Clear();

                    var current = UnityEditor.Selection.objects.ToList();
                    _brushSelectStrokeIsDeselecting = current.Contains(_blockBrushSelectFaceTargets[0]);

                    foreach (var obj in _blockBrushSelectFaceTargets)
                    {
                        _brushSelectStrokeProcessed.Add(obj);
                        if (_brushSelectStrokeIsDeselecting)
                        {
                            current.Remove(obj);
                        }
                        else if (!current.Contains(obj))
                        {
                            current.Add(obj);
                        }
                    }

                    if (!_brushSelectStrokeIsDeselecting && !Event.current.shift)
                        UnityEditor.Selection.objects = _blockBrushSelectFaceTargets.ToArray();
                    else
                        UnityEditor.Selection.objects = current.ToArray();
                }
                else if (Event.current.type == EventType.MouseDrag)
                {
                    if (_blockBrushSelectFaceTargets.Count > 0)
                    {
                        var current = UnityEditor.Selection.objects.ToList();
                        var changed = false;
                        foreach (var obj in _blockBrushSelectFaceTargets)
                        {
                            if (_brushSelectStrokeProcessed.Contains(obj)) continue;
                            _brushSelectStrokeProcessed.Add(obj);
                            changed = true;

                            if (_brushSelectStrokeIsDeselecting)
                                current.Remove(obj);
                            else if (!current.Contains(obj))
                                current.Add(obj);
                        }
                        if (changed)
                            UnityEditor.Selection.objects = current.ToArray();
                    }
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    _brushSelectStrokeProcessed.Clear();
                }
            }
        }
        #endregion
    }
}
#pragma warning restore UDR0001
