using UnityEditor;
using UnityEngine;

namespace Fusion.Addons.KCC
{
    [CustomPropertyDrawer(typeof(DisabledInPlayModeAttribute))]
    public sealed class DisabledInPlayModeDrawer : PropertyDrawer
    {
        // PropertyDrawer INTERFACE

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            EditorGUI.PropertyField(position, property, label, true);
            EditorGUI.EndDisabledGroup();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}