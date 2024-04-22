using UnityEditor;
using UnityEngine;

namespace Fusion.Addons.KCC.Editor
{
    [CustomPropertyDrawer(typeof(KCCProcessorReferenceAttribute))]
    public sealed class KCCProcessorReferenceDrawer : PropertyDrawer
    {
        // PropertyDrawer INTERFACE

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var currentObject = ResolveCurrentObject(property);
            var selectedObject = EditorGUI.ObjectField(position, label, currentObject, typeof(Object), true);

            if (ReferenceEquals(selectedObject, currentObject))
                return;

            if (selectedObject == null)
            {
                property.objectReferenceValue = selectedObject;
                EditorUtility.SetDirty(property.serializedObject.targetObject);
                return;
            }

            var isResolved = KCCUtility.ResolveProcessor(selectedObject, out var processor, out var gameObject,
                out var component, out var scriptableObject);
            if (isResolved == false)
            {
                Debug.LogError(
                    $"Failed to resolve {nameof(IKCCProcessor)} in {selectedObject.name} ({selectedObject.GetType().FullName})",
                    selectedObject);
                return;
            }

            if (ReferenceEquals(component, null) == false)
            {
                if (ReferenceEquals(component, currentObject) == false)
                {
                    property.objectReferenceValue = component;
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                }
            }
            else if (ReferenceEquals(scriptableObject, null) == false)
            {
                if (ReferenceEquals(scriptableObject, currentObject) == false)
                {
                    property.objectReferenceValue = scriptableObject;
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                }
            }
            else
            {
                Debug.LogError(
                    $"Failed to resolve serializable Unity object in {selectedObject.name} ({selectedObject.GetType().FullName})",
                    selectedObject);
            }
        }

        // PRIVATE METHODS

        private Object ResolveCurrentObject(SerializedProperty property)
        {
            var currentObject = property.objectReferenceValue;
            if (ReferenceEquals(currentObject, null))
                return null;

            var currentProcessor = currentObject as IKCCProcessor;
            if (ReferenceEquals(currentProcessor, null) == false)
            {
                var currentComponent = currentObject as Component;
                if (ReferenceEquals(currentComponent, null) == false)
                    return currentObject;

                var currentScriptableObject = currentObject as ScriptableObject;
                if (ReferenceEquals(currentScriptableObject, null) == false)
                    return currentObject;
            }

            var isResolved = KCCUtility.ResolveProcessor(currentObject, out var processor, out var gameObject,
                out var component, out var scriptableObject);
            if (isResolved)
            {
                if (ReferenceEquals(component, null) == false)
                {
                    property.objectReferenceValue = component;
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                    return component;
                }

                if (ReferenceEquals(scriptableObject, null) == false)
                {
                    property.objectReferenceValue = scriptableObject;
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                    return scriptableObject;
                }
            }

            property.objectReferenceValue = null;
            EditorUtility.SetDirty(property.serializedObject.targetObject);
            return null;
        }
    }
}