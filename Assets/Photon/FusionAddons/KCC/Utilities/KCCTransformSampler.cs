using System;
using UnityEngine;

namespace Fusion.Addons.KCC
{
    public sealed class KCCTransformSampler : MonoBehaviour
    {
        // CONSTANTS

        private const int HISTORY_SIZE = KCC.HISTORY_SIZE;

        // PRIVATE MEMBERS

        [SerializeField] private Transform _target;

        [SerializeField] private bool _enableLogs;

        private readonly TransformSample _lastFixedSample = new();
        private int _lastPredictedFixedTick;
        private int _lastPredictedRenderFrame;
        private readonly TransformSample _lastRenderSample = new();

        private TransformSample[] _samples;

        // PUBLIC METHODS

        public void Sample(KCC kcc)
        {
            if (_samples == null) _samples = new TransformSample[HISTORY_SIZE];

            if (kcc.IsInFixedUpdate)
                SampleFixedUpdate(kcc.Runner.Tick);
            else
                SampleRenderUpdate(kcc.Runner.Tick, kcc.Runner.LocalAlpha);
        }

        public bool ResolveRenderPositionAndRotation(KCC kcc, float renderAlpha, out Vector3 renderPosition,
            out Quaternion renderRotation)
        {
            if (_samples == null)
            {
                _target.GetPositionAndRotation(out renderPosition, out renderRotation);
                return false;
            }

            renderAlpha = Mathf.Clamp01(renderAlpha);

            KCCData fromKCCData;
            KCCData toKCCData;
            TransformSample fromSample;
            TransformSample toSample;

            bool useAntiJitter;
            var shiftPosition = false;

            if (kcc.IsInFixedUpdate)
            {
                useAntiJitter = true;

                int currentTick = kcc.Runner.Tick;

                if (kcc.Settings.InputAuthorityBehavior == EKCCAuthorityBehavior.PredictFixed_PredictRender)
                {
                    if (_lastPredictedFixedTick < currentTick)
                    {
                        if (_enableLogs)
                            kcc.LogError(
                                "Missing data for calculation of render position and rotation for current tick. The KCC is set to predict in render, therefore Sample() must be called before this method!");

                        _target.GetPositionAndRotation(out renderPosition, out renderRotation);
                        return false;
                    }

                    if (kcc.LastPredictedFixedTick < currentTick)
                    {
                        if (_enableLogs)
                            kcc.LogError(
                                "Missing data for calculation of render position and rotation for current tick. The KCC is set to predict in render and must run fixed update for current tick before calling this method!");

                        _target.GetPositionAndRotation(out renderPosition, out renderRotation);
                        return false;
                    }

                    fromKCCData = kcc.GetHistoryData(currentTick - 1);
                    toKCCData = kcc.GetHistoryData(currentTick);
                    fromSample = GetFixedSample(currentTick - 1);
                    toSample = GetFixedSample(currentTick);
                }
                else if (kcc.Settings.InputAuthorityBehavior == EKCCAuthorityBehavior.PredictFixed_InterpolateRender)
                {
                    if (kcc.Settings.ForcePredictedLookRotation)
                    {
                        if (_lastPredictedFixedTick < currentTick)
                        {
                            if (_enableLogs)
                                kcc.LogError(
                                    "Missing data for calculation of render position and rotation for current tick. The KCC has Force Predicted Look Rotation enabled, therefore Sample() must be called before this method!");

                            _target.GetPositionAndRotation(out renderPosition, out renderRotation);
                            return false;
                        }

                        if (kcc.LastPredictedFixedTick < currentTick)
                        {
                            if (_enableLogs)
                                kcc.LogError(
                                    "Missing data for calculation of render position and rotation for current tick. The KCC has Force Predicted Look Rotation enabled and must run fixed update for current tick before calling this method!");

                            _target.GetPositionAndRotation(out renderPosition, out renderRotation);
                            return false;
                        }

                        fromKCCData = kcc.GetHistoryData(currentTick - 2);
                        toKCCData = kcc.GetHistoryData(currentTick - 1);
                        fromSample = GetFixedSample(currentTick - 1);
                        toSample = GetFixedSample(currentTick);

                        shiftPosition = true;
                    }
                    else
                    {
                        fromKCCData = kcc.GetHistoryData(currentTick - 2);
                        toKCCData = kcc.GetHistoryData(currentTick - 1);
                        fromSample = GetFixedSample(currentTick - 2);
                        toSample = GetFixedSample(currentTick - 1);
                    }
                }
                else
                {
                    throw new NotImplementedException(kcc.Settings.InputAuthorityBehavior.ToString());
                }
            }
            else
            {
                useAntiJitter = kcc.IsPredictingInRenderUpdate == false &&
                                kcc.Settings.ForcePredictedLookRotation == false;

                if (kcc.Settings.InputAuthorityBehavior == EKCCAuthorityBehavior.PredictFixed_PredictRender)
                {
                    if (_lastPredictedRenderFrame < Time.frameCount)
                    {
                        if (_enableLogs)
                            kcc.LogError(
                                "Missing data for calculation of render position and rotation for current frame. The KCC is set to predict in render, therefore Sample() must be called before this method!");

                        _target.GetPositionAndRotation(out renderPosition, out renderRotation);
                        return false;
                    }

                    if (kcc.LastPredictedRenderFrame < Time.frameCount)
                    {
                        if (_enableLogs)
                            kcc.LogError(
                                "Missing data for calculation of render position and rotation for current frame. The KCC is set to predict in render and must run render update for current frame before calling this method!");

                        _target.GetPositionAndRotation(out renderPosition, out renderRotation);
                        return false;
                    }

                    fromKCCData = kcc.FixedData;
                    toKCCData = kcc.RenderData;
                    fromSample = _lastFixedSample;
                    toSample = _lastRenderSample;

                    if (renderAlpha > _lastRenderSample.Alpha)
                        renderAlpha = 1.0f;
                    else if (_lastRenderSample.Alpha > 0.000001f)
                        renderAlpha = Mathf.Clamp01(renderAlpha / _lastRenderSample.Alpha);
                }
                else if (kcc.Settings.InputAuthorityBehavior == EKCCAuthorityBehavior.PredictFixed_InterpolateRender)
                {
                    int currentTick = kcc.Runner.Tick;

                    fromKCCData = kcc.GetHistoryData(currentTick - 1);
                    toKCCData = kcc.GetHistoryData(currentTick);

                    if (kcc.Settings.ForcePredictedLookRotation)
                    {
                        if (_lastPredictedRenderFrame < Time.frameCount)
                        {
                            if (_enableLogs)
                                kcc.LogError(
                                    "Missing data for calculation of render position and rotation for current frame. The KCC has Force Predicted Look Rotation enabled, therefore Sample() must be called before this method!");

                            _target.GetPositionAndRotation(out renderPosition, out renderRotation);
                            return false;
                        }

                        if (kcc.LastPredictedRenderFrame < Time.frameCount)
                        {
                            if (_enableLogs)
                                kcc.LogError(
                                    "Missing data for calculation of render position and rotation for current frame. The KCC has Force Predicted Look Rotation enabled and must run render update for current frame before calling this method!");

                            _target.GetPositionAndRotation(out renderPosition, out renderRotation);
                            return false;
                        }

                        fromSample = _lastFixedSample;
                        toSample = _lastRenderSample;

                        if (renderAlpha > _lastRenderSample.Alpha)
                            renderAlpha = 1.0f;
                        else if (_lastRenderSample.Alpha > 0.000001f)
                            renderAlpha = Mathf.Clamp01(renderAlpha / _lastRenderSample.Alpha);
                    }
                    else
                    {
                        fromSample = GetFixedSample(currentTick - 1);
                        toSample = GetFixedSample(currentTick);
                    }
                }
                else
                {
                    throw new NotImplementedException(kcc.Settings.InputAuthorityBehavior.ToString());
                }
            }

            if (fromSample == null || toSample == null)
            {
                if (_enableLogs) kcc.LogWarning("Missing data for calculation of render position and rotation.");

                _target.GetPositionAndRotation(out renderPosition, out renderRotation);
                return false;
            }

            Vector3 kccPositionDelta = default;

            if (fromKCCData != null && toKCCData != null)
                kccPositionDelta = toKCCData.TargetPosition - fromKCCData.TargetPosition;

            if (Vector3.SqrMagnitude(kccPositionDelta) >=
                kcc.Settings.TeleportThreshold * kcc.Settings.TeleportThreshold)
            {
                if (renderAlpha >= 0.5f)
                {
                    renderPosition = toSample.Position;
                    renderRotation = toSample.Rotation;
                }
                else
                {
                    renderPosition = fromSample.Position;
                    renderRotation = toSample.Rotation;
                }
            }
            else
            {
                var targetPosition = Vector3.Lerp(fromSample.Position, toSample.Position, renderAlpha);

                if (useAntiJitter && kcc.ActiveFeatures.Has(EKCCFeature.AntiJitter))
                {
                    var maxAntiJitterDistance = kcc.Settings.AntiJitterDistance;
                    if (maxAntiJitterDistance.IsZero() == false && kccPositionDelta.IsAlmostZero(0.0001f) == false)
                    {
                        maxAntiJitterDistance.x = Mathf.Min(Vector3.Magnitude(kccPositionDelta.OnlyXZ()),
                            maxAntiJitterDistance.x);
                        maxAntiJitterDistance.y = Mathf.Min(Mathf.Abs(kccPositionDelta.y), maxAntiJitterDistance.y);

                        var fromPosition = fromSample.Position;
                        var toPosition = toSample.Position;
                        var positionDelta = Vector3.Lerp(fromPosition, toPosition, renderAlpha) - fromPosition;

                        targetPosition = fromPosition;

                        var distanceY = Mathf.Abs(positionDelta.y);
                        if (distanceY > 0.000001f && distanceY > maxAntiJitterDistance.y)
                            targetPosition.y += positionDelta.y * ((distanceY - maxAntiJitterDistance.y) / distanceY);

                        var positionDeltaXZ = positionDelta.OnlyXZ();

                        var distanceXZ = Vector3.Magnitude(positionDeltaXZ);
                        if (distanceXZ > 0.000001f && distanceXZ > maxAntiJitterDistance.x)
                            targetPosition += positionDeltaXZ * ((distanceXZ - maxAntiJitterDistance.x) / distanceXZ);
                    }
                }

                if (shiftPosition) targetPosition -= kccPositionDelta;

                renderPosition = targetPosition;
                renderRotation = Quaternion.Slerp(fromSample.Rotation, toSample.Rotation, renderAlpha);
            }

            return true;
        }

