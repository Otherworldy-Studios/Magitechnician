/*
Copyright(c) Omar Duarte
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
using System.Linq;

namespace PluginMaster
{
    [System.Serializable]
    public struct WallCellSize
    {
        [SerializeField] private string _name;
        [SerializeField] private float _size;
        public WallCellSize(string name, float size)
        {
            _name = name;
            _size = size;
        }
        public string name { get => _name; set => _name = value; }
        public float size { get => _size; set => _size = value; }
    }
    [System.Serializable]
    public class WallSettings : ModularToolBase
    {
        public void SetCustomLength(float value)
        {
            var wallLenghtAxis = AxesUtils.GetOtherAxis(forwardAxis, upwardAxis);
            AxesUtils.SetAxisValue(ref _moduleSize, wallLenghtAxis, value);
            WallManager.wallLength = value;
            PWBCore.SetSavePending();
        }

        public void SetThickness(float value)
        {
            AxesUtils.SetAxisValue(ref _moduleSize, forwardAxis, value);
            WallManager.wallThickness = value;
            PWBCore.SetSavePending();
        }

        [SerializeField] private bool _autoCalculateAxes = true;
        public bool autoCalculateAxes
        {
            get => _autoCalculateAxes;
            set
            {
                if (_autoCalculateAxes == value) return;
                _autoCalculateAxes = value;
                OnDataChanged();
            }
        }

        public override void Copy(IToolSettings other)
        {
            base.Copy(other);
            var otherWallSettings = other as WallSettings;
            if (otherWallSettings == null) return;
            _autoCalculateAxes = otherWallSettings.autoCalculateAxes;
        }
        public override TilesUtils.SizeType moduleSizeType
        { 
            get => base.moduleSizeType;
            set
            {
                if (base.moduleSizeType == value) return;
                base.moduleSizeType = value;
                if (value == TilesUtils.SizeType.CUSTOM)
                {
                    _autoCalculateAxes = false;
                    _subtractBrushOffset = false;
                    SetCustomLength(WallManager.wallLength);
                }
            }
        }
        
    }
    [System.Serializable]
    public class WallManager : ToolControllerBase<WallSettings, WallManager>
    {
        public enum ToolState
        {
            FIRST_WALL_PREVIEW,
            EDITING
        }

        public static ToolState state { get; set; } = ToolState.FIRST_WALL_PREVIEW;

        public static float wallThickness { get; set; } = 1f;
        public static float wallLength { get; set; } = 1f;
        public static AxesUtils.Axis wallLenghtAxis { get; set; } = AxesUtils.Axis.X;
        public static Vector3 startPoint { get; set; } = Vector3.zero;
        public static Vector3 startPointSnapped { get; set; } = Vector3.zero;
        public static Vector3 endPointSnapped { get; set; } = Vector3.zero;
        public static bool halfTurn { get; set; } = false;

        #region SIZES

        [SerializeField] private WallCellSize[] _sizes = null;
        private const string DEFAULT_SIZE_NAME = "Default";
        [SerializeField] private string _selectedSizeName = DEFAULT_SIZE_NAME;
        private System.Collections.Generic.Dictionary<string, float> _sizesDictionary
            = new System.Collections.Generic.Dictionary<string, float>() { { DEFAULT_SIZE_NAME, 1 } };
        public string selectedSizeName
        {
            get => _selectedSizeName;
            set
            {
                if (_selectedSizeName == value) return;
                _selectedSizeName = value;
                var newSize = settings.moduleSize;
                AxesUtils.SetAxisValue(ref newSize, WallManager.wallLenghtAxis, _sizesDictionary[selectedSizeName]);
                settings.moduleSize = newSize;
                PWBCore.SetSavePending();
            }
        }

        public void SaveSize(string name)
        {
            var size = AxesUtils.GetAxisValue(settings.moduleSize, WallManager.wallLenghtAxis);
            if (_sizesDictionary.ContainsKey(name))
                _sizesDictionary[name] = size;
            else _sizesDictionary.Add(name, size);
            _selectedSizeName = name;
            PWBCore.SetSavePending();
        }

        public string[] GetSizesNames() => _sizesDictionary.Keys.ToArray();
        public void DeleteSelectedSize()
        {
            _sizesDictionary.Remove(_selectedSizeName);
            selectedSizeName = DEFAULT_SIZE_NAME;
        }
        public int GetIndexOfSize(string name) => _sizesDictionary.Keys.Select((key, index) => new { key, index })
            .FirstOrDefault(pair => pair.key == name)?.index ?? -1;
        public int GetIndexOfSelectedSize() => GetIndexOfSize(selectedSizeName);
        public string GetSizeAt(int index) => _sizesDictionary.Keys.ElementAt(index);
        public void SelectSize(int index) => selectedSizeName = GetSizeAt(index);
        public void ResetSize()
        {
            var newSize = settings.moduleSize;
            AxesUtils.SetAxisValue(ref newSize, WallManager.wallLenghtAxis, _sizesDictionary[selectedSizeName]);
            settings.moduleSize = newSize;
            PWBCore.SetSavePending();
        }
        #endregion
        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            _sizes = _sizesDictionary.Select(pair => new WallCellSize(pair.Key, pair.Value)).ToArray();
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            if (_sizes == null || _sizes.Length == 0) return;
            _sizesDictionary = _sizes.ToDictionary(origin => origin.name, origin => origin.size);
        }
    }
}
#pragma warning restore UDR0001
