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
    public interface IToolName { string value { get; } }
    public interface IPersistentData
    {
        long id { get; }
        string name { get; set; }
        void Rename(string newName, bool renameParentObject);
        public enum Visibility
        {
            SHOW_ALL,
            SHOW_OBJECTS,
            HIDE_ALL,
        }
        Visibility visibility { get; set; }
        void ToggleVisibility();
        GameObject[] objects { get; }
        bool isSelected { get; set; }
        void ToggleSelection();
        void ClearSelection();
        void SelectAll();
        bool AllPointsAreSelected();
        Bounds GetBounds(float sizeMultiplier);
        GameObject GetParent();
        bool ControlPointIsSelected(int idx);
        string toolName { get; }
    }
    [System.Serializable]
    public partial class PersistentData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT>
        : IPersistentData, ISerializationCallbackReceiver
        where TOOL_NAME : IToolName, new()
        where TOOL_SETTINGS : IToolSettings, new()
        where CONTROL_POINT : ControlPoint, new()
    {
        #region ID & NAME
        
        private static long _nextId = System.DateTime.Now.Ticks;
        [SerializeField] protected long _id = _nextId;
        public static long nextId => _nextId;
        public static string HexId(long value) => (new TOOL_NAME()).value + "_" + value.ToString("X");
        public static string nextHexId => HexId(_nextId);
        public static void SetNextId() => _nextId = System.DateTime.Now.Ticks;
        public long id => _id;
        public string hexId => HexId(id);

        [SerializeField] private string _name = string.Empty;
        public string name
        {
            get
            {
                if (string.IsNullOrEmpty(_name)) _name = hexId;
                return _name;
            }
            set
            {
                if (_name == value) return;
                _name = value;
                PWBCore.SetSavePending();
            }
        }

        public void Rename(string newName, bool renameParentObject)
        {
            var oldName = name;
            if (oldName == newName) return;
            name = newName;
            if (!renameParentObject) return;
            var parent = GetParent().transform;
            if (parent == null) return;
            do
            {
                if (parent.name == oldName)
                {
                    parent.name = newName;
                    return;
                }
                parent = parent.transform.parent;
            }
            while (parent != null);
        }
        #endregion
        #region VISIBILITY
        [SerializeField] private IPersistentData.Visibility _visibility = IPersistentData.Visibility.SHOW_ALL;
        public IPersistentData.Visibility visibility
        {
            get => _visibility;
            set
            {
                if (_visibility == value) return;
                _visibility = value;
                PWBCore.SetSavePending();
            }
        }
        public void ToggleVisibility()
        {
            switch (visibility)
            {
                case IPersistentData.Visibility.SHOW_ALL: _visibility = IPersistentData.Visibility.SHOW_OBJECTS; break;
                case IPersistentData.Visibility.SHOW_OBJECTS: _visibility = IPersistentData.Visibility.HIDE_ALL; break;
                case IPersistentData.Visibility.HIDE_ALL: _visibility = IPersistentData.Visibility.SHOW_ALL; break;
            }
            PWBCore.SetSavePending();
        }
        #endregion
        #region SELECTION
        public bool isSelected { get; set; }
        public virtual void ToggleSelection() => isSelected = !isSelected;
        #endregion
        #region SETTINGS
        [SerializeField] protected TOOL_SETTINGS _settings = new TOOL_SETTINGS();
        public TOOL_SETTINGS settings { get => _settings; set => _settings = value; }
        #endregion
        #region STATE
        [SerializeField] private ToolController.ToolState _state = ToolController.ToolState.NONE;
        public virtual ToolController.ToolState state
        {
            get => _state;
            set
            {
                if (_state == value) return;
                ToolProperties.RegisterUndo(COMMAND_NAME);
                _state = value;
            }
        }
        #endregion
        #region COMMON
        public string toolName => (new TOOL_NAME()).value;
        protected virtual void Initialize()
        {
            _selectedPointIdx = -1;
            _selection.Clear();
            _state = ToolController.ToolState.NONE;
            _controlPoints.Clear();
            UpdatePoints();
        }

        [SerializeField] protected long _initialBrushId = -1;

        public PersistentData() => Initialize();
        public PersistentData((GameObject, int)[] objects, long initialBrushId,
            PersistentData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT> data)
        {
            Copy(data);
            _name = data.name;
            _settings = new TOOL_SETTINGS();
            _settings.Copy(data._settings);
            _id = nextId;
            SetNextId();
            _initialBrushId = initialBrushId;
            _selectedPointIdx = -1;
            _selection.Clear();
            _state = ToolController.ToolState.PERSISTENT;
            if (objects == null || objects.Length == 0) return;
            _poses = new System.Collections.Generic.List<ObjectPose>();
            _objectIds = new System.Collections.Generic.List<ObjectId>();
            _objects = new System.Collections.Generic.List<GameObject>();
            AddObjects(objects);
        }

        public long initialBrushId => _initialBrushId;
        public void SetInitialBrushId(long value) => _initialBrushId = value;

        protected void Clone(PersistentData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT> clone)
        {
            if (clone == null) clone = new PersistentData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT>();
            clone._id = id;
            clone._controlPoints.Clear();
            foreach (var point in _controlPoints)
            {
                var pointClone = new CONTROL_POINT();
                pointClone.Copy(point);
                clone._controlPoints.Add(pointClone);
            }
            clone._pointPositions = _pointPositions == null ? null : _pointPositions.ToArray();
            clone._poses = _poses.ToList();
            clone._objectIds = _objectIds.ToList();
            clone._objects = _objects.ToList();
            clone._initialBrushId = _initialBrushId;
            clone.settings.Copy(_settings);
            clone._selectedPointIdx = -1;
            clone._selection.Clear();
        }
        public virtual void Copy(PersistentData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT> other)
        {
            _controlPoints.Clear();
            foreach (var point in other._controlPoints)
            {
                var pointClone = new CONTROL_POINT();
                pointClone.Copy(point);
                _controlPoints.Add(pointClone);
            }
            _selectedPointIdx = other._selectedPointIdx;
            _selection = other._selection.ToList();
            _pointPositions = other._pointPositions == null ? null : other._pointPositions.ToArray();

            _settings = other._settings;
            _poses = _poses.ToList();
            _objectIds = _objectIds.ToList();
            _objects = _objects.ToList();
            _initialBrushId = other._initialBrushId;
        }

        public virtual void Duplicate(PersistentData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT> other)
        {
            _controlPoints.Clear();
            foreach (var point in other._controlPoints)
            {
                var pointClone = new CONTROL_POINT();
                pointClone.Copy(point);
                _controlPoints.Add(pointClone);
            }
            _selectedPointIdx = other._selectedPointIdx;
            _selection = other._selection.ToList();
            _pointPositions = other._pointPositions == null ? null : other._pointPositions.ToArray();

            _settings = other._settings;
            _initialBrushId = other._initialBrushId;

            foreach (var obj in other._objects)
            {
                GameObject clone = null;
                var prefabName = obj.name;
                var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(obj);
                if (prefab == null)
                {
                    clone = GameObject.Instantiate(obj);
                    prefabName = obj.name;
                }
                else
                {
                    clone = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
                    prefabName = prefab.name;
                }
                clone.transform.position = obj.transform.position;
                clone.transform.rotation = obj.transform.rotation;
                clone.transform.localScale = obj.transform.lossyScale;
                clone.name = prefabName;

                Transform surface = obj.transform.parent;
                while (surface != null)
                {
                    var compCount = surface.gameObject.GetComponents<Component>().Length;
                    if (compCount == 1) surface = surface.parent;
                    else break;
                }

                var settings = other.settings as IPaintToolSettings;
                var parent = PWBIO.GetParent(settings, prefabName,
                    create: true, surface, hexId);

                var commandName = "Duplicate item";
                UnityEditor.Undo.RegisterCreatedObjectUndo(obj, commandName);
                UnityEditor.Undo.SetTransformParent(clone.transform, parent, commandName);


                AddPose(new ObjectId(clone), new ObjectPose(clone));
                PWBIO.AddObjectToOctree(clone);
            }
        }

        private bool _deserializing = false;
        protected bool deserializing { get => _deserializing; set => _deserializing = value; }
        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize()
        {
            deserializing = true;
            deserializing = false;
            UpdatePoints(deserializing: true);
            PWBIO.repaint = true;
        }
        #endregion
    }
}
#pragma warning restore UDR0001
