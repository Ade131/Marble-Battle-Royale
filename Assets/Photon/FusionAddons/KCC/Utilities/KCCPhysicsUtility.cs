using UnityEngine;

namespace Fusion.Addons.KCC
{
    public static class KCCPhysicsUtility
    {
        // PUBLIC METHODS

        public static bool ProjectOnGround(Vector3 groundNormal, Vector3 vector, out Vector3 projectedVector)
        {
            var dot1 = Vector3.Dot(Vector3.up, groundNormal);
            var dot2 = -Vector3.Dot(vector, groundNormal);

            if (dot1.IsAlmostZero(0.001f) == false)
            {
                projectedVector = new Vector3(vector.x, vector.y + dot2 / dot1, vector.z);
                return true;
            }

            projectedVector = default;
            return false;
        }

        public static void ProjectVerticalPenetration(ref Vector3 direction, ref float distance)
        {
            var desiredCorrection = direction * distance;
            var desiredCorrectionXZ = desiredCorrection.OnlyXZ();
            var correctionDistanceXZ = Vector3.Magnitude(desiredCorrectionXZ);

            if (correctionDistanceXZ >= 0.000001f)
            {
                var reflectedDistanceXZ = desiredCorrection.y * desiredCorrection.y / correctionDistanceXZ;

                direction = desiredCorrectionXZ / correctionDistanceXZ;
                distance = correctionDistanceXZ + reflectedDistanceXZ;
            }
        }

        public static void ProjectHorizontalPenetration(ref Vector3 direction, ref float distance)
        {
            var desiredCorrection = direction * distance;

            direction = Vector3.up;
            distance = 0.0f;

            if (desiredCorrection.y > -0.000001f && desiredCorrection.y < 0.000001f)
                return;

            distance = desiredCorrection.y +
                       (desiredCorrection.x * desiredCorrection.x + desiredCorrection.z * desiredCorrection.z) /
                       desiredCorrection.y;

            if (distance < 0.0f)
            {
                direction = -direction;
                distance = -distance;
            }
        }

        public static bool CheckGround(Collider collider, Vector3 position, Collider groundCollider,
            Vector3 groundPosition, Quaternion groundRotation, float radius, float height, float extent,
            float minGroundDot, out Vector3 groundNormal, out float groundDistance, out bool isWithinExtent)
        {
            isWithinExtent = false;

#if KCC_DISABLE_TERRAIN
			if (groundCollider is MeshCollider)
#else
            if (groundCollider is MeshCollider || groundCollider is TerrainCollider)
#endif
            {
                if (UnityEngine.Physics.ComputePenetration(collider, position - new Vector3(0.0f, extent, 0.0f),
                        Quaternion.identity, groundCollider, groundPosition, groundRotation, out var direction,
                        out var distance))
                {
                    isWithinExtent = true;

                    var directionUpDot = Vector3.Dot(direction, Vector3.up);
                    if (directionUpDot >= minGroundDot)
                    {
                        var projectedDirection = direction;
                        var projectedDistance = distance;

                        ProjectHorizontalPenetration(ref projectedDirection, ref projectedDistance);

                        var verticalDistance = Mathf.Max(0.0f, extent - projectedDistance);

                        groundNormal = direction;
                        groundDistance = verticalDistance * directionUpDot;

                        return true;
                    }
                }
            }
            else
            {
                var radiusSqr = radius * radius;
                var radiusExtent = radius + extent;
                var radiusExtentSqr = radiusExtent * radiusExtent;
                var centerPosition = position + new Vector3(0.0f, radius, 0.0f);
                var closestPoint =
                    UnityEngine.Physics.ClosestPoint(centerPosition, groundCollider, groundPosition, groundRotation);
                var closestPointOffset = closestPoint - centerPosition;
                var closestPointOffsetXZ = closestPointOffset.OnlyXZ();
                var closestPointDistanceXZSqr = closestPointOffsetXZ.sqrMagnitude;

                if (closestPointDistanceXZSqr <= radiusExtentSqr)
                {
                    if (closestPointOffset.y < 0.0f)
                    {
                        var closestPointDistance = Vector3.Magnitude(closestPointOffset);
                        if (closestPointDistance <= radiusExtent)
                        {
                            isWithinExtent = true;

                            var closestPointDirection = closestPointOffset / closestPointDistance;
                            var closestGroundNormal = -closestPointDirection;

                            var closestGroundDot = Vector3.Dot(closestGroundNormal, Vector3.up);
                            if (closestGroundDot >= minGroundDot)
                            {
                                groundNormal = closestGroundNormal;
                                groundDistance = Mathf.Max(0.0f, closestPointDistance - radius);

                                return true;
                            }
                        }
                    }
                    else if (closestPointOffset.y < height - radius * 2.0f)
                    {
                        isWithinExtent = true;
                    }
                }
            }

            groundNormal = Vector3.up;
            groundDistance = 0.0f;

            return false;
        }

