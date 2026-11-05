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
using System.Linq;

namespace PluginMaster
{
    [System.Serializable]
    public struct GridOrigin
    {
        [SerializeField] private string _name;
        [SerializeField] private Pose _pose;
        public GridOrigin(string name, Pose point)
        {
            _name = name;
            _pose = point;
        }
        public string name { get => _name; set => _name = value; }
        public Vector3 position { get => _pose.position; set => _pose.position = value; }
        public Quaternion rotation { get => _pose.rotation; set => _pose.rotation = value; }
        public Pose pose { get => _pose; set => _pose = value; }
    }
    [System.Serializable]
    public class GridSettings : ISerializationCallbackReceiver
    {
        [System.Serializable]
        private struct Bool3
        {
            public bool x, y, z;
            public Bool3(bool x = true, bool y = false, bool z = true) => (this.x, this.y, this.z) = (x, y, z);
        }
        [SerializeField] private bool _snappingEnabled = false;
        [SerializeField] private Bool3 _snappingOn = new Bool3();
        [SerializeField] private bool _visibleGrid = false;
        [SerializeField] private Bool3 _gridOn = new Bool3(false, true, false);
        [SerializeField] private bool _lockedGrid = false;
        [SerializeField] private bool _boundsSnapping = false;
        [SerializeField] private Vector3 _step = Vector3.one;
        [SerializeField] private Vector3 _origin = Vector3.zero;
        [SerializeField] private Quaternion _rotation = Quaternion.identity;
        [SerializeField] private bool _showPositionHandle = true;
        [SerializeField] private bool _showRotationHandle = false;
        [SerializeField] private bool _showScaleHandle = false;
        [SerializeField] private bool _radialGridEnabled = false;
        [SerializeField] private float _radialStep = 1f;
        [SerializeField] private int _radialSectors = 8;
        [SerializeField] private bool _snapToRadius = true;
        [SerializeField] private bool _snapToCircunference = true;
        [SerializeField] private Vector3Int _majorLinesGap = Vector3Int.one * 10;
        [SerializeField] private bool _midpointSnapping = false;
        [SerializeField] private GridOrigin[] _origins = null;
        [SerializeField] private bool _drawGridAsTexture = true;
        [SerializeField] private bool _autoCameraAlignment = true;
        private const string DEFAULT_ORIGIN_NAME = "Default";
        [SerializeField] private string _selectedOrigin = DEFAULT_ORIGIN_NAME;
        private System.Collections.Generic.Dictionary<string, Pose> _originsDictionary
            = new System.Collections.Generic.Dictionary<string, Pose>() { { DEFAULT_ORIGIN_NAME, Pose.identity } };

        public System.Action OnGridOriginChange;
        public System.Action OnDataChanged;
        public System.Action OnGridOrientationChange;
        public void DataChanged(bool repaint = true, bool forceSave = true)
        {
            if (!repaint)
            {
                PWBCore.staticData.SetSavePending();
                return;
            }
            if (forceSave) PWBCore.SetSavePending();
            if (OnDataChanged != null) OnDataChanged();
            UnityEditor.SceneView.RepaintAll();
        }
        public Vector3 step
        {
            get => _step;
            set
            {
                value = Vector3.Max(value, Vector3.one * 0.1f);
                if (_step == value) return;
                _step = value;
                DataChanged(false);
            }
        }

        public bool snappingEnabled
        {
            get => _snappingEnabled;
            set
            {
                if (_snappingEnabled == value) return;
                _snappingEnabled = value;
                if (_snappingEnabled) visibleGrid = true;
                DataChanged();
            }
        }
        public bool snappingOnX
        {
            get => _snappingOn.x;
            set
            {
                if (_snappingOn.x == value) return;
                _snappingOn.x = value;
                DataChanged();
            }
        }
        public bool snappingOnY
        {
            get => _snappingOn.y;
            set
            {
                if (_snappingOn.y == value) return;
                _snappingOn.y = value;
                DataChanged();
            }
        }
        public bool snappingOnZ
        {
            get => _snappingOn.z;
            set
            {
                if (_snappingOn.z == value) return;
                _snappingOn.z = value;
                DataChanged();
            }
        }

