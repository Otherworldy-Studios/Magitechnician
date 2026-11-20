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
using System.Linq;

namespace PluginMaster
{
    [System.Serializable]
    public class PWBShortcutCombination : System.IEquatable<PWBShortcutCombination>
    {
        [SerializeField] protected EventModifiers _modifiers = EventModifiers.None;
        public virtual EventModifiers modifiers => _modifiers;
        public bool control => (modifiers & EventModifiers.Control) != 0;
        public bool alt => (modifiers & EventModifiers.Alt) != 0;
        public bool shift => (modifiers & EventModifiers.Shift) != 0;
        public static EventModifiers FilterModifiers(EventModifiers modifiers)
            => modifiers & (EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift);
        public PWBShortcutCombination(EventModifiers modifiers) => _modifiers = FilterModifiers(modifiers);

        public virtual bool Check(PWBShortcut.Group group = PWBShortcut.Group.NONE, bool ignoreMousePressed = true)
        {
            if (Event.current == null) return false;
            var currentModifiers = FilterModifiers(Event.current.modifiers);
            return currentModifiers == modifiers;
        }

        public bool Equals(PWBShortcutCombination other)
        {
            if (other == null) return false;
            return modifiers == other.modifiers;
        }

        public override bool Equals(object other)
        {
            if (other == null) return false;
            if (!(other is PWBShortcutCombination otherCombination)) return false;
            return Equals(otherCombination);
        }
        public override int GetHashCode()
        {
            int hashCode = 822824530;
            hashCode = hashCode * -1521134295 + modifiers.GetHashCode();
            return hashCode;
        }
        public static bool operator ==(PWBShortcutCombination lhs, PWBShortcutCombination rhs)
        {
            if ((object)lhs == null && (object)rhs == null) return true;
            if ((object)lhs == null || (object)rhs == null) return false;
            return lhs.Equals(rhs);
        }
        public static bool operator !=(PWBShortcutCombination lhs, PWBShortcutCombination rhs) => !(lhs == rhs);
        public override string ToString()
        {
            var result = string.Empty;
            if (control) result = "Ctrl";
            if (alt) result += (result == string.Empty ? "Alt" : "+Alt");
            if (shift) result += (result == string.Empty ? "Shift" : "+Shift");
            if (result != string.Empty) result += "+";
            return result;
        }
    }
    [System.Serializable]
    public class PWBKeyCombination : PWBShortcutCombination, System.IEquatable<PWBKeyCombination>
    {
        [SerializeField] private KeyCode _keyCode = KeyCode.None;
        public virtual KeyCode keyCode => _keyCode;

        public void Set(KeyCode keyCode, EventModifiers modifiers = EventModifiers.None)
        {
            _keyCode = keyCode;
            _modifiers = FilterModifiers(modifiers);
        }
        public PWBKeyCombination(KeyCode keyCode, EventModifiers modifiers = EventModifiers.None) : base(modifiers)
            => _keyCode = keyCode;

        public PWBKeyCombination() : base(EventModifiers.None) { }
        public bool Equals(PWBKeyCombination other)
        {
            if (other == null) return false;
            return ToString() == other.ToString();
        }
        public override bool Equals(object other)
        {
            if (other == null) return false;
            if (!(other is PWBKeyCombination otherCombination)) return false;
            return ToString() == otherCombination.ToString();
        }
        public override int GetHashCode()
        {
            int hashCode = 822824530;
            hashCode = hashCode * -1521134295 + _modifiers.GetHashCode();
            hashCode = hashCode * -1521134295 + keyCode.GetHashCode();
            return hashCode;
        }
        public static bool operator ==(PWBKeyCombination lhs, PWBKeyCombination rhs)
        {
            if ((object)lhs == null && (object)rhs == null) return true;
            if ((object)lhs == null || (object)rhs == null) return false;
            return lhs.ToString() == rhs.ToString();
        }
        public static bool operator !=(PWBKeyCombination lhs, PWBKeyCombination rhs) => !(lhs == rhs);

        public override string ToString()
        {
            var result = string.Empty;
            if (keyCode == KeyCode.None) return "Disabled";
            result += base.ToString() + keyCode.ToString().Replace("Alpha", "");
            return result;
        }
        public bool isDissabled() => keyCode == KeyCode.None;
        public override bool Check(PWBShortcut.Group group = PWBShortcut.Group.NONE, bool ignoreMousePressed = false)
        {
            if (!ignoreMousePressed && PWBIO.mousePressed) return false;
            if (keyCode == KeyCode.None) return false;
            if (Event.current.type != EventType.KeyDown || Event.current.keyCode != keyCode) return false;
            return base.Check(group, ignoreMousePressed);
        }
    }

    [System.Serializable]
    public class PWBKeyCombinationUSM : PWBKeyCombination
    {
        private string _shortcutId = null;

        public PWBKeyCombinationUSM(string shortcutId)
            : base(KeyCode.None, EventModifiers.None) => _shortcutId = shortcutId;

