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
        
        private static bool _snappedToVertex = false;
        private static bool SnapToVertex(Ray ray, out RaycastHit closestVertexInfo,
            bool in2DMode, GameObject[] selection = null)
        {
            Vector2 origin2D = ray.origin;
            bool snappedToVertex = false;
            float radius = 1f;
            RaycastHit[] hitArray = null;
            Collider2D[] collider2DArray = null;

            do
            {
                var collidingWithOctreeNodes = new System.Collections.Generic.List<GameObject>();
                boundsOctree.GetColliding(collidingWithOctreeNodes, ray, radius, float.PositiveInfinity);
                for (int i = 0; i < collidingWithOctreeNodes.Count; ++i)
                {
                    if(collidingWithOctreeNodes[i] == null) continue;
                    PWBCore.AddTempCollider(collidingWithOctreeNodes[i]);
                }

                if (selection == null)
                {
                    if (Physics.SphereCast(ray, radius, out RaycastHit hitInfo))
                    {
                        hitArray = new RaycastHit[1];
                        hitArray[0] = hitInfo;
                    }
                    else
                    {
                        hitArray = null;
                    }
                }
                else
                {
                    var allHits = Physics.SphereCastAll(ray, radius);
                    int selLen = selection.Length;
                    int filteredCount = 0;
                    RaycastHit[] tempFiltered = new RaycastHit[allHits.Length];
                    for (int i = 0; i < allHits.Length; ++i)
                    {
                        var colliderObj = allHits[i].collider.gameObject;
#if UNITY_6000_3_OR_NEWER
                        var hitID = colliderObj.GetEntityId();
#else
                        var hitID = colliderObj.GetInstanceID();
#endif
                        if (PWBCore.IsTempCollider(hitID))
                        {
                            colliderObj = PWBCore.GetGameObjectFromTempColliderId(hitID);
#if UNITY_6000_3_OR_NEWER
                            hitID = colliderObj.GetEntityId();
#else
                            hitID = colliderObj.GetInstanceID();
#endif
                        }
                        for (int j = 0; j < selLen; ++j)
                        {
#if UNITY_6000_3_OR_NEWER
                            if (hitID == selection[j].GetEntityId())
#else
                            if (hitID == selection[j].GetInstanceID())
#endif
                            {
                                tempFiltered[filteredCount++] = allHits[i];
                                break;
                            }
                        }
                    }
                    if (filteredCount > 0)
                    {
                        hitArray = new RaycastHit[filteredCount];
                        for (int i = 0; i < filteredCount; ++i)
                            hitArray[i] = tempFiltered[i];
                    }
                    else
                    {
                        hitArray = null;
                    }
                }

                // visibility filtering
                if (hitArray != null && hitArray.Length > 0)
                {
                    int visibleCount = 0;
                    RaycastHit[] tempVisible = new RaycastHit[hitArray.Length];
                    for (int i = 0; i < hitArray.Length; ++i)
                    {
                        var obj = hitArray[i].collider.gameObject;
#if UNITY_6000_3_OR_NEWER
                        if (PWBCore.IsTempCollider(obj.GetEntityId()))
                            obj = PWBCore.GetGameObjectFromTempColliderId(obj.GetEntityId());
#else
                        if (PWBCore.IsTempCollider(obj.GetInstanceID()))
                            obj = PWBCore.GetGameObjectFromTempColliderId(obj.GetInstanceID());
#endif
                        if (IsVisible(ref obj))
                        {
                            tempVisible[visibleCount++] = hitArray[i];
                        }
                    }
                    if (visibleCount > 0)
                    {
                        if (visibleCount < hitArray.Length)
                        {
                            RaycastHit[] newArray = new RaycastHit[visibleCount];
                            for (int i = 0; i < visibleCount; ++i)
                                newArray[i] = tempVisible[i];
                            hitArray = newArray;
                        }
                        else
                        {
                            hitArray = tempVisible;
                        }
                        break;
                    }
                    else
                    {
                        hitArray = null;
                    }
                }

                if (in2DMode)
                {
                    var allColliders = Physics2D.OverlapCircleAll(origin2D, radius);
                    int filteredCount = 0;
                    Collider2D[] tempFiltered = new Collider2D[allColliders.Length];
                    for (int i = 0; i < allColliders.Length; ++i)
                    {
                        var colliderObj = allColliders[i].gameObject;
#if UNITY_6000_3_OR_NEWER
                        var hitID = colliderObj.GetEntityId();
#else
                        var hitID = colliderObj.GetInstanceID();
#endif
                        if (PWBCore.IsTempCollider(hitID))
                        {
                            colliderObj = PWBCore.GetGameObjectFromTempColliderId(hitID);
#if UNITY_6000_3_OR_NEWER
                            hitID = colliderObj.GetEntityId();
#else
                            hitID = colliderObj.GetInstanceID();
#endif
                        }
                        for (int j = 0; selection != null && j < selection.Length; ++j)
                        {
#if UNITY_6000_3_OR_NEWER
                            if (hitID == selection[j].GetEntityId())
#else
                            if (hitID == selection[j].GetInstanceID())
#endif
                            {
                                tempFiltered[filteredCount++] = allColliders[i];
                                break;
                            }
                        }
                    }
                    if (filteredCount > 0)
                    {
                        collider2DArray = new Collider2D[filteredCount];
                        for (int i = 0; i < filteredCount; ++i)
                            collider2DArray[i] = tempFiltered[i];
                        break;
                    }
                    else
                    {
                        collider2DArray = null;
                    }
                }
                radius *= 2f;
            } while (radius <= 1024f);

            if (hitArray != null && hitArray.Length > 0)
            {
                float minDist = float.MaxValue;
                GameObject closestObj = null;
                for (int i = 0; i < hitArray.Length; ++i)
                {
                    if (hitArray[i].distance < minDist)
                    {
                        minDist = hitArray[i].distance;
                        closestObj = hitArray[i].collider.gameObject;
#if UNITY_6000_3_OR_NEWER
                        if (PWBCore.IsTempCollider(closestObj.GetEntityId()))
                            closestObj = PWBCore.GetGameObjectFromTempColliderId(closestObj.GetEntityId());
#else
                        if (PWBCore.IsTempCollider(closestObj.GetInstanceID()))
                            closestObj = PWBCore.GetGameObjectFromTempColliderId(closestObj.GetInstanceID());
#endif
                    }
                }
                if (closestObj != null && DistanceUtils.FindNearestVertexToMouse(out closestVertexInfo, closestObj.transform))
                    return true;
            }

            snappedToVertex = false;
            closestVertexInfo = new RaycastHit();

            if (in2DMode && collider2DArray != null && collider2DArray.Length > 0)
            {
                float minSqrDistance = float.MaxValue;
                for (int i = 0; i < collider2DArray.Length; ++i)
                {
                    var obj = collider2DArray[i].gameObject;
#if UNITY_6000_3_OR_NEWER
                    if (PWBCore.IsTempCollider(obj.GetEntityId()))
                        obj = PWBCore.GetGameObjectFromTempColliderId(obj.GetEntityId());
#else
                    if (PWBCore.IsTempCollider(obj.GetInstanceID()))
                        obj = PWBCore.GetGameObjectFromTempColliderId(obj.GetInstanceID());
#endif

                    RaycastHit closestVertexInfo2D;
                    if (DistanceUtils.FindNearestVertexToMouse(out closestVertexInfo2D, obj.transform))
                    {
                        float sqrDistance = ((Vector2)closestVertexInfo2D.point - origin2D).sqrMagnitude;
                        if (sqrDistance < minSqrDistance)
                        {
                            minSqrDistance = sqrDistance;
                            closestVertexInfo = closestVertexInfo2D;
                            snappedToVertex = true;
                        }
                    }
                }
                return snappedToVertex;
            }

#if UNITY_2020_2_OR_NEWER
            return DistanceUtils.FindNearestVertexToMouse(out closestVertexInfo, null);
#else
            return false;
#endif
        }
    }
}
#pragma warning restore UDR0001
