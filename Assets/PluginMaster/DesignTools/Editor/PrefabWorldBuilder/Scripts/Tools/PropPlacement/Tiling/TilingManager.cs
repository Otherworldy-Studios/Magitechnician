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
    public class TilingSettings : PaintOnSurfaceToolSettings, IPaintToolSettings
    {
        #region TILING SETTINGS

        [SerializeField] private TilesUtils.SizeType _cellSizeType = TilesUtils.SizeType.SMALLEST_OBJECT;
        [SerializeField] private Vector2 _cellSize = Vector2.one;
        [SerializeField] private Quaternion _rotation = Quaternion.identity;
        [SerializeField] private Vector2 _spacing = Vector2.zero;
        [SerializeField] private AxesUtils.SignedAxis _axisAlignedWithNormal = AxesUtils.SignedAxis.UP;
        [SerializeField] private bool _showPreview = true;
        public Quaternion rotation
        {
            get => _rotation;
            set
            {
                if (_rotation == value) return;
                var prevRotation = _rotation;
                _rotation = value;
                OnDataChanged();
            }
        }

        public TilesUtils.SizeType cellSizeType
        {
            get => _cellSizeType;
            set
            {
                if (_cellSizeType == value) return;
                _cellSizeType = value;
                UpdateCellSize();
            }
        }
        public Vector2 cellSize
        {
            get => _cellSize;
            set
            {
                if (_cellSize == value) return;
                _cellSize = value;
                OnDataChanged();
            }
        }
        public Vector2 spacing
        {
            get => _spacing;
            set
            {
                if (_spacing == value) return;
                _spacing = value;
                OnDataChanged();
            }
        }
        public AxesUtils.SignedAxis axisAlignedWithNormal
        {
            get => _axisAlignedWithNormal;
            set
            {
                if (_axisAlignedWithNormal == value) return;
                _axisAlignedWithNormal = value;
                UpdateCellSize();
                OnDataChanged();
            }
        }
        public bool showPreview
        {
            get => _showPreview;
            set
            {
                if (_showPreview == value) return;
                _showPreview = value;
                OnDataChanged();
            }
        }
        public void UpdateCellSize()
        {
            if (ToolController.current != ToolController.Tool.TILING) return;

            if (_cellSizeType != TilesUtils.SizeType.CUSTOM)
            {
                var toolSettings = TilingManager.settings;
                BrushSettings brush = PaletteManager.selectedBrush;
                if (ToolController.editMode && brush == null) brush = brushSettings;
                else if (toolSettings.overwriteBrushProperties) brush = toolSettings.brushSettings;
                if (brush == null) return;
                AxesUtils.SignedAxis forwardAxis = AxesUtils.SignedAxis.FORWARD;
                if (_axisAlignedWithNormal == AxesUtils.SignedAxis.DOWN) forwardAxis = AxesUtils.SignedAxis.BACK;
                else if (_axisAlignedWithNormal == AxesUtils.SignedAxis.RIGHT) forwardAxis = AxesUtils.SignedAxis.UP;
                else if (_axisAlignedWithNormal == AxesUtils.SignedAxis.LEFT) forwardAxis = AxesUtils.SignedAxis.DOWN;
                else if (_axisAlignedWithNormal == AxesUtils.SignedAxis.FORWARD) forwardAxis = AxesUtils.SignedAxis.RIGHT;
                else if (_axisAlignedWithNormal == AxesUtils.SignedAxis.BACK) forwardAxis = AxesUtils.SignedAxis.LEFT;
                _cellSize = TilesUtils.GetCellSize(_cellSizeType, brush, _axisAlignedWithNormal,
                    forwardAxis, _cellSize, tangentSpace: true, quarterTurns: 0, subtractBrushOffset: false);
                ToolProperties.RepainWindow();
                UnityEditor.SceneView.RepaintAll();
            }
            OnDataChanged();
        }
        #endregion

        #region ON DATA CHANGED
        public TilingSettings() : base()
        {
            _paintTool.OnDataChanged += DataChanged;
            _paintTool.brushSettings.OnDataChangedAction += DataChanged;
        }

        public override void DataChanged()
        {
            base.DataChanged();
            PWBIO.UpdateStroke();
        }
        #endregion

        #region PAINT TOOL
        [SerializeField] private PaintToolSettings _paintTool = new PaintToolSettings();
        public Transform parent { get => _paintTool.parent; set => _paintTool.parent = value; }
        public bool overwritePrefabLayer
        {
            get => _paintTool.overwritePrefabLayer;
            set => _paintTool.overwritePrefabLayer = value;
        }
        public int layer { get => _paintTool.layer; set => _paintTool.layer = value; }
        public bool autoCreateParent { get => _paintTool.autoCreateParent; set => _paintTool.autoCreateParent = value; }
        public bool setSurfaceAsParent { get => _paintTool.setSurfaceAsParent; set => _paintTool.setSurfaceAsParent = value; }
        public bool setLastSelectedAsParent
        {
            get => _paintTool.setLastSelectedAsParent;
            set => _paintTool.setLastSelectedAsParent = value;
        }
        public bool createSubparentPerPalette
        {
            get => _paintTool.createSubparentPerPalette;
            set => _paintTool.createSubparentPerPalette = value;
        }
        public bool createSubparentPerTool
        {
            get => _paintTool.createSubparentPerTool;
            set => _paintTool.createSubparentPerTool = value;
        }
        public bool createSubparentPerBrush
        {
            get => _paintTool.createSubparentPerBrush;
            set => _paintTool.createSubparentPerBrush = value;
        }
        public bool createSubparentPerPrefab
        {
            get => _paintTool.createSubparentPerPrefab;
            set => _paintTool.createSubparentPerPrefab = value;
        }
        public bool overwriteBrushProperties
        {
            get => _paintTool.overwriteBrushProperties;
            set
            {
                _paintTool.overwriteBrushProperties = value;
                OnDataChanged();
            }
        }
        public BrushSettings brushSettings => _paintTool.brushSettings;
        public bool overwriteParentingSettings
        {
            get => _paintTool.overwriteParentingSettings;
            set => _paintTool.overwriteParentingSettings = value;
        }
        public IToolParentingSettings GetParentingSettings() => _paintTool as ToolParentingSettings;
        #endregion

        public override void Copy(IToolSettings other)
        {
            var otherTilingSettings = other as TilingSettings;
            base.Copy(other);
            _paintTool.Copy(otherTilingSettings._paintTool);
            _cellSizeType = otherTilingSettings._cellSizeType;
            _cellSize = otherTilingSettings._cellSize;
            _rotation = otherTilingSettings._rotation;
            _spacing = otherTilingSettings._spacing;
            _axisAlignedWithNormal = otherTilingSettings._axisAlignedWithNormal;

        }

        public TilingSettings Clone()
        {
            var clone = new TilingSettings();
            clone.Copy(this);
            return clone;
        }
    }

    public class TilingToolName : IToolName { public string value => "Tiling"; }

    [System.Serializable]
    public class TilingData : PersistentData<TilingToolName, TilingSettings, ControlPoint>
    {
        [System.NonSerialized]
        private System.Collections.Generic.List<Vector3> _tilingCenters
            = new System.Collections.Generic.List<Vector3>();
        public System.Collections.Generic.List<Vector3> tilingCenters => _tilingCenters;
        public TilingData() : base() { }
        public TilingData((GameObject, int)[] objects, long initialBrushId, TilingData tilingData)
        : base(objects, initialBrushId, tilingData) { }

        
        private static TilingData _instance = null;
        public static TilingData instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TilingData();
                    _instance._settings = TilingManager.settings;
                }
                return _instance;
            }
        }
        protected override void Initialize()
        {
            base.Initialize();
            const int pointCount = 9;
            for (int i = 0; i < pointCount; i++) _controlPoints.Add(new ControlPoint());
            _pointPositions = new Vector3[pointCount];
        }
        public TilingData Clone()
        {
            var clone = new TilingData();
            base.Clone(clone);
            clone._tilingCenters = _tilingCenters.ToList();
            return clone;
        }

        public Vector3 GetCenter() => GetPoint(8);
    }

    [System.Serializable]
    public class TilingSceneData : SceneData<TilingToolName, TilingSettings, ControlPoint, TilingData>
    {
        public TilingSceneData() : base() { }
        public TilingSceneData(string sceneGUID) : base(sceneGUID) { }
    }

    [System.Serializable]
    public class TilingManager : PersistentToolControllerBase<TilingToolName, TilingSettings,
        ControlPoint, TilingData, TilingSceneData, TilingManager>
    { }
}
#pragma warning restore UDR0001
