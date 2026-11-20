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
    [System.Serializable]
    public class OccupiedCell : System.IEquatable<OccupiedCell>
    {
        private const float PRECISION = 10e5f;

        [SerializeField] private Vector3 _center;
        [SerializeField] private Vector3 _size;
        [SerializeField] private Quaternion _rotation;
        [SerializeField] private long _brushId;

        public OccupiedCell(Vector3 center, Vector3 size, Quaternion rotation, long brushId)
        {
            _center = center;
            _size = size;
            _rotation = rotation;
            _brushId = brushId;
        }
        public OccupiedCell(OccupiedCell other)
        {
            _center = other._center;
            _size = other._size;
            _rotation = other._rotation;
            _brushId = other._brushId;
        }
        public Vector3 center => _center;
        public Vector3 size => _size;
        public Quaternion rotation => _rotation;
        public long brushId => _brushId;

        public Bounds GetBounds() => new Bounds(_center, _size);

        public bool Overlaps(Vector3 otherCenter, Vector3 otherSize, Quaternion gridRotation, float tolerance = 0.01f)
        {
            var distance = (otherCenter - _center).magnitude;
            var minDistance = Mathf.Min(_size.x, _size.y, _size.z, otherSize.x, otherSize.y, otherSize.z) * 0.5f * tolerance;
            if (distance < minDistance) return true;

            var halfSize = _size * 0.5f;
            var otherHalfSize = otherSize * 0.5f;

            var invRotation = Quaternion.Inverse(gridRotation);
            var localOtherCenter = invRotation * (otherCenter - _center);

            return Mathf.Abs(localOtherCenter.x) < (halfSize.x + otherHalfSize.x - tolerance) &&
                   Mathf.Abs(localOtherCenter.y) < (halfSize.y + otherHalfSize.y - tolerance) &&
                   Mathf.Abs(localOtherCenter.z) < (halfSize.z + otherHalfSize.z - tolerance);
        }

        private static int RoundToInt(float value) => Mathf.RoundToInt(value * PRECISION);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + RoundToInt(_center.x);
                hash = hash * 31 + RoundToInt(_center.y);
                hash = hash * 31 + RoundToInt(_center.z);
                hash = hash * 31 + RoundToInt(_size.x);
                hash = hash * 31 + RoundToInt(_size.y);
                hash = hash * 31 + RoundToInt(_size.z);
                hash = hash * 31 + RoundToInt(_rotation.x);
                hash = hash * 31 + RoundToInt(_rotation.y);
                hash = hash * 31 + RoundToInt(_rotation.z);
                hash = hash * 31 + RoundToInt(_rotation.w);
                return hash;
            }
        }

        public override bool Equals(object obj) => Equals(obj as OccupiedCell);

        public bool Equals(OccupiedCell other)
        {
            if (other == null) return false;
            return GetHashCode() == other.GetHashCode();
        }
    }
}
#pragma warning restore UDR0001
