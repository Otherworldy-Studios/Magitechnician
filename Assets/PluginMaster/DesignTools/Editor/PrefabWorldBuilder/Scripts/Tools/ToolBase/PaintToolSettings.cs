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
    public interface IToolSettings
    {
        void DataChanged();
        void Copy(IToolSettings other);
    }

    public interface IToolParentingSettings
    {
        bool autoCreateParent { get; set; }
        bool setSurfaceAsParent { get; set; }
        bool setLastSelectedAsParent { get; set; }

        bool createSubparentPerPalette { get; set; }
        bool createSubparentPerTool { get; set; }
        bool createSubparentPerBrush { get; set; }
        bool createSubparentPerPrefab { get; set; }
        Transform parent { get; set; }
    }

    public interface IPaintToolSettings
    {
        bool autoCreateParent { get; set; }
        bool setSurfaceAsParent { get; set; }
        bool setLastSelectedAsParent { get; set; }

        bool createSubparentPerPalette { get; set; }
        bool createSubparentPerTool { get; set; }
        bool createSubparentPerBrush { get; set; }
        bool createSubparentPerPrefab { get; set; }
        Transform parent { get; set; }
        bool overwriteParentingSettings { get; set; }

        bool overwritePrefabLayer { get; set; }
        int layer { get; set; }
        bool overwriteBrushProperties { get; set; }
        BrushSettings brushSettings { get; }
        IToolParentingSettings GetParentingSettings();
    }

    [System.Serializable]
    public class ToolParentingSettings : IToolParentingSettings, ISerializationCallbackReceiver, IToolSettings
    {
        private Transform _parent = null;
        [SerializeField] private string _parentGlobalId = null;
        [SerializeField] private bool _autoCreateParent = true;
        [SerializeField] private bool _setSurfaceAsParent = false;
        [SerializeField] private bool _setLastSelectedAsParent = false;
        [SerializeField] private bool _createSubparentPerPalette = true;
        [SerializeField] private bool _createSubparentPerTool = true;
        [SerializeField] private bool _createSubparentPerBrush = false;
        [SerializeField] private bool _createSubparentPerPrefab = false;

        public System.Action OnDataChanged;

        public ToolParentingSettings()
        {
            OnDataChanged += DataChanged;
        }

        public Transform parent
        {
            get
            {
                if (_parent == null && _parentGlobalId != null)
                {
                    var obj = ObjectId.FindObject<GameObject>(_parentGlobalId, ignorePrefabMode: true);
                    if (obj == null) _parentGlobalId = null;
                    else _parent = obj.transform;
                }
                return _parent;
            }
            set
            {
                if (_parent == value) return;
                _parent = value;
                _parentGlobalId = _parent == null ? null
                    : UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(_parent.gameObject).ToString();
                OnDataChanged();
            }
        }
        public bool autoCreateParent
        {
            get => _autoCreateParent;
            set
            {
                if (_autoCreateParent == value) return;
                _autoCreateParent = value;
                OnDataChanged();
            }
        }

        public bool setSurfaceAsParent
        {
            get => _setSurfaceAsParent;
            set
            {
                if (_setSurfaceAsParent == value) return;
                _setSurfaceAsParent = value;
                OnDataChanged();
            }
        }

        public bool setLastSelectedAsParent
        {
            get => _setLastSelectedAsParent;
            set
            {
                if (_setLastSelectedAsParent == value) return;
                _setLastSelectedAsParent = value;
                OnDataChanged();
            }
        }

        public bool createSubparentPerPalette
        {
            get => _createSubparentPerPalette;
            set
            {
                if (_createSubparentPerPalette == value) return;
                _createSubparentPerPalette = value;
                OnDataChanged();
            }
        }
        public bool createSubparentPerTool
        {
            get => _createSubparentPerTool;
            set
            {
                if (_createSubparentPerTool == value) return;
                _createSubparentPerTool = value;
                OnDataChanged();
            }
        }
        public bool createSubparentPerBrush
        {
            get => _createSubparentPerBrush;
            set
            {
                if (_createSubparentPerBrush == value) return;
                _createSubparentPerBrush = value;
                OnDataChanged();
            }
        }

        public bool createSubparentPerPrefab
        {
            get => _createSubparentPerPrefab;
            set
            {
                if (_createSubparentPerPrefab == value) return;
                _createSubparentPerPrefab = value;
                OnDataChanged();
            }
        }

        public virtual void DataChanged() => PWBCore.SetSavePending();

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize() => _parent = null;

        public virtual void Copy(IToolSettings other)
        {
            var otherPaintToolSettings = other as ToolParentingSettings;
            if (otherPaintToolSettings == null) return;

            _autoCreateParent = otherPaintToolSettings._autoCreateParent;
            _setSurfaceAsParent = otherPaintToolSettings._setSurfaceAsParent;
            _createSubparentPerPalette = otherPaintToolSettings._createSubparentPerPalette;
            _createSubparentPerTool = otherPaintToolSettings._createSubparentPerTool;
            _createSubparentPerBrush = otherPaintToolSettings._createSubparentPerBrush;
            _createSubparentPerPrefab = otherPaintToolSettings._createSubparentPerPrefab;
        }
    }

    [System.Serializable]
    public class PaintToolSettings : ToolParentingSettings, IPaintToolSettings
    {
        [SerializeField] private bool _overwritePrefabLayer = false;
        [SerializeField] private int _layer = 0;
        [SerializeField] private bool _overwriteBrushProperties = false;
        [SerializeField] private BrushSettings _brushSettings = new BrushSettings();
        [SerializeField] private bool _overwriteParentingSettings = false;

        public PaintToolSettings()
        {
            _brushSettings.OnDataChangedAction += DataChanged;
        }

        public bool overwritePrefabLayer
        {
            get => _overwritePrefabLayer;
            set
            {
                if (_overwritePrefabLayer == value) return;
                _overwritePrefabLayer = value;
                OnDataChanged();
            }
        }

        public int layer
        {
            get => _layer;
            set
            {
                if (_layer == value) return;
                _layer = value;
                OnDataChanged();
            }
        }

        public bool overwriteBrushProperties
        {
            get => _overwriteBrushProperties;
            set
            {
                if (_overwriteBrushProperties == value) return;
                _overwriteBrushProperties = value;
                OnDataChanged();
            }
        }

        public BrushSettings brushSettings => _brushSettings;

        public bool overwriteParentingSettings
        {
            get => _overwriteParentingSettings;
            set
            {
                if (_overwriteParentingSettings == value) return;
                _overwriteParentingSettings = value;
                OnDataChanged();
            }
        }

        public override void Copy(IToolSettings other)
        {
            var otherPaintToolSettings = other as PaintToolSettings;
            if (otherPaintToolSettings == null) return;
            base.Copy(otherPaintToolSettings);
            _overwritePrefabLayer = otherPaintToolSettings._overwritePrefabLayer;
            _layer = otherPaintToolSettings._layer;
            _overwriteBrushProperties = otherPaintToolSettings._overwriteBrushProperties;
            _brushSettings.Copy(otherPaintToolSettings._brushSettings);
            _overwriteParentingSettings = otherPaintToolSettings._overwriteParentingSettings;
        }

        public IToolParentingSettings GetParentingSettings() => this as ToolParentingSettings;
    }
}
#pragma warning restore UDR0001
