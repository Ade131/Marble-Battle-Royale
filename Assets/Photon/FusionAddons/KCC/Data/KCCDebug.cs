using System;
using System.Text;
using UnityEngine;

namespace Fusion.Addons.KCC
{
    /// <summary>
    ///     Debug configuration, logging, tracking, visualization.
    /// </summary>
    public sealed class KCCDebug
    {
        public static readonly Color FixedPathColor = Color.red;
        public static readonly Color RenderPathColor = Color.green;
        public static readonly Color FixedToRenderPathColor = Color.blue;
        public static readonly Color PredictionCorrectionColor = Color.yellow;
        public static readonly Color PredictionErrorColor = Color.magenta;
        public static readonly Color IsGroundedColor = Color.green;
        public static readonly Color WasGroundedColor = Color.red;
        public static readonly Color SpeedColor = Color.green;
        public static readonly Color IsSteppingUpColor = Color.green;
        public static readonly Color WasSteppingUpColor = Color.red;
        public static readonly Color GroundNormalColor = Color.magenta;
        public static readonly Color GroundTangentColor = Color.yellow;
        public static readonly Color GroundSnapingColor = Color.cyan;
        public static readonly Color GroundSnapTargetColor = Color.blue;
        public static readonly Color GroundSnapPositionColor = Color.red;
        public static readonly Color MoveDirectionColor = Color.yellow;

        // PRIVATE MEMBERS

        public StringBuilder _stringBuilder = new(1024);

        public float DisplayTime = 30.0f;
        // PUBLIC MEMBERS

        public float LogsTime;
        public float PointSize = 0.01f;
        public bool ShowGrounding;
        public bool ShowGroundNormal;
        public bool ShowGroundSnapping;
        public bool ShowGroundTangent;
        public bool ShowMoveDirection;
        public bool ShowPath;
        public bool ShowSpeed;
        public bool ShowSteppingUp;
        public float SpeedScale = 0.1f;
        public bool TraceExecution;
        public int TraceInfoCount;
        public KCCTraceInfo[] TraceInfos = new KCCTraceInfo[0];

        // PUBLIC METHODS

        public void SetDefaults()
        {
            LogsTime = default;
            ShowPath = default;
            ShowSpeed = default;
            ShowGrounding = default;
            ShowSteppingUp = default;
            ShowGroundSnapping = default;
            ShowGroundNormal = default;
            ShowGroundTangent = default;
            ShowMoveDirection = default;
            TraceExecution = default;
            TraceInfoCount = default;
            DisplayTime = 30.0f;
            SpeedScale = 0.1f;
            PointSize = 0.01f;

            if (TraceInfos.Length > 0) Array.Clear(TraceInfos, 0, TraceInfos.Length);
        }

        public void BeforePredictedFixedMove(KCC kcc)
        {
            TraceInfoCount = default;
        }

        public void AfterPredictedFixedMove(KCC kcc)
        {
#if UNITY_EDITOR
            if (ShowPath)
            {
                var fixedData = kcc.FixedData;

                Debug.DrawLine(fixedData.BasePosition, fixedData.TargetPosition, FixedPathColor, DisplayTime);

                DrawPoint(fixedData.TargetPosition, FixedPathColor, PointSize, DisplayTime);
            }
#endif

            if (LogsTime != default)
            {
                if (LogsTime > 0.0f && Time.realtimeSinceStartup >= LogsTime) LogsTime = default;

                Log(kcc, true);
            }
        }

        public void AfterRenderUpdate(KCC kcc)
        {
#if UNITY_EDITOR
            var fixedData = kcc.FixedData;
            var renderData = kcc.RenderData;

            if (ShowPath)
            {
                if (kcc.IsPredictingInRenderUpdate)
                    Debug.DrawLine(renderData.BasePosition, renderData.TargetPosition, RenderPathColor, DisplayTime);

                DrawPoint(renderData.TargetPosition, RenderPathColor, PointSize, DisplayTime);
            }

            var selectedData = kcc.Object.IsInSimulation ? fixedData : renderData;

            if (ShowSpeed)
                Debug.DrawLine(selectedData.TargetPosition,
                    selectedData.TargetPosition + Vector3.up * selectedData.RealVelocity.magnitude * SpeedScale,
                    SpeedColor, DisplayTime);

            if (ShowGrounding)
            {
                if (selectedData.IsGrounded && selectedData.WasGrounded == false)
                    Debug.DrawLine(selectedData.TargetPosition, selectedData.TargetPosition + Vector3.up,
                        IsGroundedColor, DisplayTime);
                else if (selectedData.IsGrounded == false && selectedData.WasGrounded)
                    Debug.DrawLine(selectedData.BasePosition, selectedData.BasePosition + Vector3.up, WasGroundedColor,
                        DisplayTime);
            }

            if (ShowSteppingUp)
            {
                if (selectedData.IsSteppingUp && selectedData.WasSteppingUp == false)
                    Debug.DrawLine(selectedData.TargetPosition, selectedData.TargetPosition + Vector3.up,
                        IsSteppingUpColor, DisplayTime);
                else if (selectedData.IsSteppingUp == false && selectedData.WasSteppingUp)
                    Debug.DrawLine(selectedData.TargetPosition, selectedData.TargetPosition + Vector3.up,
                        WasSteppingUpColor, DisplayTime);
            }

            if (ShowGroundNormal)
                Debug.DrawLine(selectedData.TargetPosition, selectedData.TargetPosition + selectedData.GroundNormal,
                    GroundNormalColor, DisplayTime);
            if (ShowGroundTangent)
                Debug.DrawLine(selectedData.TargetPosition, selectedData.TargetPosition + selectedData.GroundTangent,
                    GroundTangentColor, DisplayTime);
            if (ShowMoveDirection)
                Debug.DrawLine(selectedData.TargetPosition,
                    selectedData.TargetPosition + selectedData.RealVelocity.ClampToNormalized(), MoveDirectionColor,
                    DisplayTime);
#endif

            if (LogsTime != default)
            {
                if (LogsTime > 0.0f && Time.realtimeSinceStartup >= LogsTime) LogsTime = default;

                Log(kcc, false);
            }
        }

