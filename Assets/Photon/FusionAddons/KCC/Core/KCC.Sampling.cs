using System;
using UnityEngine;

namespace Fusion.Addons.KCC
{
    // This file contains API which resolves render position of a KCC / child transform and render look rotation of the KCC with given render alpha.
    // These methods can be used to get render accurate position of the origin for sub-tick accurate lag compensated casts (for example player's camera state on client at the time of clicling mouse).
    // It is required to pass correct render alpha (Runner.LocalAlpha passed from client through input, check KCC sample for correct usage).
    public partial class KCC
    {
        // PUBLIC METHODS

        /// <summary>
        ///     Returns render position of the KCC with given render alpha.
        /// </summary>
        /// <param name="renderAlpha">Runner.LocalAlpha from the client at the time of Render().</param>
        /// <param name="renderPosition">Position of the KCC in render with given render alpha.</param>
        public bool ResolveRenderPosition(float renderAlpha, out Vector3 renderPosition)
        {
            renderAlpha = Mathf.Clamp01(renderAlpha);

            KCCData fromData;
            KCCData toData;

            if (IsInFixedUpdate)
            {
                int currentTick = Runner.Tick;

                if (_settings.InputAuthorityBehavior == EKCCAuthorityBehavior.PredictFixed_PredictRender)
                {
                    if (LastPredictedFixedTick < currentTick)
                    {
                        LogError(
                            "Missing data for calculation of render position for current tick. The KCC is set to predict in render and must run fixed update for current tick before calling this method!");
                        renderPosition = FixedData.TargetPosition;
                        return false;
                    }

                    fromData = GetHistoryData(currentTick - 1);
                    toData = GetHistoryData(currentTick);
                }
                else if (_settings.InputAuthorityBehavior == EKCCAuthorityBehavior.PredictFixed_InterpolateRender)
                {
                    fromData = GetHistoryData(currentTick - 2);
                    toData = GetHistoryData(currentTick - 1);
                }
                else
                {
                    throw new NotImplementedException(_settings.InputAuthorityBehavior.ToString());
                }
            }
            else
            {
                if (_settings.InputAuthorityBehavior == EKCCAuthorityBehavior.PredictFixed_PredictRender)
                {
                    if (_lastPredictedLookRotationFrame < Time.frameCount)
                    {
                        LogError(
                            "Missing data for calculation of render position for current frame. The KCC is set to predict in render and must run render update for current frame before calling this method!");
                        renderPosition = RenderData.TargetPosition;
                        return false;
                    }

                    fromData = FixedData;
                    toData = RenderData;

                    if (renderAlpha > RenderData.Alpha)
                        renderAlpha = 1.0f;
                    else if (RenderData.Alpha > 0.000001f)
                        renderAlpha = Mathf.Clamp01(renderAlpha / RenderData.Alpha);
                }
                else if (_settings.InputAuthorityBehavior == EKCCAuthorityBehavior.PredictFixed_InterpolateRender)
                {
                    int currentTick = Runner.Tick;

                    fromData = GetHistoryData(currentTick - 1);
                    toData = GetHistoryData(currentTick);
                }
                else
                {
                    throw new NotImplementedException(_settings.InputAuthorityBehavior.ToString());
                }
            }

            if (fromData == null || toData == null)
            {
                renderPosition = Data.TargetPosition;
                return false;
            }

            var fromPosition = fromData.TargetPosition;
            var toPosition = toData.TargetPosition;
            var positionDelta = toPosition - fromPosition;

            if (Vector3.SqrMagnitude(positionDelta) >= _settings.TeleportThreshold * _settings.TeleportThreshold)
            {
                if (renderAlpha >= 0.5f)
                    renderPosition = toPosition;
                else
                    renderPosition = fromPosition;
            }
            else
            {
                Vector3 targetPosition;

                if (ActiveFeatures.Has(EKCCFeature.AntiJitter) && _settings.AntiJitterDistance.IsZero() == false)
                {
                    targetPosition = fromPosition;
                    positionDelta = Vector3.Lerp(fromPosition, toPosition, renderAlpha) - fromPosition;

                    var distanceY = Mathf.Abs(positionDelta.y);
                    if (distanceY > 0.000001f && distanceY > _settings.AntiJitterDistance.y)
                        targetPosition.y +=
                            positionDelta.y * ((distanceY - _settings.AntiJitterDistance.y) / distanceY);

                    var positionDeltaXZ = positionDelta.OnlyXZ();

                    var distanceXZ = Vector3.Magnitude(positionDeltaXZ);
                    if (distanceXZ > 0.000001f && distanceXZ > _settings.AntiJitterDistance.x)
                        targetPosition +=
                            positionDeltaXZ * ((distanceXZ - _settings.AntiJitterDistance.x) / distanceXZ);
                }
                else
                {
                    targetPosition = Vector3.Lerp(fromPosition, toPosition, renderAlpha);
                }

                renderPosition = targetPosition;
            }

            return true;
        }

