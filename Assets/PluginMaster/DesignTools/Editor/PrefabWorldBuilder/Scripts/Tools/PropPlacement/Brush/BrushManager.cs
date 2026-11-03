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
    public class BrushToolSettings : BrushToolBase, IPaintOnSurfaceToolSettings, ISerializationCallbackReceiver
    {
        [SerializeField] private float _maxHeightFromCenter = 2f;
        public enum HeightType { CUSTOM, RADIUS }
        [SerializeField] private HeightType _heightType = HeightType.RADIUS;

        public enum AvoidOverlappingType
        {
            DISABLED,
            WITH_PALETTE_PREFABS,
            WITH_BRUSH_PREFABS,
            WITH_SAME_PREFABS,
            WITH_ALL_OBJECTS
        }
        [SerializeField] private AvoidOverlappingType _avoidOverlapping = AvoidOverlappingType.WITH_ALL_OBJECTS;
        [SerializeField] private float _overlapPadding = 0f;

        [SerializeField] private LayerMask _layerFilter = -1;
        [SerializeField] private System.Collections.Generic.List<string> _tagFilter = null;
        [SerializeField] private RandomUtils.Range _slopeFilter = new RandomUtils.Range(0, 60);
        [SerializeField] private string[] _terrainLayerIds = null;

        [SerializeField] private bool _showPreview = false;
        private TerrainLayer[] _terrainLayerFilter = null;
        private bool _updateTerrainFilter = false;
        private long id = 0;
        public BrushToolSettings() : base()
        {
            id = System.DateTime.Now.Ticks;
            _paintOnSurfaceToolSettings.OnDataChanged += DataChanged;
        }
        #region PAINT ON SURFACE
        [SerializeField] private PaintOnSurfaceToolSettings _paintOnSurfaceToolSettings = new PaintOnSurfaceToolSettings();
        public bool paintOnMeshesWithoutCollider
        {
            get => _paintOnSurfaceToolSettings.paintOnMeshesWithoutCollider;
            set => _paintOnSurfaceToolSettings.paintOnMeshesWithoutCollider = value;
        }
        public bool ignoreSceneColliders
        {
            get => _paintOnSurfaceToolSettings.ignoreSceneColliders;
            set => _paintOnSurfaceToolSettings.ignoreSceneColliders = value;
        }
        public bool paintOnSelectedOnly
        {
            get => _paintOnSurfaceToolSettings.paintOnSelectedOnly;
            set => _paintOnSurfaceToolSettings.paintOnSelectedOnly = value;
        }
        public bool paintOnPalettePrefabs
        {
            get => _paintOnSurfaceToolSettings.paintOnPalettePrefabs;
            set => _paintOnSurfaceToolSettings.paintOnPalettePrefabs = value;
        }
        #endregion
        public bool showPreview
        {
            get => _showPreview;
            set
            {
                if (_showPreview == value) return;
                _showPreview = value;
                DataChanged();
            }
        }

        public float maxHeightFromCenter
        {
            get => _maxHeightFromCenter;
            set
            {
                if (_maxHeightFromCenter == value) return;
                _maxHeightFromCenter = value;
                DataChanged();
            }
        }
        public HeightType heightType
        {
            get => _heightType;
            set
            {
                if (_heightType == value) return;
                _heightType = value;
                DataChanged();
            }
        }
        public AvoidOverlappingType avoidOverlapping
        {
            get => _avoidOverlapping;
            set
            {
                if (_avoidOverlapping == value) return;
                _avoidOverlapping = value;
                DataChanged();
            }
        }

        public float overlapPadding
        {
            get => _overlapPadding;
            set
            {
                value = Mathf.Max(0, value);
                if (_overlapPadding == value) return;
                _overlapPadding = value;
                DataChanged();
            }
        }
        public virtual LayerMask layerFilter
        {
            get => _layerFilter;
            set
            {
                if (_layerFilter == value) return;
                _layerFilter = value;
                DataChanged();
            }
        }
        public virtual System.Collections.Generic.List<string> tagFilter
        {
            get
            {
                if (_tagFilter == null) UpdateTagFilter();
                return _tagFilter;
            }
            set
            {
                if (_tagFilter == value) return;
                _tagFilter = value;
                DataChanged();
            }
        }
        public virtual RandomUtils.Range slopeFilter
        {
            get => _slopeFilter;
            set
            {
                if (_slopeFilter == value) return;
                _slopeFilter = value;
                DataChanged();
            }
        }

        public TerrainLayer[] terrainLayerFilter
        {
            get
            {
                if ((_terrainLayerFilter == null && _terrainLayerIds != null) || _updateTerrainFilter) UpdateTerrainFilter();
                return _terrainLayerFilter;
            }
            set
            {
                if (Equals(_terrainLayerFilter, value)) return;
                if (value == null)
                {
                    _terrainLayerFilter = null;
                    _terrainLayerIds = null;
                    return;
                }
                var layerList = new System.Collections.Generic.List<TerrainLayer>();
                var terrainLayerIds = new System.Collections.Generic.List<string>();
                foreach (var layer in value)
                {
                    layerList.Add(layer);
                    if (layer == null) continue;
                    terrainLayerIds.Add(UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(layer).ToString());
                }
                _terrainLayerFilter = layerList.ToArray();
                _terrainLayerIds = terrainLayerIds.ToArray();
            }
        }
        public override void Copy(IToolSettings other)
        {
            var otherBrushToolSettings = other as BrushToolSettings;
            if (otherBrushToolSettings == null) return;
            base.Copy(other);
            _paintOnSurfaceToolSettings.Copy(otherBrushToolSettings._paintOnSurfaceToolSettings);
            _maxHeightFromCenter = otherBrushToolSettings._maxHeightFromCenter;
            _heightType = otherBrushToolSettings._heightType;
            _avoidOverlapping = otherBrushToolSettings._avoidOverlapping;
            _layerFilter = otherBrushToolSettings._layerFilter;
            _tagFilter = otherBrushToolSettings._tagFilter == null ? null
                : new System.Collections.Generic.List<string>(otherBrushToolSettings._tagFilter);
            _slopeFilter = new RandomUtils.Range(otherBrushToolSettings._slopeFilter);
            _terrainLayerFilter = otherBrushToolSettings._terrainLayerFilter == null ? null
                : otherBrushToolSettings._terrainLayerFilter.ToArray();
            _terrainLayerIds = otherBrushToolSettings._terrainLayerIds == null ? null
                : otherBrushToolSettings._terrainLayerIds.ToArray();
        }

        private void UpdateTagFilter()
        {
            if (_tagFilter != null) return;
            _tagFilter = new System.Collections.Generic.List<string>(UnityEditorInternal.InternalEditorUtility.tags);
        }

        private void UpdateTerrainFilter()
        {
            _updateTerrainFilter = false;
            if (_terrainLayerIds == null) return;
            var terrainLayerList = new System.Collections.Generic.List<TerrainLayer>();
            foreach (var globalId in _terrainLayerIds)
            {
                var layer = ObjectId.FindObject<TerrainLayer>(globalId, ignorePrefabMode: true);
                if (layer == null) continue;
                terrainLayerList.Add(layer);
            }
            _terrainLayerFilter = terrainLayerList.ToArray();
        }
        public void OnBeforeSerialize()
        {
            UpdateTagFilter();
            UpdateTerrainFilter();
        }
        public void OnAfterDeserialize()
        {
            UpdateTagFilter();
            _updateTerrainFilter = true;
        }
    }

    [System.Serializable]
    public class BrushManager : ToolControllerBase<BrushToolSettings, BrushManager> { }
}
#pragma warning restore UDR0001
