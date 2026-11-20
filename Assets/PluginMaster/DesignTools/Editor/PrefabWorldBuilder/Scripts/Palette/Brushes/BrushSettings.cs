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
    public class BrushSettings : ISerializationCallbackReceiver
    {
        [SerializeField] private long _id = -1;
        [SerializeField] private float _surfaceDistance = 0f;
        [SerializeField] private bool _randomSurfaceDistance = false;
        [SerializeField] private RandomUtils.Range _randomSurfaceDistanceRange = new RandomUtils.Range(-0.005f, 0.005f);
        [SerializeField] protected bool _embedInSurface = false;
        [SerializeField] protected bool _embedAtPivotHeight = false;
        [SerializeField] protected Vector3 _localPositionOffset = Vector3.zero;
        [SerializeField] protected bool _rotateToTheSurface = true;
        [SerializeField] private Vector3 _eulerOffset = Vector3.zero;
        [SerializeField] private bool _addRandomRotation = false;
        [SerializeField] private float _rotationFactor = 90;
        [SerializeField] private bool _rotateInMultiples = false;
        [SerializeField]
        private RandomUtils.Range3 _randomEulerOffset = new RandomUtils.Range3(Vector3.zero, Vector3.zero);
        [SerializeField] private bool _alwaysOrientUp = false;
        [SerializeField] private bool _separateScaleAxes = false;
        [SerializeField] private Vector3 _scaleMultiplier = Vector3.one;
        [SerializeField] private bool _randomScaleMultiplier = false;
        [SerializeField]
        private RandomUtils.Range3 _randomScaleMultiplierRange = new RandomUtils.Range3(Vector3.one, Vector3.one);

        public enum FlipAction { NONE, FLIP, RANDOM }
        [SerializeField] private FlipAction _flipX = FlipAction.NONE;
        [SerializeField] private FlipAction _flipY = FlipAction.NONE;

        [SerializeField] private ThumbnailSettings _thumbnailSettings = new ThumbnailSettings();
        [field: System.NonSerialized] private Texture2D _thumbnail = null;
        public System.Action OnDataChangedAction;

        
        public static System.Action OnBrushSettingsChanged = null;
        protected virtual void OnDataChanged()
        {
            if (OnDataChangedAction != null) OnDataChangedAction();
            if (OnBrushSettingsChanged != null) OnBrushSettingsChanged();
        }
        public long id => _id;
        public virtual float surfaceDistance
        {
            get => _surfaceDistance;
            set
            {
                if (_surfaceDistance == value) return;
                _surfaceDistance = value;
                OnDataChanged();
            }
        }

        public virtual bool randomSurfaceDistance
        {
            get => _randomSurfaceDistance;
            set
            {
                if (_randomSurfaceDistance == value) return;
                _randomSurfaceDistance = value;
                OnDataChanged();
            }
        }

        public virtual RandomUtils.Range randomSurfaceDistanceRange
        {
            get => _randomSurfaceDistanceRange;
            set
            {
                if (_randomSurfaceDistanceRange == value) return;
                _randomSurfaceDistanceRange = value;
                OnDataChanged();
            }
        }
        public virtual bool embedInSurface
        {
            get => _embedInSurface;
            set
            {
                if (_embedInSurface == value) return;
                _embedInSurface = value;
                OnDataChanged();
            }
        }
        public virtual bool embedAtPivotHeight
        {
            get => _embedAtPivotHeight;
            set
            {
                if (_embedAtPivotHeight == value) return;
                _embedAtPivotHeight = value;
                OnDataChanged();
            }
        }
        public virtual void UpdateBottomVertices() { }

        public virtual Vector3 localPositionOffset
        {
            get => _localPositionOffset;
            set
            {
                if (_localPositionOffset == value) return;
                _localPositionOffset = value;
                OnDataChanged();
            }
        }

        public void SetLocalPositionOffset(float value, AxesUtils.Axis axis)
        {
#if UNITY_2021_1_OR_NEWER
            value = System.MathF.Round(value, digits: 5);
#else
            value = (float)System.Math.Round(value, digits: 5);
#endif
            var currentValue = AxesUtils.GetAxisValue(_localPositionOffset, axis);
            if (currentValue == value) return;
            AxesUtils.SetAxisValue(ref _localPositionOffset, axis, value);
            OnDataChanged();
        }
        public virtual bool rotateToTheSurface
        {
            get => _rotateToTheSurface;
            set
            {
                if (_rotateToTheSurface == value) return;
                _rotateToTheSurface = value;
                OnDataChanged();
            }
        }
        public virtual Vector3 eulerOffset
        {
            get => _eulerOffset;
            set
            {
                if (_eulerOffset == value) return;
                _eulerOffset = value;
                _randomEulerOffset.v1 = _randomEulerOffset.v2 = Vector3.zero;
                OnDataChanged();
            }
        }
        public virtual bool addRandomRotation
        {
            get => _addRandomRotation;
            set
            {
                if (_addRandomRotation == value) return;
                _addRandomRotation = value;
                OnDataChanged();
            }
        }
        public virtual float rotationFactor
        {
            get => _rotationFactor;
            set
            {
                value = Mathf.Max(value, 0f);
                if (_rotationFactor == value) return;
                _rotationFactor = value;
                OnDataChanged();
            }
        }
        public virtual bool rotateInMultiples
        {
            get => _rotateInMultiples;
            set
            {
                if (_rotateInMultiples == value) return;
                _rotateInMultiples = value;
                OnDataChanged();
            }
        }
        public virtual RandomUtils.Range3 randomEulerOffset
        {
            get => _randomEulerOffset;
            set
            {
                if (_randomEulerOffset == value) return;
                _randomEulerOffset = value;
                _eulerOffset = Vector3.zero;
                OnDataChanged();
            }
        }
        public virtual bool alwaysOrientUp
        {
            get => rotateToTheSurface ? _alwaysOrientUp : false;
            set
            {
                if (_alwaysOrientUp == value) return;
                _alwaysOrientUp = value;
                OnDataChanged();
            }
        }
        public virtual bool separateScaleAxes
        {
            get => _separateScaleAxes;
            set
            {
                if (_separateScaleAxes == value) return;
                _separateScaleAxes = value;
                OnDataChanged();
            }
        }
        public virtual Vector3 scaleMultiplier
        {
            get => _scaleMultiplier;
            set
            {
                if (Mathf.Approximately(value.x, 0) && Mathf.Approximately(value.y, 0) && Mathf.Approximately(value.z, 0))
                    return;
                if (_scaleMultiplier == value) return;
                _scaleMultiplier = value;
                _randomScaleMultiplierRange.v1 = _randomScaleMultiplierRange.v2 = Vector3.one;
                OnDataChanged();
            }
        }
        public virtual RandomUtils.Range3 randomScaleMultiplierRange
        {
            get => _randomScaleMultiplierRange;
            set
            {
                if (_randomScaleMultiplierRange == value) return;
                _randomScaleMultiplierRange = value;
                _scaleMultiplier = Vector3.one;
                OnDataChanged();
            }
        }
        public virtual bool randomScaleMultiplier
        {
            get => _randomScaleMultiplier;
            set
            {
                if (_randomScaleMultiplier == value) return;
                _randomScaleMultiplier = value;
                _randomScaleMultiplierRange.v1 = _randomScaleMultiplierRange.v2 = _scaleMultiplier = Vector3.one;
                OnDataChanged();
            }
        }

        public virtual bool isAsset2D { get; set; }
        public virtual FlipAction flipX
        {
            get => _flipX;
            set
            {
                if (_flipX == value) return;
                _flipX = value;
                OnDataChanged();
            }
        }
        public virtual FlipAction flipY
        {
            get => _flipY;
            set
            {
                if (_flipY == value) return;
                _flipY = value;
                OnDataChanged();
            }
        }


        public virtual Vector3 maxScaleMultiplier
            => randomScaleMultiplier ? randomScaleMultiplierRange.max : scaleMultiplier;
        public virtual Vector3 minScaleMultiplier
            => randomScaleMultiplier ? randomScaleMultiplierRange.min : scaleMultiplier;

        public Vector3 GetAdditionalAngle()
        {
            if (addRandomRotation)
            {
                var randomAngle = randomEulerOffset.randomVector;
                if (rotateInMultiples)
                {
                    randomAngle = new Vector3(
                        Mathf.Round(randomAngle.x / rotationFactor) * rotationFactor,
                        Mathf.Round(randomAngle.y / rotationFactor) * rotationFactor,
                        Mathf.Round(randomAngle.z / rotationFactor) * rotationFactor);
                }
                return randomAngle;
            }
            return eulerOffset;
        }

        public Vector3 GetScaleMultiplier()
        {
            var scale = randomScaleMultiplier ? randomScaleMultiplierRange.randomVector : scaleMultiplier;
            if (!separateScaleAxes) scale.z = scale.y = scale.x;
            return scale;
        }

        public bool GetFlipX() => flipX == FlipAction.NONE ? false : flipX == FlipAction.FLIP ? true : Random.value > 0.5;
        public bool GetFlipY() => flipY == FlipAction.NONE ? false : flipY == FlipAction.FLIP ? true : Random.value > 0.5;
        public float GetSurfaceDistance() => randomSurfaceDistance ? randomSurfaceDistanceRange.randomValue : surfaceDistance;

        public virtual string thumbnailPath { get; }
        public virtual ThumbnailSettings thumbnailSettings
        {
            get => _thumbnailSettings;
            set => _thumbnailSettings.Copy(value);
        }

        public Texture2D thumbnail
        {
            get
            {
                if (_thumbnail == null) LoadThumbnailFromFile();
                if (_thumbnail == null) UpdateThumbnail(updateItemThumbnails: true, savePng: true);
                return _thumbnail;
            }
        }

        public void LoadThumbnailFromFile()
        {
            var filePath = thumbnailPath;
            if (filePath != null)
            {
                if (System.IO.File.Exists(filePath))
                {
                    var fileData = System.IO.File.ReadAllBytes(filePath);
                    _thumbnail = new Texture2D(ThumbnailUtils.SIZE, ThumbnailUtils.SIZE);
                    _thumbnail.LoadImage(fileData);
                }
                else
                {
                    _thumbnailSettings.useCustomImage = false;
                    UpdateThumbnail(updateItemThumbnails: true, savePng: true);
                }
            }
        }

        public Texture2D thumbnailTexture
        {
            get
            {
                if (_thumbnail == null) _thumbnail = new Texture2D(ThumbnailUtils.SIZE, ThumbnailUtils.SIZE);
                return _thumbnail;
            }
        }

        public void SetCustomThumbnailTexture(Texture2D customThumbnailTexture, bool savePng)
        {
            _thumbnail = customThumbnailTexture;
            if (savePng) ThumbnailUtils.SavePngResource(_thumbnail, thumbnailPath);
        }


        public void UpdateThumbnail(bool updateItemThumbnails, bool savePng)
            => ThumbnailUtils.UpdateThumbnail(brushItem: this, updateItemThumbnails, savePng);

        public virtual BrushSettings Clone()
        {
            var clone = new BrushSettings();
            clone.Copy(this);
            if (_thumbnail != null)
            {
                var thumbnailClone = new Texture2D(_thumbnail.width, _thumbnail.height, _thumbnail.format, false);
                thumbnailClone.SetPixels(_thumbnail.GetPixels());
                thumbnailClone.Apply();
                clone._thumbnail = thumbnailClone;
            }
            return clone;
        }

        public virtual void Copy(BrushSettings other)
        {
            _surfaceDistance = other._surfaceDistance;
            _randomSurfaceDistance = other._randomSurfaceDistance;
            _randomSurfaceDistanceRange = other._randomSurfaceDistanceRange;
            _embedInSurface = other._embedInSurface;
            _embedAtPivotHeight = other._embedAtPivotHeight;
            _localPositionOffset = other._localPositionOffset;
            _rotateToTheSurface = other._rotateToTheSurface;
            _addRandomRotation = other._addRandomRotation;
            _eulerOffset = other._eulerOffset;
            _randomEulerOffset = new RandomUtils.Range3(other._randomEulerOffset);
            _randomScaleMultiplier = other._randomScaleMultiplier;
            _alwaysOrientUp = other._alwaysOrientUp;
            _separateScaleAxes = other._separateScaleAxes;
            _scaleMultiplier = other._scaleMultiplier;
            _randomScaleMultiplierRange = new RandomUtils.Range3(other._randomScaleMultiplierRange);
            _thumbnailSettings.Copy(other._thumbnailSettings);
            _rotationFactor = other._rotationFactor;
            _rotateInMultiples = other._rotateInMultiples;
            _flipX = other._flipX;
            _flipY = other._flipY;
        }
        public void Reset()
        {
            _surfaceDistance = 0f;
            _randomSurfaceDistance = false;
            _randomSurfaceDistanceRange = new RandomUtils.Range(-0.005f, 0.005f);
            _embedInSurface = false;
            _embedAtPivotHeight = true;
            _localPositionOffset = Vector3.zero;
            _rotateToTheSurface = true;
            _addRandomRotation = false;
            _eulerOffset = Vector3.zero;
            _randomEulerOffset = new RandomUtils.Range3(Vector3.zero, Vector3.zero);
            _randomScaleMultiplier = false;
            _alwaysOrientUp = false;
            _separateScaleAxes = false;
            _scaleMultiplier = Vector3.one;
            _randomScaleMultiplierRange = new RandomUtils.Range3(Vector3.one, Vector3.one);
            _thumbnailSettings = new ThumbnailSettings();
            _rotationFactor = 90;
            _rotateInMultiples = false;
            _flipX = FlipAction.NONE;
            _flipY = FlipAction.NONE;
        }

        
        private static long _prevId = 0;
        protected void SetId()
        {
            _id = System.DateTime.Now.Ticks;
            if (_id <= _prevId) _id = _prevId + 1;
            _prevId = _id;
        }
        public BrushSettings() { }
        public BrushSettings(BrushSettings other) => Copy(other);

        public virtual void OnBeforeSerialize() { }

        public virtual void OnAfterDeserialize() { }

        public override int GetHashCode()
        {
            int hashCode = 917907199;
            hashCode = hashCode * -1521134295 + _id.GetHashCode();
            hashCode = hashCode * -1521134295 + _surfaceDistance.GetHashCode();
            hashCode = hashCode * -1521134295 + _randomSurfaceDistance.GetHashCode();
            hashCode = hashCode * -1521134295 + _randomSurfaceDistanceRange.GetHashCode();
            hashCode = hashCode * -1521134295 + _embedInSurface.GetHashCode();
            hashCode = hashCode * -1521134295 + _embedAtPivotHeight.GetHashCode();
            hashCode = hashCode * -1521134295 + _localPositionOffset.GetHashCode();
            hashCode = hashCode * -1521134295 + _rotateToTheSurface.GetHashCode();
            hashCode = hashCode * -1521134295 + _eulerOffset.GetHashCode();
            hashCode = hashCode * -1521134295 + _addRandomRotation.GetHashCode();
            hashCode = hashCode * -1521134295 + _rotationFactor.GetHashCode();
            hashCode = hashCode * -1521134295 + _rotateInMultiples.GetHashCode();
            hashCode = hashCode * -1521134295 + _randomEulerOffset.GetHashCode();
            hashCode = hashCode * -1521134295 + _alwaysOrientUp.GetHashCode();
            hashCode = hashCode * -1521134295 + _separateScaleAxes.GetHashCode();
            hashCode = hashCode * -1521134295 + _scaleMultiplier.GetHashCode();
            hashCode = hashCode * -1521134295 + _randomScaleMultiplier.GetHashCode();
            hashCode = hashCode * -1521134295 + _randomScaleMultiplier.GetHashCode();
            hashCode = hashCode * -1521134295 + _flipX.GetHashCode();
            hashCode = hashCode * -1521134295 + _flipY.GetHashCode();
            hashCode = hashCode * -1521134295 + _thumbnailSettings.GetHashCode();
            return hashCode;
        }
    }
}
#pragma warning restore UDR0001
