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
    public class SelectionToolSettings : SelectionToolBase, IToolSettings, ISerializationCallbackReceiver
    {
        [SerializeField] private bool _move = true;
        [SerializeField] private bool _rotate = false;
        [SerializeField] private bool _scale = false;
        [SerializeField] private Space _handleSpace = Space.Self;
        [SerializeField] private Space _boxSpace = Space.Self;
        [SerializeField] private bool _paletteFilter = false;
        [SerializeField] private bool _brushFilter = false;
        [SerializeField] private LayerMask _layerFilter = -1;
        [SerializeField] private System.Collections.Generic.List<string> _tagFilter = null;

        public Space handleSpace
        {
            get => _handleSpace;
            set
            {
                if (_handleSpace == value) return;
                _handleSpace = value;
                if (_handleSpace == Space.World) _scale = false;
                DataChanged();
            }
        }

        public bool move
        {
            get => _move;
            set
            {
                if (_move == value) return;
                _move = value;
                DataChanged();
            }
        }
        public bool rotate
        {
            get => _rotate;
            set
            {
                if (_rotate == value) return;
                _rotate = value;
                DataChanged();
            }
        }
        public bool scale
        {
            get => _scale;
            set
            {
                if (_scale == value) return;
                _scale = value;
                if (_scale) _handleSpace = Space.Self;
                DataChanged();
            }
        }

        public Space boxSpace
        {
            get => _boxSpace;
            set
            {
                if (_boxSpace == value) return;
                _boxSpace = value;
                DataChanged();
            }
        }

        public bool paletteFilter
        {
            get => _paletteFilter;
            set
            {
                if (_paletteFilter == value) return;
                _paletteFilter = value;
                DataChanged();
            }
        }

        public bool brushFilter
        {
            get => _brushFilter;
            set
            {
                if (_brushFilter == value) return;
                _brushFilter = value;
                DataChanged();
            }
        }
        public LayerMask layerFilter
        {
            get => _layerFilter;
            set
            {
                if (_layerFilter == value) return;
                _layerFilter = value;
                DataChanged();
            }
        }
        public System.Collections.Generic.List<string> tagFilter
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
        private void UpdateTagFilter()
        {
            if (_tagFilter != null) return;
            _tagFilter = new System.Collections.Generic.List<string>(UnityEditorInternal.InternalEditorUtility.tags);
        }
        public void OnBeforeSerialize() => UpdateTagFilter();
        public void OnAfterDeserialize() => UpdateTagFilter();

        public override void Copy(IToolSettings other)
        {
            var otherSelectionToolSettings = other as SelectionToolSettings;
            if (otherSelectionToolSettings == null) return;
            base.Copy(other);
            _move = otherSelectionToolSettings._move;
            _rotate = otherSelectionToolSettings._rotate;
            _scale = otherSelectionToolSettings._scale;
            _handleSpace = otherSelectionToolSettings._handleSpace;
            _boxSpace = otherSelectionToolSettings._boxSpace;
            _paletteFilter = otherSelectionToolSettings._paletteFilter;
            _brushFilter = otherSelectionToolSettings._brushFilter;
            _layerFilter = otherSelectionToolSettings._layerFilter;
            _tagFilter = otherSelectionToolSettings._tagFilter == null ? null
                : new System.Collections.Generic.List<string>(otherSelectionToolSettings._tagFilter);
        }
    }

    [System.Serializable]
    public class SelectionToolController : ToolControllerBase<SelectionToolSettings, SelectionToolController> { }
}
#pragma warning restore UDR0001
