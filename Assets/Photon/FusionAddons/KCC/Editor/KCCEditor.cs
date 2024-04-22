using System;
using Fusion.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusion.Addons.KCC.Editor
{
    [InitializeOnLoad]
    [CustomEditor(typeof(KCC), true)]
    public class KCCEditor : UnityEditor.Editor
    {
        // CONSTANTS

        private const string SETTINGS_PROPERTY_NAME = "_settings";
        private const string INTERPOLATION_DATA_SOURCE_PROPERTY_NAME = "_interpolationDataSource";

        // PRIVATE MEMBERS

        private static readonly string[] _excludedProperties =
            { SETTINGS_PROPERTY_NAME, INTERPOLATION_DATA_SOURCE_PROPERTY_NAME };

        private static bool _localProcessorsFoldout;
        private static bool _modifiersFoldout;
        private static bool _collisionsFoldout;
        private static bool _ignoresFoldout;
        private static bool _hitsFoldout;
        private static bool _resetTools;
        private static GUIStyle _stageButtonStyle;
        private static GUIStyle _buttonFoldoutStyle;
        private static GUIStyle _foldoutDotStyle;

        private void OnSceneGUI()
        {
            _resetTools = true;

            if (Application.isPlaying == false)
                return;

            var kcc = target as KCC;
            if (kcc.Object == null)
                return;

            if (Tools.current == Tool.Move)
            {
                Tools.hidden = true;

                var currentPosition = kcc.Transform.position;
                var newPosition = Handles.PositionHandle(currentPosition, kcc.Transform.rotation);

                if (newPosition.Equals(currentPosition) == false)
                {
                    kcc.FixedData.TargetPosition = newPosition;
                    kcc.RenderData.TargetPosition = newPosition;

                    kcc.SynchronizeTransform(true, false, false);
                }
            }
            else if (Tools.current == Tool.Rotate)
            {
                Tools.hidden = true;

                var currentRotation = kcc.Transform.rotation;
                var newRotation = Handles.RotationHandle(currentRotation, kcc.Transform.position);

                if (newRotation.Equals(currentRotation) == false)
                {
                    var difference = (Quaternion.Inverse(currentRotation) * newRotation).eulerAngles;

                    if (difference.y != 0.0f)
                    {
                        kcc.Data.AddLookRotation(0.0f, difference.y);

                        var lookYaw = kcc.Data.LookYaw;

                        kcc.FixedData.LookYaw = lookYaw;
                        kcc.RenderData.LookYaw = lookYaw;

                        kcc.SynchronizeTransform(false, true, false);
                    }
                }
            }
            else
            {
                Tools.hidden = false;
            }
        }

        // Initialization

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;

            return;

            static void OnSelectionChanged()
            {
                if (_resetTools)
                {
                    _resetTools = false;
                    Tools.hidden = false;
                }
            }
        }

        // Editor INTERFACE

        public override bool RequiresConstantRepaint()
        {
            return true;
        }

        public override void OnInspectorGUI()
        {
            FusionEditorGUI.InjectScriptHeaderDrawer(serializedObject);

            DrawPropertiesExcluding(serializedObject, _excludedProperties);

            var settingsProperty = serializedObject.FindProperty(SETTINGS_PROPERTY_NAME);
            var innerSettingsProperties = settingsProperty.GetEnumerator();
            while (innerSettingsProperties.MoveNext())
                if (innerSettingsProperties.Current is SerializedProperty innerSettingsProperty)
                    if (innerSettingsProperty.propertyPath.IndexOf('.') ==
                        innerSettingsProperty.propertyPath.LastIndexOf('.'))
                        EditorGUILayout.PropertyField(innerSettingsProperty);
            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying == false)
            {
                DrawLine(Color.gray);
                EditorGUILayout.HelpBox("Additional information appears at runtime.", MessageType.Info);
                DrawLine(Color.gray);
                return;
            }

            var kcc = target as KCC;
            if (kcc.Object == null)
                return;

            var kccDebug = kcc.Debug;

            var defaultColor = GUI.color;
            var defaultContentColor = GUI.contentColor;
            var defaultBackgroundColor = GUI.backgroundColor;
            var enabledBackgroundColor = Color.green;
            var disabledBackgroundColor = defaultBackgroundColor;
            var colliderBackgroundColor = defaultBackgroundColor;
            var processorBackgroundColor = Color.cyan;
            var foldoutStaticOffset = 8.0f;
            var foldoutLevelOffset = 20.0f;

            DrawLine(Color.gray);

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical();
                {
                    if (DrawButton("Input Authority", kcc.HasInputAuthority, enabledBackgroundColor,
                            disabledBackgroundColor))
                    {
                    }

                    if (DrawButton("Draw Path", kccDebug.ShowPath, enabledBackgroundColor, disabledBackgroundColor))
                        kccDebug.ShowPath = !kccDebug.ShowPath;
                    if (DrawButton("Draw Ground Normal", kccDebug.ShowGroundNormal, KCCDebug.GroundNormalColor,
                            disabledBackgroundColor)) kccDebug.ShowGroundNormal = !kccDebug.ShowGroundNormal;
                    if (DrawButton("Draw Ground Tangent", kccDebug.ShowGroundTangent, KCCDebug.GroundTangentColor,
                            disabledBackgroundColor)) kccDebug.ShowGroundTangent = !kccDebug.ShowGroundTangent;
                    if (DrawButton("Draw Ground Snapping", kccDebug.ShowGroundSnapping, KCCDebug.GroundSnapingColor,
                            disabledBackgroundColor)) kccDebug.ShowGroundSnapping = !kccDebug.ShowGroundSnapping;
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical();
                {
                    if (DrawButton("State Authority", kcc.HasStateAuthority, enabledBackgroundColor,
                            disabledBackgroundColor))
                    {
                    }

                    if (DrawButton("Draw Speed", kccDebug.ShowSpeed, KCCDebug.SpeedColor, disabledBackgroundColor))
                        kccDebug.ShowSpeed = !kccDebug.ShowSpeed;
                    if (DrawButton("Draw Grounding", kccDebug.ShowGrounding, KCCDebug.IsGroundedColor,
                            disabledBackgroundColor)) kccDebug.ShowGrounding = !kccDebug.ShowGrounding;
                    if (DrawButton("Draw Stepping Up", kccDebug.ShowSteppingUp, KCCDebug.IsSteppingUpColor,
                            disabledBackgroundColor)) kccDebug.ShowSteppingUp = !kccDebug.ShowSteppingUp;
                    if (DrawButton("Draw Move Direction", kccDebug.ShowMoveDirection, KCCDebug.MoveDirectionColor,
                            disabledBackgroundColor)) kccDebug.ShowMoveDirection = !kccDebug.ShowMoveDirection;
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            if (DrawButton("Logs", kccDebug.LogsTime != default, enabledBackgroundColor, disabledBackgroundColor))
                kccDebug.LogsTime = -1.0f;

            if (kccDebug.ShowPath || kccDebug.ShowSpeed || kccDebug.ShowGrounding || kccDebug.ShowGroundSnapping ||
                kccDebug.ShowGroundNormal || kccDebug.ShowMoveDirection || kccDebug.ShowSteppingUp ||
                kccDebug.ShowGroundTangent)
                kccDebug.DisplayTime = EditorGUILayout.Slider("Display Time", kccDebug.DisplayTime, 1.0f, 60.0f);

            if (kccDebug.ShowPath)
                kccDebug.PointSize = EditorGUILayout.Slider("Point Size", kccDebug.PointSize, 0.001f, 0.1f);

            if (kccDebug.ShowSpeed)
                kccDebug.SpeedScale = EditorGUILayout.Slider("Speed Scale", kccDebug.SpeedScale, 0.01f, 1.0f);

            GUI.backgroundColor = defaultBackgroundColor;

            DrawLine(Color.gray);

            var data = kcc.FixedData;

            EditorGUILayout.LabelField("Word Count", kcc.DynamicWordCount.Value.ToString());
            EditorGUILayout.LabelField("Shape", kcc.Settings.Shape.ToString());
            EditorGUILayout.Toggle("Is Active", data.IsActive);
            EditorGUILayout.Toggle("Is Grounded", data.IsGrounded);
            EditorGUILayout.Toggle("Is On Edge", data.IsOnEdge);
            EditorGUILayout.Toggle("Is Stepping Up", data.IsSteppingUp);
            EditorGUILayout.Toggle("Is Snapping To Ground", data.IsSnappingToGround);
            EditorGUILayout.Toggle("Has Manual Update", kcc.HasManualUpdate);
            EditorGUILayout.LabelField("Look Pitch", data.LookPitch.ToString("0.00°"));
            EditorGUILayout.LabelField("Look Yaw", data.LookYaw.ToString("0.00°"));
            EditorGUILayout.LabelField("Real Speed", data.RealSpeed.ToString("0.00"));
            EditorGUILayout.LabelField("Ground Angle", data.GroundAngle.ToString("0.00°"));
            EditorGUILayout.LabelField("Ground Distance", data.IsGrounded ? data.GroundDistance.ToString("F6") : "N/A");
            EditorGUILayout.LabelField("Collision Hits", data.Hits.Count.ToString());
            EditorGUILayout.LabelField("Prediction Error", kcc.PredictionError.magnitude.ToString("F6"));
            EditorGUILayout.EnumFlagsField("Active Features", kcc.ActiveFeatures);

            DrawLine(Color.gray);

            if (_foldoutDotStyle == null)
            {
                _foldoutDotStyle = new GUIStyle(GUI.skin.label);
                _foldoutDotStyle.alignment = TextAnchor.MiddleCenter;
                _foldoutDotStyle.fixedWidth = foldoutLevelOffset * 0.5f;
                _foldoutDotStyle.stretchHeight = true;
            }

            var localProcessors = kcc.LocalProcessors;

            if (DrawButtonFoldout($"Local Processors ({localProcessors.Count})", ref _localProcessorsFoldout,
                    enabledBackgroundColor, disabledBackgroundColor) && localProcessors.Count > 0)
                for (var i = 0; i < localProcessors.Count; ++i)
                {
                    var processor = localProcessors[i] as Component;
                    if (processor != null)
                        DrawPingDotButton($"{processor.gameObject.name}\n({processor.GetType().Name})",
                            processor.gameObject, foldoutStaticOffset, processorBackgroundColor);
                }

            var modifiers = data.Modifiers.All;

            if (DrawButtonFoldout($"Networked Modifiers ({modifiers.Count})", ref _modifiersFoldout,
                    enabledBackgroundColor, disabledBackgroundColor) && modifiers.Count > 0)
                for (var i = 0; i < modifiers.Count; ++i)
                {
                    var processor = modifiers[i].Processor as Component;
                    var provider = modifiers[i].Provider as Component;

                    if (processor != null)
                        DrawPingDotButton($"{processor.gameObject.name}\n({processor.GetType().Name})",
                            processor.gameObject, foldoutStaticOffset, processorBackgroundColor);
                    else if (provider != null)
                        DrawPingDotButton($"{provider.gameObject.name}\n({provider.GetType().Name})",
                            provider.gameObject, foldoutStaticOffset, processorBackgroundColor);
                    else
                        DrawPingDotButton($"{modifiers[i].NetworkObject.gameObject.name}",
                            modifiers[i].NetworkObject.gameObject, foldoutStaticOffset, processorBackgroundColor);
                }

            var collisions = data.Collisions.All;

            if (DrawButtonFoldout($"Networked Collisions ({collisions.Count})", ref _collisionsFoldout,
                    enabledBackgroundColor, disabledBackgroundColor) && collisions.Count > 0)
                for (var i = 0; i < collisions.Count; ++i)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(foldoutStaticOffset);
                        GUILayout.Label("•", _foldoutDotStyle);

                        var collision = collisions[i];

                        DrawPingButton(
                            $"{collision.Collider.name}\n{(collision.Collider.isTrigger ? "[Trigger] " : "")}({collision.Collider.GetType().Name})",
                            collision.Collider.gameObject, colliderBackgroundColor);

                        var processor = collision.Processor as Component;
                        var provider = collision.Provider as Component;

                        if (processor != null)
                            DrawPingButton($"{processor.gameObject.name}\n({processor.GetType().Name})",
                                processor.gameObject, processorBackgroundColor);
                        else if (provider != null)
                            DrawPingButton($"{provider.gameObject.name}\n({provider.GetType().Name})",
                                provider.gameObject, processorBackgroundColor);
                    }
                    EditorGUILayout.EndHorizontal();
                }

            var ignores = data.Ignores.All;

            if (DrawButtonFoldout($"Ignored Colliders ({ignores.Count})", ref _ignoresFoldout, enabledBackgroundColor,
                    disabledBackgroundColor) && ignores.Count > 0)
                for (var i = 0; i < ignores.Count; ++i)
                {
                    var ignore = ignores[i];

                    DrawPingDotButton(
                        $"{ignore.Collider.name}\n{(ignore.Collider.isTrigger ? "[Trigger] " : "")}({ignore.Collider.GetType().Name})",
                        ignore.Collider.gameObject, foldoutStaticOffset, colliderBackgroundColor);
                }

            var hits = data.Hits.All;

            if (DrawButtonFoldout($"Collision Hits ({hits.Count})", ref _hitsFoldout, enabledBackgroundColor,
                    disabledBackgroundColor) && hits.Count > 0)
                for (var i = 0; i < hits.Count; ++i)
                {
                    var hit = hits[i];

                    var colliderInfo = hit.CollisionType != default ? $"[{hit.CollisionType}] " : "[---] ";

                    DrawPingDotButton($"{hit.Collider.name}\n{colliderInfo}({hit.Collider.GetType().Name})",
                        hit.Collider.gameObject, foldoutStaticOffset);
                }

            var traceExecution = kccDebug.TraceExecution;
            if (traceExecution == false || kccDebug.TraceInfoCount <= 0)
            {
                DrawButtonFoldout("Execution Stack", ref traceExecution, enabledBackgroundColor,
                    disabledBackgroundColor);
            }
            else
            {
                GUILayout.BeginHorizontal();

                var stageTraces = 0;
                var processorTraces = 0;

                for (var i = 0; i < kccDebug.TraceInfoCount; ++i)
                {
                    var traceInfo = kccDebug.TraceInfos[i];
                    if (traceInfo.Trace == EKCCTrace.Stage)
                        ++stageTraces;
                    else if (traceInfo.Trace == EKCCTrace.Processor) ++processorTraces;
                }

                DrawButtonFoldout($"Execution Stack ({stageTraces} + {processorTraces})", ref traceExecution,
                    enabledBackgroundColor, disabledBackgroundColor);

                if (GUILayout.Button("▼", GUILayout.Width(24.0f)))
                {
                    traceExecution = true;

                    for (var i = 0; i < kccDebug.TraceInfoCount; ++i)
                    {
                        var traceInfo = kccDebug.TraceInfos[i];
                        traceInfo.IsVisible = true;
                    }
                }

                if (GUILayout.Button("▲", GUILayout.Width(24.0f)))
                    for (var i = 0; i < kccDebug.TraceInfoCount; ++i)
                    {
                        var traceInfo = kccDebug.TraceInfos[i];
                        traceInfo.IsVisible = false;
                    }

                GUILayout.EndHorizontal();
            }

            if (kccDebug.TraceExecution != traceExecution)
            {
                kccDebug.TraceExecution = traceExecution;
                kccDebug.TraceInfoCount = default;
            }

            if (traceExecution && kccDebug.TraceInfoCount > 0)
            {
                if (_stageButtonStyle == null)
                {
                    _stageButtonStyle = new GUIStyle(GUI.skin.button);
                    _stageButtonStyle.alignment = TextAnchor.MiddleLeft;
                }

                /*kccDebug.TraceExecution = false;
                for (int i = 0; i < kccDebug.TraceInfoCount; ++i)
                {
                    Debug.LogError($"{kccDebug.TraceInfos[i].Trace} {kccDebug.TraceInfos[i].Name} {kccDebug.TraceInfos[i].Level} {kccDebug.TraceInfos[i].Processor}");
                }
                kccDebug.TraceInfoCount = 0;*/

                for (var i = 0; i < kccDebug.TraceInfoCount; ++i)
                {
                    var traceInfo = kccDebug.TraceInfos[i];
                    if (traceInfo.IsStage && traceInfo.Level == default)
                        DrawStageTrace(kccDebug.TraceInfos, kccDebug.TraceInfoCount, i, default);
                }
            }

            DrawLine(Color.gray);

            GUI.color = defaultColor;
            GUI.contentColor = defaultContentColor;
            GUI.backgroundColor = defaultBackgroundColor;

            void DrawStageTrace(KCCTraceInfo[] traceInfos, int traceInfoCount, int traceInfoIndex, int indentLevel)
            {
                var stageTraceInfo = traceInfos[traceInfoIndex];
                if (stageTraceInfo.IsStage == false)
                {
                    Debug.LogError($"Unexpected trace {stageTraceInfo.Trace}!");
                    return;
                }

                var childCount = 0;
                var childStages = 0;
                var childProcessors = 0;
                var childStageLevel = stageTraceInfo.Level + 1;
                var childProcessorLevel = stageTraceInfo.Level;

                for (var i = traceInfoIndex + 1; i < traceInfoCount; ++i)
                {
                    var nextTraceInfo = traceInfos[i];
                    var nextTraceLevel = nextTraceInfo.Level;

                    if (nextTraceInfo.IsStage)
                    {
                        if (nextTraceLevel < childStageLevel)
                            break;
                        if (nextTraceLevel > childStageLevel)
                            continue;

                        ++childStages;
                    }
                    else if (nextTraceInfo.IsProcessor)
                    {
                        if (nextTraceLevel < childProcessorLevel)
                            break;
                        if (nextTraceLevel > childProcessorLevel)
                            continue;

                        ++childProcessors;
                    }
                    else
                    {
                        throw new NotImplementedException(nextTraceInfo.Trace.ToString());
                    }

                    ++childCount;
                }

                var indent = foldoutStaticOffset + indentLevel * foldoutLevelOffset;

                GUILayout.BeginHorizontal();
                GUILayout.Space(indent);
                GUILayout.Label("•", _foldoutDotStyle);
                DrawButtonFoldout($"{stageTraceInfo.Name} ({childProcessors})", ref stageTraceInfo.IsVisible,
                    Color.yellow, disabledBackgroundColor, _stageButtonStyle);
                GUILayout.EndHorizontal();

                if (stageTraceInfo.IsVisible && childCount > 0)
                {
                    var nextIndentLevel = indentLevel + 1;

                    while (true)
                    {
                        ++traceInfoIndex;
                        if (traceInfoIndex >= traceInfoCount)
                            break;

                        var nextTraceInfo = traceInfos[traceInfoIndex];
                        var nextTraceLevel = nextTraceInfo.Level;

                        if (nextTraceInfo.IsStage)
                        {
                            if (nextTraceLevel < childStageLevel)
                                break;
                            if (nextTraceLevel > childStageLevel)
                                continue;

                            DrawStageTrace(traceInfos, traceInfoCount, traceInfoIndex, nextIndentLevel);
                        }
                        else if (nextTraceInfo.IsProcessor)
                        {
                            if (nextTraceLevel < childProcessorLevel)
                                break;
                            if (nextTraceLevel > childProcessorLevel)
                                continue;

                            var processor = nextTraceInfo.Processor;
                            GameObject gameObject = null;
                            var name = "N/A";

                            if (processor is Component processorComponent)
                            {
                                gameObject = processorComponent.gameObject;
                                name = gameObject.name;
                            }

                            DrawPingDotButton($"{name}\n({processor.GetType().Name})", gameObject,
                                indent + foldoutLevelOffset, processorBackgroundColor);

                            nextIndentLevel = indentLevel + 2;
                        }
                        else
                        {
                            throw new NotImplementedException(nextTraceInfo.Trace.ToString());
                        }
                    }
                }
            }
        }

        // PRIVATE METHODS

        public static void DrawLine(Color color, float thickness = 1.0f, float padding = 10.0f)
        {
            var controlRect = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));

            controlRect.height = thickness;
            controlRect.y += padding * 0.5f;

            EditorGUI.DrawRect(controlRect, color);
        }

        private static bool DrawButton(string label, bool condition, Color enabledBackgroundColor,
            Color disabledBackgroundColor)
        {
            var backupBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = condition ? enabledBackgroundColor : disabledBackgroundColor;
            var result = GUILayout.Button(label);
            GUI.backgroundColor = backupBackgroundColor;
            return result;
        }

        private static bool DrawButton(string label, bool condition, Color enabledBackgroundColor,
            Color disabledBackgroundColor, GUIStyle style)
        {
            var backupBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = condition ? enabledBackgroundColor : disabledBackgroundColor;
            var result = GUILayout.Button(label, style);
            GUI.backgroundColor = backupBackgroundColor;
            return result;
        }

        private static bool DrawPingButton(string label, GameObject gameObject, Color backgroundColor)
        {
            var backupBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = backgroundColor;
            var result = GUILayout.Button(label);
            GUI.backgroundColor = backupBackgroundColor;
            if (result) EditorGUIUtility.PingObject(gameObject);
            return result;
        }

        private static bool DrawPingButton(string label, GameObject gameObject, Color backgroundColor, GUIStyle style)
        {
            var backupBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = backgroundColor;
            var result = GUILayout.Button(label, style);
            GUI.backgroundColor = backupBackgroundColor;
            if (result) EditorGUIUtility.PingObject(gameObject);
            return result;
        }

        private static bool DrawPingButton(string label, GameObject gameObject, bool condition,
            Color enabledBackgroundColor, Color disabledBackgroundColor)
        {
            var backupBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = condition ? enabledBackgroundColor : disabledBackgroundColor;
            var result = GUILayout.Button(label);
            GUI.backgroundColor = backupBackgroundColor;
            if (result) EditorGUIUtility.PingObject(gameObject);
            return result;
        }

        private static bool DrawPingButton(string label, GameObject gameObject, bool condition,
            Color enabledBackgroundColor, Color disabledBackgroundColor, GUIStyle style)
        {
            var backupBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = condition ? enabledBackgroundColor : disabledBackgroundColor;
            var result = GUILayout.Button(label, style);
            GUI.backgroundColor = backupBackgroundColor;
            if (result) EditorGUIUtility.PingObject(gameObject);
            return result;
        }

        private static bool DrawPingDotButton(string label, GameObject gameObject, float offset)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(offset);
            GUILayout.Label("•", _foldoutDotStyle);

            var result = GUILayout.Button(label);
            if (result) EditorGUIUtility.PingObject(gameObject);

            GUILayout.EndHorizontal();

            return result;
        }

        private static bool DrawPingDotButton(string label, GameObject gameObject, float offset, Color backgroundColor)
        {
            var backupBackgroundColor = GUI.backgroundColor;

            GUILayout.BeginHorizontal();
            GUILayout.Space(offset);
            GUILayout.Label("•", _foldoutDotStyle);

            GUI.backgroundColor = backgroundColor;
            var result = GUILayout.Button(label);
            GUI.backgroundColor = backupBackgroundColor;
            if (result) EditorGUIUtility.PingObject(gameObject);

            GUILayout.EndHorizontal();

            return result;
        }

        private static bool DrawButtonFoldout(string label, ref bool condition, Color enabledBackgroundColor,
            Color disabledBackgroundColor)
        {
            return DrawButtonFoldout("▼", "▶", label, ref condition, enabledBackgroundColor, disabledBackgroundColor);
        }

        private static bool DrawButtonFoldout(string label, ref bool condition, Color enabledBackgroundColor,
            Color disabledBackgroundColor, GUIStyle style)
        {
            return DrawButtonFoldout("▼", "▶", label, ref condition, enabledBackgroundColor, disabledBackgroundColor,
                style);
        }

        private static bool DrawButtonFoldout(string enabledPrefix, string disabledPrefix, string label,
            ref bool condition, Color enabledBackgroundColor, Color disabledBackgroundColor)
        {
            if (_buttonFoldoutStyle == null)
            {
                _buttonFoldoutStyle = new GUIStyle(GUI.skin.button);
                _buttonFoldoutStyle.alignment = TextAnchor.MiddleLeft;
            }

            return DrawButtonFoldout(enabledPrefix, disabledPrefix, label, ref condition, enabledBackgroundColor,
                disabledBackgroundColor, _buttonFoldoutStyle);
        }

        private static bool DrawButtonFoldout(string enabledPrefix, string disabledPrefix, string label,
            ref bool condition, Color enabledBackgroundColor, Color disabledBackgroundColor, GUIStyle style)
        {
            var backupBackgroundColor = GUI.backgroundColor;

            if (condition)
            {
                GUI.backgroundColor = enabledBackgroundColor;
                if (string.IsNullOrEmpty(enabledPrefix) == false) label = $"{enabledPrefix} {label}";
            }
            else
            {
                GUI.backgroundColor = disabledBackgroundColor;
                if (string.IsNullOrEmpty(disabledPrefix) == false) label = $"{disabledPrefix} {label}";
            }

            if (GUILayout.Button(label, style)) condition = !condition;

            GUI.backgroundColor = backupBackgroundColor;

            return condition;
        }
    }
}