using UnityEngine;

namespace Fusion.Addons.KCC
{
	/// <summary>
	///     Default processor implementation with support of [Networked] properties, runtime lookup and synchronization.
	///     Execution of methods is fully supported on 1) Prefabs, 2) Instances spawned with GameObject.Instantiate(), 3)
	///     Instances spawned with Runner.Spawn().
	/// </summary>
	[DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public abstract class NetworkKCCProcessor : NetworkBehaviour, IKCCProcessor, IKCCProcessorProvider
    {
        // IKCCProcessor INTERFACE

        /// <summary>
        ///     Processors with higher priority are executed first.
        ///     If two processors have same priority, they are sorted by their source category: Modifier > Collision > Local >
        ///     External.
        ///     If two processors from same category have same priority, they are sorted by the time of registration to KCC - first
        ///     registered is first executed.
        ///     This method is implicit implementation of IKCCProcessor.GetPriority() and all IKCCStage&lt;T&gt;.GetPriority().
        ///     Priority of any stage can be redefined by explicit interface implementation.
        /// </summary>
        public virtual float GetPriority(KCC kcc)
        {
            return default;
        }

        /// <summary>
        ///     Called when a KCC starts interacting with the processor.
        /// </summary>
        public virtual void OnEnter(KCC kcc, KCCData data)
        {
        }

        /// <summary>
        ///     Called when a KCC stops interacting with the processor.
        /// </summary>
        public virtual void OnExit(KCC kcc, KCCData data)
        {
        }

        /// <summary>
        ///     Called when a KCC interacts with the processor and the movement is fully predicted or extrapolated.
        /// </summary>
        public virtual void OnStay(KCC kcc, KCCData data)
        {
        }

        /// <summary>
        ///     Called when a KCC interacts with the processor and the movement is interpolated.
        /// </summary>
        public virtual void OnInterpolate(KCC kcc, KCCData data)
        {
        }

        // IKCCInteractionProvider INTERFACE

        /// <summary>
        ///     Used to control start of the interaction with KCC.
        /// </summary>
        public virtual bool CanStartInteraction(KCC kcc, KCCData data)
        {
            return true;
        }

        /// <summary>
        ///     Used to control end of the interaction with KCC.
        ///     All interactions are force stopped on despawn regardless of the return value.
        /// </summary>
        public virtual bool CanStopInteraction(KCC kcc, KCCData data)
        {
            return true;
        }

        // IKCCProcessorProvider INTERFACE

        IKCCProcessor IKCCProcessorProvider.GetProcessor()
        {
            return this;
        }
    }
}