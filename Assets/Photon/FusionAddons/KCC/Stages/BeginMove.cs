namespace Fusion.Addons.KCC
{
	/// <summary>
	///     Stage executed at the beginning of a fully predicted or extrapolated move.
	///     Use to configure KCC, enable/suppress features, add forces, ...
	/// </summary>
	public interface IBeginMove : IKCCStage<BeginMove>
    {
    }

	/// <summary>
	///     Stage object used for <c>IBeginMove</c> stage.
	/// </summary>
	public sealed class BeginMove : IBeginMove
    {
        // IKCCStage<BeginMove> INTERFACE

        void IKCCStage<BeginMove>.Execute(BeginMove stage, KCC kcc, KCCData data)
        {
        }
    }
}