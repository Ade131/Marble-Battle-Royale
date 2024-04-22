using UnityEngine;

namespace Fusion.Addons.KCC
{
    // This file contains penetration solver.
    public partial class KCC
    {
        // PUBLIC METHODS

        public Vector3 ResolvePenetration(KCCOverlapInfo overlapInfo, KCCData data, Vector3 basePosition,
            Vector3 targetPosition, bool probeGrounding, int maxSteps, int resolverIterations, bool resolveTriggers)
        {
            if (_settings.SuppressConvexMeshColliders) overlapInfo.ToggleConvexMeshColliders(false);

            if (overlapInfo.ColliderHitCount == 1)
                targetPosition = DepenetrateSingle(overlapInfo, data, basePosition, targetPosition, probeGrounding,
                    maxSteps);
            else if (overlapInfo.ColliderHitCount > 1)
                targetPosition = DepenetrateMultiple(overlapInfo, data, basePosition, targetPosition, probeGrounding,
                    maxSteps, resolverIterations);

            RecalculateGroundProperties(data);

            if (resolveTriggers)
                for (var i = 0; i < overlapInfo.TriggerHitCount; ++i)
                {
                    var hit = overlapInfo.TriggerHits[i];
                    hit.Transform.GetPositionAndRotation(out hit.CachedPosition, out hit.CachedRotation);

                    var hasPenetration = UnityEngine.Physics.ComputePenetration(_collider.Collider, data.TargetPosition,
                        Quaternion.identity, hit.Collider, hit.CachedPosition, hit.CachedRotation, out var direction,
                        out var distance);

                    hit.HasPenetration = hasPenetration;
                    hit.IsWithinExtent = hasPenetration;
                    hit.CollisionType = hasPenetration ? ECollisionType.Trigger : ECollisionType.None;

                    if (distance > hit.MaxPenetration) hit.MaxPenetration = distance;
                }

            if (_settings.SuppressConvexMeshColliders) overlapInfo.ToggleConvexMeshColliders(true);

            return targetPosition;
        }

        // PRIVATE METHODS

