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
    [System.Serializable]
    public class ControlPoint
    {
        public Vector3 position = Vector3.zero;
        public ControlPoint() { }

        public ControlPoint(Vector3 position) => this.position = position;
        public ControlPoint(ControlPoint other) => position = other.position;

        public virtual void Copy(ControlPoint other)
        {
            position = other.position;
        }
        public static implicit operator ControlPoint(Vector3 position) => new ControlPoint(position);
        public static implicit operator Vector3(ControlPoint point) => point.position;
        public static Vector3[] PointArrayToVectorArray(ControlPoint[] array)
            => array.Select(point => point.position).ToArray();
        public static ControlPoint[] VectorArrayToPointArray(Vector3[] array)
            => array.Select(position => new ControlPoint(position)).ToArray();
    }

    public partial class PersistentData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT>
        : IPersistentData, ISerializationCallbackReceiver
        where TOOL_NAME : IToolName, new()
        where TOOL_SETTINGS : IToolSettings, new()
        where CONTROL_POINT : ControlPoint, new()
    {
        [SerializeField]
        protected System.Collections.Generic.List<CONTROL_POINT> _controlPoints
            = new System.Collections.Generic.List<CONTROL_POINT>();
        private int _selectedPointIdx = -1;
        protected System.Collections.Generic.List<int> _selection = new System.Collections.Generic.List<int>();
        protected Vector3[] _pointPositions = null;
        
        private static string _commandName = null;
        public const string RESET_COMMAND_NAME = "Reset persistent pose";
        public static string COMMAND_NAME
        {
            get
            {
                if (_commandName == null) _commandName = "Edit " + (new TOOL_NAME()).value;
                return _commandName;
            }
        }
        public Vector3[] points => _pointPositions;
        public int pointsCount => _pointPositions.Length;
        public Vector3 GetPoint(int idx)
        {
            if (idx < 0) idx += _pointPositions.Length;
            return _pointPositions[idx];
        }
        public Vector3 selectedPoint => _pointPositions[_selectedPointIdx];
        public bool ControlPointIsSelected(int idx) => _selection.Contains(idx);
        public CONTROL_POINT[] controlPoints => _controlPoints.ToArray();
        public int selectionCount => _selection.Count;
        public virtual bool SetPoint(int idx, Vector3 value, bool registerUndo, bool selectAll, bool moveSelection = true)
        {
            if (_pointPositions.Length <= 1) Initialize();
            if (idx < 0 || idx >= _pointPositions.Length) return false;
            if (_pointPositions[idx] == value) return false;
            if (registerUndo) ToolProperties.RegisterUndo(COMMAND_NAME);
            var delta = value - _pointPositions[idx];
            _pointPositions[idx] = _controlPoints[idx].position = value;
            var selection = _selection.ToArray();
            if (!moveSelection) return true;
            if (selectAll)
            {
                selection = new int[_controlPoints.Count];
                for (int i = 0; i < selection.Length; ++i) selection[i] = i;
            }
            foreach (var selectedIdx in selection)
            {
                if (selectedIdx == idx) continue;
                _controlPoints[selectedIdx].position += delta;
                _pointPositions[selectedIdx] = _controlPoints[selectedIdx].position;
            }
            return true;
        }

        public void AddDeltaToSelection(Vector3 delta)
        {
            foreach (var selectedIdx in _selection)
            {
                _controlPoints[selectedIdx].position += delta;
                _pointPositions[selectedIdx] = _controlPoints[selectedIdx].position;
            }
        }

        public void AddValue(int idx, Vector3 value)
        {
            ToolProperties.RegisterUndo(COMMAND_NAME);
            _controlPoints[idx].position += value;
            _pointPositions[idx] = _controlPoints[idx].position;
        }

        protected virtual void UpdatePoints(bool deserializing = false)
            => _pointPositions = ControlPoint.PointArrayToVectorArray(_controlPoints.ToArray());

        public void RemoveSelectedPoints()
        {
            if (_selectedPointIdx == -1)
            {
                _selection.Clear();
                return;
            }
            RemovePoints(_selection.ToArray());
        }

        public void RemovePoint(int idx)
        {
            ToolProperties.RegisterUndo(COMMAND_NAME);
            if (_controlPoints.Count <= 2)
            {
                Initialize();
                return;
            }
            _controlPoints.RemoveAt(idx);
            if (_selectedPointIdx == idx) _selectedPointIdx = -1;
            RemoveFromSelection(idx);
            UpdatePoints();
        }
        public void RemovePoints(int[] indexes)
        {
            ToolProperties.RegisterUndo(COMMAND_NAME);
            var toRemove = new System.Collections.Generic.List<int>(indexes);
            toRemove.Sort();
            if (toRemove.Count >= _pointPositions.Length - 1)
            {
                Initialize();
                return;
            }
            for (int i = toRemove.Count - 1; i >= 0; --i) _controlPoints.RemoveAt(toRemove[i]);
            _selectedPointIdx = -1;
            _selection.Clear();
            UpdatePoints();
        }

        public void InsertPoint(int idx, CONTROL_POINT point)
        {
            if (idx < 0) return;
            idx = Mathf.Max(idx, 1);
            ToolProperties.RegisterUndo(COMMAND_NAME);
            _controlPoints.Insert(idx, point);
            UpdatePoints();
        }

        protected void AddPoint(CONTROL_POINT point, bool registerUndo = true)
        {
            if (registerUndo) ToolProperties.RegisterUndo(COMMAND_NAME);
            _controlPoints.Add(point);
            UpdatePoints();
        }
        protected void AddPointRange(System.Collections.Generic.IEnumerable<CONTROL_POINT> collection)
        {
            ToolProperties.RegisterUndo(COMMAND_NAME);
            _controlPoints.AddRange(collection);
            UpdatePoints();
        }
        protected void PointsRemoveRange(int index, int count)
        {
            ToolProperties.RegisterUndo(COMMAND_NAME);
            _controlPoints.RemoveRange(index, count);
            UpdatePoints();
        }
        protected CONTROL_POINT[] PointsGetRange(int index, int count) => _controlPoints.GetRange(index, count).ToArray();
        public int selectedPointIdx
        {
            get
            {
                if (_selectedPointIdx >= _pointPositions.Length) ClearSelection();
                return _selectedPointIdx;
            }
            set
            {
                if (_selectedPointIdx == value) return;
                _selectedPointIdx = value;
            }
        }
        public void AddToSelection(int idx)
        {
            if (!_selection.Contains(idx)) _selection.Add(idx);
        }
        public void SelectAll()
        {
            _selection.Clear();
            for (int i = 0; i < pointsCount; ++i) _selection.Add(i);
            if (_selectedPointIdx < 0) _selectedPointIdx = 0;
        }

        public bool AllPointsAreSelected() => _selection.Count == pointsCount;
        public void RemoveFromSelection(int idx)
        {
            if (_selection.Contains(idx)) _selection.Remove(idx);
        }
        public void ClearSelection()
        {
            _selectedPointIdx = -1;
            _selection.Clear();
            isSelected = false;
        }
        public void Reset() => Initialize();

        public Bounds GetBounds(float sizeMultiplier)
        {
            var max = BoundsUtils.MIN_VECTOR3;
            var min = BoundsUtils.MAX_VECTOR3;
            foreach (var point in _controlPoints)
            {
                max = Vector3.Max(max, point);
                min = Vector3.Min(min, point);
            }
            var size = (max - min);
            var center = size / 2 + min;
            size *= sizeMultiplier;
            return new Bounds(center, size);
        }
    }
}
#pragma warning restore UDR0001
