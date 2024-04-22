using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.KCC
{
	/// <summary>
	///     Data structure representing single collider/trigger overlap (radius + extent). Read-only, managed entirely by
	///     <c>KCC</c>.
	/// </summary>
	public sealed class KCCHit
    {
        // PUBLIC MEMBERS

        /// <summary>Reference to collider/trigger component.</summary>
        public Collider Collider;

        /// <summary>
        ///     Collision type, valid only for penetrating collisions.
        ///     Non-penetrating collisions within (radius + extent) have ECollisionType.None.
        /// </summary>
        public ECollisionType CollisionType;

        /// <summary>Reference to collider transform component.</summary>
        public Transform Transform;
    }

	/// <summary>
	///     Collection dedicated to tracking all colliders/triggers the KCC collides with (radius + extent). Managed entirely
	///     by <c>KCC</c>.
	/// </summary>
	public sealed class KCCHits
    {
        // PRIVATE MEMBERS

        private static readonly KCCFastStack<KCCHit> _pool = new(256, true);
        // PUBLIC MEMBERS

        public readonly List<KCCHit> All = new();

        public int Count => All.Count;

        // PUBLIC METHODS

        public bool HasCollider(Collider collider)
        {
            return Find(collider, out var index) != null;
        }

        public KCCHit Add(KCCOverlapHit overlapHit)
        {
            var hit = _pool.PopOrCreate();
            hit.Collider = overlapHit.Collider;
            hit.Transform = overlapHit.Transform;
            hit.CollisionType = overlapHit.CollisionType;

            All.Add(hit);

            return hit;
        }

        public void CopyFromOther(KCCHits other)
        {
            KCCHit thisHit;
            KCCHit otherHit;
            var thisCount = All.Count;
            var otherCount = other.All.Count;

            if (thisCount == otherCount)
            {
                if (thisCount == 0)
                    return;

                for (var i = 0; i < thisCount; ++i)
                {
                    thisHit = All[i];
                    otherHit = other.All[i];

                    thisHit.Collider = otherHit.Collider;
                    thisHit.Transform = otherHit.Transform;
                    thisHit.CollisionType = otherHit.CollisionType;
                }
            }
            else
            {
                Clear();

                for (var i = 0; i < otherCount; ++i)
                {
                    thisHit = _pool.PopOrCreate();
                    otherHit = other.All[i];

                    thisHit.Collider = otherHit.Collider;
                    thisHit.Transform = otherHit.Transform;
                    thisHit.CollisionType = otherHit.CollisionType;

                    All.Add(thisHit);
                }
            }
        }

        public void Clear()
        {
            for (int i = 0, count = All.Count; i < count; ++i)
            {
                var hit = All[i];
                hit.Collider = default;
                hit.Transform = default;
                hit.CollisionType = default;
                _pool.Push(hit);
            }

            All.Clear();
        }

        // PRIVATE METHODS

        private KCCHit Find(Collider collider, out int index)
        {
            for (int i = 0, count = All.Count; i < count; ++i)
            {
                var hit = All[i];
                if (ReferenceEquals(hit.Collider, collider))
                {
                    index = i;
                    return hit;
                }
            }

            index = -1;
            return default;
        }
    }
}