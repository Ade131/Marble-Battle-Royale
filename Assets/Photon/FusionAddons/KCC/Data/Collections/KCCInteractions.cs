using System.Collections.Generic;

namespace Fusion.Addons.KCC
{
	/// <summary>
	///     Base container for all interactions.
	/// </summary>
	public abstract class KCCInteraction<TInteraction> where TInteraction : KCCInteraction<TInteraction>, new()
    {
        // PUBLIC MEMBERS

        public KCCNetworkID NetworkID;
        public NetworkObject NetworkObject;
        public IKCCInteractionProvider Provider;

        // KCCInteraction<TInteraction> INTERFACE

        public abstract void Initialize();
        public abstract void Deinitialize();
        public abstract void CopyFromOther(TInteraction other);
    }

	/// <summary>
	///     Base collection for tracking all interactions and their providers.
	/// </summary>
	public abstract class KCCInteractions<TInteraction> where TInteraction : KCCInteraction<TInteraction>, new()
    {
        // PRIVATE MEMBERS

        private static readonly KCCFastStack<TInteraction> _pool = new(256, true);
        // PUBLIC MEMBERS

        public readonly List<TInteraction> All = new();

        public int Count => All.Count;

        // PUBLIC METHODS

        public bool HasProvider<T>() where T : class
        {
            for (int i = 0, count = All.Count; i < count; ++i)
                if (All[i].Provider is T)
                    return true;

            return false;
        }

        public bool HasProvider(IKCCInteractionProvider provider)
        {
            for (int i = 0, count = All.Count; i < count; ++i)
                if (ReferenceEquals(All[i].Provider, provider))
                    return true;

            return false;
        }

        public T GetProvider<T>() where T : class
        {
            for (int i = 0, count = All.Count; i < count; ++i)
                if (All[i].Provider is T provider)
                    return provider;

            return default;
        }

        public void GetProviders<T>(List<T> providers, bool clearList = true) where T : class
        {
            if (clearList) providers.Clear();

            for (int i = 0, count = All.Count; i < count; ++i)
                if (All[i].Provider is T provider)
                    providers.Add(provider);
        }

        public TInteraction Find(IKCCInteractionProvider provider)
        {
            return Find(provider, out var index);
        }

        public TInteraction Add(NetworkObject networkObject, IKCCInteractionProvider provider)
        {
            return AddInternal(networkObject, provider, true);
        }

        public bool Add(NetworkObject networkObject, KCCNetworkID networkID)
        {
            if (networkObject == null)
                return false;

            var provider = networkObject.GetComponentNoAlloc<IKCCInteractionProvider>();

            var interaction = _pool.PopOrCreate();
            interaction.NetworkID = networkID;
            interaction.NetworkObject = networkObject;
            interaction.Provider = provider;
            interaction.Initialize();

            All.Add(interaction);

            return true;
        }

        public bool Remove(TInteraction interaction)
        {
            for (int i = 0, count = All.Count; i < count; ++i)
                if (All[i] == interaction)
                {
                    All.RemoveAt(i);
                    ReturnToPool(interaction);
                    return true;
                }

            return false;
        }

        public void CopyFromOther<T>(T other) where T : KCCInteractions<TInteraction>
        {
            var thisCount = All.Count;
            var otherCount = other.All.Count;

            if (thisCount == otherCount)
            {
                if (thisCount == 0)
                    return;

                for (var i = 0; i < thisCount; ++i)
                {
                    var interaction = All[i];
                    var otherInteraction = other.All[i];

                    interaction.NetworkID = otherInteraction.NetworkID;
                    interaction.NetworkObject = otherInteraction.NetworkObject;
                    interaction.Provider = otherInteraction.Provider;
                    interaction.CopyFromOther(otherInteraction);
                }
            }
            else
            {
                Clear();

                for (var i = 0; i < otherCount; ++i)
                {
                    var otherInteraction = other.All[i];

                    var interaction = _pool.PopOrCreate();
                    interaction.NetworkID = otherInteraction.NetworkID;
                    interaction.NetworkObject = otherInteraction.NetworkObject;
                    interaction.Provider = otherInteraction.Provider;
                    interaction.CopyFromOther(otherInteraction);

                    All.Add(interaction);
                }
            }
        }

        public void Clear()
        {
            for (int i = 0, count = All.Count; i < count; ++i) ReturnToPool(All[i]);

            All.Clear();
        }

        // PROTECTED METHODS

        protected TInteraction AddInternal(NetworkObject networkObject, IKCCInteractionProvider provider,
            bool invokeInitialize)
        {
            var interaction = _pool.PopOrCreate();
            interaction.NetworkID = KCCNetworkID.GetNetworkID(networkObject);
            interaction.NetworkObject = networkObject;
            interaction.Provider = provider;

            if (invokeInitialize) interaction.Initialize();

            All.Add(interaction);

            return interaction;
        }

        protected void AddInternal(TInteraction interaction, NetworkObject networkObject,
            IKCCInteractionProvider provider, bool invokeInitialize)
        {
            interaction.NetworkID = KCCNetworkID.GetNetworkID(networkObject);
            interaction.NetworkObject = networkObject;
            interaction.Provider = provider;

            if (invokeInitialize) interaction.Initialize();

            All.Add(interaction);
        }

        protected TInteraction Find(IKCCInteractionProvider provider, out int index)
        {
            for (int i = 0, count = All.Count; i < count; ++i)
            {
                var interaction = All[i];
                if (ReferenceEquals(interaction.Provider, provider))
                {
                    index = i;
                    return interaction;
                }
            }

            index = -1;
            return default;
        }

        protected static TInteraction GetFromPool()
        {
            return _pool.PopOrCreate();
        }

        // PRIVATE METHODS

        private static void ReturnToPool(TInteraction interaction)
        {
            interaction.Deinitialize();
            interaction.NetworkID = default;
            interaction.NetworkObject = default;
            interaction.Provider = default;
            _pool.Push(interaction);
        }
    }
}