        /// <summary>
        ///     Returns render position of a KCC child transform with given render alpha.
        /// </summary>
        /// <param name="origin">Child transform of the KCC. For example a camera handle.</param>
        /// <param name="renderAlpha">Runner.LocalAlpha from a client at the time of Render().</param>
        /// <param name="renderPosition">Position of the origin in render with given render alpha.</param>
        public bool ResolveRenderPosition(Transform origin, float renderAlpha, out Vector3 renderPosition)
        {
            if (ReferenceEquals(origin, Transform))
                return ResolveRenderPosition(renderAlpha, out renderPosition);

            if (origin.IsChildOf(Transform) == false)
                throw new NotSupportedException("Origin must be child of the KCC!");

            renderAlpha = Mathf.Clamp01(renderAlpha);

            KCCData fromData;
            KCCData toData;

            if (IsInFixedUpdate)
            {
                int currentTick = Runner.Tick;

                if (_settings.InputAuthorityBehavior == EKCCAuthorityBehavior.PredictFixed_PredictRender)
                {
                    if (LastPredictedFixedTick < currentTick)
                    {
                        LogError(
                            "Missing data for calculation of render position for current tick. The KCC is set to predict in render and must run fixed update for current tick before calling this method!");
                        renderPosition = origin.position;
                        return false;
                    }

                    fromData = GetHistoryData(currentTick - 1);
                    toData = GetHistoryData(currentTick);
                }
                else if (_settings.InputAuthorityBehavior == EKCCAuthorityBehavior.PredictFixed_InterpolateRender)
                {
                    fromData = GetHistoryData(currentTick - 2);
                    toData = GetHistoryData(currentTick - 1);
                }
                else
                {
                    throw new NotImplementedException(_settings.InputAuthorityBehavior.ToString());
                }
            }
            else
            {
                if (_settings.InputAuthorityBehavior == EKCCAuthorityBehavior.PredictFixed_PredictRender)
                {
                    if (_lastPredictedLookRotationFrame < Time.frameCount)
                    {
                        LogError(
                            "Missing data for calculation of render position for current frame. The KCC is set to predict in render and must run render update for current frame before calling this method!");
                        renderPosition = origin.position;
                        return false;
                    }

                    fromData = FixedData;
                    toData = RenderData;

                    if (renderAlpha > RenderData.Alpha)
                        renderAlpha = 1.0f;
                    else if (RenderData.Alpha > 0.000001f)
                        renderAlpha = Mathf.Clamp01(renderAlpha / RenderData.Alpha);
                }
                else if (_settings.InputAuthorityBehavior == EKCCAuthorityBehavior.PredictFixed_InterpolateRender)
                {
                    int currentTick = Runner.Tick;

                    fromData = GetHistoryData(currentTick - 1);
                    toData = GetHistoryData(currentTick);
                }
                else
                {
                    throw new NotImplementedException(_settings.InputAuthorityBehavior.ToString());
                }
            }

            if (fromData == null || toData == null)
            {
                renderPosition = origin.position;
                return false;
            }

            var fromPosition = fromData.TargetPosition;
            var toPosition = toData.TargetPosition;
            var positionDelta = toPosition - fromPosition;
            var offset = Transform.InverseTransformPoint(origin.position);

            if (Vector3.SqrMagnitude(positionDelta) >= _settings.TeleportThreshold * _settings.TeleportThreshold)
            {
                if (renderAlpha >= 0.5f)
                    renderPosition = toPosition + toData.TransformRotation * offset;
                else
                    renderPosition = fromPosition + fromData.TransformRotation * offset;
            }
            else
            {
                var fromOffset = fromData.TransformRotation * offset;
                var toOffset = toData.TransformRotation * offset;

                Vector3 targetPosition;

                if (ActiveFeatures.Has(EKCCFeature.AntiJitter) && _settings.AntiJitterDistance.IsZero() == false)
                {
                    targetPosition = fromPosition;
                    positionDelta = Vector3.Lerp(fromPosition, toPosition, renderAlpha) - fromPosition;

                    var distanceY = Mathf.Abs(positionDelta.y);
                    if (distanceY > 0.000001f && distanceY > _settings.AntiJitterDistance.y)
                        targetPosition.y +=
                            positionDelta.y * ((distanceY - _settings.AntiJitterDistance.y) / distanceY);

                    var positionDeltaXZ = positionDelta.OnlyXZ();

                    var distanceXZ = Vector3.Magnitude(positionDeltaXZ);
                    if (distanceXZ > 0.000001f && distanceXZ > _settings.AntiJitterDistance.x)
                        targetPosition +=
                            positionDeltaXZ * ((distanceXZ - _settings.AntiJitterDistance.x) / distanceXZ);
                }
                else
                {
                    targetPosition = Vector3.Lerp(fromPosition, toPosition, renderAlpha);
                }

                renderPosition = targetPosition + Vector3.Slerp(fromOffset, toOffset, renderAlpha);
            }

            return true;
        }