        private Vector3 DepenetrateSingle(KCCOverlapInfo overlapInfo, KCCData data, Vector3 basePosition,
            Vector3 targetPosition, bool probeGrounding, int maxSteps)
        {
            var minGroundDot = Mathf.Cos(Mathf.Clamp(data.MaxGroundAngle, 0.0f, 90.0f) * Mathf.Deg2Rad);
            var minWallDot = -Mathf.Cos(Mathf.Clamp(90.0f - data.MaxWallAngle, 0.0f, 90.0f) * Mathf.Deg2Rad);
            var minHangDot = -Mathf.Cos(Mathf.Clamp(90.0f - data.MaxHangAngle, 0.0f, 90.0f) * Mathf.Deg2Rad);
            var groundNormal = Vector3.up;
            float groundDistance = default;

            var hit = overlapInfo.ColliderHits[0];
            hit.UpDirectionDot = float.MinValue;
            hit.Transform.GetPositionAndRotation(out hit.CachedPosition, out hit.CachedRotation);

            if (maxSteps > 1)
            {
                var minStepDistance = 0.001f;
                var targetDistance = Vector3.Distance(basePosition, targetPosition);

                if (targetDistance < maxSteps * minStepDistance)
                    maxSteps = Mathf.Max(1, (int)(targetDistance / minStepDistance));
            }

            if (maxSteps <= 1)
            {
                hit.HasPenetration = UnityEngine.Physics.ComputePenetration(_collider.Collider, targetPosition,
                    Quaternion.identity, hit.Collider, hit.CachedPosition, hit.CachedRotation, out var direction,
                    out var distance);
                if (hit.HasPenetration)
                {
                    hit.IsWithinExtent = true;

                    if (distance > hit.MaxPenetration) hit.MaxPenetration = distance;

                    var directionUpDot = Vector3.Dot(direction, Vector3.up);
                    if (directionUpDot > hit.UpDirectionDot)
                    {
                        hit.UpDirectionDot = directionUpDot;

                        if (directionUpDot >= minGroundDot)
                        {
                            hit.CollisionType = ECollisionType.Ground;

                            data.IsGrounded = true;

                            groundNormal = direction;
                        }
                        else if (directionUpDot > -minWallDot)
                        {
                            hit.CollisionType = ECollisionType.Slope;
                        }
                        else if (directionUpDot >= minWallDot)
                        {
                            hit.CollisionType = ECollisionType.Wall;
                        }
                        else if (directionUpDot >= minHangDot)
                        {
                            hit.CollisionType = ECollisionType.Hang;
                        }
                        else
                        {
                            hit.CollisionType = ECollisionType.Top;
                        }
                    }

                    if (directionUpDot > 0.0f && directionUpDot < minGroundDot)
                        if (distance >= 0.000001f && data.DynamicVelocity.y <= 0.0f)
                        {
                            var positionDelta = targetPosition - basePosition;

                            var movementDot = Vector3.Dot(positionDelta.OnlyXZ(), direction.OnlyXZ());
                            if (movementDot < 0.0f)
                                KCCPhysicsUtility.ProjectVerticalPenetration(ref direction, ref distance);
                        }

                    targetPosition += direction * distance;
                }
            }
            else
            {
                var stepPositionDelta = (targetPosition - basePosition) / maxSteps;
                var desiredPosition = basePosition;
                var remainingSteps = maxSteps;

                while (remainingSteps > 0)
                {
                    --remainingSteps;

                    desiredPosition += stepPositionDelta;

                    hit.HasPenetration = UnityEngine.Physics.ComputePenetration(_collider.Collider, desiredPosition,
                        Quaternion.identity, hit.Collider, hit.CachedPosition, hit.CachedRotation, out var direction,
                        out var distance);
                    if (hit.HasPenetration == false)
                        continue;

                    hit.IsWithinExtent = true;

                    if (distance > hit.MaxPenetration) hit.MaxPenetration = distance;

                    var directionUpDot = Vector3.Dot(direction, Vector3.up);
                    if (directionUpDot > hit.UpDirectionDot)
                    {
                        hit.UpDirectionDot = directionUpDot;

                        if (directionUpDot >= minGroundDot)
                        {
                            hit.CollisionType = ECollisionType.Ground;

                            data.IsGrounded = true;

                            groundNormal = direction;
                        }
                        else if (directionUpDot > -minWallDot)
                        {
                            hit.CollisionType = ECollisionType.Slope;
                        }
                        else if (directionUpDot >= minWallDot)
                        {
                            hit.CollisionType = ECollisionType.Wall;
                        }
                        else if (directionUpDot >= minHangDot)
                        {
                            hit.CollisionType = ECollisionType.Hang;
                        }
                        else
                        {
                            hit.CollisionType = ECollisionType.Top;
                        }
                    }

                    if (directionUpDot > 0.0f && directionUpDot < minGroundDot)
                        if (distance >= 0.000001f && data.DynamicVelocity.y <= 0.0f)
                        {
                            var movementDot = Vector3.Dot(stepPositionDelta.OnlyXZ(), direction.OnlyXZ());
                            if (movementDot < 0.0f)
                                KCCPhysicsUtility.ProjectVerticalPenetration(ref direction, ref distance);
                        }

                    desiredPosition += direction * distance;
                }

                targetPosition = desiredPosition;
            }

            if (hit.UpDirectionDot == float.MinValue) hit.UpDirectionDot = default;

            if (probeGrounding && data.IsGrounded == false)
            {
                var isGrounded = KCCPhysicsUtility.CheckGround(_collider.Collider, targetPosition, hit.Collider,
                    hit.CachedPosition, hit.CachedRotation, _settings.Radius, _settings.Height, _settings.Extent,
                    minGroundDot, out var checkGroundNormal, out var checkGroundDistance, out var isWithinExtent);
                if (isGrounded)
                {
                    data.IsGrounded = true;

                    groundNormal = checkGroundNormal;
                    groundDistance = checkGroundDistance;

                    hit.IsWithinExtent = true;
                    hit.CollisionType = ECollisionType.Ground;
                }
                else if (isWithinExtent)
                {
                    hit.IsWithinExtent = true;

                    if (hit.CollisionType == ECollisionType.None) hit.CollisionType = ECollisionType.Slope;
                }
            }

            if (data.IsGrounded)
            {
                data.GroundNormal = groundNormal;
                data.GroundAngle = Vector3.Angle(groundNormal, Vector3.up);
                data.GroundPosition = targetPosition + new Vector3(0.0f, _settings.Radius, 0.0f) -
                                      groundNormal * (_settings.Radius + groundDistance);
                data.GroundDistance = groundDistance;
            }

            return targetPosition;
        }

