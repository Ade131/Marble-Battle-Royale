namespace Fusion.Addons.KCC
{
	/// <summary>
	///     Stage executed before calculation of position delta and processing move steps.
	///     This stage is executed only if pending delta time > KCCSettings.ExtrapolationDeltaTimeThreshold (0.05ms), otherwise
	///     extrapolation is used.
	/// </summary>
	public interface IPrepareData : IKCCStage<PrepareData>
    {
    }

	/// <summary>
	///     Stage object used for <c>IPrepareData</c> stage.
	/// </summary>
	public sealed class PrepareData : IPrepareData
    {
        // IKCCStage<PrepareData> INTERFACE

        void IKCCStage<PrepareData>.Execute(PrepareData stage, KCC kcc, KCCData data)
        {
        }
    }
}