        /// <summary>
        ///     Returns render look rotation of the KCC with given render alpha.
        /// </summary>
        /// <param name="renderAlpha">Runner.LocalAlpha from the client at the time of Render().</param>
        /// <param name="renderLookRotation">Look Rotation of the KCC in render with given render alpha.</param>
        public bool ResolveRenderLookRotation(float renderAlpha, out Quaternion renderLookRotation)
        {
            renderAlpha = Mathf.Clamp01(renderAlpha);

            KCCData fromPositionData;
            KCCData toPositionData;
            KCCData fromRotationData;
            KCCData toRotationData;

            if (IsInFixedUpdate)
            {
                int currentTick = Runner.Tick;

                if (_settings.InputAuthorityBehavior == EKCCAuthorityBehavior.PredictFixed_PredictRender)
                {
                    if (LastPredictedFixedTick < currentTick)
                    {
                        LogError(
                            "Missing data for calculation of render look rotation for current tick. The KCC is set to predict in render and must run fixed update for current tick before calling this method!");
                        renderLookRotation = FixedData.LookRotation;
                        return false;
                    }

                    fromPositionData = GetHistoryData(currentTick - 1);
                    toPositionData = GetHistoryData(currentTick);
                    fromRotationData = fromPositionData;
                    toRotationData = toPositionData;
                }
                else if (_settings.InputAuthorityBehavior == EKCCAuthorityBehavior.PredictFixed_InterpolateRender)
                {
                    if (_settings.ForcePredictedLookRotation)
                    {
                        if (LastPredictedFixedTick < currentTick)
                        {
                            LogError(
                                "Missing data for calculation of render look rotation for current tick. The KCC has Force Predicted Look Rotation enabled and must run fixed update for current tick before calling this method!");
                            renderLookRotation = FixedData.LookRotation;
                            return false;
                        }

                        fromPositionData = GetHistoryData(currentTick - 2);
                        toPositionData = GetHistoryData(currentTick - 1);
                        fromRotationData = toPositionData;
                        toRotationData = GetHistoryData(currentTick);
                    }
                    else
                    {
                        fromPositionData = GetHistoryData(currentTick - 2);
                        toPositionData = GetHistoryData(currentTick - 1);
                        fromRotationData = fromPositionData;
                        toRotationData = toPositionData;
                    }
                }
                else
                {
                    throw new NotImplementedException(_settings.InputAuthorityBehavior.ToString());
                }
            }
            else
            {
                if (_settings.InputAuthorityBehavior == EKCCAuthorityBehavior.PredictFixed_PredictRender)
                {
                    if (_lastPredictedLookRotationFrame < Time.frameCount)
                    {
                        LogError(
                            "Missing data for calculation of render look rotation for current frame. The KCC is set to predict in render and must run render update for current frame before calling this method!");
                        renderLookRotation = RenderData.LookRotation;
                        return false;
                    }

                    fromPositionData = FixedData;
                    toPositionData = RenderData;
                    fromRotationData = FixedData;
                    toRotationData = RenderData;

                    if (renderAlpha > RenderData.Alpha)
                        renderAlpha = 1.0f;
                    else if (RenderData.Alpha > 0.000001f)
                        renderAlpha = Mathf.Clamp01(renderAlpha / RenderData.Alpha);
                }
                else if (_settings.InputAuthorityBehavior == EKCCAuthorityBehavior.PredictFixed_InterpolateRender)
                {
                    int currentTick = Runner.Tick;

                    fromPositionData = GetHistoryData(currentTick - 1);
                    toPositionData = GetHistoryData(currentTick);

                    if (_settings.ForcePredictedLookRotation)
                    {
                        if (_lastPredictedLookRotationFrame < Time.frameCount)
                        {
                            LogError(
                                "Missing data for calculation of render look rotation for current frame. The KCC has Force Predicted Look Rotation enabled and must run render update for current frame before calling this method!");
                            renderLookRotation = RenderData.LookRotation;
                            return false;
                        }

                        fromRotationData = FixedData;
                        toRotationData = RenderData;

                        if (renderAlpha > RenderData.Alpha)
                            renderAlpha = 1.0f;
                        else if (RenderData.Alpha > 0.000001f)
                            renderAlpha = Mathf.Clamp01(renderAlpha / RenderData.Alpha);
                    }
                    else
                    {
                        fromRotationData = fromPositionData;
                        toRotationData = toPositionData;
                    }
                }
                else
                {
                    throw new NotImplementedException(_settings.InputAuthorityBehavior.ToString());
                }
            }

            if (fromPositionData == null || toPositionData == null || fromRotationData == null ||
                toRotationData == null)
            {
                renderLookRotation = Data.LookRotation;
                return false;
            }

            var fromPosition = fromPositionData.TargetPosition;
            var toPosition = toPositionData.TargetPosition;
            var positionDelta = toPosition - fromPosition;

            if (Vector3.SqrMagnitude(positionDelta) >= _settings.TeleportThreshold * _settings.TeleportThreshold)
            {
                if (renderAlpha >= 0.5f)
                    renderLookRotation = toRotationData.LookRotation;
                else
                    renderLookRotation = fromRotationData.LookRotation;
            }
            else
            {
                renderLookRotation = Quaternion.Slerp(fromRotationData.LookRotation, toRotationData.LookRotation,
                    renderAlpha);
            }

            return true;
        }

