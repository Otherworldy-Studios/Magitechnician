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
    public class ModularToolBase : IToolSettings, IPaintToolSettings
    {
        [SerializeField] private TilesUtils.SizeType _moduleSizeType = TilesUtils.SizeType.BIGGEST_OBJECT;
        [SerializeField] protected Vector3 _moduleSize = Vector3.one;
        [SerializeField] protected bool _subtractBrushOffset = false;
        [SerializeField] private Vector3 _spacing = Vector3.zero;
        [SerializeField] protected AxesUtils.SignedAxis _upwardAxis = AxesUtils.SignedAxis.UP;
        [SerializeField] private AxesUtils.SignedAxis _forwardAxis = AxesUtils.SignedAxis.FORWARD;
        public System.Action OnDataChanged;

        public ModularToolBase() : base()
        {
            _paintTool.OnDataChanged += DataChanged;
            OnDataChanged += DataChanged;
        }
        public virtual TilesUtils.SizeType moduleSizeType
        {
            get => _moduleSizeType;
            set
            {
                if (_moduleSizeType == value) return;
                _moduleSizeType = value;
                UpdateCellSize();
                OnDataChanged();
            }
        }
        public virtual Vector3 moduleSize
        {
            get => _moduleSize;
            set
            {
                if (_moduleSize == value) return;
                _moduleSize = value;
                OnDataChanged();
            }
        }
        public bool subtractBrushOffset
        {
            get => _subtractBrushOffset;
            set
            {
                if (_subtractBrushOffset == value) return;
                _subtractBrushOffset = value;
                UpdateCellSize();
                OnDataChanged();
            }
        }
        public Vector3 spacing
        {
            get => _spacing;
            set
            {
                if (_spacing == value) return;
                _spacing = value;
                OnDataChanged();
            }
        }
        public AxesUtils.SignedAxis upwardAxis
        {
            get => _upwardAxis;
            set
            {
                if (_upwardAxis == value) return;
                if (_forwardAxis.axis == value) _forwardAxis = _upwardAxis;
                _upwardAxis = value;
                UpdateCellSize();
                OnDataChanged();
            }
        }
        public AxesUtils.SignedAxis forwardAxis
        {
            get => _forwardAxis;
            set
            {
                if (_forwardAxis == value) return;
                if (_upwardAxis.axis == value) _upwardAxis = _forwardAxis;
                _forwardAxis = value;
                UpdateCellSize();
                OnDataChanged();
            }
        }
        public void SetUpwardAxis(AxesUtils.SignedAxis value) => _upwardAxis = value;
        public void SetForwardAxis(AxesUtils.SignedAxis value) => _forwardAxis = value;
        public virtual Vector3 GetCellSize(BrushSettings brush)
        {
            if (moduleSizeType == TilesUtils.SizeType.CUSTOM) return _moduleSize;
            if (brush == null) return Vector3.one;
            int quarterTurns = 0;
            if (this is FloorSettings) quarterTurns = FloorManager.quarterTurns;
            else if (this is BlockSettings) quarterTurns = BlockManager.quarterTurns;
            return TilesUtils.GetCellSize(moduleSizeType, brush, upwardAxis, forwardAxis,
                moduleSize, tangentSpace: false, quarterTurns, subtractBrushOffset);
        }
        public virtual void UpdateCellSize()
        {
            if (moduleSizeType == TilesUtils.SizeType.CUSTOM && !subtractBrushOffset) return;

            BrushSettings brush = PaletteManager.selectedBrush;
            if (overwriteBrushProperties) brush = brushSettings;
            if (brush == null) return;

            int quarterTurns = 0;
            if (this is FloorSettings) quarterTurns = FloorManager.quarterTurns;
            else if (this is WallSettings) quarterTurns = WallManager.halfTurn ? 2 : 0;
            else if (this is BlockSettings) quarterTurns = BlockManager.quarterTurns;
            _moduleSize = TilesUtils.GetCellSize(moduleSizeType, brush, upwardAxis, forwardAxis,
                moduleSize, tangentSpace: false, quarterTurns, subtractBrushOffset);
            ToolProperties.RepainWindow();
            UnityEditor.SceneView.RepaintAll();
        }

        public void SetCellSize(Vector3 value)
        {
            _moduleSize = value;
            ToolProperties.RepainWindow();
            UnityEditor.SceneView.RepaintAll();
        }
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
            set => _paintTool.overwriteBrushProperties = value;
        }
        public BrushSettings brushSettings => _paintTool.brushSettings;
        public bool overwriteParentingSettings
        {
            get => _paintTool.overwriteParentingSettings;
            set => _paintTool.overwriteParentingSettings = value;
        }
        public IToolParentingSettings GetParentingSettings() => _paintTool as ToolParentingSettings;
        #endregion
        public void DataChanged()
        {
            PWBCore.SetSavePending();
        }
        public virtual void Copy(IToolSettings other)
        {
            var otherModularToolSettings = other as ModularToolBase;
            if (otherModularToolSettings == null) return;
            _paintTool.Copy(otherModularToolSettings._paintTool);
            _moduleSizeType = otherModularToolSettings._moduleSizeType;
            _moduleSize = otherModularToolSettings._moduleSize;
            _spacing = otherModularToolSettings._spacing;
            _upwardAxis = otherModularToolSettings._upwardAxis;
        }
    }
}
#pragma warning restore UDR0001
