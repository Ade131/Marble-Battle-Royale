using System;
using UnityEngine;

namespace Fusion.Addons.KCC
{
    // This file contains implementation related to physics.
    public partial class KCC
    {
        // PUBLIC METHODS

        /// <summary>
        ///     Sphere overlap using same filtering as for KCC physics query.
        /// </summary>
        /// <param name="overlapInfo">Contains results of the overlap.</param>
        /// <param name="position">Center position of the sphere.</param>
        /// <param name="radius">Radius of the sphere.</param>
        /// <param name="triggerInteraction">Use to enable/disable trigger hits.</param>
        public bool SphereOverlap(KCCOverlapInfo overlapInfo, Vector3 position, float radius,
            QueryTriggerInteraction triggerInteraction)
        {
            return SphereOverlap(overlapInfo, Data, position, radius, default, _settings.CollisionLayerMask,
                triggerInteraction);
        }

        /// <summary>
        ///     Capsule overlap using same filtering as for KCC physics query.
        /// </summary>
        /// <param name="overlapInfo">Contains results of the overlap.</param>
        /// <param name="position">Bottom position of the capsule.</param>
        /// <param name="radius">Radius of the capsule.</param>
        /// <param name="height">Height of the capsule.</param>
        /// <param name="triggerInteraction">Use to enable/disable trigger hits.</param>
        public bool CapsuleOverlap(KCCOverlapInfo overlapInfo, Vector3 position, float radius, float height,
            QueryTriggerInteraction triggerInteraction)
        {
            return CapsuleOverlap(overlapInfo, Data, position, radius, height, default, _settings.CollisionLayerMask,
                triggerInteraction);
        }

        /// <summary>
        ///     Ray cast using same filtering as for KCC physics query.
        /// </summary>
        /// <param name="shapeCastInfo">Contains results of the cast sorted by distance.</param>
        /// <param name="position">Origin position of the cast.</param>
        /// <param name="direction">Direction of the cast.</param>
        /// <param name="maxDistance">Distance of the cast.</param>
        /// <param name="triggerInteraction">Use to enable/disable trigger hits.</param>
        public bool RayCast(KCCShapeCastInfo shapeCastInfo, Vector3 position, Vector3 direction, float maxDistance,
            QueryTriggerInteraction triggerInteraction)
        {
            return RayCast(shapeCastInfo, Data, position, direction, maxDistance, _settings.CollisionLayerMask,
                triggerInteraction);
        }

        /// <summary>
        ///     Sphere cast using same filtering as for KCC physics query.
        /// </summary>
        /// <param name="shapeCastInfo">Contains results of the cast sorted by distance.</param>
        /// <param name="position">Center position of the sphere.</param>
        /// <param name="radius">Radius of the sphere.</param>
        /// <param name="direction">Direction of the cast.</param>
        /// <param name="maxDistance">Distance of the cast.</param>
        /// <param name="triggerInteraction">Use to enable/disable trigger hits.</param>
        /// <param name="trackInitialOverlaps">Set to true for the result to contain initially overlapping colliders.</param>
        public bool SphereCast(KCCShapeCastInfo shapeCastInfo, Vector3 position, float radius, Vector3 direction,
            float maxDistance, QueryTriggerInteraction triggerInteraction, bool trackInitialOverlaps = true)
        {
            return SphereCast(shapeCastInfo, Data, position, radius, default, direction, maxDistance,
                _settings.CollisionLayerMask, triggerInteraction, trackInitialOverlaps);
        }

        /// <summary>
        ///     Capsule cast using same filtering as for KCC physics query.
        /// </summary>
        /// <param name="shapeCastInfo">Contains results of the cast sorted by distance.</param>
        /// <param name="position">Bottom position of the capsule.</param>
        /// <param name="radius">Radius of the capsule.</param>
        /// <param name="height">Height of the capsule.</param>
        /// <param name="direction">Direction of the cast.</param>
        /// <param name="maxDistance">Distance of the cast.</param>
        /// <param name="triggerInteraction">Use to enable/disable trigger hits.</param>
        /// <param name="trackInitialOverlaps">Set to true for the result to contain initially overlapping colliders.</param>
        public bool CapsuleCast(KCCShapeCastInfo shapeCastInfo, Vector3 position, float radius, float height,
            Vector3 direction, float maxDistance, QueryTriggerInteraction triggerInteraction,
            bool trackInitialOverlaps = true)
        {
            return CapsuleCast(shapeCastInfo, Data, position, radius, height, default, direction, maxDistance,
                _settings.CollisionLayerMask, triggerInteraction, trackInitialOverlaps);
        }