        /// <summary>
        ///     Returns render position and look rotation of the KCC with given render alpha.
        /// </summary>
        /// <param name="renderAlpha">Runner.LocalAlpha from the client at the time of Render().</param>
        /// <param name="renderPosition">Position of the KCC in render with given render alpha.</param>
        /// <param name="renderLookRotation">Look Rotation of the KCC in render with given render alpha.</param>
        public bool ResolveRenderPositionAndLookRotation(float renderAlpha, out Vector3 renderPosition,
            out Quaternion renderLookRotation)
        {
            renderAlpha = Mathf.Clamp01(renderAlpha);

            var positionAlpha = renderAlpha;
            var rotationAlpha = renderAlpha;

            KCCData fromPositionData;
            KCCData toPositionData;
            KCCData fromRotationData;
            KCCData toRotationData;

            if (IsInFixedUpdate)
            {
                int currentTick = Runner.Tick;

                if (_settings.InputAuthorityBehavior == EKCCAuthorityBehavior.PredictFixed_PredictRender)
                {
                    if (LastPredictedFixedTick < currentTick)
                    {
                        LogError(
                            "Missing data for calculation of render position and look rotation for current tick. The KCC is set to predict in render and must run fixed update for current tick before calling this method!");
                        renderPosition = FixedData.TargetPosition;
                        renderLookRotation = FixedData.LookRotation;
                        return false;
                    }

                    fromPositionData = GetHistoryData(currentTick - 1);
                    toPositionData = GetHistoryData(currentTick);
                    fromRotationData = fromPositionData;
                    toRotationData = toPositionData;
                }
                else if (_settings.InputAuthorityBehavior == EKCCAuthorityBehavior.PredictFixed_InterpolateRender)
                {
                    if (_settings.ForcePredictedLookRotation)
                    {
                        if (LastPredictedFixedTick < currentTick)
                        {
                            LogError(
                                "Missing data for calculation of render position and look rotation for current tick. The KCC has Force Predicted Look Rotation enabled and must run fixed update for current tick before calling this method!");
                            renderPosition = FixedData.TargetPosition;
                            renderLookRotation = FixedData.LookRotation;
                            return false;
                        }

                        fromPositionData = GetHistoryData(currentTick - 2);
                        toPositionData = GetHistoryData(currentTick - 1);
                        fromRotationData = toPositionData;
                        toRotationData = GetHistoryData(currentTick);
                    }
                    else
                    {
                        fromPositionData = GetHistoryData(currentTick - 2);
                        toPositionData = GetHistoryData(currentTick - 1);
                        fromRotationData = fromPositionData;
                        toRotationData = toPositionData;
                    }
                }
                else
                {
                    throw new NotImplementedException(_settings.InputAuthorityBehavior.ToString());
                }
            }
            else
            {
                if (_settings.InputAuthorityBehavior == EKCCAuthorityBehavior.PredictFixed_PredictRender)
                {
                    if (_lastPredictedLookRotationFrame < Time.frameCount)
                    {
                        LogError(
                            "Missing data for calculation of render position and look rotation for current frame. The KCC is set to predict in render and must run render update for current frame before calling this method!");
                        renderPosition = RenderData.TargetPosition;
                        renderLookRotation = RenderData.LookRotation;
                        return false;
                    }

                    fromPositionData = FixedData;
                    toPositionData = RenderData;
                    fromRotationData = FixedData;
                    toRotationData = RenderData;

                    if (rotationAlpha > RenderData.Alpha)
                        rotationAlpha = 1.0f;
                    else if (RenderData.Alpha > 0.000001f)
                        rotationAlpha = Mathf.Clamp01(rotationAlpha / RenderData.Alpha);

                    positionAlpha = rotationAlpha;
                }
                else if (_settings.InputAuthorityBehavior == EKCCAuthorityBehavior.PredictFixed_InterpolateRender)
                {
                    int currentTick = Runner.Tick;

                    fromPositionData = GetHistoryData(currentTick - 1);
                    toPositionData = GetHistoryData(currentTick);

                    if (_settings.ForcePredictedLookRotation)
                    {
                        if (_lastPredictedLookRotationFrame < Time.frameCount)
                        {
                            LogError(
                                "Missing data for calculation of render position and look rotation for current frame. The KCC has Force Predicted Look Rotation enabled and must run render update for current frame before calling this method!");
                            renderPosition = RenderData.TargetPosition;
                            renderLookRotation = RenderData.LookRotation;
                            return false;
                        }

                        fromRotationData = FixedData;
                        toRotationData = RenderData;

                        if (rotationAlpha > RenderData.Alpha)
                            rotationAlpha = 1.0f;
                        else if (RenderData.Alpha > 0.000001f)
                            rotationAlpha = Mathf.Clamp01(rotationAlpha / RenderData.Alpha);
                    }
                    else
                    {
                        fromRotationData = fromPositionData;
                        toRotationData = toPositionData;
                    }
                }
                else
                {
                    throw new NotImplementedException(_settings.InputAuthorityBehavior.ToString());
                }
            }

            if (fromPositionData == null || toPositionData == null || fromRotationData == null ||
                toRotationData == null)
            {
                renderPosition = Data.TargetPosition;
                renderLookRotation = Data.LookRotation;
                return false;
            }

            var fromPosition = fromPositionData.TargetPosition;
            var toPosition = toPositionData.TargetPosition;
            var positionDelta = toPosition - fromPosition;

            if (Vector3.SqrMagnitude(positionDelta) >= _settings.TeleportThreshold * _settings.TeleportThreshold)
            {
                if (positionAlpha >= 0.5f)
                {
                    renderPosition = toPosition;
                    renderLookRotation = toRotationData.LookRotation;
                }
                else
                {
                    renderPosition = fromPosition;
                    renderLookRotation = fromRotationData.LookRotation;
                }
            }
            else
            {
                Vector3 targetPosition;

                if (ActiveFeatures.Has(EKCCFeature.AntiJitter) && _settings.AntiJitterDistance.IsZero() == false)
                {
                    targetPosition = fromPosition;
                    positionDelta = Vector3.Lerp(fromPosition, toPosition, positionAlpha) - fromPosition;

                    var distanceY = Mathf.Abs(positionDelta.y);
                    if (distanceY > 0.000001f && distanceY > _settings.AntiJitterDistance.y)
                        targetPosition.y +=
                            positionDelta.y * ((distanceY - _settings.AntiJitterDistance.y) / distanceY);

                    var positionDeltaXZ = positionDelta.OnlyXZ();

                    var distanceXZ = Vector3.Magnitude(positionDeltaXZ);
                    if (distanceXZ > 0.000001f && distanceXZ > _settings.AntiJitterDistance.x)
                        targetPosition +=
                            positionDeltaXZ * ((distanceXZ - _settings.AntiJitterDistance.x) / distanceXZ);
                }
                else
                {
                    targetPosition = Vector3.Lerp(fromPosition, toPosition, positionAlpha);
                }

                renderPosition = targetPosition;
                renderLookRotation = Quaternion.Slerp(fromRotationData.LookRotation, toRotationData.LookRotation,
                    rotationAlpha);
            }

            return true;
        }

