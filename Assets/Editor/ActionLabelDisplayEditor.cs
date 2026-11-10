#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Samples.RebindUI;

[CustomEditor(typeof(ActionLabelDisplay))]
public class ActionLabelDisplayEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "m_BindingId");

        var actionProp = serializedObject.FindProperty("m_Action");
        var idProp = serializedObject.FindProperty("m_BindingId");
        var action = (actionProp.objectReferenceValue as InputActionReference)?.action;

        if (action == null)
        {
            EditorGUILayout.HelpBox("Assign an action to choose a binding.", MessageType.Info);
        }
        else
        {
            var bindings = action.bindings;
            var labels = new string[bindings.Count];
            var selected = -1;

            for (var i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                var prefix = b.isPartOfComposite ? "    " : "";
                var groups = string.IsNullOrEmpty(b.groups) ? "" : $" [{b.groups}]";
                labels[i] = $"{prefix}{b.name} {b.effectivePath}{groups}".Trim();

                if (b.id.ToString() == idProp.stringValue)
                    selected = i;
            }

            var newSelected = EditorGUILayout.Popup("Binding", selected, labels);
            if (newSelected != selected && newSelected >= 0)
                idProp.stringValue = bindings[newSelected].id.ToString();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif