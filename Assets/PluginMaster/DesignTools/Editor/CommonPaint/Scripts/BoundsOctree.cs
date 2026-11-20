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
    public class BoundsOctree
    {
        public int Count { get; private set; }
        BoundsOctreeNode rootNode;
        readonly float looseness;
        readonly float initialSize;
        readonly float minSize;

        public BoundsOctree(float initialWorldSize, Vector3 initialWorldPos, float minNodeSize, float loosenessVal)
        {
            if (minNodeSize > initialWorldSize)
            {
                Debug.LogWarning("Minimum node size must be at least as big as the initial world size. Was: "
                    + minNodeSize + " Adjusted to: " + initialWorldSize);
                minNodeSize = initialWorldSize;
            }
            Count = 0;
            initialSize = initialWorldSize;
            minSize = minNodeSize;
            looseness = Mathf.Clamp(loosenessVal, 1.0f, 2.0f);
            rootNode = new BoundsOctreeNode(initialSize, minSize, looseness, initialWorldPos);
        }

        public void Add(GameObject obj, Bounds objBounds)
        {
            int count = 0;
            while (!rootNode.Add(obj, objBounds))
            {
                Grow(objBounds.center - rootNode.Center);
                if (++count > 20)
                {
                    Debug.LogError("Aborted Add operation as it seemed to be going on forever ("
                        + (count - 1) + ") attempts at growing the octree.");
                    return;
                }
            }
            Count++;
        }

        public void Update(GameObject obj, Bounds oldBounds, Bounds newBounds)
        {
            if (Remove(obj, oldBounds)) Add(obj, newBounds);
            else Update(obj, newBounds);
        }

        public void Update(GameObject obj, Bounds newBounds)
        {
            if (Remove(obj)) Add(obj, newBounds);
        }

        public bool Remove(GameObject obj)
        {
            bool removed = rootNode.Remove(obj);

            if (removed)
            {
                Count--;
                Shrink();
            }

            return removed;
        }

        public bool Remove(GameObject obj, Bounds objBounds)
        {
            bool removed = rootNode.Remove(obj, objBounds);

            if (removed)
            {
                Count--;
                Shrink();
            }

            return removed;
        }

        public bool IsColliding(Bounds checkBounds)
        {
            return rootNode.IsColliding(ref checkBounds);
        }

        public bool IsColliding(Ray checkRay, float maxDistance)
        {
            return rootNode.IsColliding(ref checkRay, maxDistance);
        }

        public void GetColliding(System.Collections.Generic.List<GameObject> collidingWith, Bounds checkBounds)
        {
            rootNode.GetColliding(ref checkBounds, collidingWith);
        }

        public void GetColliding(Vector3 center, Vector3 localInnerRadius, Quaternion gridRotation,
            Quaternion objectRotation, System.Collections.Generic.List<GameObject> result)
        {
            rootNode.GetColliding(center, localInnerRadius, gridRotation, objectRotation, result);
        }

        public void GetColliding(System.Collections.Generic.List<GameObject> collidingWith, Ray checkRay,
            float maxDistance = float.PositiveInfinity)
        {
            rootNode.GetColliding(ref checkRay, collidingWith, maxDistance);
        }

        public GameObject[] GetColliding(Ray checkRay, float maxDistance = float.PositiveInfinity)
        {
            var collidingWith = new System.Collections.Generic.List<GameObject>();
            rootNode.GetColliding(ref checkRay, collidingWith, maxDistance);
            return collidingWith.ToArray();
        }

        public System.Collections.Generic.List<GameObject> GetWithinFrustum(Camera cam)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(cam);

            var list = new System.Collections.Generic.List<GameObject>();
            rootNode.GetWithinFrustum(planes, list);
            return list;
        }

        public bool GetCollidingtWithinFrustum(Ray checkRay, float radius,
            Camera cam, out (GameObject, Bounds)[] collidingWith, float maxDistance = float.PositiveInfinity)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(cam);
            var result = new System.Collections.Generic.List<(GameObject, Bounds)>();
            rootNode.GetCollidingWithinFrustum(planes, result, checkRay, radius, cam, maxDistance);
            collidingWith = result.ToArray();
            return collidingWith.Length > 0;
        }

        public Bounds GetMaxBounds()
        {
            return rootNode.GetBounds();
        }

        void Grow(Vector3 direction)
        {
            int xDirection = direction.x >= 0 ? 1 : -1;
            int yDirection = direction.y >= 0 ? 1 : -1;
            int zDirection = direction.z >= 0 ? 1 : -1;
            BoundsOctreeNode oldRoot = rootNode;
            float half = rootNode.BaseLength / 2;
            float newLength = rootNode.BaseLength * 2;
            Vector3 newCenter = rootNode.Center + new Vector3(xDirection * half, yDirection * half, zDirection * half);

            rootNode = new BoundsOctreeNode(newLength, minSize, looseness, newCenter);

            if (oldRoot.HasAnyObjects())
            {
                int rootPos = rootNode.BestFitChild(oldRoot.Center);
                BoundsOctreeNode[] children = new BoundsOctreeNode[8];
                for (int i = 0; i < 8; i++)
                {
                    if (i == rootPos)
                    {
                        children[i] = oldRoot;
                    }
                    else
                    {
                        xDirection = i % 2 == 0 ? -1 : 1;
                        yDirection = i > 3 ? -1 : 1;
                        zDirection = (i < 2 || (i > 3 && i < 6)) ? -1 : 1;
                        children[i] = new BoundsOctreeNode(oldRoot.BaseLength, minSize, looseness,
                            newCenter + new Vector3(xDirection * half, yDirection * half, zDirection * half));
                    }
                }

                rootNode.SetChildren(children);
            }
        }

        void Shrink()
        {
            rootNode = rootNode.ShrinkIfPossible(initialSize);
        }

        public void GetColliding(System.Collections.Generic.List<GameObject> collidingWith, Ray checkRay, float radius,
            float maxDistance)
        {
            rootNode.GetColliding(ref checkRay, collidingWith, radius, maxDistance);
        }
    }
}
#pragma warning restore UDR0001