        public static Vector3 GetAcceleration(Vector3 velocity, Vector3 direction, Vector3 axis, float maxSpeed,
            bool clampSpeed, float inputAcceleration, float constantAcceleration, float relativeAcceleration,
            float proportionalAcceleration, float deltaTime)
        {
            if (inputAcceleration <= 0.0f)
                return Vector3.zero;
            if (constantAcceleration <= 0.0f && relativeAcceleration <= 0.0f && proportionalAcceleration <= 0.0f)
                return Vector3.zero;
            if (direction.IsZero())
                return Vector3.zero;

            var baseSpeed = new Vector3(velocity.x * axis.x, velocity.y * axis.y, velocity.z * axis.z).magnitude;
            var baseDirection = new Vector3(direction.x * axis.x, direction.y * axis.y, direction.z * axis.z)
                .normalized;

            var missingSpeed = Mathf.Max(0.0f, maxSpeed - baseSpeed);

            if (constantAcceleration < 0.0f) constantAcceleration = 0.0f;
            if (relativeAcceleration < 0.0f) relativeAcceleration = 0.0f;
            if (proportionalAcceleration < 0.0f) proportionalAcceleration = 0.0f;

            constantAcceleration *= inputAcceleration;
            relativeAcceleration *= inputAcceleration;
            proportionalAcceleration *= inputAcceleration;

            var speedGain = (constantAcceleration + maxSpeed * relativeAcceleration +
                             missingSpeed * proportionalAcceleration) * deltaTime;
            if (speedGain <= 0.0f)
                return Vector3.zero;

            if (clampSpeed && speedGain > missingSpeed) speedGain = missingSpeed;

            return baseDirection.normalized * speedGain;
        }

        public static Vector3 GetAcceleration(Vector3 velocity, Vector3 direction, Vector3 axis, Vector3 normal,
            float targetSpeed, bool clampSpeed, float inputAcceleration, float constantAcceleration,
            float relativeAcceleration, float proportionalAcceleration, float deltaTime)
        {
            var accelerationMultiplier = 1.0f - Mathf.Clamp01(Vector3.Dot(direction.normalized, normal));

            constantAcceleration *= accelerationMultiplier;
            relativeAcceleration *= accelerationMultiplier;
            proportionalAcceleration *= accelerationMultiplier;

            return GetAcceleration(velocity, direction, axis, targetSpeed, clampSpeed, inputAcceleration,
                constantAcceleration, relativeAcceleration, proportionalAcceleration, deltaTime);
        }