        public Vector3 origin
        {
            get => _origin;
            set
            {
                if (_origin == value) return;
                _origin = value;
                DataChanged(false);
                if (OnGridOriginChange != null) OnGridOriginChange();
            }
        }
        public bool lockedGrid
        {
            get => _lockedGrid;
            set
            {
                if (_lockedGrid == value) return;
                _lockedGrid = value;
                DataChanged();
            }
        }
        public bool visibleGrid
        {
            get => _visibleGrid;
            set
            {
                if (_visibleGrid == value) return;
                _visibleGrid = value;
                DataChanged();
            }
        }
        public bool gridOnX
        {
            get => _gridOn.x;
            set
            {
                if (_gridOn.x == value) return;
                _gridOn.x = value;
                if (value)
                {
                    _gridOn.y = _gridOn.z = false;
                    _snappingOn.x = false;
                    _snappingOn.y = _snappingOn.z = true;
                    if (OnGridOrientationChange != null) OnGridOrientationChange();
                }
                DataChanged();
            }
        }
        public bool gridOnY
        {
            get => _gridOn.y;
            set
            {
                if (_gridOn.y == value) return;
                _gridOn.y = value;
                if (value)
                {
                    _gridOn.x = _gridOn.z = false;
                    _snappingOn.y = false;
                    _snappingOn.x = _snappingOn.z = true;
                    if (OnGridOrientationChange != null) OnGridOrientationChange();
                }
                DataChanged();
            }
        }
        public bool gridOnZ
        {
            get => _gridOn.z;
            set
            {
                if (_gridOn.z == value) return;
                _gridOn.z = value;
                if (value)
                {
                    _gridOn.x = _gridOn.y = false;
                    _snappingOn.z = false;
                    _snappingOn.y = _snappingOn.x = true;
                    if (OnGridOrientationChange != null) OnGridOrientationChange();
                }
                DataChanged();
            }
        }

        public bool boundsSnapping
        {
            get => _boundsSnapping;
            set
            {
                if (_boundsSnapping == value) return;
                _boundsSnapping = value;
                DataChanged();
            }
        }

        public AxesUtils.Axis gridAxis => gridOnX ? AxesUtils.Axis.X : (gridOnY ? AxesUtils.Axis.Y : AxesUtils.Axis.Z);
        public Quaternion rotation
        {
            get => _rotation;
            set
            {
                if (_rotation == value) return;
                _rotation = value;
                DataChanged(false);
                if (OnGridOriginChange != null) OnGridOriginChange();
            }
        }
        public bool showPositionHandle
        {
            get => _showPositionHandle;
            set
            {
                if (_showPositionHandle == value) return;
                _showPositionHandle = value;
                if (_showPositionHandle)
                {
                    _showRotationHandle = false;
                    _showScaleHandle = false;
                }
                GridManager.FrameGridOrigin();
                DataChanged();
            }
        }
        public bool showRotationHandle
        {
            get => _showRotationHandle;
            set
            {
                if (_showRotationHandle == value) return;
                _showRotationHandle = value;
                if (_showRotationHandle)
                {
                    _showPositionHandle = false;
                    _showScaleHandle = false;
                    GridManager.FrameGridOrigin();
                }
                DataChanged();
            }
        }
        public bool showScaleHandle
        {
            get => _showScaleHandle;
            set
            {
                if (_showScaleHandle == value) return;
                _showScaleHandle = value;
                if (_showScaleHandle)
                {
                    _showPositionHandle = false;
                    _showRotationHandle = false;
                    GridManager.FrameGridOrigin();
                }
                DataChanged();
            }
        }
        public bool radialGridEnabled
        {
            get => _radialGridEnabled;
            set
            {
                if (_radialGridEnabled == value) return;
                _radialGridEnabled = value;
                DataChanged();
            }
        }
        public float radialStep
        {
            get => _radialStep;
            set
            {
                value = Mathf.Max(value, 0.1f);
                if (_radialStep == value) return;
                _radialStep = value;
                DataChanged();
            }
        }
        public int radialSectors
        {
            get => _radialSectors;
            set
            {
                value = Mathf.Max(value, 3);
                if (_radialSectors == value) return;
                _radialSectors = value;
                DataChanged();
            }
        }
        public bool snapToRadius
        {
            get => _snapToRadius;
            set
            {
                if (_snapToRadius == value) return;
                _snapToRadius = value;
                DataChanged();
            }
        }
        public bool snapToCircunference
        {
            get => _snapToCircunference;
            set
            {
                if (_snapToCircunference == value) return;
                _snapToCircunference = value;
            }
        }

        public Vector3Int majorLinesGap
        {
            get => _majorLinesGap;
            set
            {
                value = Vector3Int.Max(value, Vector3Int.one);
                if (_majorLinesGap == value) return;
                _majorLinesGap = value;
                DataChanged();
            }
        }

