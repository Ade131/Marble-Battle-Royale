using System;
using UnityEngine;

namespace Fusion.Addons.KCC
{
    public sealed unsafe class KCCNetworkFloatArray<TContext> : KCCNetworkProperty<TContext> where TContext : class
    {
        // PRIVATE MEMBERS

        private readonly int _count;
        private readonly Func<TContext, int, float> _get;
        private readonly Func<TContext, int, float, float, float, float> _interpolate;
        private readonly float _readAccuracy;

        private readonly Action<TContext, int, float> _set;
        private readonly float _writeAccuracy;

        // CONSTRUCTORS

        public KCCNetworkFloatArray(TContext context, int count, float accuracy, Action<TContext, int, float> set,
            Func<TContext, int, float> get, Func<TContext, int, float, float, float, float> interpolate) : base(context,
            count)
        {
            _count = count;
            _readAccuracy = accuracy > 0.0f ? accuracy : 0.0f;
            _writeAccuracy = accuracy > 0.0f ? 1.0f / accuracy : 0.0f;

            _set = set;
            _get = get;
            _interpolate = interpolate;
        }

        // KCCNetworkProperty INTERFACE

        public override void Read(int* ptr)
        {
            for (var i = 0; i < _count; ++i)
            {
                float value;

                if (_readAccuracy <= 0.0f)
                    value = *(float*)ptr;
                else
                    value = *ptr * _readAccuracy;

                _set(Context, i, value);

                ++ptr;
            }
        }

        public override void Write(int* ptr)
        {
            for (var i = 0; i < _count; ++i)
            {
                var value = _get(Context, i);

                if (_writeAccuracy <= 0.0f)
                    *(float*)ptr = value;
                else
                    *ptr = value < 0.0f ? (int)(value * _writeAccuracy - 0.5f) : (int)(value * _writeAccuracy + 0.5f);

                ++ptr;
            }
        }

        public override void Interpolate(KCCInterpolationInfo interpolationInfo)
        {
            for (var i = 0; i < _count; ++i)
            {
                var fromValue = _readAccuracy <= 0.0f
                    ? interpolationInfo.FromBuffer.ReinterpretState<float>(interpolationInfo.Offset)
                    : interpolationInfo.FromBuffer.ReinterpretState<int>(interpolationInfo.Offset) * _readAccuracy;
                var toValue = _readAccuracy <= 0.0f
                    ? interpolationInfo.ToBuffer.ReinterpretState<float>(interpolationInfo.Offset)
                    : interpolationInfo.ToBuffer.ReinterpretState<int>(interpolationInfo.Offset) * _readAccuracy;
                float value;

                if (_interpolate != null)
                    value = _interpolate(Context, i, interpolationInfo.Alpha, fromValue, toValue);
                else
                    value = Mathf.Lerp(fromValue, toValue, interpolationInfo.Alpha);

                _set(Context, i, value);

                ++interpolationInfo.Offset;
            }
        }
    }
}