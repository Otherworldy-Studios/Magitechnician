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
    public class ThumbnailSettings
    {
        [SerializeField] private Color _backgroudColor = Color.gray;
        [SerializeField] private Vector2 _lightEuler = new Vector2(130, -165);
        [SerializeField] private Color _lightColor = Color.white;
        [SerializeField] private float _lightIntensity = 1;
        [SerializeField] private float _zoom = 1;
        [SerializeField] private Vector3 _targetEuler = new Vector3(0, 125, 0);
        [SerializeField] private Vector2 _targetOffset = Vector2.zero;
        [SerializeField] private bool _useCustomImage = false;

        public Color backgroudColor { get => _backgroudColor; set => _backgroudColor = value; }
        public Vector2 lightEuler { get => _lightEuler; set => _lightEuler = value; }
        public Color lightColor { get => _lightColor; set => _lightColor = value; }
        public float lightIntensity { get => _lightIntensity; set => _lightIntensity = value; }
        public float zoom { get => _zoom; set => _zoom = value; }
        public Vector3 targetEuler { get => _targetEuler; set => _targetEuler = value; }
        public Vector2 targetOffset { get => _targetOffset; set => _targetOffset = value; }
        public bool useCustomImage { get => _useCustomImage; set => _useCustomImage = value; }
        public ThumbnailSettings() { }

        
        private static ThumbnailSettings _defaultTAsset2DThumbnailSettings = null;
        public static ThumbnailSettings GetDefaultTAsset2DThumbnailSettings()
        {
            if (_defaultTAsset2DThumbnailSettings == null)
            {
                _defaultTAsset2DThumbnailSettings = new ThumbnailSettings();
                _defaultTAsset2DThumbnailSettings.targetEuler = new Vector3(17.5f, 0f, 0f);
                _defaultTAsset2DThumbnailSettings.zoom = 1.47f;
                _defaultTAsset2DThumbnailSettings.targetOffset = new Vector2(0f, -0.06f);
            }
            return _defaultTAsset2DThumbnailSettings;
        }
        public ThumbnailSettings(Color backgroudColor, Vector3 lightEuler, Color lightColor, float lightIntensity,
            float zoom, Vector3 targetEuler, Vector2 targetOffset, bool useCustomImage)
        {
            _backgroudColor = backgroudColor;
            _lightEuler = lightEuler;
            _lightColor = lightColor;
            _lightIntensity = lightIntensity;
            _zoom = zoom;
            _targetEuler = targetEuler;
            _targetOffset = targetOffset;
            _useCustomImage = useCustomImage;
        }

        public ThumbnailSettings(ThumbnailSettings other) => Copy(other);
        public void Copy(ThumbnailSettings other)
        {
            _backgroudColor = other._backgroudColor;
            _lightEuler = other._lightEuler;
            _lightColor = other._lightColor;
            _lightIntensity = other._lightIntensity;
            _zoom = other._zoom;
            _targetEuler = other._targetEuler;
            _targetOffset = other._targetOffset;
            _useCustomImage = other._useCustomImage;
        }

        public ThumbnailSettings Clone()
        {
            var clone = new ThumbnailSettings();
            clone.Copy(this);
            return clone;
        }

        public override int GetHashCode()
        {
            int hashCode = 917907199;
            hashCode = hashCode * -1521134295 + _backgroudColor.GetHashCode();
            hashCode = hashCode * -1521134295 + _lightEuler.GetHashCode();
            hashCode = hashCode * -1521134295 + _lightColor.GetHashCode();
            hashCode = hashCode * -1521134295 + _lightIntensity.GetHashCode();
            hashCode = hashCode * -1521134295 + _zoom.GetHashCode();
            hashCode = hashCode * -1521134295 + _targetEuler.GetHashCode();
            hashCode = hashCode * -1521134295 + _targetOffset.GetHashCode();
            hashCode = hashCode * -1521134295 + _useCustomImage.GetHashCode();
            return hashCode;
        }
    }
}
#pragma warning restore UDR0001
