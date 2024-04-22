using System;
using UnityEngine;

namespace Fusion.Addons.KCC
{
    public sealed unsafe class KCCNetworkVector2<TContext> : KCCNetworkProperty<TContext> where TContext : class
    {
        private readonly Func<TContext, Vector2> _get;

        private readonly Func<TContext, float, Vector2, Vector2, Vector2> _interpolate;
        // PRIVATE MEMBERS

        private readonly float _readAccuracy;

        private readonly Action<TContext, Vector2> _set;
        private readonly float _writeAccuracy;

        // CONSTRUCTORS

        public KCCNetworkVector2(TContext context, float accuracy, Action<TContext, Vector2> set,
            Func<TContext, Vector2> get, Func<TContext, float, Vector2, Vector2, Vector2> interpolate) : base(context,
            2)
        {
            _readAccuracy = accuracy > 0.0f ? accuracy : 0.0f;
            _writeAccuracy = accuracy > 0.0f ? 1.0f / accuracy : 0.0f;

            _set = set;
            _get = get;
            _interpolate = interpolate;
        }

        // KCCNetworkProperty INTERFACE

        public override void Read(int* ptr)
        {
            Vector2 value = default;

            if (_readAccuracy <= 0.0f)
            {
                value.x = *(float*)(ptr + 0);
                value.y = *(float*)(ptr + 1);
            }
            else
            {
                value.x = *(ptr + 0) * _readAccuracy;
                value.y = *(ptr + 1) * _readAccuracy;
            }

            _set(Context, value);
        }

        public override void Write(int* ptr)
        {
            var value = _get(Context);

            if (_writeAccuracy <= 0.0f)
            {
                *(float*)(ptr + 0) = value.x;
                *(float*)(ptr + 1) = value.y;
            }
            else
            {
                *(ptr + 0) = value.x < 0.0f
                    ? (int)(value.x * _writeAccuracy - 0.5f)
                    : (int)(value.x * _writeAccuracy + 0.5f);
                *(ptr + 1) = value.y < 0.0f
                    ? (int)(value.y * _writeAccuracy - 0.5f)
                    : (int)(value.y * _writeAccuracy + 0.5f);
            }
        }

        public override void Interpolate(KCCInterpolationInfo interpolationInfo)
        {
            var offset = interpolationInfo.Offset;

            Vector2 fromValue;
            Vector2 toValue;
            Vector2 value;

            if (_readAccuracy <= 0.0f)
            {
                fromValue.x = interpolationInfo.FromBuffer.ReinterpretState<float>(offset + 0);
                fromValue.y = interpolationInfo.FromBuffer.ReinterpretState<float>(offset + 1);

                toValue.x = interpolationInfo.ToBuffer.ReinterpretState<float>(offset + 0);
                toValue.y = interpolationInfo.ToBuffer.ReinterpretState<float>(offset + 1);
            }
            else
            {
                fromValue.x = interpolationInfo.FromBuffer.ReinterpretState<int>(offset + 0) * _readAccuracy;
                fromValue.y = interpolationInfo.FromBuffer.ReinterpretState<int>(offset + 1) * _readAccuracy;

                toValue.x = interpolationInfo.ToBuffer.ReinterpretState<int>(offset + 0) * _readAccuracy;
                toValue.y = interpolationInfo.ToBuffer.ReinterpretState<int>(offset + 1) * _readAccuracy;
            }

            if (_interpolate != null)
                value = _interpolate(Context, interpolationInfo.Alpha, fromValue, toValue);
            else
                value = Vector2.Lerp(fromValue, toValue, interpolationInfo.Alpha);

            _set(Context, value);
        }
    }
}