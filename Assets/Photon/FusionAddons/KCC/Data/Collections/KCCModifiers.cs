using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.KCC
{
	/// <summary>
	///     Container for manually registered modifiers. Processor needs to be cached (accessed every frame).
	/// </summary>
	public sealed class KCCModifier : KCCInteraction<KCCModifier>
    {
        // PUBLIC MEMBERS

        public IKCCProcessor Processor;

        // KCCInteraction<TInteraction> INTERFACE

        public override void Initialize()
        {
            Processor = Provider is IKCCProcessorProvider processorProvider ? processorProvider.GetProcessor() : null;
        }

        public override void Deinitialize()
        {
            Processor = null;
        }

        public override void CopyFromOther(KCCModifier other)
        {
            Processor = other.Processor;
        }
    }

	/// <summary>
	///     Collection dedicated to tracking of manually registered modifiers and their processors. Managed entirely by
	///     <c>KCC</c> component.
	/// </summary>
	public sealed class KCCModifiers : KCCInteractions<KCCModifier>
    {
        // PUBLIC METHODS

        public bool HasProcessor<T>() where T : class
        {
            for (int i = 0, count = All.Count; i < count; ++i)
                if (All[i].Processor is T)
                    return true;

            return false;
        }

        public bool HasProcessor<T>(T processor) where T : Component, IKCCProcessor
        {
            for (int i = 0, count = All.Count; i < count; ++i)
                if (ReferenceEquals(All[i].Processor, processor))
                    return true;

            return false;
        }

        public T GetProcessor<T>() where T : class
        {
            for (int i = 0, count = All.Count; i < count; ++i)
                if (All[i].Processor is T processor)
                    return processor;

            return default;
        }

        public void GetProcessors<T>(List<T> processors, bool clearList = true) where T : class
        {
            if (clearList) processors.Clear();

            for (int i = 0, count = All.Count; i < count; ++i)
                if (All[i].Processor is T processor)
                    processors.Add(processor);
        }
    }
}