using System;

namespace Fusion.Addons.KCC
{
    public sealed unsafe class KCCNetworkBool<TContext> : KCCNetworkProperty<TContext> where TContext : class
    {
        private readonly Func<TContext, bool> _get;

        private readonly Func<TContext, float, bool, bool, bool> _interpolate;
        // PRIVATE MEMBERS

        private readonly Action<TContext, bool> _set;

        // CONSTRUCTORS

        public KCCNetworkBool(TContext context, Action<TContext, bool> set, Func<TContext, bool> get,
            Func<TContext, float, bool, bool, bool> interpolate) : base(context, 1)
        {
            _set = set;
            _get = get;
            _interpolate = interpolate;
        }

        // KCCNetworkProperty INTERFACE

        public override void Read(int* ptr)
        {
            _set(Context, *ptr != 0 ? true : false);
        }

        public override void Write(int* ptr)
        {
            *ptr = _get(Context) ? 1 : 0;
        }

        public override void Interpolate(KCCInterpolationInfo interpolationInfo)
        {
            var fromValue = interpolationInfo.FromBuffer.ReinterpretState<int>(interpolationInfo.Offset) != 0
                ? true
                : false;
            var toValue = interpolationInfo.ToBuffer.ReinterpretState<int>(interpolationInfo.Offset) != 0
                ? true
                : false;
            bool value;

            if (_interpolate != null)
                value = _interpolate(Context, interpolationInfo.Alpha, fromValue, toValue);
            else
                value = interpolationInfo.Alpha < 0.5f ? fromValue : toValue;

            _set(Context, value);
        }
    }
}