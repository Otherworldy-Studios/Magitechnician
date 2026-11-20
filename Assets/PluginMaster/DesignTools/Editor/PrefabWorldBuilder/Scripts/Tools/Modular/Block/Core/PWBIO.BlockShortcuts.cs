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

        private static bool _blockRotateMode = false;
        private static GameObject _blockRotateTarget = null;
        private static Vector3 _blockRotateCellCenter;
        private static bool _hasBlockRotatePreview = false;
        private static Vector3 _blockRotateLocalAxis = Vector3.zero;


        private static bool UpdateBlockRotateState()
        {
            var shortcuts = PWBSettings.shortcuts;

            var yCWCombo = shortcuts.blockRotate90YCW.combination as PWBHoldKeysAndMouseCombination;
            var yCCWCombo = shortcuts.blockRotate90YCCW.combination as PWBHoldKeysAndMouseCombination;
            var xCWCombo = shortcuts.blockRotate90XCW.combination as PWBHoldKeysAndMouseCombination;
            var xCCWCombo = shortcuts.blockRotate90XCCW.combination as PWBHoldKeysAndMouseCombination;
            var zCWCombo = shortcuts.blockRotate90ZCW.combination as PWBHoldKeysAndMouseCombination;
            var zCCWCombo = shortcuts.blockRotate90ZCCW.combination as PWBHoldKeysAndMouseCombination;

            if ((yCWCombo?.holdingKeys ?? false) || (yCCWCombo?.holdingKeys ?? false))
            {
                _blockRotateLocalAxis = Vector3.up;
                return true;
            }
            if ((xCWCombo?.holdingKeys ?? false) || (xCCWCombo?.holdingKeys ?? false))
            {
                _blockRotateLocalAxis = Vector3.right;
                return true;
            }
            if ((zCWCombo?.holdingKeys ?? false) || (zCCWCombo?.holdingKeys ?? false))
            {
                _blockRotateLocalAxis = Vector3.forward;
                return true;
            }

            _blockRotateLocalAxis = Vector3.zero;
            return false;
        }

        private static void BlockShortcutsInput()
        {
            var shortcuts = PWBSettings.shortcuts;

            if (shortcuts.blockPreviewRotate90YCW.Check())
            {
                ++BlockManager.quarterTurns;
                if (BlockManager.quarterTurns >= 4) BlockManager.quarterTurns = 0;
                BlockManager.settings.UpdateCellSize();
                SetSnapStepToBlockCellSize();
                BlockToolModes.ResetDrawModeState();
                BrushstrokeManager.UpdateBlockByBlockBrushstroke(setNextIdx: false);
                repaint = true;
            }

            bool yCW = shortcuts.blockRotate90YCW.Check();
            bool yCCW = shortcuts.blockRotate90YCCW.Check();
            bool xCW = shortcuts.blockRotate90XCW.Check();
            bool xCCW = shortcuts.blockRotate90XCCW.Check();
            bool zCW = shortcuts.blockRotate90ZCW.Check();
            bool zCCW = shortcuts.blockRotate90ZCCW.Check();

            _blockRotateMode = UpdateBlockRotateState();

            var gridRotation = GridManager.settings.rotation;
            Vector3 axis = Vector3.zero;
            float angle = 0f;
            if (yCW) { axis = gridRotation * Vector3.up; angle = 90f; }
            else if (yCCW) { axis = gridRotation * Vector3.up; angle = -90f; }
            else if (xCW) { axis = gridRotation * Vector3.right; angle = 90f; }
            else if (xCCW) { axis = gridRotation * Vector3.right; angle = -90f; }
            else if (zCW) { axis = gridRotation * Vector3.forward; angle = 90f; }
            else if (zCCW) { axis = gridRotation * Vector3.forward; angle = -90f; }

            if (axis != Vector3.zero && _blockRotateTarget != null)
            {
                RotateBlockObject(_blockRotateTarget, axis, angle);
                repaint = true;
                Event.current.Use();
            }

            if (!_blockRotateMode)
            {
                _blockRotateTarget = null;
                _hasBlockRotatePreview = false;
            }
        }

        private static void RotateBlockObject(GameObject obj, Vector3 axis, float angle)
        {
            var root = UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(obj);
            if (root != null) obj = root;
            UnityEditor.Undo.RecordObject(obj.transform, "Rotate Block");
            obj.transform.RotateAround(_blockRotateCellCenter, axis, angle);
        }

        private static float GetRotationCircleRadius(Vector3 cellSize, Vector3 localAxis)
        {
            if (localAxis == Vector3.up)
                return Mathf.Max(cellSize.x, cellSize.z);
            if (localAxis == Vector3.right)
                return Mathf.Max(cellSize.y, cellSize.z);
            return Mathf.Max(cellSize.x, cellSize.y);
        }

        private static void PreviewBlockRotation(Camera camera)
        {
            if (!_blockRotateMode || _modularDeleteMode) return;

            var mousePos2D = Event.current.mousePosition;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePos2D);

            var nearbyObjects = new System.Collections.Generic.List<GameObject>();
            boundsOctree.GetColliding(nearbyObjects, mouseRay, float.MaxValue);

            GameObject targetObj = null;
            float minDistance = float.MaxValue;

            foreach (var obj in nearbyObjects)
            {
                if (obj == null) continue;
                if (!obj.activeInHierarchy) continue;
                if (!PaletteManager.selectedPalette.ContainsSceneObject(obj)) continue;

                var objBounds = BoundsUtils.GetBoundsRecursive(obj.transform);
                if (objBounds.IntersectRay(mouseRay, out float distance))
                {
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        targetObj = obj;
                    }
                }
            }

            if (targetObj != null)
            {
                var root = UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(targetObj);
                if (root != null) targetObj = root;
                _blockRotateTarget = targetObj;

                var targetBounds = BoundsUtils.GetBoundsRecursive(targetObj.transform);
                _blockRotateCellCenter = SnapPositionToBlockCellCenter(targetBounds.center);
                _hasBlockRotatePreview = true;
            }
            else if (Event.current.type == EventType.MouseMove)
            {
                _blockRotateTarget = null;
                _hasBlockRotatePreview = false;
            }

            if (_hasBlockRotatePreview && Event.current.type == EventType.Repaint)
            {
                var toolSettings = BlockManager.settings;
                var cellSize = toolSettings.moduleSize;
                var gridRotation = GridManager.settings.rotation;

                var TRS = Matrix4x4.TRS(_blockRotateCellCenter, gridRotation, cellSize);
                Graphics.DrawMesh(cubeMesh, TRS, snapBoxMaterial, layer: 0, camera);

                var circleNormal = gridRotation * _blockRotateLocalAxis;
                var radius = GetRotationCircleRadius(cellSize, _blockRotateLocalAxis);

                var prevColor = UnityEditor.Handles.color;
                UnityEditor.Handles.color = _blockRotateLocalAxis == Vector3.right
                    ? new Color(1f, 0.3f, 0.15f, 1f)
                    : _blockRotateLocalAxis == Vector3.up
                    ? new Color(0.6f, 1f, 0.3f, 1f)
                    : new Color(0.3f, 0.6f, 1f, 1f);
                UnityEditor.Handles.DrawWireDisc(_blockRotateCellCenter, circleNormal, radius, 3);

                var axisHalfLength = _blockRotateLocalAxis == Vector3.right
                    ? cellSize.x
                    : _blockRotateLocalAxis == Vector3.up
                    ? cellSize.y
                    : cellSize.z;
                var axisDirection = circleNormal * axisHalfLength;
                UnityEditor.Handles.DrawLine(
                    _blockRotateCellCenter - axisDirection,
                    _blockRotateCellCenter + axisDirection, 3);
                UnityEditor.Handles.color = prevColor;
            }
        }
    }
}
#pragma warning restore UDR0001
