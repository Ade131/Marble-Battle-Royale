namespace Fusion.Addons.KCC
{
	/// <summary>
	///     Base type for storing non-generic stage reference.
	/// </summary>
	public interface IKCCStage
    {
    }

	/// <summary>
	///     Base type for stages.
	/// </summary>
	public interface IKCCStage<TStageObject> : IKCCStage where TStageObject : IKCCStage<TStageObject>
    {
	    /// <summary>
	    ///     Priority of the stage.
	    ///     Use explicit interface implementation to override public Priority property in processors.
	    /// </summary>
	    float GetPriority(KCC kcc)
        {
            return default;
        }

	    /// <summary>
	    ///     Called on all processors and stage object when executing the stage.
	    /// </summary>
	    void Execute(TStageObject stage, KCC kcc, KCCData data);
    }

	/// <summary>
	///     Callback interface for stage objects, called on the beginning of the stage execution.
	/// </summary>
	public interface IBeforeStage
    {
        void BeforeStage(KCC kcc, KCCData data);
    }

	/// <summary>
	///     Callback interface for stage objects, called on the end of the stage execution (after all stage post-processes).
	/// </summary>
	public interface IAfterStage
    {
        void AfterStage(KCC kcc, KCCData data);
    }
}