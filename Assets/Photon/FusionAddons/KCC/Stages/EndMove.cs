namespace Fusion.Addons.KCC
{
	/// <summary>
	///     Stage executed on the end of fully predicted or extrapolated move.
	///     Use to apply any post processing.
	/// </summary>
	public interface IEndMove : IKCCStage<EndMove>
    {
    }

	/// <summary>
	///     Stage object used for <c>IEndMove</c> stage.
	/// </summary>
	public sealed class EndMove : IEndMove
    {
        // IKCCStage<EndMove> INTERFACE

        void IKCCStage<EndMove>.Execute(EndMove stage, KCC kcc, KCCData data)
        {
        }
    }
}