using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.KCC
{
    public class KCCNetworkContext
    {
        public KCCData Data;
        public KCC KCC;
        public KCCSettings Settings;
    }

    // This file contains implementation related to network synchronization and interpolation based on network buffers.
    public unsafe partial class KCC
    {
        private int _interpolationAttempts;

        private int _interpolationTick;
        // PRIVATE MEMBERS

        private KCCNetworkContext _networkContext;
        private IKCCNetworkProperty[] _networkProperties;

        // PUBLIC METHODS

        /// <summary>
        ///     Returns position stored in network buffer.
        /// </summary>
        public Vector3 GetNetworkBufferPosition()
        {
            fixed (int* ptr = &ReinterpretState<int>())
            {
                return ((NetworkTRSPData*)ptr)->Position + KCCNetworkUtility.ReadVector3(ptr + NetworkTRSPData.WORDS);
            }
        }

        /// <summary>
        ///     Returns interpolated position based on data stored in network buffers.
        /// </summary>
        public bool GetInterpolatedNetworkBufferPosition(out Vector3 interpolatedPosition)
        {
            interpolatedPosition = default;

            var defaultSource = Object.RenderSource;
            var defaultTimeframe = Object.RenderTimeframe;

            Object.RenderSource = RenderSource.Interpolated;
            Object.RenderTimeframe = GetInterpolationTimeframe();

            var buffersValid = TryGetSnapshotsBuffers(out var fromBuffer, out var toBuffer, out var alpha);

            Object.RenderSource = defaultSource;
            Object.RenderTimeframe = defaultTimeframe;

            if (buffersValid == false)
                return false;

            KCCNetworkProperties.ReadPositions(fromBuffer, toBuffer, out var fromPosition, out var toPosition);

            interpolatedPosition = Vector3.Lerp(fromPosition, toPosition, alpha);

            return true;
        }

        // PRIVATE METHODS

        private int GetNetworkDataWordCount()
        {
            InitializeNetworkProperties();

            var wordCount = 0;

            for (int i = 0, count = _networkProperties.Length; i < count; ++i)
            {
                var property = _networkProperties[i];
                wordCount += property.WordCount;
            }

            return wordCount;
        }

        private void ReadNetworkData()
        {
            _networkContext.Data = FixedData;

            fixed (int* statePtr = &ReinterpretState<int>())
            {
                var ptr = statePtr;

                for (int i = 0, count = _networkProperties.Length; i < count; ++i)
                {
                    var property = _networkProperties[i];
                    property.Read(ptr);
                    ptr += property.WordCount;
                }
            }
        }

        private void WriteNetworkData()
        {
            _networkContext.Data = FixedData;

            fixed (int* statePtr = &ReinterpretState<int>())
            {
                var ptr = statePtr;

                for (int i = 0, count = _networkProperties.Length; i < count; ++i)
                {
                    var property = _networkProperties[i];
                    property.Write(ptr);
                    ptr += property.WordCount;
                }
            }
        }

        private void InterpolateNetworkData(RenderSource renderSource, RenderTimeframe renderTimeframe,
            float interpolationAlpha = -1.0f)
        {
            var defaultSource = Object.RenderSource;
            var defaultTimeframe = Object.RenderTimeframe;

            Object.RenderSource = renderSource;
            Object.RenderTimeframe = renderTimeframe;

            var buffersValid = TryGetSnapshotsBuffers(out var fromBuffer, out var toBuffer, out var alpha);

            Object.RenderSource = defaultSource;
            Object.RenderTimeframe = defaultTimeframe;

            if (buffersValid == false)
                return;
            if (UpdateInterpolationTick(fromBuffer.Tick, toBuffer.Tick) == false)
                return;

            if (interpolationAlpha >= 0.0f && interpolationAlpha <= 1.0f) alpha = interpolationAlpha;

            var deltaTime = Runner.DeltaTime;
            var renderTick = fromBuffer.Tick + alpha * (toBuffer.Tick - fromBuffer.Tick);

            RenderData.CopyFromOther(FixedData);

            RenderData.Frame = Time.frameCount;
            RenderData.Tick = Mathf.RoundToInt(renderTick);
            RenderData.Alpha = alpha;
            RenderData.DeltaTime = deltaTime;
            RenderData.UpdateDeltaTime = deltaTime;
            RenderData.Time = renderTick * deltaTime;

            _networkContext.Data = RenderData;

            var interpolationInfo = new KCCInterpolationInfo();
            interpolationInfo.FromBuffer = fromBuffer;
            interpolationInfo.ToBuffer = toBuffer;
            interpolationInfo.Alpha = alpha;

            for (int i = 0, count = _networkProperties.Length; i < count; ++i)
            {
                var property = _networkProperties[i];
                property.Interpolate(interpolationInfo);
                interpolationInfo.Offset += property.WordCount;
            }

            // User interpolation and post-processing.
            InterpolateUserNetworkData(RenderData, interpolationInfo);
        }

        private void InterpolateNetworkTransform()
        {
            var defaultSource = Object.RenderSource;
            var defaultTimeframe = Object.RenderTimeframe;

            Object.RenderSource = RenderSource.Interpolated;
            Object.RenderTimeframe = RenderTimeframe.Remote;

            var buffersValid = TryGetSnapshotsBuffers(out var fromBuffer, out var toBuffer, out var alpha);

            Object.RenderSource = defaultSource;
            Object.RenderTimeframe = defaultTimeframe;

            if (buffersValid == false)
                return;
            if (UpdateInterpolationTick(fromBuffer.Tick, toBuffer.Tick) == false)
                return;

            KCCNetworkProperties.ReadTransforms(fromBuffer, toBuffer, out var fromPosition, out var toPosition,
                out var fromLookPitch, out var toLookPitch, out var fromLookYaw, out var toLookYaw);

            FixedData.BasePosition = fromPosition;
            FixedData.DesiredPosition = toPosition;
            FixedData.TargetPosition = Vector3.Lerp(fromPosition, toPosition, alpha);
            FixedData.LookPitch = Mathf.Lerp(fromLookPitch, toLookPitch, alpha);
            FixedData.LookYaw = KCCUtility.InterpolateRange(fromLookYaw, toLookYaw, -180.0f, 180.0f, alpha);

            RenderData.BasePosition = FixedData.BasePosition;
            RenderData.DesiredPosition = FixedData.DesiredPosition;
            RenderData.TargetPosition = FixedData.TargetPosition;
            RenderData.LookPitch = FixedData.LookPitch;
            RenderData.LookYaw = FixedData.LookYaw;

            Transform.SetPositionAndRotation(RenderData.TargetPosition, RenderData.TransformRotation);
        }

        private bool UpdateInterpolationTick(int fromTick, int toTick)
        {
            var ticks = toTick - fromTick;
            if (ticks <= 0 && _interpolationTick == fromTick)
            {
                // There's no new data for interpolation.
                _interpolationAttempts = 30;
                return false;
            }

            if (_interpolationAttempts > 0)
            {
                // We have new data for remote snapshot interpolation, however the buffer has invalid tick equal to Runner.Tick.
                // Just ignore this case, the tick should be corrected within several frames.
                if (toTick == Runner.Tick)
                {
                    --_interpolationAttempts;
                    return false;
                }

                _interpolationAttempts = 0;
            }

            _interpolationTick = fromTick;
            return true;
        }

        private void RestoreHistoryData(KCCData historyData)
        {
            // Some values can be synchronized from user code.
            // We have to ensure these properties are in correct state with other properties.

            if (FixedData.IsGrounded)
            {
                // Reset IsGrounded and WasGrounded to history state, otherwise using GroundNormal and other ground related properties leads to undefined behavior and NaN propagation.
                // This has effect only if IsGrounded and WasGrounded is synchronized over network.
                FixedData.IsGrounded = historyData.IsGrounded;
                FixedData.WasGrounded = historyData.WasGrounded;
            }

            // User history data restoration.

            RestoreUserHistoryData(historyData);
        }

        private void InitializeNetworkProperties()
        {
            if (_networkContext != null)
                return;

            _networkContext = new KCCNetworkContext();
            _networkContext.KCC = this;
            _networkContext.Settings = _settings;

            var properties = new List<IKCCNetworkProperty>(32);
            properties.Add(new KCCNetworkProperties(_networkContext));

            InitializeUserNetworkProperties(_networkContext, properties);

            _networkProperties = properties.ToArray();
        }

        // PARTIAL METHODS

        partial void InitializeUserNetworkProperties(KCCNetworkContext networkContext,
            List<IKCCNetworkProperty> networkProperties);

        partial void InterpolateUserNetworkData(KCCData data, KCCInterpolationInfo interpolationInfo);
        partial void RestoreUserHistoryData(KCCData historyData);
    }
}