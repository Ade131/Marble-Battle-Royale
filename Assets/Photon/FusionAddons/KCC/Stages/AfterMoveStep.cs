namespace Fusion.Addons.KCC
{
	/// <summary>
	///     Stage executed after each move step (physics query, depenetration from colliders, updating collision hits), before
	///     firing OnEnter/OnExit callbacks.
	///     Use for position corrections, step detection, ground snapping, vector projections, ...
	///     This stage is executed only if pending delta time > KCCSettings.ExtrapolationDeltaTimeThreshold (0.05ms), otherwise
	///     extrapolation is used.
	/// </summary>
	public interface IAfterMoveStep : IKCCStage<AfterMoveStep>
    {
    }

	/// <summary>
	///     Stage object used for IAfterMoveStep stage, contains overlap info with collision hits.
	///     Refreshing KCCData.Hits after the stage can be explicitly requested if needed (typically after modifying
	///     KCCData.TargetPosition).
	/// </summary>
	public sealed class AfterMoveStep : IAfterMoveStep, IBeforeStage, IAfterStage
    {
        // PUBLIC MEMBERS

        public readonly KCCOverlapInfo OverlapInfo = new(KCC.CACHE_SIZE);
        private EKCCHitsOverlapQuery _updateHitsOverlapQuery;

        private bool _updateHitsRequested;

        // IKCCStage<AfterMoveStep> INTERFACE

        void IKCCStage<AfterMoveStep>.Execute(AfterMoveStep stage, KCC kcc, KCCData data)
        {
        }

        // IAfterStage INTERFACE

        void IAfterStage.AfterStage(KCC kcc, KCCData data)
        {
            if (_updateHitsRequested) kcc.UpdateHits(OverlapInfo, _updateHitsOverlapQuery);
        }

        // IBeforeStage INTERFACE

        void IBeforeStage.BeforeStage(KCC kcc, KCCData data)
        {
            _updateHitsRequested = false;
            _updateHitsOverlapQuery = EKCCHitsOverlapQuery.Default;
        }

        // PUBLIC METHODS

        /// <summary>
        ///     Request KCCData.Hits update after the stage. Metadata (collision type, penetration, ...) for new hits will be
        ///     missing as they require full depenetration pass.
        /// </summary>
        /// <param name="forceNewOverlapQuery">Force execute new overlap query.</param>
        public void RequestUpdateHits(bool forceNewOverlapQuery)
        {
            _updateHitsRequested |= true;

            if (forceNewOverlapQuery) _updateHitsOverlapQuery = EKCCHitsOverlapQuery.New;
        }
    }
}