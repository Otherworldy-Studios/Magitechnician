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
    public class CircleToolBase : IToolSettings
    {
        [SerializeField] private float _radius = 1f;

        public float radius
        {
            get => _radius;
            set
            {
                value = Mathf.Max(value, 0.05f);
                if (_radius == value) return;
                _radius = value;
                DataChanged();
            }
        }

        public virtual void Copy(IToolSettings other)
        {
            var otherCircleToolBase = other as CircleToolBase;
            if (otherCircleToolBase == null) return;
            _radius = otherCircleToolBase._radius;
        }

        public virtual void DataChanged() => PWBCore.SetSavePending();
    }
    [System.Serializable]
    public class BrushToolBase : CircleToolBase, IPaintToolSettings, IToolParentingSettings
    {
        [SerializeField] private PaintToolSettings _paintTool = new PaintToolSettings();
        public enum BrushShape { POINT, CIRCLE, SQUARE }
        [SerializeField] protected BrushShape _brushShape = BrushShape.CIRCLE;
        [SerializeField] private int _density = 50;
        [SerializeField] private bool _orientAlongBrushstroke = false;
        [SerializeField] private Vector3 _additionalOrientationAngle = Vector3.zero;
        public enum SpacingType { AUTO, CUSTOM }
        [SerializeField] private SpacingType _spacingType = SpacingType.AUTO;
        [SerializeField] protected float _minSpacing = 1f;
        [SerializeField] private bool _randomizePositions = true;
        [SerializeField] private float _randomness = 1f;
        public BrushToolBase() : base() => _paintTool.OnDataChanged += DataChanged;

        public BrushShape brushShape
        {
            get => _brushShape;
            set
            {
                if (_brushShape == value) return;
                _brushShape = value;
                DataChanged();
            }
        }
        public int density
        {
            get => _density;
            set
            {
                value = Mathf.Clamp(value, 0, 100);
                if (_density == value) return;
                _density = value;
                DataChanged();
            }
        }
        public bool orientAlongBrushstroke
        {
            get => _orientAlongBrushstroke;
            set
            {
                if (_orientAlongBrushstroke == value) return;
                _orientAlongBrushstroke = value;
                DataChanged();
            }
        }
        public Vector3 additionalOrientationAngle
        {
            get => _additionalOrientationAngle;
            set
            {
                if (_additionalOrientationAngle == value) return;
                _additionalOrientationAngle = value;
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
        public float minSpacing
        {
            get => _minSpacing;
            set
            {
                value = Mathf.Max(value, 0.01f);
                if (_minSpacing == value) return;
                _minSpacing = value;
                DataChanged();
            }
        }
        public bool randomizePositions
        {
            get => _randomizePositions;
            set
            {
                if (_randomizePositions == value) return;
                _randomizePositions = value;
                DataChanged();
            }
        }

        public float randomness
        {
            get => _randomness;
            set
            {
                value = Mathf.Clamp01(value);
                if (_randomness == value) return;
                _randomness = value;
                DataChanged();
            }
        }

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

        public override void DataChanged()
        {
            base.DataChanged();
            BrushstrokeManager.UpdateBrushstroke();
        }

        public override void Copy(IToolSettings other)
        {
            var otherBrushToolBase = other as BrushToolBase;
            if (otherBrushToolBase == null) return;
            base.Copy(other);
            _paintTool.Copy(otherBrushToolBase._paintTool);
            _brushShape = otherBrushToolBase._brushShape;
            _density = otherBrushToolBase.density;
            _orientAlongBrushstroke = otherBrushToolBase._orientAlongBrushstroke;
            _additionalOrientationAngle = otherBrushToolBase._additionalOrientationAngle;
            _spacingType = otherBrushToolBase._spacingType;
            _minSpacing = otherBrushToolBase._minSpacing;
            _randomizePositions = otherBrushToolBase._randomizePositions;
        }

    }
}
#pragma warning restore UDR0001