        public static Vector3 GetFriction(Vector3 velocity, Vector3 direction, Vector3 axis, float maxSpeed,
            bool clampSpeed, float constantFriction, float relativeFriction, float proportionalFriction,
            float deltaTime)
        {
            if (constantFriction <= 0.0f && relativeFriction <= 0.0f && proportionalFriction <= 0.0f)
                return Vector3.zero;
            if (direction.IsZero())
                return Vector3.zero;

            var baseSpeed = new Vector3(velocity.x * axis.x, velocity.y * axis.y, velocity.z * axis.z).magnitude;
            var baseDirection = new Vector3(direction.x * axis.x, direction.y * axis.y, direction.z * axis.z)
                .normalized;

            if (constantFriction < 0.0f) constantFriction = 0.0f;
            if (relativeFriction < 0.0f) relativeFriction = 0.0f;
            if (proportionalFriction < 0.0f) proportionalFriction = 0.0f;

            var speedDrop = (constantFriction + maxSpeed * relativeFriction + baseSpeed * proportionalFriction) *
                            deltaTime;
            if (speedDrop <= 0.0f)
                return Vector3.zero;

            if (clampSpeed && speedDrop > baseSpeed) speedDrop = baseSpeed;

            return -baseDirection * speedDrop;
        }

        public static Vector3 GetFriction(Vector3 velocity, Vector3 direction, Vector3 axis, Vector3 normal,
            float maxSpeed, bool clampSpeed, float constantFriction, float relativeFriction, float proportionalFriction,
            float deltaTime)
        {
            var frictionMultiplier = 1.0f - Mathf.Clamp01(Vector3.Dot(direction.normalized, normal));

            constantFriction *= frictionMultiplier;
            relativeFriction *= frictionMultiplier;
            proportionalFriction *= frictionMultiplier;

            return GetFriction(velocity, direction, axis, maxSpeed, clampSpeed, constantFriction, relativeFriction,
                proportionalFriction, deltaTime);
        }

        public static Vector3 CombineAccelerationAndFriction(Vector3 velocity, Vector3 acceleration, Vector3 friction)
        {
            velocity.x = CombineAxis(velocity.x, acceleration.x, friction.x);
            velocity.y = CombineAxis(velocity.y, acceleration.y, friction.y);
            velocity.z = CombineAxis(velocity.z, acceleration.z, friction.z);

            return velocity;

            static float CombineAxis(float axisVelocity, float axisAcceleration, float axisFriction)
            {
                var velocityDelta = axisAcceleration + axisFriction;

                if (Mathf.Abs(axisAcceleration) >= Mathf.Abs(axisFriction))
                {
                    axisVelocity += velocityDelta;
                }
                else
                {
                    if (axisVelocity > 0.0)
                        axisVelocity = Mathf.Max(0.0f, axisVelocity + velocityDelta);
                    else if (axisVelocity < 0.0) axisVelocity = Mathf.Min(axisVelocity + velocityDelta, 0.0f);
                }

                return axisVelocity;
            }
        }

        public static Vector3 AccumulatePenetrationCorrection(Vector3 accumulatedCorrection,
            Vector3 contactCorrectionDirection, float contactCorrectionMagnitude)
        {
            var accumulatedCorrectionMagnitude = Vector3.Magnitude(accumulatedCorrection);
            var accumulatedCorrectionDirection = accumulatedCorrectionMagnitude > 0.0000000001f
                ? accumulatedCorrection / accumulatedCorrectionMagnitude
                : Vector3.zero;

            float deltaCorrectionMagnitude = default;
            var deltaCorrectionDirection = Vector3
                .Cross(Vector3.Cross(accumulatedCorrectionDirection, contactCorrectionDirection),
                    accumulatedCorrectionDirection).normalized;

            var cos = Vector3.Dot(contactCorrectionDirection, accumulatedCorrectionDirection);
            var sinSqr = 1.0f - cos * cos;

            if (sinSqr > 0.001f)
            {
                deltaCorrectionMagnitude = (contactCorrectionMagnitude - accumulatedCorrectionMagnitude * cos) /
                                           Mathf.Sqrt(sinSqr);
            }
            else if (contactCorrectionMagnitude > accumulatedCorrectionMagnitude)
            {
                deltaCorrectionMagnitude = contactCorrectionMagnitude - accumulatedCorrectionMagnitude;
                deltaCorrectionDirection = accumulatedCorrectionDirection;
            }

            accumulatedCorrection += deltaCorrectionDirection * deltaCorrectionMagnitude;

            return accumulatedCorrection;
        }
    }
}