        public override KeyCode keyCode
        {
            get
            {
                var keyCombinationSequence = UnityEditor.ShortcutManagement.ShortcutManager.instance
                .GetShortcutBinding(_shortcutId).keyCombinationSequence;
                if (keyCombinationSequence.Count() == 0) return KeyCode.None;
                return keyCombinationSequence.First().keyCode;
            }
        }
        public override EventModifiers modifiers
        {
            get
            {
                var keyCombinationSequence = UnityEditor.ShortcutManagement.ShortcutManager.instance
                    .GetShortcutBinding(_shortcutId).keyCombinationSequence;

                if (keyCombinationSequence.Count() == 0) return EventModifiers.None;

                var mods = keyCombinationSequence.First().modifiers;
                var result = EventModifiers.None;
                if ((mods & UnityEditor.ShortcutManagement.ShortcutModifiers.Action)
                    == UnityEditor.ShortcutManagement.ShortcutModifiers.Action) result |= EventModifiers.Control;
                if ((mods & UnityEditor.ShortcutManagement.ShortcutModifiers.Alt)
                    == UnityEditor.ShortcutManagement.ShortcutModifiers.Alt) result |= EventModifiers.Alt;
                if ((mods & UnityEditor.ShortcutManagement.ShortcutModifiers.Shift)
                    == UnityEditor.ShortcutManagement.ShortcutModifiers.Shift) result |= EventModifiers.Shift;
                return result;
            }
        }
        public void Rebind(KeyCode keyCode, EventModifiers modifiers)
        {
            var mods = UnityEditor.ShortcutManagement.ShortcutModifiers.None;
            if ((modifiers & EventModifiers.Control) == EventModifiers.Control)
                mods |= UnityEditor.ShortcutManagement.ShortcutModifiers.Action;
            if ((modifiers & EventModifiers.Alt) == EventModifiers.Alt)
                mods |= UnityEditor.ShortcutManagement.ShortcutModifiers.Alt;
            if ((modifiers & EventModifiers.Shift) == EventModifiers.Shift)
                mods |= UnityEditor.ShortcutManagement.ShortcutModifiers.Shift;
            UnityEditor.ShortcutManagement.ShortcutManager.instance.RebindShortcut(_shortcutId,
                new UnityEditor.ShortcutManagement.ShortcutBinding(
                    new UnityEditor.ShortcutManagement.KeyCombination(keyCode, mods)));
        }

        public void Reset()
        {
            UnityEditor.ShortcutManagement.ShortcutManager.instance.ClearShortcutOverride(_shortcutId);
        }
    }

    [System.Serializable]
    public class PWBHoldKeysAndMouseCombination : PWBKeyCombination
    {
        private bool _holdingKeys = false;
        public bool holdingKeys => _holdingKeys;
        private bool _holdingChanged = false;
        public bool holdingChanged => _holdingChanged;
        public enum MouseEvent { MOVE, CLICK }
        private MouseEvent _mouseEvent = MouseEvent.CLICK;
        public MouseEvent mouseEvent => _mouseEvent;
        public PWBHoldKeysAndMouseCombination(KeyCode keyCode, EventModifiers modifiers, MouseEvent mouseEvent)
            : base(keyCode, modifiers) => _mouseEvent = mouseEvent;
        public PWBHoldKeysAndMouseCombination() : base() { }
        public override bool Check(PWBShortcut.Group group = PWBShortcut.Group.NONE, bool ignoreMousePressed = true)
        {
            CheckIsHoldingKeys(group, ignoreMousePressed);
            return _holdingKeys && Event.current.button == 0
                && Event.current.type == (_mouseEvent == MouseEvent.CLICK ? EventType.MouseDown : EventType.MouseMove);
        }
        public bool CheckIsHoldingKeys(PWBShortcut.Group group = PWBShortcut.Group.NONE, bool ignoreMousePressed = true)
        {
            _holdingChanged = false;
            if (Event.current.keyCode == keyCode)
            {
                var prevHolding = _holdingKeys;
                if (Event.current.type == EventType.KeyDown && base.Check(group, ignoreMousePressed)) _holdingKeys = true;
                else if (Event.current.type == EventType.KeyUp) _holdingKeys = false;
                _holdingChanged = prevHolding != _holdingKeys;
            }
            return _holdingKeys;
        }
        public override string ToString()
        {
            var result = base.ToString();
            if (keyCode != KeyCode.None)
                result = "Hold " + result + " + " + (_mouseEvent == MouseEvent.CLICK ? "Click" : "Mouse move");
            return result;
        }
    }

    [System.Serializable]
    public class PWBMouseCombination : PWBShortcutCombination, System.IEquatable<PWBMouseCombination>
    {
        public enum MouseEvents
        {
            NONE,
            SCROLL_WHEEL,
            DRAG_R_H,
            DRAG_R_V,
            DRAG_M_H,
            DRAG_M_V
        }

        [SerializeField] private MouseEvents _mouseEvent = MouseEvents.NONE;

