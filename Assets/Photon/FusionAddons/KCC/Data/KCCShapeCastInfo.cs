using UnityEngine;

namespace Fusion.Addons.KCC
{
    public sealed class KCCShapeCastInfo
    {
        // PRIVATE MEMBERS

        private static readonly float[] _sortDistances = new float[KCC.CACHE_SIZE];
        public int AllHitCount;
        public KCCShapeCastHit[] AllHits;
        public int ColliderHitCount;
        public KCCShapeCastHit[] ColliderHits;
        public Vector3 Direction;
        public float Extent;
        public float Height;
        public LayerMask LayerMask;

        public float MaxDistance;
        // PUBLIC MEMBERS

        public Vector3 Position;
        public float Radius;
        public int TriggerHitCount;
        public KCCShapeCastHit[] TriggerHits;
        public QueryTriggerInteraction TriggerInteraction;

        // CONSTRUCTORS

        public KCCShapeCastInfo() : this(KCC.CACHE_SIZE)
        {
        }

        public KCCShapeCastInfo(int maxHits)
        {
            AllHits = new KCCShapeCastHit[maxHits];
            TriggerHits = new KCCShapeCastHit[maxHits];
            ColliderHits = new KCCShapeCastHit[maxHits];

            for (var i = 0; i < maxHits; ++i) AllHits[i] = new KCCShapeCastHit();
        }

        // PUBLIC METHODS

        public void AddHit(RaycastHit raycastHit)
        {
            if (AllHitCount == AllHits.Length)
                return;

            var hit = AllHits[AllHitCount];
            if (hit.Set(raycastHit))
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

        public void Sort()
        {
            var count = AllHitCount;
            if (count <= 1)
                return;

            var isSorted = false;
            var hasChanged = false;
            var allHits = AllHits;
            var distances = _sortDistances;
            int leftIndex;
            int rightIndex;
            float leftDistance;
            float rightDistance;
            KCCShapeCastHit leftHit;
            KCCShapeCastHit rightHit;

            for (var i = 0; i < count; ++i) distances[i] = allHits[i].RaycastHit.distance;

            while (isSorted == false)
            {
                isSorted = true;

                leftIndex = 0;
                rightIndex = 1;
                leftDistance = distances[leftIndex];

                while (rightIndex < count)
                {
                    rightDistance = distances[rightIndex];

                    if (leftDistance <= rightDistance)
                    {
                        leftDistance = rightDistance;
                    }
                    else
                    {
                        distances[leftIndex] = rightDistance;
                        distances[rightIndex] = leftDistance;

                        leftHit = allHits[leftIndex];
                        rightHit = allHits[rightIndex];

                        allHits[leftIndex] = rightHit;
                        allHits[rightIndex] = leftHit;

                        isSorted = false;
                        hasChanged = true;
                    }

                    ++leftIndex;
                    ++rightIndex;
                }
            }

            if (hasChanged)
            {
                TriggerHitCount = 0;
                ColliderHitCount = 0;

                KCCShapeCastHit hit;

                for (var i = 0; i < count; ++i)
                {
                    hit = allHits[i];

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
        }

        public void Reset(bool deep)
        {
            Position = default;
            Radius = default;
            Height = default;
            Extent = default;
            Direction = default;
            MaxDistance = default;
            Radius = default;
            LayerMask = default;
            TriggerInteraction = QueryTriggerInteraction.Collide;
            AllHitCount = default;
            ColliderHitCount = default;
            TriggerHitCount = default;

            if (deep)
                for (int i = 0, count = AllHits.Length; i < count; ++i)
                    AllHits[i].Reset();
        }

        public void DumpHits(KCC kcc)
        {
            if (AllHitCount <= 0)
                return;

            kcc.Log($"ShapeCast Hits ({AllHitCount})");

            var hits = AllHits;
            for (int i = 0, count = AllHitCount; i < count; ++i)
            {
                var hit = AllHits[i];
                kcc.Log(
                    $"Collider: {hit.Collider.name}, Type: {hit.Type}, IsTrigger: {hit.IsTrigger}, Distance: {hit.RaycastHit.distance}");
            }
        }
    }
}