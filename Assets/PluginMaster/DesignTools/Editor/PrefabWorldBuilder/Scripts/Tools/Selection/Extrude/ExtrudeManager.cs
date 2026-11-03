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
    public class ExtrudeSettings : SelectionToolBaseBasic, IToolSettings, IPaintToolSettings, IToolParentingSettings
    {
        [SerializeField] private Space _space = Space.World;
        [SerializeField] private Vector3 _spacing = Vector3.zero;
        public enum SpacingType { BOX_SIZE, CUSTOM }
        [SerializeField] private SpacingType _spacingType = SpacingType.CUSTOM;
        [SerializeField] private Vector3 _multiplier = Vector3.one;

        public enum RotationAccordingTo { FRIST_SELECTED, LAST_SELECTED }
        [SerializeField] private RotationAccordingTo _rotationAccordingTo = RotationAccordingTo.FRIST_SELECTED;

        [SerializeField] private bool _sameParentAsSource = true;

        [SerializeField] private Vector3 _eulerOffset = Vector3.zero;
        [SerializeField] private bool _addRandomRotation = false;
        [SerializeField] private float _rotationFactor = 90;
        [SerializeField] private bool _rotateInMultiples = false;
        [SerializeField]
        private RandomUtils.Range3 _randomEulerOffset = new RandomUtils.Range3(Vector3.zero, Vector3.zero);


        public Space space
        {
            get => _space;
            set
            {
                if (_space == value) return;
                _space = value;
                DataChanged();
            }
        }
        public Vector3 multiplier
        {
            get => _multiplier;
            set
            {
                if (_multiplier == value) return;
                _multiplier = value;
                DataChanged();
            }
        }

        public RotationAccordingTo rotationAccordingTo
        {
            get => _rotationAccordingTo;
            set
            {
                if (_rotationAccordingTo == value) return;
                _rotationAccordingTo = value;
                DataChanged();
            }
        }

        public Vector3 spacing
        {
            get => _spacing;
            set
            {
                if (_spacing == value) return;
                _spacing = value;
                DataChanged();
            }
        }
        public SpacingType spacingType
        {
            get => _spacingType;
            set
            {
                if (_spacingType == value) return;
                _spacingType = value;
                DataChanged();
            }
        }

        public ExtrudeSettings Clone()
        {
            var clone = new ExtrudeSettings();
            clone.Copy(this);
            return clone;
        }


        public Vector3 eulerOffset
        {
            get => _eulerOffset;
            set
            {
                if (_eulerOffset == value) return;
                _eulerOffset = value;
                _randomEulerOffset.v1 = _randomEulerOffset.v2 = Vector3.zero;
            }
        }
        public bool addRandomRotation
        {
            get => _addRandomRotation;
            set
            {
                if (_addRandomRotation == value) return;
                _addRandomRotation = value;
            }
        }
        public float rotationFactor
        {
            get => _rotationFactor;
            set
            {
                value = Mathf.Max(value, 0f);
                if (_rotationFactor == value) return;
                _rotationFactor = value;
            }
        }
        public bool rotateInMultiples
        {
            get => _rotateInMultiples;
            set
            {
                if (_rotateInMultiples == value) return;
                _rotateInMultiples = value;
            }
        }
        public RandomUtils.Range3 randomEulerOffset
        {
            get => _randomEulerOffset;
            set
            {
                if (_randomEulerOffset == value) return;
                _randomEulerOffset = value;
                _eulerOffset = Vector3.zero;
            }
        }

        public override void Copy(IToolSettings other)
        {
            var otherExtrudeSettings = other as ExtrudeSettings;
            if (otherExtrudeSettings == null) return;
            base.Copy(other);
            _paintTool.Copy(otherExtrudeSettings._paintTool);
            _sameParentAsSource = otherExtrudeSettings._sameParentAsSource;
            _space = otherExtrudeSettings._space;
            _multiplier = otherExtrudeSettings._multiplier;
            _rotationAccordingTo = otherExtrudeSettings._rotationAccordingTo;
            _spacing = otherExtrudeSettings._spacing;
            _spacingType = otherExtrudeSettings._spacingType;
            _eulerOffset = otherExtrudeSettings._eulerOffset;
            _addRandomRotation = otherExtrudeSettings._addRandomRotation;
            _rotationFactor = otherExtrudeSettings._rotationFactor;
            _rotateInMultiples = otherExtrudeSettings._rotateInMultiples;
            _randomEulerOffset = otherExtrudeSettings._randomEulerOffset;
        }

        public bool sameParentAsSource
        {
            get => _sameParentAsSource;
            set
            {
                if (_sameParentAsSource == value) return;
                _sameParentAsSource = value;
                DataChanged();
            }
        }

        #region PAINT TOOL
        [SerializeField] private PaintToolSettings _paintTool = new PaintToolSettings();
        public Transform parent { get => _paintTool.parent; set => _paintTool.parent = value; }
        public bool overwritePrefabLayer
        { get => _paintTool.overwritePrefabLayer; set => _paintTool.overwritePrefabLayer = value; }
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
        { get => _paintTool.overwriteBrushProperties; set => _paintTool.overwriteBrushProperties = value; }
        public BrushSettings brushSettings => _paintTool.brushSettings;
        public bool overwriteParentingSettings
        {
            get => _paintTool.overwriteParentingSettings;
            set => _paintTool.overwriteParentingSettings = value;
        }
        public IToolParentingSettings GetParentingSettings() => _paintTool as ToolParentingSettings;
        #endregion
    }

    [System.Serializable]
    public class ExtrudeManager : ToolControllerBase<ExtrudeSettings, ExtrudeManager> { }
}
#pragma warning restore UDR0001
