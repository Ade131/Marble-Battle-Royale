using UnityEngine;

namespace Fusion.Addons.KCC
{
    public sealed class KCCOverlapInfo
    {
        public int AllHitCount;
        public KCCOverlapHit[] AllHits;
        public int ColliderHitCount;
        public KCCOverlapHit[] ColliderHits;
        public float Extent;
        public float Height;

        public LayerMask LayerMask;
        // PUBLIC MEMBERS

        public Vector3 Position;
        public float Radius;
        public int TriggerHitCount;
        public KCCOverlapHit[] TriggerHits;
        public QueryTriggerInteraction TriggerInteraction;

        // CONSTRUCTORS

        public KCCOverlapInfo() : this(KCC.CACHE_SIZE)
        {
        }

        public KCCOverlapInfo(int maxHits)
        {
            AllHits = new KCCOverlapHit[maxHits];
            TriggerHits = new KCCOverlapHit[maxHits];
            ColliderHits = new KCCOverlapHit[maxHits];

            for (var i = 0; i < maxHits; ++i) AllHits[i] = new KCCOverlapHit();
        }

        // PUBLIC METHODS

        public void AddHit(Collider collider)
        {
            if (AllHitCount == AllHits.Length)
                return;

            var hit = AllHits[AllHitCount];
            if (hit.Set(collider))
            {
                ++AllHitCount;

                if (hit.IsTrigger)
                {
                    TriggerHits[TriggerHitCount] = hit;
                    ++TriggerHitCount;
                }
                else
                {
                    ColliderHits[ColliderHitCount] = hit;
                    ++ColliderHitCount;
                }
            }
        }

        public void ToggleConvexMeshColliders(bool convex)
        {
            KCCOverlapHit hit;

            for (var i = 0; i < ColliderHitCount; ++i)
            {
                hit = ColliderHits[i];

                if (hit.Type == EColliderType.Mesh && hit.IsConvertible) ((MeshCollider)hit.Collider).convex = convex;
            }
        }

        public bool AllHitsWithinExtent()
        {
            var hits = AllHits;
            for (int i = 0, count = AllHitCount; i < count; ++i)
                if (AllHits[i].IsWithinExtent == false)
                    return false;

            return true;
        }

        public void Reset(bool deep)
        {
            Position = default;
            Radius = default;
            Height = default;
            Extent = default;
            LayerMask = default;
            TriggerInteraction = QueryTriggerInteraction.Collide;
            AllHitCount = default;
            TriggerHitCount = default;
            ColliderHitCount = default;

            if (deep)
                for (int i = 0, count = AllHits.Length; i < count; ++i)
                    AllHits[i].Reset();
        }

        public void CopyFromOther(KCCOverlapInfo other)
        {
            Position = other.Position;
            Radius = other.Radius;
            Height = other.Height;
            Extent = other.Extent;
            LayerMask = other.LayerMask;
            TriggerInteraction = other.TriggerInteraction;
            AllHitCount = other.AllHitCount;
            TriggerHitCount = default;
            ColliderHitCount = default;

            KCCOverlapHit hit;

            for (var i = 0; i < AllHitCount; ++i)
            {
                hit = AllHits[i];

                hit.CopyFromOther(other.AllHits[i]);

                if (hit.IsTrigger)
                {
                    TriggerHits[TriggerHitCount] = hit;
                    ++TriggerHitCount;
                }
                else
                {
                    ColliderHits[ColliderHitCount] = hit;
                    ++ColliderHitCount;
                }
            }
        }

        public void DumpHits(KCC kcc)
        {
            if (AllHitCount <= 0)
                return;

            kcc.Log($"Overlap Hits ({AllHitCount})");

            var hits = AllHits;
            for (int i = 0, count = AllHitCount; i < count; ++i)
            {
                var hit = AllHits[i];
                kcc.Log(
                    $"Collider: {hit.Collider.name}, Type: {hit.Type}, IsTrigger: {hit.IsTrigger}, IsConvex: {hit.IsConvex}, IsWithinExtent: {hit.IsWithinExtent}, HasPenetration: {hit.HasPenetration}, CollisionType: {hit.CollisionType}");
            }
        }
    }
}