        /// <summary>
        ///     Returns render position of a KCC child transform and render look rotation of the KCC with given render alpha.
        /// </summary>
        /// <param name="origin">Child transform of the KCC. For example a camera handle.</param>
        /// <param name="renderAlpha">Runner.LocalAlpha from a client at the time of Render().</param>
        /// <param name="renderPosition">Position of the origin in render with given render alpha.</param>
        /// <param name="renderLookRotation">Look rotation of the KCC in render with given render alpha.</param>
        public bool ResolveRenderPositionAndLookRotation(Transform origin, float renderAlpha,
            out Vector3 renderPosition, out Quaternion renderLookRotation)
        {
            if (ReferenceEquals(origin, Transform))
                return ResolveRenderPositionAndLookRotation(renderAlpha, out renderPosition, out renderLookRotation);

            if (origin.IsChildOf(Transform) == false)
                throw new NotSupportedException("Origin must be child of the KCC!");

            var positionAlpha = renderAlpha;
            var rotationAlpha = renderAlpha;

            KCCData fromPositionData;
            KCCData toPositionData;
            KCCData fromRotationData;
            KCCData toRotationData;

            if (IsInFixedUpdate)
            {
                int currentTick = Runner.Tick;

                if (_settings.InputAuthorityBehavior == EKCCAuthorityBehavior.PredictFixed_PredictRender)
                {
                    if (LastPredictedFixedTick < currentTick)
                    {
                        LogError(
                            "Missing data for calculation of render position and look rotation for current tick. The KCC is set to predict in render and must run fixed update for current tick before calling this method!");
                        renderPosition = origin.position;
                        renderLookRotation = FixedData.LookRotation;
                        return false;
                    }

                    fromPositionData = GetHistoryData(currentTick - 1);
                    toPositionData = GetHistoryData(currentTick);
                    fromRotationData = fromPositionData;
                    toRotationData = toPositionData;
                }
                else if (_settings.InputAuthorityBehavior == EKCCAuthorityBehavior.PredictFixed_InterpolateRender)
                {
                    if (_settings.ForcePredictedLookRotation)
                    {
                        if (LastPredictedFixedTick < currentTick)
                        {
                            LogError(
                                "Missing data for calculation of render position and look rotation for current tick. The KCC has Force Predicted Look Rotation enabled and must run fixed update for current tick before calling this method!");
                            renderPosition = origin.position;
                            renderLookRotation = FixedData.LookRotation;
                            return false;
                        }

                        fromPositionData = GetHistoryData(currentTick - 2);
                        toPositionData = GetHistoryData(currentTick - 1);
                        fromRotationData = toPositionData;
                        toRotationData = GetHistoryData(currentTick);
                    }
                    else
                    {
                        fromPositionData = GetHistoryData(currentTick - 2);
                        toPositionData = GetHistoryData(currentTick - 1);
                        fromRotationData = fromPositionData;
                        toRotationData = toPositionData;
                    }
                }
                else
                {
                    throw new NotImplementedException(_settings.InputAuthorityBehavior.ToString());
                }
            }
            else
            {
                if (_settings.InputAuthorityBehavior == EKCCAuthorityBehavior.PredictFixed_PredictRender)
                {
                    if (_lastPredictedLookRotationFrame < Time.frameCount)
                    {
                        LogError(
                            "Missing data for calculation of render position and look rotation for current frame. The KCC is set to predict in render and must run render update for current frame before calling this method!");
                        renderPosition = origin.position;
                        renderLookRotation = RenderData.LookRotation;
                        return false;
                    }

                    fromPositionData = FixedData;
                    toPositionData = RenderData;
                    fromRotationData = FixedData;
                    toRotationData = RenderData;

                    if (rotationAlpha > RenderData.Alpha)
                        rotationAlpha = 1.0f;
                    else if (RenderData.Alpha > 0.000001f)
                        rotationAlpha = Mathf.Clamp01(rotationAlpha / RenderData.Alpha);

                    positionAlpha = rotationAlpha;
                }
                else if (_settings.InputAuthorityBehavior == EKCCAuthorityBehavior.PredictFixed_InterpolateRender)
                {
                    int currentTick = Runner.Tick;

                    fromPositionData = GetHistoryData(currentTick - 1);
                    toPositionData = GetHistoryData(currentTick);

                    if (_settings.ForcePredictedLookRotation)
                    {
                        if (_lastPredictedLookRotationFrame < Time.frameCount)
                        {
                            LogError(
                                "Missing data for calculation of render position and look rotation for current frame. The KCC has Force Predicted Look Rotation enabled and must run render update for current frame before calling this method!");
                            renderPosition = origin.position;
                            renderLookRotation = RenderData.LookRotation;
                            return false;
                        }

                        fromRotationData = FixedData;
                        toRotationData = RenderData;

                        if (rotationAlpha > RenderData.Alpha)
                            rotationAlpha = 1.0f;
                        else if (RenderData.Alpha > 0.000001f)
                            rotationAlpha = Mathf.Clamp01(rotationAlpha / RenderData.Alpha);
                    }
                    else
                    {
                        fromRotationData = fromPositionData;
                        toRotationData = toPositionData;
                    }
                }
                else
                {
                    throw new NotImplementedException(_settings.InputAuthorityBehavior.ToString());
                }
            }

            if (fromPositionData == null || toPositionData == null || fromRotationData == null ||
                toRotationData == null)
            {
                renderPosition = origin.position;
                renderLookRotation = Data.LookRotation;
                return false;
            }

            var fromPosition = fromPositionData.TargetPosition;
            var toPosition = toPositionData.TargetPosition;
            var positionDelta = toPosition - fromPosition;
            var offset = Transform.InverseTransformPoint(origin.position);

            if (Vector3.SqrMagnitude(positionDelta) >= _settings.TeleportThreshold * _settings.TeleportThreshold)
            {
                if (positionAlpha >= 0.5f)
                {
                    renderPosition = toPosition + toPositionData.TransformRotation * offset;
                    renderLookRotation = toRotationData.LookRotation;
                }
                else
                {
                    renderPosition = fromPosition + fromPositionData.TransformRotation * offset;
                    renderLookRotation = fromRotationData.LookRotation;
                }
            }
            else
            {
                var fromOffset = fromPositionData.TransformRotation * offset;
                var toOffset = toPositionData.TransformRotation * offset;

                Vector3 targetPosition;

                if (ActiveFeatures.Has(EKCCFeature.AntiJitter) && _settings.AntiJitterDistance.IsZero() == false)
                {
                    targetPosition = fromPosition;
                    positionDelta = Vector3.Lerp(fromPosition, toPosition, positionAlpha) - fromPosition;

                    var distanceY = Mathf.Abs(positionDelta.y);
                    if (distanceY > 0.000001f && distanceY > _settings.AntiJitterDistance.y)
                        targetPosition.y +=
                            positionDelta.y * ((distanceY - _settings.AntiJitterDistance.y) / distanceY);

                    var positionDeltaXZ = positionDelta.OnlyXZ();

                    var distanceXZ = Vector3.Magnitude(positionDeltaXZ);
                    if (distanceXZ > 0.000001f && distanceXZ > _settings.AntiJitterDistance.x)
                        targetPosition +=
                            positionDeltaXZ * ((distanceXZ - _settings.AntiJitterDistance.x) / distanceXZ);
                }
                else
                {
                    targetPosition = Vector3.Lerp(fromPosition, toPosition, positionAlpha);
                }

                renderPosition = targetPosition + Vector3.Slerp(fromOffset, toOffset, positionAlpha);
                renderLookRotation = Quaternion.Slerp(fromRotationData.LookRotation, toRotationData.LookRotation,
                    rotationAlpha);
            }

            return true;
        }
    }
}