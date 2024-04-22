namespace Fusion.Addons.KCC
{
	/// <summary>
	///     Base interface for all KCC processors.
	///     Execution of methods is fully supported on 1) Prefabs, 2) Instances spawned with GameObject.Instantiate(), 3)
	///     Instances spawned with Runner.Spawn()
	/// </summary>
	public interface IKCCProcessor
    {
	    /// <summary>
	    ///     Controls whether the processor is active in current execution loop.
	    ///     Inactive processors still get OnEnter/OnExit callbacks, but everything else is ignored including stages.
	    ///     This method can be used to filter out processor early as performance optimization.
	    /// </summary>
	    bool IsActive(KCC kcc)
        {
            return true;
        }

	    /// <summary>
	    ///     Processors with higher priority are executed first.
	    ///     If two processors have same priority, they are sorted by their source category: Modifier > Collision > Local >
	    ///     External.
	    ///     If two processors from same category have same priority, they are sorted by the time of registration to KCC - first
	    ///     registered is first executed.
	    /// </summary>
	    float GetPriority(KCC kcc)
        {
            return default;
        }

	    /// <summary>
	    ///     Called when a KCC starts interacting with the processor.
	    /// </summary>
	    void OnEnter(KCC kcc, KCCData data)
        {
        }

	    /// <summary>
	    ///     Called when a KCC stops interacting with the processor.
	    /// </summary>
	    void OnExit(KCC kcc, KCCData data)
        {
        }

	    /// <summary>
	    ///     Called when a KCC interacts with the processor and the movement is fully predicted or extrapolated.
	    /// </summary>
	    void OnStay(KCC kcc, KCCData data)
        {
        }

	    /// <summary>
	    ///     Called when a KCC interacts with the processor and the movement is interpolated.
	    /// </summary>
	    void OnInterpolate(KCC kcc, KCCData data)
        {
        }
    }
}