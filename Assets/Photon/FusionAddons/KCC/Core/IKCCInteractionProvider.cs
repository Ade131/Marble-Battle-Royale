namespace Fusion.Addons.KCC
{
	/// <summary>
	///     Base interface for all KCC interaction providers.
	/// </summary>
	public interface IKCCInteractionProvider
    {
	    /// <summary>
	    ///     Used to control start of the interaction with KCC.
	    /// </summary>
	    bool CanStartInteraction(KCC kcc, KCCData data)
        {
            return true;
        }

	    /// <summary>
	    ///     Used to control end of the interaction with KCC.
	    ///     All interactions are force stopped on despawn regardless of the return value.
	    /// </summary>
	    bool CanStopInteraction(KCC kcc, KCCData data)
        {
            return true;
        }
    }
}