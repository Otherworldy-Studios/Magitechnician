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

        private static bool _offsetPicking = false;
        private static AxesUtils.Axis _offsetPickingAxis;
        private static float _offsetPickingValue = 0f;
        private static BrushSettings _offsetPickingBrush = null;

        public static void EnableOffsetPicking(AxesUtils.Axis axis, BrushSettings brush)
        {
            _offsetPickingBrush = brush;
            ToolController.current = ToolController.Tool.NONE;
            _offsetPicking = true;
            _offsetPickingAxis = axis;
            _offsetPickingValue = 0f;
            UpdateOctree();
            if (UnityEditor.SceneView.sceneViews.Count > 0)
                ((UnityEditor.SceneView)UnityEditor.SceneView.sceneViews[0]).Focus();
        }

        public static bool OffsetRaycast(out RaycastHit mouseHit, out GameObject collider)
        {
            mouseHit = new RaycastHit();
            collider = null;
            if (boundsOctree == null) return false;

            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            GameObject[] nearbyObjects = null;

            nearbyObjects = boundsOctree.GetColliding(mouseRay);
            if (nearbyObjects.Length == 0) return false;
            nearbyObjects = nearbyObjects.Where(o => o != null).ToArray();
            if (nearbyObjects.Length == 0) return false;

            var validHit = false;
            var minDistance = float.MaxValue;

            foreach (var obj in nearbyObjects)
            {
                if (!MeshUtils.RayIntersectsGameObject(mouseRay, obj, includeInactive: false, out Vector3 hitPoint,
                    out float distance, out Vector3 hitNormal)) continue;
                if (distance >= minDistance) continue;
                minDistance = distance;
                mouseHit.point = hitPoint;
                mouseHit.distance = distance;
                collider = obj;
                validHit = true;
            }
            return validHit;
        }

        private static void OffsetPicking(Camera sceneCamera)
        {
            _offsetPickingValue = 0;
            if (!OffsetRaycast(out RaycastHit hit, out GameObject obj)) return;
            if (obj == null) return;
            var bounds = BoundsUtils.GetBoundsRecursive(obj.transform, obj.transform.rotation);
            var localHit = obj.transform.InverseTransformPoint(hit.point);

            var localCenter = obj.transform.InverseTransformPoint(bounds.center);
            var halfLocalSize = new Vector3(bounds.size.x / obj.transform.lossyScale.x,
                bounds.size.y / obj.transform.lossyScale.y,
                bounds.size.z / obj.transform.lossyScale.z) * 0.5f;
            var localMin = localCenter - halfLocalSize;
            var localMax = localCenter + halfLocalSize;

            var minShift = AxesUtils.GetAxisValue(localMin, _offsetPickingAxis);
            var maxShift = AxesUtils.GetAxisValue(localMax, _offsetPickingAxis);
            var hitShift = AxesUtils.GetAxisValue(localHit, _offsetPickingAxis);

            _offsetPickingValue = Mathf.Abs(hitShift >= 0f ? maxShift - hitShift : hitShift - minShift);
#if UNITY_2021_1_OR_NEWER
            _offsetPickingValue = System.MathF.Round(_offsetPickingValue, digits: 5);
#else
            _offsetPickingValue = (float)System.Math.Round(_offsetPickingValue, digits: 5);
#endif
        }
    }
}
#pragma warning restore UDR0001
