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
        public static Vector3 SnapToBounds(Vector3 mousePos)
        {
            if (!GridManager.settings.boundsSnapping) return mousePos;
            var sceneView = UnityEditor.SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null)
                return mousePos;
            Camera cam = sceneView.camera;
            float maxDistance = float.MaxValue;
            float radius = Mathf.Max(UnityEditor.HandleUtility.GetHandleSize(mousePos) * 0.1f, 0.02f);

            Vector3 SnapToBoundsInDirection(Vector3 position, Vector3 direction)
            {
                (GameObject obj, Bounds bounds)[] objectsColliding = null;
                var ray = new Ray(position, direction);
                boundsOctree.GetCollidingtWithinFrustum(ray, radius, cam, out objectsColliding, maxDistance);
                Vector3 bestPoint = position;
                float bestDistanceToRay = radius;
                Bounds bestBox = new Bounds();
                float bestOriginToPointDistance = float.MaxValue;
                foreach (var colliding in objectsColliding)
                {
                    var b = colliding.bounds;
                    Vector3 min = b.min, max = b.max, mid = b.center;

                    var pts = new Vector3[]
                    {
                        new Vector3(min.x, min.y, 0),
                        new Vector3(min.x, min.y, mid.z),
                        new Vector3(min.x, min.y, max.z),
                        new Vector3(min.x, mid.y, 0),
                        new Vector3(min.x, mid.y, mid.z),
                        new Vector3(min.x, mid.y, max.z),
                        new Vector3(min.x, max.y, 0),
                        new Vector3(min.x, max.y, mid.z),
                        new Vector3(min.x, max.y, max.z),

                        new Vector3(mid.x, min.y, 0),
                        new Vector3(mid.x, min.y, mid.z),
                        new Vector3(mid.x, min.y, max.z),
                        new Vector3(mid.x, mid.y, 0),
                        new Vector3(mid.x, mid.y, mid.z),
                        new Vector3(mid.x, mid.y, max.z),
                        new Vector3(mid.x, max.y, 0),
                        new Vector3(mid.x, max.y, mid.z),
                        new Vector3(mid.x, max.y, max.z),

                        new Vector3(max.x, min.y, 0),
                        new Vector3(max.x, min.y, mid.z),
                        new Vector3(max.x, min.y, max.z),
                        new Vector3(max.x, mid.y, 0),
                        new Vector3(max.x, mid.y, mid.z),
                        new Vector3(max.x, mid.y, max.z),
                        new Vector3(max.x, max.y, 0),
                        new Vector3(max.x, max.y, mid.z),
                        new Vector3(max.x, max.y, max.z),
                    };

                    foreach (var p in pts)
                    {
                        var originToPoint = p - position;
#if UNITY_2021_1_OR_NEWER
                        var distanceToRay = System.MathF.Round(Vector3.Cross(direction, originToPoint).magnitude, 5);
#else
                        var distanceToRay = (float)System.Math.Round(Vector3.Cross(direction, originToPoint).magnitude, 5);
#endif

                        if (distanceToRay > bestDistanceToRay) continue;
                        var originToPointDistance = originToPoint.magnitude;
                        if (distanceToRay == bestDistanceToRay && originToPointDistance > bestOriginToPointDistance) continue;
                        bestDistanceToRay = distanceToRay;
                        bestPoint = p;
                        bestBox = b;
                        bestOriginToPointDistance = originToPointDistance;
                    }
                }

                var plane = new Plane(direction, position);
                if (bestDistanceToRay < radius)
                {
                    var projectedPoint = plane.ClosestPointOnPlane(bestPoint);
                    UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                    UnityEditor.Handles.color = new Color(1f, 0.5f, 0.8f, 1f);
                    UnityEditor.Handles.DrawLine(projectedPoint, bestPoint, thickness: 3f);

                    float worldRadius = UnityEditor.HandleUtility.GetHandleSize(bestPoint) * 0.05f;
                    UnityEditor.Handles.DrawSolidDisc(bestPoint, (cam.transform.position - bestPoint), worldRadius);
                    var TRS = Matrix4x4.TRS(bestBox.center, Quaternion.identity, bestBox.size);
                    Graphics.DrawMesh(cubeMesh, TRS, snapBoxMaterial, layer: 0, cam);

                    return projectedPoint;
                }
                return position;
            }
            var result = SnapToBoundsInDirection(mousePos, Vector3.right);
            result = SnapToBoundsInDirection(result, Vector3.forward);
            result = SnapToBoundsInDirection(result, Vector3.up);
            result = SnapToBoundsInDirection(result, Vector3.left);
            result = SnapToBoundsInDirection(result, Vector3.back);
            result = SnapToBoundsInDirection(result, Vector3.down);
            return result;
        }
    }
}
#pragma warning restore UDR0001
