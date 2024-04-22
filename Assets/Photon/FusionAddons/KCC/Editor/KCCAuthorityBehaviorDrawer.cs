using UnityEditor;
using UnityEngine;

namespace Fusion.Addons.KCC.Editor
{
    [CustomPropertyDrawer(typeof(EKCCAuthorityBehavior))]
    public sealed class KCCAuthorityBehaviorDrawer : PropertyDrawer
    {
        // PRIVATE MEMBERS

        private static readonly int[] _behaviorIDs =
        {
            (int)EKCCAuthorityBehavior.PredictFixed_InterpolateRender,
            (int)EKCCAuthorityBehavior.PredictFixed_PredictRender
        };

        private static readonly GUIContent[] _behaviorNames =
            { new("Predict Fixed   |   Interpolate Render"), new("Predict Fixed   |   Predict Render") };

        // PropertyDrawer INTERFACE

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var storedBehaviorIndex = _behaviorIDs.IndexOf(property.intValue);
            if (storedBehaviorIndex < 0)
            {
                storedBehaviorIndex = 0;
                property.intValue = _behaviorIDs[0];
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }

            var selectedBehaviorIndex = EditorGUI.Popup(position, label, storedBehaviorIndex, _behaviorNames);
            if (selectedBehaviorIndex >= 0 && selectedBehaviorIndex != storedBehaviorIndex)
            {
                property.intValue = _behaviorIDs[selectedBehaviorIndex];
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }
        }
    }
}