        public void DrawGroundSnapping(Vector3 targetPosition, Vector3 targetGroundedPosition,
            Vector3 targetSnappedPosition, bool isInFixedUpdate)
        {
            if (isInFixedUpdate == false)
                return;
            if (ShowGroundSnapping == false)
                return;

            Debug.DrawLine(targetPosition, targetPosition + Vector3.up, GroundSnapingColor, DisplayTime);
            Debug.DrawLine(targetPosition, targetGroundedPosition, GroundSnapTargetColor, DisplayTime);
            Debug.DrawLine(targetPosition, targetSnappedPosition, GroundSnapPositionColor, DisplayTime);
        }

        public bool TraceStage(KCC kcc, Type type, int level)
        {
            if (TraceExecution == false)
                return false;
            if (kcc.IsInFixedUpdate == false)
                return false;

            if (TraceInfoCount >= TraceInfos.Length) Array.Resize(ref TraceInfos, TraceInfos.Length + KCC.CACHE_SIZE);

            var traceInfo = TraceInfos[TraceInfoCount];
            if (traceInfo == null)
            {
                traceInfo = new KCCTraceInfo();
                TraceInfos[TraceInfoCount] = traceInfo;
            }

            traceInfo.Set(EKCCTrace.Stage, type, type.Name, level, default);
            ++TraceInfoCount;

            return true;
        }

        public bool TraceStage(KCC kcc, Type type, string name, int level)
        {
            if (TraceExecution == false)
                return false;
            if (kcc.IsInFixedUpdate == false)
                return false;

            if (TraceInfoCount >= TraceInfos.Length) Array.Resize(ref TraceInfos, TraceInfos.Length + KCC.CACHE_SIZE);

            var traceInfo = TraceInfos[TraceInfoCount];
            if (traceInfo == null)
            {
                traceInfo = new KCCTraceInfo();
                TraceInfos[TraceInfoCount] = traceInfo;
            }

            traceInfo.Set(EKCCTrace.Stage, type, name, level, default);
            ++TraceInfoCount;

            return true;
        }

        public bool TraceProcessor(IKCCProcessor processor, int level)
        {
            if (TraceExecution == false)
                return false;

            if (TraceInfoCount >= TraceInfos.Length) Array.Resize(ref TraceInfos, TraceInfos.Length + KCC.CACHE_SIZE);

            var traceInfo = TraceInfos[TraceInfoCount];
            if (traceInfo == null)
            {
                traceInfo = new KCCTraceInfo();
                TraceInfos[TraceInfoCount] = traceInfo;
            }

            traceInfo.Set(EKCCTrace.Processor, default, default, level, processor);
            ++TraceInfoCount;

            return true;
        }

        [HideInCallstack]
        public void Dump(KCC kcc)
        {
            Log(kcc, kcc.IsInFixedUpdate);
        }

        public void EnableLogs(KCC kcc, float duration)
        {
            if (duration == default)
                LogsTime = default;
            else if (duration >= 0.0f)
                LogsTime = Time.realtimeSinceStartup + duration;
            else
                LogsTime = -1.0f;
        }

        // PRIVATE METHODS

