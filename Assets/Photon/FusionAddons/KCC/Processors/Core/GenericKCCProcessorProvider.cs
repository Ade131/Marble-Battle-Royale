using UnityEngine;

namespace Fusion.Addons.KCC
{
	/// <summary>
	///     Generic <c>IKCCProcessor</c> provider which stores processor reference as <c>UnityEngine.Object</c>.
	/// </summary>
	[RequireComponent(typeof(NetworkObject))]
    public sealed class GenericKCCProcessorProvider : MonoBehaviour, IKCCProcessorProvider
    {
        // PRIVATE MEMBERS

        [SerializeField] [KCCProcessorReference]
        private Object _processor;

        // IKCCInteractionProvider INTERFACE

        bool IKCCInteractionProvider.CanStartInteraction(KCC kcc, KCCData data)
        {
            return true;
        }

        bool IKCCInteractionProvider.CanStopInteraction(KCC kcc, KCCData data)
        {
            return true;
        }

        // IKCCProcessorProvider INTERFACE

        IKCCProcessor IKCCProcessorProvider.GetProcessor()
        {
            return KCCUtility.ResolveProcessor(_processor, out var processor) ? processor : default;
        }
    }
}