        /// <summary>
        ///     Force refresh KCCData.Hits based on current position. If an existing overlap info is provided, the method takes as
        ///     much information as possible from it.
        ///     Metadata (collision type, penetration, ...) for other (new) hits will be missing. Use with caution.
        /// </summary>
        /// <param name="baseOverlapInfo">
        ///     Base overlap query results. Only matching colliders metadata is taken from this info if
        ///     new overlap query is executed.
        /// </param>
        /// <param name="overlapQuery">
        ///     Controls reuse of base query results / execution of a new overlap query.
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 Default - Hits from base overlap query will be reused only if all colliders are within extent,
        ///                 otherwise new overlap query will be executed.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>Reuse - Force reuse hits from base overlap query, even if colliders are not within extent.</description>
        ///         </item>
        ///         <item>
        ///             <description>New - Force execute new overlap query.</description>
        ///         </item>
        ///     </list>
        /// </param>
        public void UpdateHits(KCCOverlapInfo baseOverlapInfo, EKCCHitsOverlapQuery overlapQuery)
        {
            UpdateHits(Data, baseOverlapInfo, overlapQuery);
        }

        /// <summary>
        ///     Check if the <c>KCC</c> potentially collides with a collider, using same filtering as physics query.
        ///     Returning <c>true</c> doesn't mean the collider overlaps. Can be used as a filter after custom overlap/shapecast
        ///     query.
        /// </summary>
        /// <param name="hitCollider">Collider instance.</param>
        public bool IsValidHitCollider(Collider hitCollider)
        {
            if (hitCollider == null)
                return false;

            return IsValidHitCollider(Data, hitCollider);
        }

        // PRIVATE METHODS

        private bool SphereOverlap(KCCOverlapInfo overlapInfo, KCCData data, Vector3 position, float radius,
            float extent, LayerMask layerMask, QueryTriggerInteraction triggerInteraction)
        {
            overlapInfo.Reset(false);

            overlapInfo.Position = position;
            overlapInfo.Radius = radius;
            overlapInfo.Height = 0.0f;
            overlapInfo.Extent = extent;
            overlapInfo.LayerMask = layerMask;
            overlapInfo.TriggerInteraction = triggerInteraction;

            Collider hitCollider;
            var hitColliders = _hitColliders;
            var hitColliderCount = Runner.GetPhysicsScene()
                .OverlapSphere(position, radius + extent, hitColliders, layerMask, triggerInteraction);

            for (var i = 0; i < hitColliderCount; ++i)
            {
                hitCollider = hitColliders[i];

                if (IsValidHitColliderUnsafe(data, hitCollider)) overlapInfo.AddHit(hitCollider);
            }

            return overlapInfo.AllHitCount > 0;
        }

        private bool CapsuleOverlap(KCCOverlapInfo overlapInfo, KCCData data, Vector3 position, float radius,
            float height, float extent, LayerMask layerMask, QueryTriggerInteraction triggerInteraction)
        {
            overlapInfo.Reset(false);

            overlapInfo.Position = position;
            overlapInfo.Radius = radius;
            overlapInfo.Height = height;
            overlapInfo.Extent = extent;
            overlapInfo.LayerMask = layerMask;
            overlapInfo.TriggerInteraction = triggerInteraction;

            var positionUp = position + new Vector3(0.0f, height - radius, 0.0f);
            var positionDown = position + new Vector3(0.0f, radius, 0.0f);

            Collider hitCollider;
            var hitColliders = _hitColliders;
            var hitColliderCount = Runner.GetPhysicsScene().OverlapCapsule(positionDown, positionUp, radius + extent,
                hitColliders, layerMask, triggerInteraction);

            for (var i = 0; i < hitColliderCount; ++i)
            {
                hitCollider = hitColliders[i];

                if (IsValidHitColliderUnsafe(data, hitCollider)) overlapInfo.AddHit(hitCollider);
            }

            return overlapInfo.AllHitCount > 0;
        }

