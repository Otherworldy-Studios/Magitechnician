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
        
        private static System.Collections.Generic.List<GameObject> _blockBrushSelectLineTargets
            = new System.Collections.Generic.List<GameObject>();

        private static void PreviewBlockBrushSelectLine(Camera camera,
            out Vector3 mousePos3D, out Vector3 localMousePos3D)
        {
            mousePos3D = Vector3.zero;
            localMousePos3D = Vector3.zero;
            _blockBrushSelectLineTargets.Clear();

            var mousePos2D = Event.current.mousePosition;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePos2D);

            Vector3 baseHitPoint = Vector3.zero;
            bool hasHit = false;

            if (PWBToolRaycast(mouseRay, out RaycastHit raycastHit, out GameObject collider,
                maxDistance: float.MaxValue, layerMask: -1, paintOnPalettePrefabs: true,
                castOnMeshesWithoutCollider: true, createTempColliders: true,
                exceptions: null, ignoreSceneColliders: true))
            {
                var boundsCenter = BoundsUtils.GetBoundsRecursive(collider.transform).center;
                baseHitPoint = SnapPositionToBlockCellCenter(boundsCenter, out localMousePos3D);
                hasHit = true;
            }
            else if (GridRaycast(mouseRay, out RaycastHit gridHit))
            {
                baseHitPoint = SnapBlockPosition(gridHit.point, out localMousePos3D, snapToGridY: false);
                hasHit = true;
            }
            if (!hasHit) return;

            mousePos3D = baseHitPoint;
            if (BlockToolModes.lineState == BlockToolModes.LineState.FIRST_POINT)
            {
                BlockToolModes.lineFirstPoint = baseHitPoint;
                BlockToolModes.lineSecondPoint = baseHitPoint;
            }
            if (BlockToolModes.lineState == BlockToolModes.LineState.SECOND_POINT)
                BlockToolModes.lineSecondPoint = baseHitPoint;

            BrushstrokeManager.UpdateDeleteBlockLineBrushstroke();

            var toolSettings = BlockManager.settings;
            var cellSize = toolSettings.moduleSize;
            var cellRotation = GridManager.settings.rotation;
            if (BlockManager.quarterTurns > 0)
            {
                cellRotation *= Quaternion.AngleAxis(BlockManager.quarterTurns * 90, toolSettings.upwardAxis);
            }

            var brushstroke = BrushstrokeManager.brushstroke;
            var halfStep = Mathf.Min(cellSize.x, cellSize.y, cellSize.z) * 0.4999f;
            foreach (var strokeItem in brushstroke)
            {
                var cellCenter = strokeItem.tangentPosition;

                if (BlockToolModes.projectionAxis != BlockToolModes.ProjectionAxis.NONE)
                {
                    var projectedPosition = ProjectBrushstrokePosition(cellCenter);
                    if (!projectedPosition.HasValue) continue;
                    cellCenter = projectedPosition.Value;
                }
                var mirroredPositions = GetMirrorAndAxisModesPositions(cellCenter);
                mirroredPositions.Add(cellCenter);
                foreach (var mirroredCenter in mirroredPositions)
                {
                    var targetObj = FindObjectAtCellCenter(mirroredCenter, halfStep);
                    if (targetObj != null)
                    {
                        var root = UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(targetObj);
                        if (root != null) targetObj = root;
                        if (!_blockBrushSelectLineTargets.Contains(targetObj))
                        {
                            _blockBrushSelectLineTargets.Add(targetObj);
                        }
                    }

                    if (Event.current.type == EventType.Repaint)
                    {
                        var TRS = Matrix4x4.TRS(mirroredCenter, cellRotation, cellSize);
                        Graphics.DrawMesh(cubeMesh, TRS, snapBoxMaterial, layer: 0, camera);
                    }
                }
            }
        }

        private static void BlockBrushSelectLineInput(Vector3 mousePos3D)
        {
            if (Event.current.button == 0 && Event.current.type == EventType.MouseDown)
            {
                BlockToolModes.lineFirstPoint = mousePos3D;
                BlockToolModes.lineSecondPoint = mousePos3D;
                BlockToolModes.lineState = BlockToolModes.LineState.SECOND_POINT;
            }

            if (BlockToolModes.lineState == BlockToolModes.LineState.SECOND_POINT)
            {
                BlockToolModes.lineSecondPoint = mousePos3D;
            }

            if (BlockToolModes.lineState == BlockToolModes.LineState.SECOND_POINT
                && Event.current.button == 0 && Event.current.type == EventType.MouseUp)
            {
                if (_blockBrushSelectLineTargets != null && _blockBrushSelectLineTargets.Count > 0)
                {
                    if (Event.current.shift)
                    {
                        var current = UnityEditor.Selection.objects.ToList();
                        foreach (var obj in _blockBrushSelectLineTargets)
                        {
                            if (!current.Contains(obj))
                                current.Add(obj);
                        }
                        UnityEditor.Selection.objects = current.ToArray();
                    }
                    else if (Event.current.control)
                    {
                        var current = UnityEditor.Selection.objects.ToList();
                        foreach (var obj in _blockBrushSelectLineTargets)
                        {
                            current.Remove(obj);
                        }
                        UnityEditor.Selection.objects = current.ToArray();
                    }
                    else
                    {
                        UnityEditor.Selection.objects = _blockBrushSelectLineTargets.ToArray();
                    }
                }
                else if (!Event.current.shift && !Event.current.control)
                {
                    UnityEditor.Selection.objects = new Object[0];
                }

                BlockToolModes.lineState = BlockToolModes.LineState.FIRST_POINT;
            }
        }
    }
}
#pragma warning restore UDR0001
