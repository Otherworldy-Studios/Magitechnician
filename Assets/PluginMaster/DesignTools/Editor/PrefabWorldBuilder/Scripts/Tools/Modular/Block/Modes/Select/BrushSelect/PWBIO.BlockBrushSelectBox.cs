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
        
        private static System.Collections.Generic.List<GameObject> _blockBrushSelectBoxTargets
            = new System.Collections.Generic.List<GameObject>();

        #region BLOCK BRUSH SELECT BOX PREVIEW
        private static void PreviewBlockBrushSelectBox(Camera camera, out Vector3 mousePos3D,
            out Vector3 localMousePos3D)
        {
            mousePos3D = Vector3.zero;
            localMousePos3D = Vector3.zero;
            _blockBrushSelectBoxTargets.Clear();

            var mousePos2D = Event.current.mousePosition;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePos2D);

            Vector3 baseHitPoint = Vector3.zero;
            bool hasHit = false;

            if (PWBToolRaycast(mouseRay, out RaycastHit raycastHit, out _,
                maxDistance: float.MaxValue, layerMask: -1, paintOnPalettePrefabs: true,
                castOnMeshesWithoutCollider: true, createTempColliders: true,
                exceptions: null, ignoreSceneColliders: true))
            {
                var hitNormal = raycastHit.normal;
                var absNormal = new Vector3(Mathf.Abs(hitNormal.x), Mathf.Abs(hitNormal.y), Mathf.Abs(hitNormal.z));
                var step = GridManager.settings.step;
                var offsetDirection = Vector3.zero;

                if (absNormal.x > absNormal.y && absNormal.x > absNormal.z)
                    offsetDirection = new Vector3(-Mathf.Sign(hitNormal.x) * step.x * 0.5f, 0f, 0f);
                else if (absNormal.z > absNormal.x && absNormal.z > absNormal.y)
                    offsetDirection = new Vector3(0f, 0f, -Mathf.Sign(hitNormal.z) * step.z * 0.5f);

                var adjustedHitPoint = raycastHit.point + GridManager.settings.rotation * offsetDirection;

                baseHitPoint = SnapPositionToBlockCellCenter(adjustedHitPoint, out localMousePos3D);
                hasHit = true;
            }
            else if (GridRaycast(mouseRay, out RaycastHit gridHit))
            {
                baseHitPoint = SnapBlockPosition(gridHit.point, out localMousePos3D, snapToGridY: false);
                hasHit = true;
            }
            if (!hasHit) return;

            mousePos3D = baseHitPoint;
            if (!BlockToolModes.blockBoxFirstPointSet)
            {
                BlockToolModes.boxFirstPoint = baseHitPoint;
                BlockToolModes.boxSecondPoint = baseHitPoint;
            }
            else BlockToolModes.boxSecondPoint = baseHitPoint;

            BrushstrokeManager.UpdateDeleteBlockBoxBrushstroke();

            var toolSettings = BlockManager.settings;
            var cellSize = toolSettings.moduleSize;
            var cellRotation = GridManager.settings.rotation;
            if (BlockManager.quarterTurns > 0)
                cellRotation *= Quaternion.AngleAxis(BlockManager.quarterTurns * 90, toolSettings.upwardAxis);

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
                        if (!_blockBrushSelectBoxTargets.Contains(targetObj))
                            _blockBrushSelectBoxTargets.Add(targetObj);
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

        #region BLOCK BRUSH SELECT BOX INPUT
        private static void BlockBrushSelectBoxInput(Vector3 mousePos3D)
        {
            if (Event.current.button == 0 && Event.current.type == EventType.MouseDown)
            {
                BlockToolModes.boxFirstPoint = mousePos3D;
                BlockToolModes.boxSecondPoint = mousePos3D;
                BlockToolModes.blockBoxFirstPointSet = true;
            }

            if (BlockToolModes.blockBoxFirstPointSet)
                BlockToolModes.boxSecondPoint = mousePos3D;

            if (BlockToolModes.blockBoxFirstPointSet
                && Event.current.button == 0 && Event.current.type == EventType.MouseUp)
            {
                if (_blockBrushSelectBoxTargets != null && _blockBrushSelectBoxTargets.Count > 0)
                {
                    if (Event.current.shift)
                    {
                        var current = UnityEditor.Selection.objects.ToList();
                        foreach (var obj in _blockBrushSelectBoxTargets)
                        {
                            if (!current.Contains(obj))
                                current.Add(obj);
                        }
                        UnityEditor.Selection.objects = current.ToArray();
                    }
                    else if (Event.current.control)
                    {
                        var current = UnityEditor.Selection.objects.ToList();
                        foreach (var obj in _blockBrushSelectBoxTargets)
                        {
                            current.Remove(obj);
                        }
                        UnityEditor.Selection.objects = current.ToArray();
                    }
                    else
                    {
                        UnityEditor.Selection.objects = _blockBrushSelectBoxTargets.ToArray();
                    }
                }
                else if (!Event.current.shift && !Event.current.control)
                {
                    UnityEditor.Selection.objects = new Object[0];
                }

                BlockToolModes.blockBoxFirstPointSet = false;
            }
        }
        #endregion
    }
}
#pragma warning restore UDR0001
