// merged MenuEditor

#region FusionMenuUIScreenEditor.cs

using Fusion.Menu;
using UnityEditor;

namespace Fusion.Editor
{
  /// <summary>
  ///     Debug FusionMenuUIScreen content.
  /// </summary>
  [CustomEditor(typeof(FusionMenuUIScreen), true)]
    public class FusionMenuUIScreenEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var data = (FusionMenuUIScreen)target;

            if (data.ConnectionArgs != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Connect Args", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("Username", data.ConnectionArgs.Username);
                    EditorGUILayout.TextField("Session", data.ConnectionArgs.Session);
                    EditorGUILayout.TextField("PreferredRegion", data.ConnectionArgs.PreferredRegion);
                    EditorGUILayout.TextField("Region", data.ConnectionArgs.Region);
                    EditorGUILayout.TextField("AppVersion", data.ConnectionArgs.AppVersion);
                    EditorGUILayout.TextField("Scene", data.ConnectionArgs.Scene.ScenePath);
                    EditorGUILayout.IntField("MaxPlayerCount", data.ConnectionArgs.MaxPlayerCount);
                    EditorGUILayout.Toggle("Creating", data.ConnectionArgs.Creating);
                }
            }
        }
    }
}

#endregion