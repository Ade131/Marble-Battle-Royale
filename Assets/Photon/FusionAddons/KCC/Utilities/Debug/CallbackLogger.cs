using UnityEngine;

namespace Fusion.Addons.KCC
{
    [DisallowMultipleComponent]
    public sealed class CallbackLogger : NetworkBehaviour, IAfterSpawned, IBeforeUpdate, IBeforeAllTicks, IBeforeTick,
        IAfterTick, IAfterAllTicks, IAfterRender, IAfterUpdate, IBeforeCopyPreviousState, IBeforeClientPredictionReset,
        IAfterClientPredictionReset
    {
#if UNITY_EDITOR
        private const string PREFIX = "<color=#FFFFFF>";
        private const string POSTFIX = "</color>";
#else
		private const string PREFIX = "";
		private const string POSTFIX = "";
#endif

        [SerializeField] private bool _enableProxySimulation = true;

        public void Awake()
        {
            KCCUtility.Log(this, null, EKCCLogType.Info, $"{PREFIX}{nameof(Awake)}{POSTFIX}");
        }

        public void Start()
        {
            KCCUtility.Log(this, null, EKCCLogType.Info, $"{PREFIX}{nameof(Start)}{POSTFIX}");
        }

        public void Update()
        {
            KCCUtility.Log(this, null, EKCCLogType.Info, $"{PREFIX}{nameof(Update)}{POSTFIX}");
        }

        public void LateUpdate()
        {
            KCCUtility.Log(this, null, EKCCLogType.Info, $"{PREFIX}{nameof(LateUpdate)}{POSTFIX}");
        }

        public override void Spawned()
        {
            if (HasStateAuthority) name += "|S";
            if (HasInputAuthority) name += "|I";
            if (IsProxy) name += "|P";

            if (IsProxy && Runner.GameMode != GameMode.Shared) Runner.SetIsSimulated(Object, _enableProxySimulation);

            KCCUtility.Log(this, null, EKCCLogType.Info, $"{PREFIX}{nameof(Spawned)}{POSTFIX}", $"Stage={Runner.Stage}",
                $"IsResimulation={Runner.IsResimulation}", $"IsLastTick={Runner.IsLastTick}",
                $"HasStateAuthority={HasStateAuthority}", $"HasInputAuthority={HasInputAuthority}");
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            KCCUtility.Log(this, null, EKCCLogType.Info, $"{PREFIX}{nameof(Despawned)}{POSTFIX}",
                $"Stage={Runner.Stage}", $"IsResimulation={Runner.IsResimulation}", $"IsLastTick={Runner.IsLastTick}",
                $"hasState={hasState}");
        }

        public override void FixedUpdateNetwork()
        {
            KCCUtility.Log(this, null, EKCCLogType.Info, $"{PREFIX}{nameof(FixedUpdateNetwork)}{POSTFIX}",
                $"Stage={Runner.Stage}", $"IsResimulation={Runner.IsResimulation}", $"IsLastTick={Runner.IsLastTick}");
        }

        public override void Render()
        {
            KCCUtility.Log(this, null, EKCCLogType.Info, $"{PREFIX}{nameof(Render)}{POSTFIX}", $"Stage={Runner.Stage}",
                $"IsResimulation={Runner.IsResimulation}", $"IsLastTick={Runner.IsLastTick}");
        }

        void IAfterSpawned.AfterSpawned()
        {
            KCCUtility.Log(this, null, EKCCLogType.Info, $"{PREFIX}{nameof(IAfterSpawned)}{POSTFIX}",
                $"Stage={Runner.Stage}", $"IsResimulation={Runner.IsResimulation}", $"IsLastTick={Runner.IsLastTick}");
        }

        void IBeforeUpdate.BeforeUpdate()
        {
            KCCUtility.Log(this, null, EKCCLogType.Info, $"{PREFIX}{nameof(IBeforeUpdate)}{POSTFIX}",
                $"Stage={Runner.Stage}", $"IsResimulation={Runner.IsResimulation}", $"IsLastTick={Runner.IsLastTick}");
        }

        void IBeforeAllTicks.BeforeAllTicks(bool resimulation, int tickCount)
        {
            KCCUtility.Log(this, null, EKCCLogType.Info, $"{PREFIX}{nameof(IBeforeAllTicks)}{POSTFIX}",
                $"Stage={Runner.Stage}", $"IsResimulation={Runner.IsResimulation}", $"IsLastTick={Runner.IsLastTick}",
                $"resimulation={resimulation}", $"tickCount={tickCount}");
        }

        void IBeforeTick.BeforeTick()
        {
            KCCUtility.Log(this, null, EKCCLogType.Info, $"{PREFIX}{nameof(IBeforeTick)}{POSTFIX}",
                $"Stage={Runner.Stage}", $"IsResimulation={Runner.IsResimulation}", $"IsLastTick={Runner.IsLastTick}");
        }

        void IAfterTick.AfterTick()
        {
            KCCUtility.Log(this, null, EKCCLogType.Info, $"{PREFIX}{nameof(IAfterTick)}{POSTFIX}",
                $"Stage={Runner.Stage}", $"IsResimulation={Runner.IsResimulation}", $"IsLastTick={Runner.IsLastTick}");
        }

        void IAfterAllTicks.AfterAllTicks(bool resimulation, int tickCount)
        {
            KCCUtility.Log(this, null, EKCCLogType.Info, $"{PREFIX}{nameof(IAfterAllTicks)}{POSTFIX}",
                $"Stage={Runner.Stage}", $"IsResimulation={Runner.IsResimulation}", $"IsLastTick={Runner.IsLastTick}",
                $"resimulation={resimulation}", $"tickCount={tickCount}");
        }

        void IAfterRender.AfterRender()
        {
            KCCUtility.Log(this, null, EKCCLogType.Info, $"{PREFIX}{nameof(IAfterRender)}{POSTFIX}",
                $"Stage={Runner.Stage}", $"IsResimulation={Runner.IsResimulation}", $"IsLastTick={Runner.IsLastTick}");
        }

        void IAfterUpdate.AfterUpdate()
        {
            KCCUtility.Log(this, null, EKCCLogType.Info, $"{PREFIX}{nameof(IAfterUpdate)}{POSTFIX}",
                $"Stage={Runner.Stage}", $"IsResimulation={Runner.IsResimulation}", $"IsLastTick={Runner.IsLastTick}");
        }

        void IBeforeCopyPreviousState.BeforeCopyPreviousState()
        {
            KCCUtility.Log(this, null, EKCCLogType.Info, $"{PREFIX}{nameof(IBeforeCopyPreviousState)}{POSTFIX}",
                $"Stage={Runner.Stage}", $"IsResimulation={Runner.IsResimulation}", $"IsLastTick={Runner.IsLastTick}");
        }

        void IBeforeClientPredictionReset.BeforeClientPredictionReset()
        {
            KCCUtility.Log(this, null, EKCCLogType.Info, $"{PREFIX}{nameof(IBeforeClientPredictionReset)}{POSTFIX}",
                $"Stage={Runner.Stage}", $"IsResimulation={Runner.IsResimulation}", $"IsLastTick={Runner.IsLastTick}",
                $"LatestServerTick:{Runner.LatestServerTick}");
        }

        void IAfterClientPredictionReset.AfterClientPredictionReset()
        {
            KCCUtility.Log(this, null, EKCCLogType.Info, $"{PREFIX}{nameof(IAfterClientPredictionReset)}{POSTFIX}",
                $"Stage={Runner.Stage}", $"IsResimulation={Runner.IsResimulation}", $"IsLastTick={Runner.IsLastTick}",
                $"LatestServerTick:{Runner.LatestServerTick}");
        }
    }
}