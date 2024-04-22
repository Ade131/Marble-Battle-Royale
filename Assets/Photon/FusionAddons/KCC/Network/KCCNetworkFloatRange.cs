using System;
using UnityEngine;

namespace Fusion.Addons.KCC
{
    public sealed unsafe class KCCNetworkFloatRange<TContext> : KCCNetworkProperty<TContext> where TContext : class
    {
        private readonly Func<TContext, float> _get;
        private readonly Func<TContext, float, float, float, float> _interpolate;

        private readonly float _max;
        // PRIVATE MEMBERS

        private readonly float _min;
        private readonly float _readAccuracy;

        private readonly Action<TContext, float> _set;
        private readonly float _writeAccuracy;

        // CONSTRUCTORS

        public KCCNetworkFloatRange(TContext context, float min, float max, float accuracy, Action<TContext, float> set,
            Func<TContext, float> get, Func<TContext, float, float, float, float> interpolate) : base(context, 1)
        {
            _min = min;
            _max = max;
            _readAccuracy = accuracy > 0.0f ? accuracy : 0.0f;
            _writeAccuracy = accuracy > 0.0f ? 1.0f / accuracy : 0.0f;

            _set = set;
            _get = get;
            _interpolate = interpolate;
        }

        // KCCNetworkProperty INTERFACE

        public override void Read(int* ptr)
        {
            float value;

            if (_readAccuracy <= 0.0f)
                value = *(float*)ptr;
            else
                value = *ptr * _readAccuracy;

            _set(Context, value);
        }

        public override void Write(int* ptr)
        {
            var value = Mathf.Clamp(_get(Context), _min, _max);

            if (_writeAccuracy <= 0.0f)
                *(float*)ptr = value;
            else
                *ptr = value < 0.0f ? (int)(value * _writeAccuracy - 0.5f) : (int)(value * _writeAccuracy + 0.5f);
        }

        public override void Interpolate(KCCInterpolationInfo interpolationInfo)
        {
            var fromValue = _readAccuracy <= 0.0f
                ? interpolationInfo.FromBuffer.ReinterpretState<float>(interpolationInfo.Offset)
                : interpolationInfo.FromBuffer.ReinterpretState<int>(interpolationInfo.Offset) * _readAccuracy;
            var toValue = _readAccuracy <= 0.0f
                ? interpolationInfo.ToBuffer.ReinterpretState<float>(interpolationInfo.Offset)
                : interpolationInfo.ToBuffer.ReinterpretState<int>(interpolationInfo.Offset) * _readAccuracy;
            float value;

            if (_interpolate != null)
                value = _interpolate(Context, interpolationInfo.Alpha, fromValue, toValue);
            else
                value = KCCUtility.InterpolateRange(fromValue, toValue, _min, _max, interpolationInfo.Alpha);

            _set(Context, value);
        }
    }
}