        // PRIVATE METHODS

        private TransformSample GetFixedSample(int tick)
        {
            if (tick < 0)
                return null;

            var sample = _samples[tick % HISTORY_SIZE];
            if (sample != null && sample.Tick == tick)
                return sample;

            return null;
        }

        private void SampleFixedUpdate(int tick)
        {
            var sample = _samples[tick % HISTORY_SIZE];
            if (sample == null)
            {
                sample = new TransformSample();
                _samples[tick % HISTORY_SIZE] = sample;
            }

            sample.Tick = tick;
            sample.Alpha = 1.0f;

            _target.GetPositionAndRotation(out sample.Position, out sample.Rotation);

            _lastFixedSample.Tick = sample.Tick;
            _lastFixedSample.Alpha = sample.Alpha;
            _lastFixedSample.Position = sample.Position;
            _lastFixedSample.Rotation = sample.Rotation;

            _lastPredictedFixedTick = tick;
        }

        private void SampleRenderUpdate(int tick, float renderAlpha)
        {
            _lastRenderSample.Tick = tick;
            _lastRenderSample.Alpha = renderAlpha;

            _target.GetPositionAndRotation(out _lastRenderSample.Position, out _lastRenderSample.Rotation);

            _lastPredictedRenderFrame = Time.frameCount;
        }

        // DATA STRUCTURES

        private sealed class TransformSample
        {
            public float Alpha;
            public Vector3 Position;
            public Quaternion Rotation;
            public int Tick;
        }
    }
}