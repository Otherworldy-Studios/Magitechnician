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
using System.Linq;
using UnityEngine;

namespace PluginMaster
{
    [System.Serializable]
    public struct FloorCellSize
    {
        [SerializeField] private string _name;
        [SerializeField] private Vector3 _size;
        public FloorCellSize(string name, Vector3 size)
        {
            _name = name;
            _size = size;
        }
        public string name { get => _name; set => _name = value; }
        public Vector3 size { get => _size; set => _size = value; }
    }
    [System.Serializable]
    public class FloorSettings : ModularToolBase
    {
        #region SIZES
        [SerializeField] private bool _swapXZ = false;
        public bool swapXZ => _swapXZ;
        public void SwapXZ()
        {
            _swapXZ = !_swapXZ;
            OnDataChanged();
        }
        public override Vector3 moduleSize
        {
            get
            {
                var size = base.moduleSize;
                if (_swapXZ)
                {
                    size.x = base.moduleSize.z;
                    size.z = base.moduleSize.x;
                }
                return size;
            }
        }
        #endregion

        #region GRID ALIGNMENT
        public enum GridAlignment
        {
            TOP_FACE,
            BOTTOM_FACE,
            PIVOT
        }
        [SerializeField] private GridAlignment _gridAlignment = GridAlignment.TOP_FACE;
        public GridAlignment gridAlignment
        {
            get => _gridAlignment;
            set
            {
                if (_gridAlignment == value) return;
                _gridAlignment = value;
                OnDataChanged();
            }
        }
        #endregion

    }

    [System.Serializable]
    public class FloorManager : ToolControllerBase<FloorSettings, FloorManager>
    {
        public enum ToolState
        {
            FIRST_CORNER,
            SECOND_CORNER
        }


        public static ToolState _state = ToolState.FIRST_CORNER;

        public static ToolState state
        {
            get => _state;
            set
            {
                if (_state == value) return;
                _state = value;
                if (_state == ToolState.FIRST_CORNER) BrushstrokeManager.ResetCellCount();
            }
        }
        public static Vector3 firstCorner { get; set; } = Vector3.zero;
        public static Vector3 secondCorner { get; set; } = Vector3.zero;
        public static int quarterTurns { get; set; } = 0;

        #region SIZES
        [SerializeField] private FloorCellSize[] _sizes = null;
        private const string DEFAULT_SIZE_NAME = "Default";
        [SerializeField] private string _selectedSizeName = DEFAULT_SIZE_NAME;
        private System.Collections.Generic.Dictionary<string, Vector3> _sizesDictionary
            = new System.Collections.Generic.Dictionary<string, Vector3>() { { DEFAULT_SIZE_NAME, Vector3.one } };
        public string selectedSizeName
        {
            get => _selectedSizeName;
            set
            {
                if (_selectedSizeName == value) return;
                _selectedSizeName = value;
                settings.moduleSize = _sizesDictionary[selectedSizeName];
                PWBCore.SetSavePending();
            }
        }
        public void SaveSize(string name)
        {
            if (_sizesDictionary.ContainsKey(name)) _sizesDictionary[name] = settings.moduleSize;
            else _sizesDictionary.Add(name, settings.moduleSize);
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
            settings.moduleSize = _sizesDictionary[selectedSizeName];
            PWBCore.SetSavePending();
        }
        
        #endregion

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            _sizes = _sizesDictionary.Select(pair => new FloorCellSize(pair.Key, pair.Value)).ToArray();
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
