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
        #region BOUNDS AND INTERSECTIONS
        public Bounds GetBounds()
        {
            return bounds;
        }

        public bool IsColliding(ref Bounds checkBounds)
        {
            if (!bounds.Intersects(checkBounds))
            {
                return false;
            }

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].Bounds.Intersects(checkBounds))
                {
                    return true;
                }
            }

            if (children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (children[i].IsColliding(ref checkBounds))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void GetColliding(ref Bounds checkBounds, System.Collections.Generic.List<GameObject> result)
        {
            if (!bounds.Intersects(checkBounds)) return;

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].Bounds.Intersects(checkBounds))
                    result.Add(objects[i].Obj);
            }

            FilterByInnermost(result);

            if (children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    children[i].GetColliding(ref checkBounds, result);
                }
            }
        }
        #endregion

        #region RAYCASTING
        public bool IsColliding(ref Ray checkRay, float maxDistance = float.PositiveInfinity)
        {
            float distance;
            if (!bounds.IntersectRay(checkRay, out distance) || distance > maxDistance)
            {
                return false;
            }

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].Bounds.IntersectRay(checkRay, out distance) && distance <= maxDistance)
                {
                    return true;
                }
            }

            if (children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (children[i].IsColliding(ref checkRay, maxDistance))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsColliding(ref Ray checkRay, float radius, float maxDistance = float.PositiveInfinity)
        {
            if (!IntersectsRay(checkRay, bounds, radius)) return false;

            for (int i = 0; i < objects.Count; i++)
                if (IntersectsRay(checkRay, objects[i].Bounds, radius)) return true;

            if (children == null) return false;
            for (int i = 0; i < 8; i++)
                if (children[i].IsColliding(ref checkRay, radius, maxDistance)) return true;
            return false;
        }

        public void GetColliding(ref Ray checkRay, System.Collections.Generic.List<GameObject> result,
            float maxDistance = float.PositiveInfinity)
        {
            float distance;
            if (!bounds.IntersectRay(checkRay, out distance) || distance > maxDistance)
            {
                return;
            }

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].Bounds.IntersectRay(checkRay, out distance) && distance <= maxDistance)
                {
                    result.Add(objects[i].Obj);
                }
            }

            FilterByInnermost(result);

            if (children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    children[i].GetColliding(ref checkRay, result, maxDistance);
                }
            }
        }

        public void GetColliding(ref Ray checkRay, float radius, System.Collections.Generic.List<GameObject> result,
            float maxDistance = float.PositiveInfinity)
        {
            if (!IntersectsRay(checkRay, bounds, radius)) return;

            for (int i = 0; i < objects.Count; i++)
                if (IntersectsRay(checkRay, objects[i].Bounds, radius)) result.Add(objects[i].Obj);

            if (children == null) return;
            for (int i = 0; i < 8; i++) children[i].GetColliding(ref checkRay, radius, result, maxDistance);
        }

        public void GetColliding(ref Ray checkRay, System.Collections.Generic.List<GameObject> result,
            float radius, float maxDistance = float.PositiveInfinity)
        {
            float nodeDistance;
            if (!bounds.IntersectRay(checkRay, out nodeDistance) || nodeDistance > maxDistance)
                return;

            for (int i = 0; i < objects.Count; i++)
            {
                var objBounds = objects[i].Bounds;
                if (RayIntersectsBoxPlanes(checkRay, objBounds, radius, maxDistance))
                {
                    result.Add(objects[i].Obj);
                }
            }

            if (children != null)
            {
                for (int i = 0; i < 8; i++)
                    children[i].GetColliding(ref checkRay, result, radius, maxDistance);
            }
        }
        #endregion

        #region FRUSTRUM AND GRID
        public void GetWithinFrustum(Plane[] planes, System.Collections.Generic.List<GameObject> result)
        {
            if (!GeometryUtility.TestPlanesAABB(planes, bounds))
            {
                return;
            }

            for (int i = 0; i < objects.Count; i++)
            {
                if (GeometryUtility.TestPlanesAABB(planes, objects[i].Bounds))
                {
                    result.Add(objects[i].Obj);
                }
            }

            if (children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    children[i].GetWithinFrustum(planes, result);
                }
            }
        }

        public void GetCollidingWithinFrustum(Plane[] planes, System.Collections.Generic.List<GameObject> result,
            Ray checkRay, float radius, Camera cam, float maxDistance = float.PositiveInfinity)
        {
            if (!GeometryUtility.TestPlanesAABB(planes, bounds)) return;

            for (int i = 0; i < objects.Count; i++)
            {
                if (!GeometryUtility.TestPlanesAABB(planes, objects[i].Bounds)) continue;
                if (IntersectsRay(checkRay, objects[i].Bounds, radius)) result.Add(objects[i].Obj);
            }

            if (children == null) return;
            for (int i = 0; i < 8; i++)
                children[i].GetCollidingWithinFrustum(planes, result, checkRay, radius, cam, maxDistance);

        }

        public void GetCollidingWithinFrustum(Plane[] planes, System.Collections.Generic.List<(GameObject, Bounds)> result,
            Ray checkRay, float radius, Camera cam, float maxDistance = float.PositiveInfinity)
        {
            if (!GeometryUtility.TestPlanesAABB(planes, bounds)) return;

            for (int i = 0; i < objects.Count; i++)
            {
                if (!GeometryUtility.TestPlanesAABB(planes, objects[i].Bounds)) continue;
                if (IntersectsRay(checkRay, objects[i].Bounds, radius)) result.Add((objects[i].Obj, objects[i].Bounds));
            }

            if (children == null) return;
            for (int i = 0; i < 8; i++)
                children[i].GetCollidingWithinFrustum(planes, result, checkRay, radius, cam, maxDistance);

        }

        public void GetColliding(Vector3 center, Vector3 localInnerRadius,
            Quaternion gridRotation, Quaternion objectRotation, System.Collections.Generic.List<GameObject> result)
        {
            var checkSize = localInnerRadius * 1.9999f;
            var checkBounds = new Bounds(center, checkSize);
            if (!bounds.Intersects(checkBounds))
            {
                return;
            }
            var nullObjectsIndexes = new System.Collections.Generic.List<int>();
            for (int i = 0; i < objects.Count; i++)
            {
                var octreeObj = objects[i];
                if (octreeObj.Obj == null)
                {
                    nullObjectsIndexes.Insert(0, i);
                    continue;
                }
                var objCenter = octreeObj.Bounds.center;

                var fromTargetToObj = objCenter - center;
                var rotatedCellCenter = center + Quaternion.Inverse(gridRotation) * fromTargetToObj;
                var rotatedBounds = new Bounds(rotatedCellCenter, octreeObj.Bounds.size);
                if (rotatedBounds.Intersects(checkBounds))
                {
                    result.Add(objects[i].Obj);
                }
            }

            foreach (var i in nullObjectsIndexes) objects.RemoveAt(i);

            if (children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    children[i].GetColliding(center, localInnerRadius, gridRotation, objectRotation, result);
                }
            }
        }
        #endregion
    }
}
#pragma warning restore UDR0001
