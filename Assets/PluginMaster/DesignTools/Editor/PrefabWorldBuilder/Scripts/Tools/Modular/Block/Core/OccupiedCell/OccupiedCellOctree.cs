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
    #region OCTREE
    public class OccupiedCellOctree
    {
        private const float MIN_NODE_SIZE = 0.5f;
        private const float INITIAL_SIZE = 100f;
        private const float LOOSENESS = 1.25f;

        private OccupiedCellOctreeNode _rootNode;
        private int _count;

        public int Count => _count;

        public OccupiedCellOctree()
        {
            _rootNode = new OccupiedCellOctreeNode(INITIAL_SIZE, MIN_NODE_SIZE, LOOSENESS, Vector3.zero);
            _count = 0;
        }

        public void Add(OccupiedCell cell)
        {
            var bounds = cell.GetBounds();
            int attempts = 0;
            while (!_rootNode.Add(cell, bounds))
            {
                Grow(bounds.center - _rootNode.Center);
                if (++attempts > 20)
                {
                    Debug.LogError("OccupiedCellOctree: Aborted Add operation after too many grow attempts.");
                    return;
                }
            }
            _count++;
        }

        public bool Remove(OccupiedCell cell)
        {
            var bounds = cell.GetBounds();
            bool removed = _rootNode.Remove(cell, bounds);
            if (removed)
            {
                _count--;
                Shrink();
            }
            return removed;
        }

        public void RemoveAt(Vector3 center, float tolerance = 0.1f)
        {
            var cellsToRemove = new System.Collections.Generic.List<OccupiedCell>();
            var checkBounds = new Bounds(center, Vector3.one * tolerance * 2);
            _rootNode.GetColliding(ref checkBounds, cellsToRemove);

            foreach (var cell in cellsToRemove)
            {
                if ((cell.center - center).magnitude < tolerance)
                {
                    Remove(cell);
                }
            }
        }

        public void Clear()
        {
            _rootNode = new OccupiedCellOctreeNode(INITIAL_SIZE, MIN_NODE_SIZE, LOOSENESS, Vector3.zero);
            _count = 0;
        }

        public bool IsOccupied(Vector3 center, Vector3 size, float tolerance = 0.01f)
        {
            return IsOccupied(center, size, out _, tolerance);
        }

        public bool IsOccupied(Vector3 center, Vector3 size, out long brushId, float tolerance = 0.01f)
        {
            var checkBounds = new Bounds(center, size * 1.1f);
            var candidates = new System.Collections.Generic.List<OccupiedCell>();
            _rootNode.GetColliding(ref checkBounds, candidates);
            brushId = -1;
            foreach (var cell in candidates)
            {
                if (cell.Overlaps(center, size, GridManager.settings.rotation, tolerance))
                {
                    brushId = cell.brushId;
                    return true;
                }
            }
            return false;
        }

        public void GetAll(System.Collections.Generic.List<OccupiedCell> result)
        {
            _rootNode.GetAll(result);
        }

        private void Grow(Vector3 direction)
        {
            int xDir = direction.x >= 0 ? 1 : -1;
            int yDir = direction.y >= 0 ? 1 : -1;
            int zDir = direction.z >= 0 ? 1 : -1;

            var oldRoot = _rootNode;
            float half = oldRoot.BaseLength / 2;
            float newLength = oldRoot.BaseLength * 2;
            var newCenter = oldRoot.Center + new Vector3(xDir * half, yDir * half, zDir * half);

            _rootNode = new OccupiedCellOctreeNode(newLength, MIN_NODE_SIZE, LOOSENESS, newCenter);

            if (oldRoot.HasAnyObjects())
            {
                int rootPos = _rootNode.BestFitChild(oldRoot.Center);
                var children = new OccupiedCellOctreeNode[8];
                for (int i = 0; i < 8; i++)
                {
                    if (i == rootPos)
                    {
                        children[i] = oldRoot;
                    }
                    else
                    {
                        int xd = i % 2 == 0 ? -1 : 1;
                        int yd = i > 3 ? -1 : 1;
                        int zd = (i < 2 || (i > 3 && i < 6)) ? -1 : 1;
                        children[i] = new OccupiedCellOctreeNode(oldRoot.BaseLength, MIN_NODE_SIZE, LOOSENESS,
                            newCenter + new Vector3(xd * half, yd * half, zd * half));
                    }
                }
                _rootNode.SetChildren(children);
            }
        }

        private void Shrink()
        {
            _rootNode = _rootNode.ShrinkIfPossible(INITIAL_SIZE);
        }
    }
    #endregion

    #region OCTREE NODE
    public class OccupiedCellOctreeNode
    {
        private const int NUM_OBJECTS_ALLOWED = 8;

        public Vector3 Center { get; private set; }
        public float BaseLength { get; private set; }

        private float _looseness;
        private float _minSize;
        private float _adjLength;
        private Bounds _bounds;
        private readonly System.Collections.Generic.List<OctreeCell> _cells
            = new System.Collections.Generic.List<OctreeCell>();
        private OccupiedCellOctreeNode[] _children = null;
        private Bounds[] _childBounds;

        private struct OctreeCell
        {
            public OccupiedCell Cell;
            public Bounds Bounds;
        }

        public OccupiedCellOctreeNode(float baseLength, float minSize, float looseness, Vector3 center)
        {
            SetValues(baseLength, minSize, looseness, center);
        }

        public bool Add(OccupiedCell cell, Bounds cellBounds)
        {
            if (!Encapsulates(_bounds, cellBounds))
                return false;
            SubAdd(cell, cellBounds);
            return true;
        }

        public bool Remove(OccupiedCell cell, Bounds cellBounds)
        {
            if (!Encapsulates(_bounds, cellBounds))
                return false;
            return SubRemove(cell, cellBounds);
        }

        public void GetColliding(ref Bounds checkBounds, System.Collections.Generic.List<OccupiedCell> result)
        {
            if (!_bounds.Intersects(checkBounds))
                return;

            foreach (var item in _cells)
            {
                if (item.Bounds.Intersects(checkBounds))
                    result.Add(item.Cell);
            }

            if (_children != null)
            {
                for (int i = 0; i < 8; i++)
                    _children[i].GetColliding(ref checkBounds, result);
            }
        }

        public void GetAll(System.Collections.Generic.List<OccupiedCell> result)
        {
            foreach (var item in _cells)
                result.Add(item.Cell);

            if (_children != null)
            {
                for (int i = 0; i < 8; i++)
                    _children[i].GetAll(result);
            }
        }

        public bool HasAnyObjects()
        {
            if (_cells.Count > 0) return true;
            if (_children != null)
            {
                for (int i = 0; i < 8; i++)
                    if (_children[i].HasAnyObjects()) return true;
            }
            return false;
        }

        public int BestFitChild(Vector3 center)
        {
            return (center.x <= Center.x ? 0 : 1) + (center.y >= Center.y ? 0 : 4) + (center.z <= Center.z ? 0 : 2);
        }

        public void SetChildren(OccupiedCellOctreeNode[] children)
        {
            if (children.Length != 8)
            {
                Debug.LogError("Child octree array must be length 8.");
                return;
            }
            _children = children;
        }

        public OccupiedCellOctreeNode ShrinkIfPossible(float minLength)
        {
            if (BaseLength < (2 * minLength))
                return this;
            if (_cells.Count == 0 && (_children == null || _children.Length == 0))
                return this;

            int bestFit = -1;
            foreach (var item in _cells)
            {
                int newBestFit = BestFitChild(item.Bounds.center);
                if (bestFit < 0)
                {
                    if (Encapsulates(_childBounds[newBestFit], item.Bounds))
                        bestFit = newBestFit;
                    else
                        return this;
                }
                else if (newBestFit != bestFit || !Encapsulates(_childBounds[newBestFit], item.Bounds))
                {
                    return this;
                }
            }

            if (_children != null)
            {
                bool childHadContent = false;
                for (int i = 0; i < _children.Length; i++)
                {
                    if (_children[i].HasAnyObjects())
                    {
                        if (childHadContent) return this;
                        if (bestFit >= 0 && bestFit != i) return this;
                        childHadContent = true;
                        bestFit = i;
                    }
                }
            }

            if (_children == null)
            {
                SetValues(BaseLength / 2, _minSize, _looseness, _childBounds[bestFit].center);
                return this;
            }

            return bestFit == -1 ? this : _children[bestFit];
        }

        private void SetValues(float baseLength, float minSize, float looseness, Vector3 center)
        {
            BaseLength = baseLength;
            _minSize = minSize;
            _looseness = looseness;
            Center = center;
            _adjLength = looseness * baseLength;

            _bounds = new Bounds(Center, new Vector3(_adjLength, _adjLength, _adjLength));

            float quarter = BaseLength / 4f;
            float childActualLength = (BaseLength / 2) * looseness;
            var childActualSize = new Vector3(childActualLength, childActualLength, childActualLength);
            _childBounds = new Bounds[8];
            _childBounds[0] = new Bounds(Center + new Vector3(-quarter, quarter, -quarter), childActualSize);
            _childBounds[1] = new Bounds(Center + new Vector3(quarter, quarter, -quarter), childActualSize);
            _childBounds[2] = new Bounds(Center + new Vector3(-quarter, quarter, quarter), childActualSize);
            _childBounds[3] = new Bounds(Center + new Vector3(quarter, quarter, quarter), childActualSize);
            _childBounds[4] = new Bounds(Center + new Vector3(-quarter, -quarter, -quarter), childActualSize);
            _childBounds[5] = new Bounds(Center + new Vector3(quarter, -quarter, -quarter), childActualSize);
            _childBounds[6] = new Bounds(Center + new Vector3(-quarter, -quarter, quarter), childActualSize);
            _childBounds[7] = new Bounds(Center + new Vector3(quarter, -quarter, quarter), childActualSize);
        }

        private void SubAdd(OccupiedCell cell, Bounds cellBounds)
        {
            if (_children == null)
            {
                if (_cells.Count < NUM_OBJECTS_ALLOWED || (BaseLength / 2) < _minSize)
                {
                    _cells.Add(new OctreeCell { Cell = cell, Bounds = cellBounds });
                    return;
                }

                Split();
                for (int i = _cells.Count - 1; i >= 0; i--)
                {
                    var existing = _cells[i];
                    int bestFit = BestFitChild(existing.Bounds.center);
                    if (Encapsulates(_children[bestFit]._bounds, existing.Bounds))
                    {
                        _children[bestFit].SubAdd(existing.Cell, existing.Bounds);
                        _cells.RemoveAt(i);
                    }
                }
            }

            int best = BestFitChild(cellBounds.center);
            if (Encapsulates(_children[best]._bounds, cellBounds))
            {
                _children[best].SubAdd(cell, cellBounds);
            }
            else
            {
                _cells.Add(new OctreeCell { Cell = cell, Bounds = cellBounds });
            }
        }

        private bool SubRemove(OccupiedCell cell, Bounds cellBounds)
        {
            bool removed = false;
            for (int i = 0; i < _cells.Count; i++)
            {
                if (ReferenceEquals(_cells[i].Cell, cell) || _cells[i].Cell.Equals(cell))
                {
                    _cells.RemoveAt(i);
                    removed = true;
                    break;
                }
            }

            if (!removed && _children != null)
            {
                int bestFit = BestFitChild(cellBounds.center);
                removed = _children[bestFit].SubRemove(cell, cellBounds);
            }

            if (removed && _children != null && ShouldMerge())
                Merge();

            return removed;
        }

        private void Split()
        {
            float quarter = BaseLength / 4f;
            float newLength = BaseLength / 2;
            _children = new OccupiedCellOctreeNode[8];
            _children[0] = new OccupiedCellOctreeNode(newLength, _minSize, _looseness,
                Center + new Vector3(-quarter, quarter, -quarter));
            _children[1] = new OccupiedCellOctreeNode(newLength, _minSize, _looseness,
                Center + new Vector3(quarter, quarter, -quarter));
            _children[2] = new OccupiedCellOctreeNode(newLength, _minSize, _looseness,
                Center + new Vector3(-quarter, quarter, quarter));
            _children[3] = new OccupiedCellOctreeNode(newLength, _minSize, _looseness,
                Center + new Vector3(quarter, quarter, quarter));
            _children[4] = new OccupiedCellOctreeNode(newLength, _minSize, _looseness,
                Center + new Vector3(-quarter, -quarter, -quarter));
            _children[5] = new OccupiedCellOctreeNode(newLength, _minSize, _looseness,
                Center + new Vector3(quarter, -quarter, -quarter));
            _children[6] = new OccupiedCellOctreeNode(newLength, _minSize, _looseness,
                Center + new Vector3(-quarter, -quarter, quarter));
            _children[7] = new OccupiedCellOctreeNode(newLength, _minSize, _looseness,
                Center + new Vector3(quarter, -quarter, quarter));
        }

        private void Merge()
        {
            for (int i = 0; i < 8; i++)
            {
                var child = _children[i];
                foreach (var item in child._cells)
                    _cells.Add(item);
            }
            _children = null;
        }

        private bool ShouldMerge()
        {
            int total = _cells.Count;
            if (_children != null)
            {
                foreach (var child in _children)
                {
                    if (child._children != null) return false;
                    total += child._cells.Count;
                }
            }
            return total <= NUM_OBJECTS_ALLOWED;
        }

        private static bool Encapsulates(Bounds outer, Bounds inner)
        {
            return outer.Contains(inner.min) && outer.Contains(inner.max);
        }
    }
    #endregion
}
#pragma warning restore UDR0001
