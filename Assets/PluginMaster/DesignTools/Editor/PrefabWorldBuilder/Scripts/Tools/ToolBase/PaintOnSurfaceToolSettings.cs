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
    public interface IPaintOnSurfaceToolSettings
    {
        bool paintOnMeshesWithoutCollider { get; set; }
        bool ignoreSceneColliders { get; set; }
        bool paintOnSelectedOnly { get; set; }
        bool paintOnPalettePrefabs { get; set; }
    }

    [System.Serializable]
    public abstract class PaintOnSurfaceToolSettingsBase : IPaintOnSurfaceToolSettings
    {
        public abstract bool paintOnMeshesWithoutCollider { get; set; }
        public abstract bool ignoreSceneColliders { get; set; }
        public abstract bool paintOnSelectedOnly { get; set; }
        public abstract bool paintOnPalettePrefabs { get; set; }
        public enum PaintMode
        {
            AUTO,
            ON_SURFACE,
            ON_SHAPE
        }
    }

    [System.Serializable]
    public class PaintOnSurfaceToolSettings : PaintOnSurfaceToolSettingsBase,
        ISerializationCallbackReceiver, IToolSettings
    {
        [SerializeField] private bool _paintOnMeshesWithoutCollider = false;
        [SerializeField] private bool _ignoreScenetColliders = false;
        [SerializeField] private bool _paintOnSelectedOnly = false;
        [SerializeField] private bool _paintOnPalettePrefabs = false;
        [SerializeField] private PaintMode _mode = PaintMode.AUTO;
        [SerializeField] private bool _paralellToTheSurface = true;
        public System.Action OnDataChanged;

        public PaintOnSurfaceToolSettings() => OnDataChanged += DataChanged;
        public PaintMode mode
        {
            get => _mode;
            set
            {
                if (_mode == value) return;
                _mode = value;
                OnDataChanged();
            }
        }
        public bool perpendicularToTheSurface
        {
            get => _paralellToTheSurface;
            set
            {
                if (_paralellToTheSurface == value) return;
                _paralellToTheSurface = value;
                OnDataChanged();
            }
        }

        public override bool paintOnMeshesWithoutCollider
        {
            get => _paintOnMeshesWithoutCollider;
            set
            {
                if (_paintOnMeshesWithoutCollider == value) return;
                _paintOnMeshesWithoutCollider = value;
                OnDataChanged();
            }
        }

        public override bool ignoreSceneColliders
        {
            get => _ignoreScenetColliders;
            set
            {
                if (_ignoreScenetColliders == value) return;
                _ignoreScenetColliders = value;
                OnDataChanged();
            }
        }
        public override bool paintOnSelectedOnly
        {
            get => _paintOnSelectedOnly;
            set
            {
                if (_paintOnSelectedOnly == value) return;
                _paintOnSelectedOnly = value;
                OnDataChanged();
            }
        }

        public override bool paintOnPalettePrefabs
        {
            get => _paintOnPalettePrefabs;
            set
            {
                if (_paintOnPalettePrefabs == value) return;
                _paintOnPalettePrefabs = value;
                OnDataChanged();
            }
        }

        public virtual void Copy(IToolSettings other)
        {
            var otherPaintOnSurfaceToolSettings = other as PaintOnSurfaceToolSettings;
            if (otherPaintOnSurfaceToolSettings == null) return;
            _paintOnMeshesWithoutCollider = otherPaintOnSurfaceToolSettings._paintOnMeshesWithoutCollider;
            _paintOnSelectedOnly = otherPaintOnSurfaceToolSettings._paintOnSelectedOnly;
            _paintOnPalettePrefabs = otherPaintOnSurfaceToolSettings._paintOnPalettePrefabs;
            _mode = otherPaintOnSurfaceToolSettings._mode;
            _paralellToTheSurface = otherPaintOnSurfaceToolSettings._paralellToTheSurface;
        }
        public virtual void DataChanged()
        {
            PWBCore.SetSavePending();
        }
        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() { }
    }
}
#pragma warning restore UDR0001