        public MouseEvents mouseEvent => _mouseEvent;
        public void Set(EventModifiers modifiers, MouseEvents mouseEvent)
        {
            _modifiers = FilterModifiers(modifiers);
            _mouseEvent = mouseEvent;
        }

        public PWBMouseCombination(EventModifiers modifiers, MouseEvents mouseEvent) : base(modifiers)
        => _mouseEvent = mouseEvent;
        public bool Equals(PWBMouseCombination other)
        {
            if (other == null) return false;
            return _mouseEvent == other._mouseEvent && _modifiers == other._modifiers;
        }
        public override bool Equals(object other)
        {
            if (other == null) return false;
            if (!(other is PWBMouseCombination otherCombination)) return false;
            return Equals(otherCombination);
        }

        public override int GetHashCode()
        {
            int hashCode = 1068782991;
            hashCode = hashCode * -1521134295 + _modifiers.GetHashCode();
            hashCode = hashCode * -1521134295 + _mouseEvent.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(PWBMouseCombination lhs, PWBMouseCombination rhs)
        {
            if ((object)lhs == null && (object)rhs == null) return true;
            if ((object)lhs == null || (object)rhs == null) return false;
            return lhs.Equals(rhs);
        }
        public static bool operator !=(PWBMouseCombination lhs, PWBMouseCombination rhs) => !(lhs == rhs);

        public bool isRDrag => mouseEvent == MouseEvents.DRAG_R_H || mouseEvent == MouseEvents.DRAG_R_V;
        public bool isMDrag => mouseEvent == MouseEvents.DRAG_M_H || mouseEvent == MouseEvents.DRAG_M_V;
        public bool isMouseDragEvent => mouseEvent == MouseEvents.DRAG_R_H || mouseEvent == MouseEvents.DRAG_R_V
                || mouseEvent == MouseEvents.DRAG_M_H || mouseEvent == MouseEvents.DRAG_M_V;
        public bool isHorizontalDragEvent => mouseEvent == MouseEvents.DRAG_R_H || mouseEvent == MouseEvents.DRAG_M_H;
        private float _delta = 0;
        public float delta => _delta;

        public override bool Check(PWBShortcut.Group group = PWBShortcut.Group.NONE, bool ignoreMousePressed = true)
        {
            if (mouseEvent == MouseEvents.NONE) return false;
            if (FilterModifiers(Event.current.modifiers) == EventModifiers.None) return false;
            if (!base.Check(group, ignoreMousePressed)) return false;
            if (isMouseDragEvent)
            {
                _delta = isHorizontalDragEvent ? Event.current.delta.x : -Event.current.delta.y;
                if (Event.current.type != EventType.MouseDrag) return false;
                if (isRDrag && Event.current.button != 1)
                    return false;
                if (isMDrag && Event.current.button != 2) return false;

                var xIsGreaterThanY = Mathf.Abs(Event.current.delta.x) > Mathf.Abs(Event.current.delta.y);
                if (isHorizontalDragEvent && !xIsGreaterThanY)
                {
                    var other = new PWBMouseCombination(base.modifiers,
                        mouseEvent == MouseEvents.DRAG_R_H ? MouseEvents.DRAG_R_V : MouseEvents.DRAG_M_V);
                    if (!PWBSettings.shortcuts.CombinationExist(other, group)) Event.current.Use();
                    return false;
                }
                if (!isHorizontalDragEvent && xIsGreaterThanY)
                {
                    var other = new PWBMouseCombination(base.modifiers,
                        mouseEvent == MouseEvents.DRAG_R_V ? MouseEvents.DRAG_R_H : MouseEvents.DRAG_M_H);
                    if (!PWBSettings.shortcuts.CombinationExist(other, group)) Event.current.Use();
                    return false;
                }
            }
            if (mouseEvent == MouseEvents.SCROLL_WHEEL)
            {
                if (!Event.current.isScrollWheel) return false;
                _delta = (Mathf.Abs(Event.current.delta.x) > Mathf.Abs(Event.current.delta.y))
                    ? Event.current.delta.x : Event.current.delta.y;
            }
            Event.current.Use();
            return true;
        }

        public override string ToString()
        {
            var result = string.Empty;
            if (mouseEvent == MouseEvents.NONE) return "Disabled";
            var mouseEventString = string.Empty;
            switch (mouseEvent)
            {
                case MouseEvents.SCROLL_WHEEL:
                    mouseEventString = "Scroll wheel";
                    break;
                case MouseEvents.DRAG_R_H:
                    mouseEventString = "Mouse R Btn H drag";
                    break;
                case MouseEvents.DRAG_R_V:
                    mouseEventString = "Mouse R Btn V drag";
                    break;
                case MouseEvents.DRAG_M_H:
                    mouseEventString = "Mouse Mid Btn H drag";
                    break;
                case MouseEvents.DRAG_M_V:
                    mouseEventString = "Mouse Mid Btn V drag";
                    break;
            }
            result += base.ToString() + mouseEventString;
            return result;
        }
    }
}
#pragma warning restore UDR0001
