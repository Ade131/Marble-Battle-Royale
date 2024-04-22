using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Fusion.Addons.KCC.Editor
{
    [CustomPropertyDrawer(typeof(KCCLayerAttribute))]
    public sealed class KCCLayerDrawer : PropertyDrawer
    {
        // PRIVATE MEMBERS

        private int[] _layerIDs;
        private GUIContent[] _layerNames;

        // PropertyDrawer INTERFACE

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (_layerNames == null)
            {
                var layerIDs = new List<int>();
                var layerNames = new List<GUIContent>();

                for (var i = 0; i < 32; ++i)
                {
                    var layerName = LayerMask.LayerToName(i);
                    if (string.IsNullOrEmpty(layerName) == false)
                    {
                        layerIDs.Add(i);
                        layerNames.Add(new GUIContent(layerName));
                    }
                }

                _layerIDs = layerIDs.ToArray();
                _layerNames = layerNames.ToArray();
            }

            var storedLayerIndex = _layerIDs.IndexOf(property.intValue);
            var selectedLayerIndex = EditorGUI.Popup(position, label, storedLayerIndex, _layerNames);

            if (selectedLayerIndex >= 0 && selectedLayerIndex != storedLayerIndex)
            {
                property.intValue = _layerIDs[selectedLayerIndex];

                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }
        }
    }
}