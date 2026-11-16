using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// Shows the current key or button bound to one input action, as text, and updates it when the binding changes.
    /// Use it on labels such as "E: Interact" in the HUD or a controls screen.
    /// </summary>
    // INTEGRATION: new script, adapted from the Input System package's rebinding UI sample.
    public class ActionLabelDisplay : MonoBehaviour
    {
        [Tooltip("The action whose binding this label shows.")]
        [SerializeField, FormerlySerializedAs("m_Action")]
        private InputActionReference _action;

        [Tooltip("The ID of the binding on the action to show. Set from the Inspector's binding dropdown.")]
        [SerializeField, FormerlySerializedAs("m_BindingId")]
        private string _bindingId;

        [Tooltip("How the binding text is formatted, for example whether to show the device name.")]
        [SerializeField, FormerlySerializedAs("m_DisplayStringOptions")]
        private InputBinding.DisplayStringOptions _displayStringOptions;

        [Tooltip("Text label that will receive the current, formatted binding string.")]
        [SerializeField, FormerlySerializedAs("m_BindingText")]
        private TMP_Text _bindingText;

        [Tooltip("Event that is triggered when the way the binding is display should be updated. This allows displaying "
            + "bindings in custom ways, e.g. using images instead of text.")]
        [SerializeField, FormerlySerializedAs("m_UpdateBindingUIEvent")]
        private UpdateBindingUIEvent _updateBindingUIEvent;

        // Every enabled label, so one Input System callback can refresh all of them when bindings change.
        private static List<ActionLabelDisplay> _activeLabels;

        /// <summary>The action whose binding this label shows. Setting it refreshes the label.</summary>
        public InputActionReference ActionReference
        {
            get => _action;
            set
            {
                _action = value;
                UpdateBindingDisplay();
            }
        }

        /// <summary>The ID, as a string, of the binding to show. Setting it refreshes the label.</summary>
        /// <seealso cref="InputBinding.id"/>
        public string BindingId
        {
            get => _bindingId;
            set
            {
                _bindingId = value;
                UpdateBindingDisplay();
            }
        }

        /// <summary>
        /// The text that receives the binding's display string. Can be null, in which case only the
        /// update event shows the binding. Setting it refreshes the label.
        /// </summary>
        public TMP_Text BindingText
        {
            get => _bindingText;
            set
            {
                _bindingText = value;
                UpdateBindingDisplay();
            }
        }

        /// <summary>How the binding text is formatted. Setting it refreshes the label.</summary>
        public InputBinding.DisplayStringOptions DisplayStringOptions
        {
            get => _displayStringOptions;
            set
            {
                _displayStringOptions = value;
                UpdateBindingDisplay();
            }
        }

        /// <summary>
        /// Reads the binding's current display string and writes it to the text, then raises the update event
        /// so listeners can show it another way, such as an icon.
        /// </summary>
        public void UpdateBindingDisplay()
        {
            var displayString = string.Empty;
            var deviceLayoutName = default(string);
            var controlPath = default(string);

            // Find the binding on the action by its ID and get its display string.
            var action = _action?.action;
            if (action != null)
            {
                var bindingIndex = action.bindings.IndexOf(x => x.id.ToString() == _bindingId);
                if (bindingIndex != -1)
                    displayString = action.GetBindingDisplayString(bindingIndex, out deviceLayoutName, out controlPath, DisplayStringOptions);
            }

            if (_bindingText != null)
                _bindingText.text = displayString;

            _updateBindingUIEvent?.Invoke(this, displayString, deviceLayoutName, controlPath);
        }

        /// <summary>Adds this label to the active list, subscribes the shared callback when it's the first, and refreshes.</summary>
        protected void OnEnable()
        {
            if (_activeLabels == null)
                _activeLabels = new List<ActionLabelDisplay>();
            _activeLabels.Add(this);
            if (_activeLabels.Count == 1)
                InputSystem.onActionChange += OnActionChange;
            UpdateBindingDisplay();
        }

        /// <summary>Removes this label, and unsubscribes the shared callback when it was the last.</summary>
        protected void OnDisable()
        {
            _activeLabels.Remove(this);
            if (_activeLabels.Count == 0)
            {
                _activeLabels = null;
                InputSystem.onActionChange -= OnActionChange;
            }
        }

        /// <summary>
        /// Called by the Input System when actions change. When bindings are resolved again, refreshes every label
        /// that shows the changed action, or an action in the changed map or asset.
        /// </summary>
        private static void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.BoundControlsChanged)
                return;

            // obj is an action, a map, or an asset, depending on what changed.
            var action = obj as InputAction;
            var actionMap = action?.actionMap ?? obj as InputActionMap;
            var actionAsset = actionMap?.asset ?? obj as InputActionAsset;

            for (var i = 0; i < _activeLabels.Count; ++i)
            {
                var component = _activeLabels[i];
                var referencedAction = component.ActionReference?.action;
                if (referencedAction == null)
                    continue;

                if (referencedAction == action ||
                    referencedAction.actionMap == actionMap ||
                    referencedAction.actionMap?.asset == actionAsset)
                    component.UpdateBindingDisplay();
            }
        }
    }

    /// <summary>
    /// Raised when a label's binding display updates, with the label, the display string, the device layout name,
    /// and the control path. Hook it up in the Inspector to show bindings as images.
    /// </summary>
    [Serializable]
    public class UpdateBindingUIEvent : UnityEvent<ActionLabelDisplay, string, string, string>
    {
    }
}