        private bool RayCast(KCCShapeCastInfo shapeCastInfo, KCCData data, Vector3 position, Vector3 direction,
            float maxDistance, LayerMask layerMask, QueryTriggerInteraction triggerInteraction)
        {
            shapeCastInfo.Reset(false);

            shapeCastInfo.Position = position;
            shapeCastInfo.Direction = direction;
            shapeCastInfo.MaxDistance = maxDistance;
            shapeCastInfo.LayerMask = layerMask;
            shapeCastInfo.TriggerInteraction = triggerInteraction;

            RaycastHit raycastHit;
            var raycastHits = _raycastHits;
            var raycastHitCount = Runner.GetPhysicsScene().Raycast(position, direction, raycastHits, maxDistance,
                layerMask, triggerInteraction);

            for (var i = 0; i < raycastHitCount; ++i)
            {
                raycastHit = raycastHits[i];

                if (IsValidHitColliderUnsafe(data, raycastHit.collider)) shapeCastInfo.AddHit(raycastHit);
            }

            shapeCastInfo.Sort();

            return shapeCastInfo.AllHitCount > 0;
        }

        private bool SphereCast(KCCShapeCastInfo shapeCastInfo, KCCData data, Vector3 position, float radius,
            float extent, Vector3 direction, float maxDistance, LayerMask layerMask,
            QueryTriggerInteraction triggerInteraction, bool trackInitialOverlaps)
        {
            shapeCastInfo.Reset(false);

            shapeCastInfo.Position = position;
            shapeCastInfo.Radius = radius;
            shapeCastInfo.Extent = extent;
            shapeCastInfo.Direction = direction;
            shapeCastInfo.MaxDistance = maxDistance;
            shapeCastInfo.LayerMask = layerMask;
            shapeCastInfo.TriggerInteraction = triggerInteraction;

            RaycastHit raycastHit;
            var raycastHits = _raycastHits;
            var raycastHitCount = Runner.GetPhysicsScene().SphereCast(position, radius + extent, direction, raycastHits,
                maxDistance, layerMask, triggerInteraction);

            for (var i = 0; i < raycastHitCount; ++i)
            {
                raycastHit = raycastHits[i];

                if (trackInitialOverlaps == false && raycastHit.distance <= 0.0f && raycastHit.point.Equals(default))
                    continue;

                if (IsValidHitColliderUnsafe(data, raycastHit.collider)) shapeCastInfo.AddHit(raycastHit);
            }

            shapeCastInfo.Sort();

            return shapeCastInfo.AllHitCount > 0;
        }

        private bool CapsuleCast(KCCShapeCastInfo shapeCastInfo, KCCData data, Vector3 position, float radius,
            float height, float extent, Vector3 direction, float maxDistance, LayerMask layerMask,
            QueryTriggerInteraction triggerInteraction, bool trackInitialOverlaps)
        {
            shapeCastInfo.Reset(false);

            shapeCastInfo.Position = position;
            shapeCastInfo.Radius = radius;
            shapeCastInfo.Height = height;
            shapeCastInfo.Extent = extent;
            shapeCastInfo.Position = position;
            shapeCastInfo.Direction = direction;
            shapeCastInfo.MaxDistance = maxDistance;
            shapeCastInfo.LayerMask = layerMask;
            shapeCastInfo.TriggerInteraction = triggerInteraction;

            var positionUp = position + new Vector3(0.0f, height - radius, 0.0f);
            var positionDown = position + new Vector3(0.0f, radius, 0.0f);

            RaycastHit raycastHit;
            var raycastHits = _raycastHits;
            var raycastHitCount = Runner.GetPhysicsScene().CapsuleCast(positionDown, positionUp, radius + extent,
                direction, raycastHits, maxDistance, layerMask, triggerInteraction);

            for (var i = 0; i < raycastHitCount; ++i)
            {
                raycastHit = raycastHits[i];

                if (trackInitialOverlaps == false && raycastHit.distance <= 0.0f && raycastHit.point.Equals(default))
                    continue;

                if (IsValidHitColliderUnsafe(data, raycastHit.collider)) shapeCastInfo.AddHit(raycastHit);
            }

            shapeCastInfo.Sort();

            return shapeCastInfo.AllHitCount > 0;
        }

