using UnityEngine;

namespace Fusion.Addons.KCC
{
	/// <summary>
	///     Default NetworkKCCProcessor provider.
	/// </summary>
	[RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkKCCProcessorProvider : MonoBehaviour, IKCCProcessorProvider
    {
        // PRIVATE MEMBERS

        [SerializeField] private NetworkKCCProcessor _processor;

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
            return _processor;
        }
    }
}