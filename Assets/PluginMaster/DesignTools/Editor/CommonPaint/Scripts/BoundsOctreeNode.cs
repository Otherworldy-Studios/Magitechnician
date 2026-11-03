/*
Copyright (c) Omar Duarte
Unauthorized copying of this file, via any medium is strictly prohibited.
Modified by Omar Duarte.

This file incorporates work covered by the following copyright and 
permission notice: 

Copyright (c) 2014, Nition, BSD licence. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
#pragma warning disable UDR0001

using UnityEngine;

namespace PluginMaster
{
    public partial class BoundsOctreeNode
    {
        #region Core Data and Initialization
        public Vector3 Center { get; private set; }
        public float BaseLength { get; private set; }
        float looseness;
        float minSize;
        float adjLength;
        Bounds bounds = default(Bounds);
        readonly System.Collections.Generic.List<OctreeObject> objects = new System.Collections.Generic.List<OctreeObject>();
        BoundsOctreeNode[] children = null;
        bool HasChildren { get { return children != null; } }
        Bounds[] childBounds;
        const int NUM_OBJECTS_ALLOWED = 8;

        struct OctreeObject
        {
            public GameObject Obj;
            public Bounds Bounds;
        }

        public BoundsOctreeNode(float baseLengthVal, float minSizeVal, float loosenessVal, Vector3 centerVal)
        {
            SetValues(baseLengthVal, minSizeVal, loosenessVal, centerVal);
        }

        void SetValues(float baseLengthVal, float minSizeVal, float loosenessVal, Vector3 centerVal)
        {
            BaseLength = baseLengthVal;
            minSize = minSizeVal;
            looseness = loosenessVal;
            Center = centerVal;
            adjLength = looseness * baseLengthVal;

            Vector3 size = new Vector3(adjLength, adjLength, adjLength);
            bounds = new Bounds(Center, size);

            float quarter = BaseLength / 4f;
            float childActualLength = (BaseLength / 2) * looseness;
            Vector3 childActualSize = new Vector3(childActualLength, childActualLength, childActualLength);
            childBounds = new Bounds[8];
            childBounds[0] = new Bounds(Center + new Vector3(-quarter, quarter, -quarter), childActualSize);
            childBounds[1] = new Bounds(Center + new Vector3(quarter, quarter, -quarter), childActualSize);
            childBounds[2] = new Bounds(Center + new Vector3(-quarter, quarter, quarter), childActualSize);
            childBounds[3] = new Bounds(Center + new Vector3(quarter, quarter, quarter), childActualSize);
            childBounds[4] = new Bounds(Center + new Vector3(-quarter, -quarter, -quarter), childActualSize);
            childBounds[5] = new Bounds(Center + new Vector3(quarter, -quarter, -quarter), childActualSize);
            childBounds[6] = new Bounds(Center + new Vector3(-quarter, -quarter, quarter), childActualSize);
            childBounds[7] = new Bounds(Center + new Vector3(quarter, -quarter, quarter), childActualSize);
        }
        #endregion

        #region Tree Lifecycle and Structure Management
        public bool Add(GameObject obj, Bounds objBounds)
        {
            if (!Encapsulates(bounds, objBounds))
            {
                return false;
            }
            SubAdd(obj, objBounds);
            return true;
        }

        void SubAdd(GameObject obj, Bounds objBounds)
        {
            if (!HasChildren)
            {
                if (objects.Count < NUM_OBJECTS_ALLOWED || (BaseLength / 2) < minSize)
                {
                    OctreeObject newObj = new OctreeObject { Obj = obj, Bounds = objBounds };
                    objects.Add(newObj);
                    return;
                }

                int bestFitChild;
                if (children == null)
                {
                    Split();
                    if (children == null)
                    {
                        Debug.LogError("Child creation failed for an unknown reason. Early exit.");
                        return;
                    }

                    for (int i = objects.Count - 1; i >= 0; i--)
                    {
                        OctreeObject existingObj = objects[i];
                        bestFitChild = BestFitChild(existingObj.Bounds.center);
                        if (Encapsulates(children[bestFitChild].bounds, existingObj.Bounds))
                        {
                            children[bestFitChild].SubAdd(existingObj.Obj, existingObj.Bounds);
                            objects.Remove(existingObj);
                        }
                    }
                }
            }

            int bestFit = BestFitChild(objBounds.center);
            if (Encapsulates(children[bestFit].bounds, objBounds))
            {
                children[bestFit].SubAdd(obj, objBounds);
            }
            else
            {
                OctreeObject newObj = new OctreeObject { Obj = obj, Bounds = objBounds };
                objects.Add(newObj);
            }
        }

        public bool Remove(GameObject obj)
        {
            bool removed = false;

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].Obj.Equals(obj))
                {
                    removed = objects.Remove(objects[i]);
                    break;
                }
            }

            if (!removed && children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    removed = children[i].Remove(obj);
                    if (removed) break;
                }
            }

            if (removed && children != null)
            {
                if (ShouldMerge())
                {
                    Merge();
                }
            }

            return removed;
        }

        public bool Remove(GameObject obj, Bounds objBounds)
        {
            if (!Encapsulates(bounds, objBounds))
            {
                return false;
            }
            return SubRemove(obj, objBounds);
        }

        bool SubRemove(GameObject obj, Bounds objBounds)
        {
            bool removed = false;

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].Obj.Equals(obj))
                {
                    removed = objects.Remove(objects[i]);
                    break;
                }
            }

            if (!removed && children != null)
            {
                int bestFitChild = BestFitChild(objBounds.center);
                removed = children[bestFitChild].SubRemove(obj, objBounds);
            }

            if (removed && children != null)
            {
                if (ShouldMerge())
                {
                    Merge();
                }
            }

            return removed;
        }

        void Split()
        {
            float quarter = BaseLength / 4f;
            float newLength = BaseLength / 2;
            children = new BoundsOctreeNode[8];
            children[0] = new BoundsOctreeNode(newLength, minSize, looseness,
                Center + new Vector3(-quarter, quarter, -quarter));
            children[1] = new BoundsOctreeNode(newLength, minSize, looseness,
                Center + new Vector3(quarter, quarter, -quarter));
            children[2] = new BoundsOctreeNode(newLength, minSize, looseness,
                Center + new Vector3(-quarter, quarter, quarter));
            children[3] = new BoundsOctreeNode(newLength, minSize, looseness,
                Center + new Vector3(quarter, quarter, quarter));
            children[4] = new BoundsOctreeNode(newLength, minSize, looseness,
                Center + new Vector3(-quarter, -quarter, -quarter));
            children[5] = new BoundsOctreeNode(newLength, minSize, looseness,
                Center + new Vector3(quarter, -quarter, -quarter));
            children[6] = new BoundsOctreeNode(newLength, minSize, looseness,
                Center + new Vector3(-quarter, -quarter, quarter));
            children[7] = new BoundsOctreeNode(newLength, minSize, looseness,
                Center + new Vector3(quarter, -quarter, quarter));
        }

        void Merge()
        {
            for (int i = 0; i < 8; i++)
            {
                BoundsOctreeNode curChild = children[i];
                int numObjects = curChild.objects.Count;
                for (int j = numObjects - 1; j >= 0; j--)
                {
                    OctreeObject curObj = curChild.objects[j];
                    objects.Add(curObj);
                }
            }
            children = null;
        }

        bool ShouldMerge()
        {
            int totalObjects = objects.Count;
            if (children != null)
            {
                foreach (BoundsOctreeNode child in children)
                {
                    if (child.children != null)
                    {
                        return false;
                    }
                    totalObjects += child.objects.Count;
                }
            }
            return totalObjects <= NUM_OBJECTS_ALLOWED;
        }

        public BoundsOctreeNode ShrinkIfPossible(float minLength)
        {
            if (BaseLength < (2 * minLength))
            {
                return this;
            }
            if (objects.Count == 0 && (children == null || children.Length == 0))
            {
                return this;
            }

            int bestFit = -1;
            for (int i = 0; i < objects.Count; i++)
            {
                OctreeObject curObj = objects[i];
                int newBestFit = BestFitChild(curObj.Bounds.center);
                if (i == 0 || newBestFit == bestFit)
                {
                    if (Encapsulates(childBounds[newBestFit], curObj.Bounds))
                    {
                        if (bestFit < 0)
                        {
                            bestFit = newBestFit;
                        }
                    }
                    else
                    {
                        return this;
                    }
                }
                else
                {
                    return this;
                }
            }

            if (children != null)
            {
                bool childHadContent = false;
                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i].HasAnyObjects())
                    {
                        if (childHadContent)
                        {
                            return this;
                        }
                        if (bestFit >= 0 && bestFit != i)
                        {
                            return this;
                        }
                        childHadContent = true;
                        bestFit = i;
                    }
                }
            }

            if (children == null)
            {
                SetValues(BaseLength / 2, minSize, looseness, childBounds[bestFit].center);
                return this;
            }

            if (bestFit == -1)
            {
                return this;
            }

            return children[bestFit];
        }

        public void SetChildren(BoundsOctreeNode[] childOctrees)
        {
            if (childOctrees.Length != 8)
            {
                Debug.LogError("Child octree array must be length 8. Was length: " + childOctrees.Length);
                return;
            }

            children = childOctrees;
        }
        #endregion

        #region Geometry and Hierarchy Utilities
        public int BestFitChild(Vector3 objBoundsCenter)
        {
            return (objBoundsCenter.x <= Center.x ? 0 : 1)
                + (objBoundsCenter.y >= Center.y ? 0 : 4)
                + (objBoundsCenter.z <= Center.z ? 0 : 2);
        }

        public bool HasAnyObjects()
        {
            if (objects.Count > 0) return true;

            if (children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (children[i].HasAnyObjects()) return true;
                }
            }

            return false;
        }

        static bool Encapsulates(Bounds outerBounds, Bounds innerBounds)
        {
            return outerBounds.Contains(innerBounds.min) && outerBounds.Contains(innerBounds.max);
        }

        private void FilterByInnermost(System.Collections.Generic.List<GameObject> result)
        {
            var resultArray = result.ToArray();
            result.Clear();
            for (int i = 0; i < resultArray.Length; ++i)
            {
                var go1 = resultArray[i];
                if (go1 == null) continue;
                bool go1IsInHierarchy = false;
                for (int j = 0; j < resultArray.Length; ++j)
                {
                    if (i == j) continue;
                    if (go1 == null) break;
                    var go2 = resultArray[j];
                    if (go2 == null) continue;
                    if (go1 == go2) continue;
                    if (HierarchyUtils.IsInHierarchy(go1.transform, go2.transform))
                    {
                        go1IsInHierarchy = true;
                        break;
                    }
                }
                if (!go1IsInHierarchy) result.Add(go1);
            }
        }

        private static bool IntersectsRay(Ray ray, Bounds bounds, float radius)
        {
            var boxCenterPlane = new Plane(-ray.direction, bounds.center);
            var sphereCenter = boxCenterPlane.ClosestPointOnPlane(ray.origin);
            var closestPoint = bounds.ClosestPoint(sphereCenter);
            var distance = (closestPoint - sphereCenter).magnitude;
            return distance <= radius;
        }

        private static bool RayIntersectsBoxPlanes(Ray ray, Bounds bounds, float radius, float maxDistance)
        {
            Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            for (int axis = 0; axis < 3; axis++)
            {
                for (int side = 0; side < 2; side++)
                {
                    Vector3 normal = axes[axis] * (side == 0 ? -1f : 1f);
                    float d = side == 0 ? min[axis] : max[axis];

                    Plane plane = new Plane(normal, d);

                    float denom = Vector3.Dot(ray.direction, normal);
                    if (Mathf.Abs(denom) < 1e-6f) continue;

                    float t = (d - Vector3.Dot(ray.origin, normal)) / denom;
                    if (t < 0 || t > maxDistance) continue;

                    Vector3 pointOnRay = ray.origin + ray.direction * t;

                    Vector3 clamped = pointOnRay;
                    for (int j = 0; j < 3; j++)
                    {
                        if (j == axis) clamped[j] = d;
                        else clamped[j] = Mathf.Clamp(clamped[j], min[j], max[j]);
                    }

                    float dist = Vector3.Distance(pointOnRay, clamped);
                    if (dist < radius)
                        return true;
                }
            }
            return false;
        }
        #endregion
    }
}
#pragma warning restore UDR0001
