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
    public class PWBShortcut
    {
        public enum Group
        {
            NONE = 0,
            GLOBAL = 1,
            GRID = 2,
            PIN = 4,
            BRUSH = 8,
            GRAVITY = 16,
            LINE = 32,
            SHAPE = 64,
            TILING = 128,
            ERASER = 256,
            REPLACER = 512,
            SELECTION = 1024,
            PALETTE = 2048,
            CIRCLE_SELECT = 4096,
            EXTRUDE = 8192,
            MIRROR = 16384,
            FLOOR = 32768,
            WALL = 65536,
            BLOCK = 131072,
            TOOL_MODES = 262144,
        }
        [SerializeField] private string _name = null;
        [SerializeField] private Group _group = Group.NONE;
        [SerializeField] private bool _conflicted = false;

        public PWBShortcut(string name, Group group)
        {
            _name = name;
            _group = group;
        }

        public string name => _name;

        public Group group => _group;
        protected void SetGroup(Group value) => _group = value;

        public bool conflicted { get => _conflicted; set => _conflicted = value; }
        public virtual void Copy(PWBShortcut other)
        {
            if (other == null) return;
            _name = other._name;
            _group = other._group;
        }
    }

    [System.Serializable]
    public class PWBKeyShortcut : PWBShortcut
    {
        [SerializeField]
        protected PWBKeyCombination _keyCombination = null;

        public PWBKeyShortcut(string name, Group group, KeyCode keyCode, EventModifiers modifiers = EventModifiers.None)
            : base(name, group) => combination.Set(keyCode, modifiers);
        public PWBKeyShortcut(string name, Group group, PWBKeyCombination keyCombination) : base(name, group)
            => _keyCombination = keyCombination;

        public virtual PWBKeyCombination combination
        {
            get
            {
                if (_keyCombination == null) _keyCombination = new PWBKeyCombination();
                return _keyCombination;
            }
        }
        public bool Check()
        {
            if (PWBIO.gridShorcutEnabled && group != Group.GRID) return false;
            return combination.Check(group);
        }

        public override void Copy(PWBShortcut other)
        {
            base.Copy(other);
            var otherKeyShortcut = other as PWBKeyShortcut;
            if (otherKeyShortcut == null) return;
            if (otherKeyShortcut._keyCombination == null) return;
            combination.Set(otherKeyShortcut._keyCombination.keyCode, otherKeyShortcut._keyCombination.modifiers);
        }
    }

    [System.Serializable]
    public class PWBHoldKeysAndClickShortcut : PWBKeyShortcut
    {
        public PWBHoldKeysAndClickShortcut(string name, Group group, KeyCode keyCode,
            EventModifiers modifiers = EventModifiers.None) : base(name, group, keyCode, modifiers) { }
        public override PWBKeyCombination combination
        {
            get
            {
                if (_keyCombination == null)
                    _keyCombination = new PWBHoldKeysAndMouseCombination(KeyCode.None, EventModifiers.None,
                        PWBHoldKeysAndMouseCombination.MouseEvent.CLICK);
                return _keyCombination;
            }
        }

        public PWBHoldKeysAndMouseCombination holdKeysAndClickCombination => _keyCombination as PWBHoldKeysAndMouseCombination;
    }

    [System.Serializable]
    public class PWBHoldKeysAndMouseMoveShortcut : PWBKeyShortcut
    {
        public PWBHoldKeysAndMouseMoveShortcut(string name, Group group, KeyCode keyCode,
            EventModifiers modifiers = EventModifiers.None) : base(name, group, keyCode, modifiers) { }
        public override PWBKeyCombination combination
        {
            get
            {
                if (_keyCombination == null) _keyCombination = new PWBHoldKeysAndMouseCombination(KeyCode.None,
                    EventModifiers.None, PWBHoldKeysAndMouseCombination.MouseEvent.MOVE);
                return _keyCombination;
            }
        }

        public PWBHoldKeysAndMouseCombination holdKeysAndMouseCombination => _keyCombination as PWBHoldKeysAndMouseCombination;
    }

    [System.Serializable]
    public class PWBTwoStepKeyShortcut : PWBKeyShortcut
    {
        [SerializeField] private bool _firstStepEnabled = true;

        public PWBTwoStepKeyShortcut(string name, Group group,
            KeyCode keyCode, EventModifiers modifiers = EventModifiers.None, bool firstStepEnabled = true)
            : base(name, group, keyCode, modifiers) { }

        public bool firstStepEnabled
        {
            get => _firstStepEnabled;
            set
            {
                if (_firstStepEnabled == value) return;
                _firstStepEnabled = value;
                if (_firstStepEnabled) SetGroup(PWBShortcut.Group.GRID);
                else SetGroup(PWBShortcut.Group.GLOBAL | PWBShortcut.Group.GRID);
            }
        }
        public override void Copy(PWBShortcut other)
        {
            base.Copy(other);
            var other2StepKeyShortcut = other as PWBTwoStepKeyShortcut;
            if (other2StepKeyShortcut == null) return;
            _firstStepEnabled = other2StepKeyShortcut._firstStepEnabled;
        }
    }
    [System.Serializable]
    public class PWBMouseShortcut : PWBShortcut
    {
        [SerializeField]
        private PWBMouseCombination _combination
            = new PWBMouseCombination(EventModifiers.None, PWBMouseCombination.MouseEvents.NONE);

        public PWBMouseShortcut(string name, Group group,
            EventModifiers modifiers, PWBMouseCombination.MouseEvents mouseEvent)
            : base(name, group) => _combination.Set(modifiers, mouseEvent);
        public PWBMouseCombination combination => _combination;
        public bool Check() => combination.Check(group);
        public override void Copy(PWBShortcut other)
        {
            base.Copy(other);
            var otherMouseShortcut = other as PWBMouseShortcut;
            if (otherMouseShortcut == null) return;
            _combination.Set(otherMouseShortcut._combination.modifiers, otherMouseShortcut._combination.mouseEvent);
        }
    }
}
#pragma warning restore UDR0001
