using UnityEditor;
using UnityEngine;

namespace Fusion.Addons.KCC.Editor
{
    [CustomEditor(typeof(StatsRecorder), true)]
    public class StatsRecorderInspector : UnityEditor.Editor
    {
        // CONSTANTS

        private const string RECORDER_TYPE_PROPERTY_NAME = "_recorderType";
        private const string REFERENCE_REFRESH_RATE_PROPERTY_NAME = "_referenceRefreshRate";
        private const string INTERPOLATION_DATA_SOURCE_PROPERTY_NAME = "_interpolationDataSource";

        // PRIVATE MEMBERS

        private static readonly string[] _defaultExcludedProperties =
            { INTERPOLATION_DATA_SOURCE_PROPERTY_NAME, REFERENCE_REFRESH_RATE_PROPERTY_NAME };

        private static readonly string[] _monitorExcludedProperties = { INTERPOLATION_DATA_SOURCE_PROPERTY_NAME };

        // Editor INTERFACE

        public override void OnInspectorGUI()
        {
            var statsRecorder = serializedObject.targetObject as StatsRecorder;
            var recorderTypeProperty = serializedObject.FindProperty(RECORDER_TYPE_PROPERTY_NAME);
            var recorderType = (ERecorderType)recorderTypeProperty.intValue;

            if (recorderType != ERecorderType.None && statsRecorder.IsSupported(recorderType) == false)
            {
                EditorGUILayout.HelpBox($"{recorderType} is not supported by {statsRecorder.GetType().Name}!",
                    MessageType.Error);

                DrawPropertiesExcluding(serializedObject, _defaultExcludedProperties);
            }
            else if (recorderType == ERecorderType.MonitorTime)
            {
                DrawPropertiesExcluding(serializedObject, _monitorExcludedProperties);
            }
            else
            {
                DrawPropertiesExcluding(serializedObject, _defaultExcludedProperties);
            }

            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();

                if (statsRecorder.IsActive)
                {
                    if (DrawButton("Stop Recording", Color.red)) statsRecorder.SetActive(false);
                }
                else
                {
                    if (DrawButton("Start Recording", Color.green)) statsRecorder.SetActive(true);
                }
            }
        }

        // PRIVATE METHODS

        private static bool DrawButton(string label, Color color)
        {
            var backupBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            var result = GUILayout.Button(label);
            GUI.backgroundColor = backupBackgroundColor;
            return result;
        }
    }
}