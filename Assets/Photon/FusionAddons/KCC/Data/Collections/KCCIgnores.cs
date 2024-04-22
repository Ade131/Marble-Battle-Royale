using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.KCC
{
	/// <summary>
	///     Data structure representing single collider/trigger ignore entry. Read-only, managed entirely by <c>KCC</c>.
	/// </summary>
	public sealed class KCCIgnore
    {
        public Collider Collider;
        public KCCNetworkID NetworkID;
        public NetworkObject NetworkObject;

        public void CopyFromOther(KCCIgnore other)
        {
            NetworkID = other.NetworkID;
            NetworkObject = other.NetworkObject;
            Collider = other.Collider;
        }

        public void Clear()
        {
            NetworkID = default;
            NetworkObject = default;
            Collider = default;
        }
    }

	/// <summary>
	///     Collection dedicated to tracking ignored colliders. Managed entirely by <c>KCC</c> component.
	/// </summary>
	public sealed class KCCIgnores
    {
        // PRIVATE MEMBERS

        private static readonly KCCFastStack<KCCIgnore> _pool = new(128, true);
        // PUBLIC MEMBERS

        public readonly List<KCCIgnore> All = new();

        public int Count => All.Count;

        // PUBLIC METHODS

        public bool HasCollider(Collider collider)
        {
            return Find(collider, out var index) != null;
        }

        public KCCIgnore Add(NetworkObject networkObject, Collider collider, bool checkExisting)
        {
            var ignore = checkExisting ? Find(collider, out var index) : null;
            if (ignore == null)
            {
                ignore = _pool.PopOrCreate();

                ignore.NetworkID = KCCNetworkID.GetNetworkID(networkObject);
                ignore.NetworkObject = networkObject;
                ignore.Collider = collider;

                All.Add(ignore);
            }

            return ignore;
        }

        public bool Add(NetworkObject networkObject, KCCNetworkID networkID)
        {
            if (networkObject == null)
                return false;

            var ignore = _pool.PopOrCreate();
            ignore.NetworkID = networkID;
            ignore.NetworkObject = networkObject;
            ignore.Collider = networkObject.GetComponentNoAlloc<Collider>();

            All.Add(ignore);

            return true;
        }

        public bool Remove(Collider collider)
        {
            var ignore = Find(collider, out var index);
            if (ignore != null)
            {
                All.RemoveAt(index);
                ignore.Clear();
                _pool.Push(ignore);
                return true;
            }

            return false;
        }

        public void CopyFromOther(KCCIgnores other)
        {
            var thisCount = All.Count;
            var otherCount = other.All.Count;

            if (thisCount == otherCount)
            {
                if (thisCount == 0)
                    return;

                for (var i = 0; i < thisCount; ++i) All[i].CopyFromOther(other.All[i]);
            }
            else
            {
                Clear();

                for (var i = 0; i < otherCount; ++i)
                {
                    var ignore = _pool.PopOrCreate();
                    ignore.CopyFromOther(other.All[i]);
                    All.Add(ignore);
                }
            }
        }

        public void Clear()
        {
            for (int i = 0, count = All.Count; i < count; ++i)
            {
                var ignore = All[i];
                ignore.Clear();
                _pool.Push(ignore);
            }

            All.Clear();
        }

        // PRIVATE METHODS

        private KCCIgnore Find(Collider collider, out int index)
        {
            for (int i = 0, count = All.Count; i < count; ++i)
            {
                var ignore = All[i];
                if (ReferenceEquals(ignore.Collider, collider))
                {
                    index = i;
                    return ignore;
                }
            }

            index = -1;
            return default;
        }
    }
}