        [HideInCallstack]
        private void Log(KCC kcc, bool isInFixedUpdate)
        {
            var data = kcc.Data;

            _stringBuilder.Clear();

            {
                _stringBuilder.Append($" | {nameof(data.Alpha)} {data.Alpha.ToString("F4")}");
                _stringBuilder.Append($" | {nameof(data.Time)} {data.Time.ToString("F6")}");
                _stringBuilder.Append($" | {nameof(data.DeltaTime)} {data.DeltaTime.ToString("F6")}");

                _stringBuilder.Append($" | {nameof(data.BasePosition)} {data.BasePosition.ToString("F4")}");
                _stringBuilder.Append($" | {nameof(data.DesiredPosition)} {data.DesiredPosition.ToString("F4")}");
                _stringBuilder.Append($" | {nameof(data.TargetPosition)} {data.TargetPosition.ToString("F4")}");
                _stringBuilder.Append($" | {nameof(data.LookPitch)} {data.LookPitch.ToString("0.00°")}");
                _stringBuilder.Append($" | {nameof(data.LookYaw)} {data.LookYaw.ToString("0.00°")}");

                _stringBuilder.Append($" | {nameof(data.InputDirection)} {data.InputDirection.ToString("F4")}");
                _stringBuilder.Append($" | {nameof(data.DynamicVelocity)} {data.DynamicVelocity.ToString("F4")}");
                _stringBuilder.Append($" | {nameof(data.KinematicSpeed)} {data.KinematicSpeed.ToString("F4")}");
                _stringBuilder.Append($" | {nameof(data.KinematicTangent)} {data.KinematicTangent.ToString("F4")}");
                _stringBuilder.Append($" | {nameof(data.KinematicDirection)} {data.KinematicDirection.ToString("F4")}");
                _stringBuilder.Append($" | {nameof(data.KinematicVelocity)} {data.KinematicVelocity.ToString("F4")}");

                _stringBuilder.Append($" | {nameof(data.IsGrounded)} {(data.IsGrounded ? "1" : "0")}");
                _stringBuilder.Append($" | {nameof(data.WasGrounded)} {(data.WasGrounded ? "1" : "0")}");
                _stringBuilder.Append($" | {nameof(data.IsOnEdge)} {(data.IsOnEdge ? "1" : "0")}");
                _stringBuilder.Append($" | {nameof(data.IsSteppingUp)} {(data.IsSteppingUp ? "1" : "0")}");
                _stringBuilder.Append($" | {nameof(data.WasSteppingUp)} {(data.WasSteppingUp ? "1" : "0")}");
                _stringBuilder.Append($" | {nameof(data.IsSnappingToGround)} {(data.IsSnappingToGround ? "1" : "0")}");
                _stringBuilder.Append(
                    $" | {nameof(data.WasSnappingToGround)} {(data.WasSnappingToGround ? "1" : "0")}");
                _stringBuilder.Append($" | {nameof(data.JumpFrames)} {data.JumpFrames.ToString()}");
                _stringBuilder.Append($" | {nameof(data.HasJumped)} {(data.HasJumped ? "1" : "0")}");
                _stringBuilder.Append($" | {nameof(data.HasTeleported)} {(data.HasTeleported ? "1" : "0")}");

                _stringBuilder.Append($" | {nameof(data.GroundNormal)} {data.GroundNormal.ToString("F4")}");
                _stringBuilder.Append($" | {nameof(data.GroundTangent)} {data.GroundTangent.ToString("F4")}");
                _stringBuilder.Append($" | {nameof(data.GroundPosition)} {data.GroundPosition.ToString("F4")}");
                _stringBuilder.Append($" | {nameof(data.GroundDistance)} {data.GroundDistance.ToString("F4")}");
                _stringBuilder.Append($" | {nameof(data.GroundAngle)} {data.GroundAngle.ToString("0.00°")}");

                _stringBuilder.Append($" | {nameof(data.RealSpeed)} {data.RealSpeed.ToString("F4")}");
                _stringBuilder.Append($" | {nameof(data.RealVelocity)} {data.RealVelocity.ToString("F4")}");

                _stringBuilder.Append($" | {nameof(data.Collisions)} {data.Collisions.Count.ToString()}");
                _stringBuilder.Append($" | {nameof(data.Modifiers)} {data.Modifiers.Count.ToString()}");
                _stringBuilder.Append($" | {nameof(data.Ignores)} {data.Ignores.Count.ToString()}");
                _stringBuilder.Append($" | {nameof(data.Hits)} {data.Hits.Count.ToString()}");
            }

            if (isInFixedUpdate == false)
                _stringBuilder.Append($" | {nameof(kcc.PredictionError)} {kcc.PredictionError.ToString("F4")}");

            kcc.Log(_stringBuilder.ToString());
        }

        private static void DrawPoint(Vector3 position, Color color, float size, float displayTime)
        {
            var pX = position + new Vector3(size, 0.0f, 0.0f);
            var nX = position + new Vector3(-size, 0.0f, 0.0f);
            var pY = position + new Vector3(0.0f, size, 0.0f);
            var nY = position + new Vector3(0.0f, -size, 0.0f);
            var pZ = position + new Vector3(0.0f, 0.0f, size);
            var nZ = position + new Vector3(0.0f, 0.0f, -size);

            Debug.DrawLine(pY, pX, color, displayTime);
            Debug.DrawLine(pY, nX, color, displayTime);
            Debug.DrawLine(pY, pZ, color, displayTime);
            Debug.DrawLine(pY, nZ, color, displayTime);

            Debug.DrawLine(nY, pX, color, displayTime);
            Debug.DrawLine(nY, nX, color, displayTime);
            Debug.DrawLine(nY, pZ, color, displayTime);
            Debug.DrawLine(nY, nZ, color, displayTime);
        }
    }
}