        private Vector3 DepenetrateMultiple(KCCOverlapInfo overlapInfo, KCCData data, Vector3 basePosition,
            Vector3 targetPosition, bool probeGrounding, int maxSteps, int resolverIterations)
        {
            var minGroundDot = Mathf.Cos(Mathf.Clamp(data.MaxGroundAngle, 0.0f, 90.0f) * Mathf.Deg2Rad);
            var minWallDot = -Mathf.Cos(Mathf.Clamp(90.0f - data.MaxWallAngle, 0.0f, 90.0f) * Mathf.Deg2Rad);
            var minHangDot = -Mathf.Cos(Mathf.Clamp(90.0f - data.MaxHangAngle, 0.0f, 90.0f) * Mathf.Deg2Rad);
            float groundDistance = default;
            float maxGroundDot = default;
            Vector3 maxGroundNormal = default;
            Vector3 averageGroundNormal = default;
            var positionDelta = targetPosition - basePosition;
            var positionDeltaXZ = positionDelta.OnlyXZ();

            for (var i = 0; i < overlapInfo.ColliderHitCount; ++i)
            {
                var hit = overlapInfo.ColliderHits[i];
                hit.UpDirectionDot = float.MinValue;
                hit.Transform.GetPositionAndRotation(out hit.CachedPosition, out hit.CachedRotation);
            }

            if (maxSteps > 1)
            {
                var minStepDistance = 0.001f;
                var targetDistance = Vector3.Distance(basePosition, targetPosition);

                if (targetDistance < maxSteps * minStepDistance)
                    maxSteps = Mathf.Max(1, (int)(targetDistance / minStepDistance));
            }

            if (maxSteps <= 1)
            {
                _resolver.Reset();

                for (var i = 0; i < overlapInfo.ColliderHitCount; ++i)
                {
                    var hit = overlapInfo.ColliderHits[i];

                    hit.HasPenetration = UnityEngine.Physics.ComputePenetration(_collider.Collider, targetPosition,
                        Quaternion.identity, hit.Collider, hit.CachedPosition, hit.CachedRotation, out var direction,
                        out var distance);
                    if (hit.HasPenetration == false)
                        continue;

                    hit.IsWithinExtent = true;

                    if (distance > hit.MaxPenetration) hit.MaxPenetration = distance;

                    var directionUpDot = Vector3.Dot(direction, Vector3.up);
                    if (directionUpDot > hit.UpDirectionDot)
                    {
                        hit.UpDirectionDot = directionUpDot;

                        if (directionUpDot >= minGroundDot)
                        {
                            hit.CollisionType = ECollisionType.Ground;

                            data.IsGrounded = true;

                            if (directionUpDot >= maxGroundDot)
                            {
                                maxGroundDot = directionUpDot;
                                maxGroundNormal = direction;
                            }

                            averageGroundNormal += direction * directionUpDot;
                        }
                        else if (directionUpDot > -minWallDot)
                        {
                            hit.CollisionType = ECollisionType.Slope;
                        }
                        else if (directionUpDot >= minWallDot)
                        {
                            hit.CollisionType = ECollisionType.Wall;
                        }
                        else if (directionUpDot >= minHangDot)
                        {
                            hit.CollisionType = ECollisionType.Hang;
                        }
                        else
                        {
                            hit.CollisionType = ECollisionType.Top;
                        }
                    }

                    if (directionUpDot > 0.0f && directionUpDot < minGroundDot)
                        if (distance >= 0.000001f && data.DynamicVelocity.y <= 0.0f)
                        {
                            var movementDot = Vector3.Dot(positionDeltaXZ, direction.OnlyXZ());
                            if (movementDot < 0.0f)
                                KCCPhysicsUtility.ProjectVerticalPenetration(ref direction, ref distance);
                        }

                    _resolver.AddCorrection(direction, distance);
                }

                var remainingSubSteps = Mathf.Max(0, resolverIterations);

                var multiplier = 1.0f - Mathf.Min(remainingSubSteps, 2) * 0.25f;

                if (_resolver.Size == 2)
                {
                    _resolver.GetCorrection(0, out var direction0);
                    _resolver.GetCorrection(1, out var direction1);

                    if (Vector3.Dot(direction0, direction1) >= 0.0f)
                        targetPosition += _resolver.CalculateMinMax() * multiplier;
                    else
                        targetPosition += _resolver.CalculateBinary() * multiplier;
                }
                else
                {
                    targetPosition += _resolver.CalculateGradientDescent(12, 0.0001f) * multiplier;
                }

                while (remainingSubSteps > 0)
                {
                    --remainingSubSteps;

                    _resolver.Reset();

                    for (var i = 0; i < overlapInfo.ColliderHitCount; ++i)
                    {
                        var hit = overlapInfo.ColliderHits[i];

                        var hasPenetration = UnityEngine.Physics.ComputePenetration(_collider.Collider, targetPosition,
                            Quaternion.identity, hit.Collider, hit.CachedPosition, hit.CachedRotation,
                            out var direction, out var distance);
                        if (hasPenetration == false)
                            continue;

                        hit.IsWithinExtent = true;
                        hit.HasPenetration = true;

                        if (distance > hit.MaxPenetration) hit.MaxPenetration = distance;

                        var directionUpDot = Vector3.Dot(direction, Vector3.up);
                        if (directionUpDot > hit.UpDirectionDot)
                        {
                            hit.UpDirectionDot = directionUpDot;

                            if (directionUpDot >= minGroundDot)
                            {
                                hit.CollisionType = ECollisionType.Ground;

                                data.IsGrounded = true;

                                if (directionUpDot >= maxGroundDot)
                                {
                                    maxGroundDot = directionUpDot;
                                    maxGroundNormal = direction;
                                }

                                averageGroundNormal += direction * directionUpDot;
                            }
                            else if (directionUpDot > -minWallDot)
                            {
                                hit.CollisionType = ECollisionType.Slope;
                            }
                            else if (directionUpDot >= minWallDot)
                            {
                                hit.CollisionType = ECollisionType.Wall;
                            }
                            else if (directionUpDot >= minHangDot)
                            {
                                hit.CollisionType = ECollisionType.Hang;
                            }
                            else
                            {
                                hit.CollisionType = ECollisionType.Top;
                            }
                        }

                        if (directionUpDot > 0.0f && directionUpDot < minGroundDot)
                            if (distance >= 0.000001f && data.DynamicVelocity.y <= 0.0f)
                            {
                                var movementDot = Vector3.Dot(positionDeltaXZ, direction.OnlyXZ());
                                if (movementDot < 0.0f)
                                    KCCPhysicsUtility.ProjectVerticalPenetration(ref direction, ref distance);
                            }

                        _resolver.AddCorrection(direction, distance);
                    }

                    if (_resolver.Size == 0)
                        break;

                    if (remainingSubSteps == 0)
                    {
                        if (_resolver.Size == 2)
                        {
                            _resolver.GetCorrection(0, out var direction0);
                            _resolver.GetCorrection(1, out var direction1);

                            if (Vector3.Dot(direction0, direction1) >= 0.0f)
                                targetPosition += _resolver.CalculateGradientDescent(12, 0.0001f);
                            else
                                targetPosition += _resolver.CalculateBinary();
                        }
                        else
                        {
                            targetPosition += _resolver.CalculateGradientDescent(12, 0.0001f);
                        }
                    }
                    else if (remainingSubSteps == 1)
                    {
                        targetPosition += _resolver.CalculateMinMax() * 0.75f;
                    }
                    else
                    {
                        targetPosition += _resolver.CalculateMinMax() * 0.5f;
                    }
                }
            }
            else
            {
                var stepPositionDelta = (targetPosition - basePosition) / maxSteps;
                var desiredPosition = basePosition;
                var remainingSteps = maxSteps;

                while (remainingSteps > 1)
                {
                    --remainingSteps;

                    desiredPosition += stepPositionDelta;

                    _resolver.Reset();

                    for (var i = 0; i < overlapInfo.ColliderHitCount; ++i)
                    {
                        var hit = overlapInfo.ColliderHits[i];

                        hit.HasPenetration = UnityEngine.Physics.ComputePenetration(_collider.Collider, desiredPosition,
                            Quaternion.identity, hit.Collider, hit.CachedPosition, hit.CachedRotation,
                            out var direction, out var distance);
                        if (hit.HasPenetration == false)
                            continue;

                        hit.IsWithinExtent = true;

                        if (distance > hit.MaxPenetration) hit.MaxPenetration = distance;

                        var directionUpDot = Vector3.Dot(direction, Vector3.up);
                        if (directionUpDot > hit.UpDirectionDot)
                        {
                            hit.UpDirectionDot = directionUpDot;

                            if (directionUpDot >= minGroundDot)
                            {
                                hit.CollisionType = ECollisionType.Ground;

                                data.IsGrounded = true;

                                if (directionUpDot >= maxGroundDot)
                                {
                                    maxGroundDot = directionUpDot;
                                    maxGroundNormal = direction;
                                }

                                averageGroundNormal += direction * directionUpDot;
                            }
                            else if (directionUpDot > -minWallDot)
                            {
                                hit.CollisionType = ECollisionType.Slope;
                            }
                            else if (directionUpDot >= minWallDot)
                            {
                                hit.CollisionType = ECollisionType.Wall;
                            }
                            else if (directionUpDot >= minHangDot)
                            {
                                hit.CollisionType = ECollisionType.Hang;
                            }
                            else
                            {
                                hit.CollisionType = ECollisionType.Top;
                            }
                        }

                        if (directionUpDot > 0.0f && directionUpDot < minGroundDot)
                            if (distance >= 0.000001f && data.DynamicVelocity.y <= 0.0f)
                            {
                                var movementDot = Vector3.Dot(stepPositionDelta.OnlyXZ(), direction.OnlyXZ());
                                if (movementDot < 0.0f)
                                    KCCPhysicsUtility.ProjectVerticalPenetration(ref direction, ref distance);
                            }

                        _resolver.AddCorrection(direction, distance);
                    }

                    if (_resolver.Size == 2)
                    {
                        _resolver.GetCorrection(0, out var direction0);
                        _resolver.GetCorrection(1, out var direction1);

                        if (Vector3.Dot(direction0, direction1) >= 0.0f)
                            desiredPosition += _resolver.CalculateMinMax();
                        else
                            desiredPosition += _resolver.CalculateBinary();
                    }
                    else
                    {
                        desiredPosition += _resolver.CalculateMinMax();
                    }
                }

                --remainingSteps;

                desiredPosition += stepPositionDelta;

                _resolver.Reset();

                for (var i = 0; i < overlapInfo.ColliderHitCount; ++i)
                {
                    var hit = overlapInfo.ColliderHits[i];

                    hit.HasPenetration = UnityEngine.Physics.ComputePenetration(_collider.Collider, desiredPosition,
                        Quaternion.identity, hit.Collider, hit.CachedPosition, hit.CachedRotation, out var direction,
                        out var distance);
                    if (hit.HasPenetration == false)
                        continue;

                    hit.IsWithinExtent = true;

                    if (distance > hit.MaxPenetration) hit.MaxPenetration = distance;

                    var directionUpDot = Vector3.Dot(direction, Vector3.up);
                    if (directionUpDot > hit.UpDirectionDot)
                    {
                        hit.UpDirectionDot = directionUpDot;

                        if (directionUpDot >= minGroundDot)
                        {
                            hit.CollisionType = ECollisionType.Ground;

                            data.IsGrounded = true;

                            if (directionUpDot >= maxGroundDot)
                            {
                                maxGroundDot = directionUpDot;
                                maxGroundNormal = direction;
                            }

                            averageGroundNormal += direction * directionUpDot;
                        }
                        else if (directionUpDot > -minWallDot)
                        {
                            hit.CollisionType = ECollisionType.Slope;
                        }
                        else if (directionUpDot >= minWallDot)
                        {
                            hit.CollisionType = ECollisionType.Wall;
                        }
                        else if (directionUpDot >= minHangDot)
                        {
                            hit.CollisionType = ECollisionType.Hang;
                        }
                        else
                        {
                            hit.CollisionType = ECollisionType.Top;
                        }
                    }

                    if (directionUpDot > 0.0f && directionUpDot < minGroundDot)
                        if (distance >= 0.000001f && data.DynamicVelocity.y <= 0.0f)
                        {
                            var movementDot = Vector3.Dot(stepPositionDelta.OnlyXZ(), direction.OnlyXZ());
                            if (movementDot < 0.0f)
                                KCCPhysicsUtility.ProjectVerticalPenetration(ref direction, ref distance);
                        }

                    _resolver.AddCorrection(direction, distance);
                }

                if (_resolver.Size == 2)
                {
                    _resolver.GetCorrection(0, out var direction0);
                    _resolver.GetCorrection(1, out var direction1);

                    if (Vector3.Dot(direction0, direction1) >= 0.0f)
                        desiredPosition += _resolver.CalculateMinMax();
                    else
                        desiredPosition += _resolver.CalculateBinary();
                }
                else
                {
                    desiredPosition += _resolver.CalculateGradientDescent(12, 0.0001f);
                }

                targetPosition = desiredPosition;
            }

            for (var i = 0; i < overlapInfo.ColliderHitCount; ++i)
            {
                var hit = overlapInfo.ColliderHits[i];
                if (hit.UpDirectionDot == float.MinValue) hit.UpDirectionDot = default;
            }

            if (probeGrounding && data.IsGrounded == false)
            {
                var closestGroundNormal = Vector3.up;
                var closestGroundDistance = 1000.0f;

                for (var i = 0; i < overlapInfo.ColliderHitCount; ++i)
                {
                    var hit = overlapInfo.ColliderHits[i];

                    var isGrounded = KCCPhysicsUtility.CheckGround(_collider.Collider, targetPosition, hit.Collider,
                        hit.CachedPosition, hit.CachedRotation, _settings.Radius, _settings.Height, _settings.Extent,
                        minGroundDot, out var checkGroundNormal, out var checkGroundDistance, out var isWithinExtent);
                    if (isGrounded)
                    {
                        data.IsGrounded = true;

                        if (checkGroundDistance < closestGroundDistance)
                        {
                            closestGroundNormal = checkGroundNormal;
                            closestGroundDistance = checkGroundDistance;
                        }

                        hit.IsWithinExtent = true;
                        hit.CollisionType = ECollisionType.Ground;
                    }
                    else if (isWithinExtent)
                    {
                        hit.IsWithinExtent = true;

                        if (hit.CollisionType == ECollisionType.None) hit.CollisionType = ECollisionType.Slope;
                    }
                }

                if (data.IsGrounded)
                {
                    maxGroundNormal = closestGroundNormal;
                    averageGroundNormal = closestGroundNormal;
                    groundDistance = closestGroundDistance;
                }
            }

            if (data.IsGrounded)
            {
                if (averageGroundNormal.IsEqual(maxGroundNormal) == false) averageGroundNormal.Normalize();

                data.GroundNormal = averageGroundNormal;
                data.GroundAngle = Vector3.Angle(data.GroundNormal, Vector3.up);
                data.GroundPosition = targetPosition + new Vector3(0.0f, _settings.Radius, 0.0f) -
                                      data.GroundNormal * (_settings.Radius + groundDistance);
                data.GroundDistance = groundDistance;
            }

            return targetPosition;
        }

        private static void RecalculateGroundProperties(KCCData data)
        {
            if (data.IsGrounded == false)
                return;

            if (KCCPhysicsUtility.ProjectOnGround(data.GroundNormal, data.GroundNormal.OnlyXZ(),
                    out var projectedGroundNormal))
            {
                data.GroundTangent = projectedGroundNormal.normalized;
                return;
            }

            if (KCCPhysicsUtility.ProjectOnGround(data.GroundNormal, data.DesiredVelocity.OnlyXZ(),
                    out var projectedDesiredVelocity))
            {
                data.GroundTangent = projectedDesiredVelocity.normalized;
                return;
            }

            data.GroundTangent = data.TransformDirection;
        }
    }
}