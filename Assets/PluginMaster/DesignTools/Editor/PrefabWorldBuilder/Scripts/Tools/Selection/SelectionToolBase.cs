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
    public class SelectionToolBaseBasic : IToolSettings
    {
        [SerializeField] private bool _embedInSurface = false;
        [SerializeField] private bool _embedAtPivotHeight = false;
        [SerializeField] private float _surfaceDistance = 0f;
        [SerializeField] private bool _createTempColliders = true;

        public bool embedInSurface
        {
            get => _embedInSurface;
            set
            {
                if (_embedInSurface == value) return;
                _embedInSurface = value;
                DataChanged();
            }
        }

        public bool embedAtPivotHeight
        {
            get => _embedAtPivotHeight;
            set
            {
                if (_embedAtPivotHeight == value) return;
                _embedAtPivotHeight = value;
                DataChanged();
            }
        }

        public float surfaceDistance
        {
            get => _surfaceDistance;
            set
            {
                if (_surfaceDistance == value) return;
                _surfaceDistance = value;
                DataChanged();
            }
        }

        public bool createTempColliders
        {
            get => _createTempColliders;
            set
            {
                if (_createTempColliders == value) return;
                _createTempColliders = value;
                DataChanged();
            }
        }
        public virtual void Copy(IToolSettings other)
        {
            var otherSelectionTool = other as SelectionToolBaseBasic;
            if (otherSelectionTool == null) return;
            _embedInSurface = otherSelectionTool._embedInSurface;
            _embedAtPivotHeight = otherSelectionTool._embedAtPivotHeight;
            _surfaceDistance = otherSelectionTool._surfaceDistance;
            _createTempColliders = otherSelectionTool._createTempColliders;
        }

        public virtual void DataChanged() => PWBCore.SetSavePending();
    }

    [System.Serializable]
    public class SelectionToolBase : SelectionToolBaseBasic
    {
        [SerializeField] private bool _rotateToTheSurface = false;

        public bool rotateToTheSurface
        {
            get => _rotateToTheSurface;
            set
            {
                if (_rotateToTheSurface == value) return;
                _rotateToTheSurface = value;
                DataChanged();
            }
        }
        public override void Copy(IToolSettings other)
        {
            var otherSelectionTool = other as SelectionToolBase;
            if (otherSelectionTool == null) return;
            base.Copy(other);
            _rotateToTheSurface = otherSelectionTool._rotateToTheSurface;
        }
    }

    public interface ISelectionBrushTool
    {
        public enum Command
        {
            SELECT_ALL,
            SELECT_PALETTE_PREFABS,
            SELECT_BRUSH_PREFABS
        }
        Command command { get; set; }
        bool onlyTheClosest { get; set; }
        bool outermostPrefabFilter { get; set; }
    }
    public interface IModifierTool
    {
        bool modifyAllButSelected { get; set; }
    }

    [System.Serializable]
    public class SelectionBrushToolSettings : ISelectionBrushTool, IToolSettings
    {
        [SerializeField] private ISelectionBrushTool.Command _command = ISelectionBrushTool.Command.SELECT_ALL;
        [SerializeField] private bool _onlyTheClosest = false;
        [SerializeField] private bool _outermostPrefabFilter = true;
        public System.Action OnDataChanged;

        public SelectionBrushToolSettings() => OnDataChanged += DataChanged;
        public ISelectionBrushTool.Command command
        {
            get => _command;
            set
            {
                if (_command == value) return;
                _command = value;
                DataChanged();
            }
        }

        public bool onlyTheClosest
        {
            get => _onlyTheClosest;
            set
            {
                if (_onlyTheClosest == value) return;
                _onlyTheClosest = value;
                DataChanged();
            }
        }

        public bool outermostPrefabFilter
        {
            get => _outermostPrefabFilter;
            set
            {
                if (_outermostPrefabFilter == value) return;
                _outermostPrefabFilter = value;
                DataChanged();
            }
        }
        public void DataChanged() => PWBCore.SetSavePending();

        public virtual void Copy(IToolSettings other)
        {
            var otherModifier = other as ISelectionBrushTool;
            if (otherModifier == null) return;
            _command = otherModifier.command;
            _onlyTheClosest = otherModifier.onlyTheClosest;
            _outermostPrefabFilter = otherModifier.outermostPrefabFilter;
        }

    }

    [System.Serializable]
    public class ModifierToolSettings : SelectionBrushToolSettings, IModifierTool
    {

        [SerializeField] private bool _allButSelected = false;

        public bool modifyAllButSelected
        {
            get => _allButSelected;
            set
            {
                if (_allButSelected == value) return;
                _allButSelected = value;
                DataChanged();
            }
        }

        public override void Copy(IToolSettings other)
        {
            var otherModifier = other as IModifierTool;
            if (otherModifier == null) return;
            base.Copy(other);
            _allButSelected = otherModifier.modifyAllButSelected;
        }
    }
}
#pragma warning restore UDR0001