        private void UpdateHits(KCCData data, KCCOverlapInfo baseOverlapInfo, EKCCHitsOverlapQuery overlapQuery)
        {
            bool reuseOverlapInfo;

            switch (overlapQuery)
            {
                case EKCCHitsOverlapQuery.Default:
                {
                    reuseOverlapInfo = baseOverlapInfo != null && baseOverlapInfo.AllHitsWithinExtent();
                    break;
                }
                case EKCCHitsOverlapQuery.Reuse:
                {
                    reuseOverlapInfo = baseOverlapInfo != null;
                    break;
                }
                case EKCCHitsOverlapQuery.New:
                {
                    reuseOverlapInfo = false;
                    break;
                }
                default:
                    throw new NotImplementedException(nameof(overlapQuery));
            }

            if (reuseOverlapInfo)
            {
                _trackOverlapInfo.CopyFromOther(baseOverlapInfo);
            }
            else
            {
                CapsuleOverlap(_trackOverlapInfo, data, data.TargetPosition, _settings.Radius, _settings.Height,
                    _settings.Extent, _settings.CollisionLayerMask, QueryTriggerInteraction.Collide);

                if (baseOverlapInfo != null)
                    for (var i = 0; i < _trackOverlapInfo.AllHitCount; ++i)
                    {
                        var trackedHit = _trackOverlapInfo.AllHits[i];

                        for (var j = 0; j < baseOverlapInfo.AllHitCount; ++j)
                        {
                            var extendedHit = baseOverlapInfo.AllHits[j];
                            if (ReferenceEquals(trackedHit.Collider, extendedHit.Collider))
                                trackedHit.CopyFromOther(extendedHit);
                        }
                    }
            }

            data.Hits.Clear();

            for (int i = 0, count = _trackOverlapInfo.AllHitCount; i < count; ++i)
                data.Hits.Add(_trackOverlapInfo.AllHits[i]);
        }

        private void ForceRemoveAllHits(KCCData data)
        {
            _trackOverlapInfo.Reset(false);
            _extendedOverlapInfo.Reset(false);

            data.Hits.Clear();
        }

        private bool IsValidHitCollider(KCCData data, Collider hitCollider)
        {
            if (hitCollider == _collider.Collider)
                return false;

            for (int i = 0, count = _childColliders.Count; i < count; ++i)
                if (hitCollider == _childColliders[i])
                    return false;

            var colliderLayerMask = 1 << hitCollider.gameObject.layer;
            if ((_settings.CollisionLayerMask & colliderLayerMask) != colliderLayerMask)
                return false;

            var ignores = data.Ignores.All;
            for (int i = 0, count = ignores.Count; i < count; ++i)
                if (hitCollider == ignores[i].Collider)
                    return false;

            if (ResolveCollision != null)
                try
                {
                    return ResolveCollision(this, hitCollider);
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                }

            return true;
        }

        private bool IsValidHitColliderUnsafe(KCCData data, Collider overlapCollider)
        {
            if (ReferenceEquals(overlapCollider, _collider.Collider))
                return false;

            for (int i = 0, count = _childColliders.Count; i < count; ++i)
                if (ReferenceEquals(overlapCollider, _childColliders[i]))
                    return false;

            var ignores = data.Ignores.All;
            for (int i = 0, count = ignores.Count; i < count; ++i)
                if (ReferenceEquals(overlapCollider, ignores[i].Collider))
                    return false;

            if (ResolveCollision != null)
                try
                {
                    return ResolveCollision(this, overlapCollider);
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                }

            return true;
        }
    }
}