        public bool midpointSnapping
        {
            get => _midpointSnapping;
            set
            {
                if (_midpointSnapping == value) return;
                _midpointSnapping = value;
                DataChanged();
            }
        }
        #region ORIGINS
        public string selectedOrigin
        {
            get => _selectedOrigin;
            set
            {
                if (_selectedOrigin == value) return;
                _selectedOrigin = value;
                _origin = _originsDictionary[_selectedOrigin].position;
                _rotation = _originsDictionary[_selectedOrigin].rotation;
                DataChanged();
                if (OnGridOriginChange != null) OnGridOriginChange();
            }
        }

        public bool drawGridAsTexture
        {
            get => _drawGridAsTexture;
            set
            {
                if (_drawGridAsTexture == value) return;
                _drawGridAsTexture = value;
                DataChanged();
            }
        }

        public bool autoCameraAlignment
        {
            get => _autoCameraAlignment;
            set
            {
                if (_autoCameraAlignment == value) return;
                _autoCameraAlignment = value;
                DataChanged();
            }
        }
        public void SaveGridOrigin(string name)
        {
            if (_originsDictionary.ContainsKey(name)) _originsDictionary[name] = new Pose(origin, rotation);
            else _originsDictionary.Add(name, new Pose(origin, rotation));
            _selectedOrigin = name;
            DataChanged();
        }
        public bool OriginsDictionaryContains(string name) => _originsDictionary.ContainsKey(name);
        public Pose GetOrigin(string name) => _originsDictionary[name];
        public string[] GetOriginNames() => _originsDictionary.Keys.ToArray();
        public void DeleteSelectedOrigin()
        {
            _originsDictionary.Remove(_selectedOrigin);
            selectedOrigin = DEFAULT_ORIGIN_NAME;
        }
        public int GetIndexOfOrigin(string name) => _originsDictionary.Keys.Select((key, index) => new { key, index })
            .FirstOrDefault(pair => pair.key == name)?.index ?? -1;
        public int GetIndexOfSelectedOrigin() => GetIndexOfOrigin(selectedOrigin);
        public string GetOriginAt(int index) => _originsDictionary.Keys.ElementAt(index);
        public void SelectOrigin(int index) => selectedOrigin = GetOriginAt(index);
        public void SetNextOrigin()
        {
            var selectedOriginIdx = GetIndexOfSelectedOrigin();
            if (selectedOriginIdx < _originsDictionary.Count - 1) ++selectedOriginIdx;
            else selectedOriginIdx = 0;
            SelectOrigin(selectedOriginIdx);
        }
        public void ResetOrigin()
        {
            _origin = _originsDictionary[_selectedOrigin].position;
            _rotation = _originsDictionary[_selectedOrigin].rotation;
            DataChanged();
            if (OnGridOriginChange != null) OnGridOriginChange();
        }
        #endregion
        public void SetOriginHeight(Vector3 point, AxesUtils.Axis axis)
        {
            var originPos = origin;
            AxesUtils.SetAxisValue(ref originPos, axis, AxesUtils.GetAxisValue(point, axis));
            origin = originPos;
        }

        public bool IsSnappingEnabledInThisDirection(Vector3 direction)
        {
            bool isParallel(Vector3 other)
                => Vector3.Cross(direction, other).magnitude < 0.0000001;
            if (isParallel(_rotation * Vector3.up) && _snappingOn.y) return true;
            if (isParallel(_rotation * Vector3.right) && _snappingOn.x) return true;
            if (isParallel(_rotation * Vector3.forward) && _snappingOn.z) return true;
            return false;
        }

        public Vector3 TransformToGridDirection(Vector3 direction)
        {
            if (direction == Vector3.zero) return _rotation * Vector3.up;
            var xProjection = Vector3.Project(direction, _rotation * Vector3.right);
            var yProjection = Vector3.Project(direction, _rotation * Vector3.up);
            var zProjection = Vector3.Project(direction, _rotation * Vector3.forward);
            var xProjectionMagnitude = xProjection.magnitude;
            var yProjectionMagnitude = yProjection.magnitude;
            var zProjectionMagnitude = zProjection.magnitude;
            var max = Mathf.Max(xProjectionMagnitude, yProjectionMagnitude, zProjectionMagnitude);
            if (xProjectionMagnitude == max) return xProjection.normalized;
            if (yProjectionMagnitude == max) return yProjection.normalized;
            return zProjection.normalized;
        }

        public void OnBeforeSerialize()
        {
            _origins = _originsDictionary.Select(pair => new GridOrigin(pair.Key, pair.Value)).ToArray();
        }

        public void OnAfterDeserialize()
        {
            if (_origins == null || _origins.Length == 0) return;
            _originsDictionary = _origins.ToDictionary(origin => origin.name, origin => origin.pose);
        }
    }
}
#pragma warning restore UDR0001
