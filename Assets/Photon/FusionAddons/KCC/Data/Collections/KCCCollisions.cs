using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.KCC
{
	/// <summary>
	///     Container for collision-based interactions. Collider and Processor need to be cached (accessed every frame).
	/// </summary>
	public sealed class KCCCollision : KCCInteraction<KCCCollision>
    {
        // PUBLIC MEMBERS

        public Collider Collider;
        public IKCCProcessor Processor;

        // KCCInteraction<TInteraction> INTERFACE

        public override void Initialize()
        {
            Collider = NetworkObject.GetComponentNoAlloc<Collider>();
            Processor = Provider is IKCCProcessorProvider processorProvider ? processorProvider.GetProcessor() : null;
        }

        public override void Deinitialize()
        {
            Collider = null;
            Processor = null;
        }

        public override void CopyFromOther(KCCCollision other)
        {
            Collider = other.Collider;
            Processor = other.Processor;
        }
    }

	/// <summary>
	///     Collection dedicated to tracking of collision-based interactions with colliders/triggers and related processors.
	///     Managed entirely by <c>KCC</c> component.
	/// </summary>
	public sealed class KCCCollisions : KCCInteractions<KCCCollision>
    {
        // PUBLIC METHODS

        public bool HasCollider(Collider collider)
        {
            return Find(collider, out var index) != null;
        }

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

        public KCCCollision Add(NetworkObject networkObject, IKCCInteractionProvider provider, Collider collider)
        {
            var collision = GetFromPool();
            collision.Collider = collider;
            collision.Processor = provider is IKCCProcessorProvider processorProvider
                ? processorProvider.GetProcessor()
                : null;
            AddInternal(collision, networkObject, provider, false);

            return collision;
        }

        // PRIVATE METHODS

        private KCCCollision Find(Collider collider, out int index)
        {
            for (int i = 0, count = All.Count; i < count; ++i)
            {
                var collision = All[i];
                if (ReferenceEquals(collision.Collider, collider))
                {
                    index = i;
                    return collision;
                }
            }

            index = -1;
            return default;
        }
    }
}