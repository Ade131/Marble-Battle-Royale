using UnityEngine;

namespace Fusion.Addons.KCC
{
	/// <summary>
	///     This processor detect steps (obstacles which block the character moving forward) and reflects blocked movement
	///     upwards.
	/// </summary>
	public class StepUpProcessor : KCCProcessor, IAfterMoveStep
    {
        // CONSTANTS

        public static readonly int DefaultPriority = -1000;

        // PRIVATE MEMBERS

        [SerializeField] [Tooltip("Maximum obstacle height to step on it.")]
        private float _stepHeight = 0.5f;

        [SerializeField] [Tooltip("Maximum depth of the step check.")]
        private float _stepDepth = 0.2f;

        [SerializeField]
        [Tooltip("Multiplier of unapplied movement projected to step up. This helps traversing obstacles faster.")]
        private float _stepSpeed = 1.0f;

        [SerializeField]
        [Tooltip(
            "Minimum proportional penetration push-back distance to activate step-up. A value of 0.5f means the KCC must be pushed back from colliding geometry by at least 50% of desired movement.")]
        [Range(0.25f, 0.75f)]
        private float _minPushBack = 0.5f;

        [SerializeField]
        [Tooltip(
            "Radius multiplier used for last sphere-cast (ground surface detection). Lower value work better with shorter step depth.")]
        [Range(0.25f, 1.0f)]
        private float _groundCheckRadiusScale = 0.5f;

        [SerializeField]
        [Tooltip(
            "Clears dynamic velocity when the step up is over. This eliminates bumps when dynamic up velocity is positive (for example after triggering jump).")]
        private bool _clearDynamicVelocityOnEnd = true;

        [SerializeField]
        [Tooltip("Step-up starts only if the target surface is walkable (angle <= KCCData.MaxGroundAngle).")]
        private bool _requireGroundTarget;

        [SerializeField] [Tooltip("Force extra update of collision hits if the step-up is active and moves the KCC.")]
        private bool _forceUpdateHits;

        private readonly KCCOverlapInfo _overlapInfo = new();
        private readonly KCCShapeCastInfo _shapeCastInfo = new();

        // KCCProcessor INTERFACE

        public override float GetPriority(KCC kcc)
        {
            return DefaultPriority;
        }

        // IAfterMoveStep INTERFACE

        public void Execute(AfterMoveStep stage, KCC kcc, KCCData data)
        {
            if (_stepHeight <= 0.0f)
                return;

            // Ignore step-up after jump and teleport.
            if (data.JumpFrames > 0 || data.HasTeleported)
            {
                ProcessStepUpResult(kcc, data, false);
                return;
            }

            var tryStepUp = false;

            if (HasCollisionsWithinExtent(stage.OverlapInfo,
                    ECollisionType.Slope | ECollisionType.Wall | ECollisionType.Hang))
            {
                tryStepUp = true;
            }
            else
            {
                // Following check desired distance with real distance traveled by KCC and triggers step-up
                // if something is pushing KCC back, lowering the distance traveled by more than 50%.

                var checkDesiredDelta = data.DesiredPosition - data.BasePosition;
                var checkDesiredDistance = checkDesiredDelta.magnitude;

                if (checkDesiredDistance > 0.001f)
                {
                    var targetDelta = data.TargetPosition - data.BasePosition;
                    var targetDistance = targetDelta.magnitude;

                    if (targetDistance / checkDesiredDistance < _minPushBack) tryStepUp = true;
                }
            }

            if (tryStepUp == false)
            {
                ProcessStepUpResult(kcc, data, false);
                return;
            }

            var basePosition = data.BasePosition;
            var desiredPosition = data.DesiredPosition;
            var targetPosition = data.TargetPosition;

            var desiredDelta = desiredPosition - basePosition;
            var desiredDirection = Vector3.Normalize(desiredDelta);

            // The step-up is not triggered if there is no pending movement from the player or external sources.
            if (desiredDirection == default)
            {
                ProcessStepUpResult(kcc, data, false);
                return;
            }

            // Ignore step-up while moving downwards (with ~25° deviation).
            if (Vector3.Dot(desiredDirection, Vector3.down) >= 0.9f)
            {
                ProcessStepUpResult(kcc, data, false);
                return;
            }

            var correctionDelta = targetPosition - desiredPosition;
            var correctionDistance = correctionDelta.magnitude;
            var correctionDirection =
                correctionDistance > 0.001f ? correctionDelta / correctionDistance : -desiredDirection;

            // The step-up is not triggered if the correction vector overlaps desired movement hemisphere.
            if (Vector3.Dot(desiredDirection, correctionDirection) >= 0.0f)
            {
                ProcessStepUpResult(kcc, data, false);
                return;
            }

            var desiredCheckDirectionXZ = Vector3.Normalize(desiredDirection.OnlyXZ());
            var correctionCheckDirectionXZ = Vector3.Normalize(-correctionDirection.OnlyXZ());
            var combinedCheckDirectionXZ = Vector3.Normalize(desiredCheckDirectionXZ + correctionCheckDirectionXZ);

            // Additional XZ comparison of desired direction and correction direction with deviation of ~85°.
            if (Vector3.Dot(desiredCheckDirectionXZ, correctionCheckDirectionXZ) < 0.1f)
            {
                ProcessStepUpResult(kcc, data, false);
                return;
            }

            // Following image shows movement step and collision with a geometry from top-down perspective.
            // P = Player position before movement.
            // D = Desired position before depenetration.
            // T = Target position after depenetration.
            // I = Impact position (intersection of P->D and collider plane).
            //
            //         D
            //         | \
            // --------T---I------- <- Obstacle
            //              \
            //                P
            //
            // Following code recalculates base step-up position from target position (T) to impact position (I).
            // This eliminates sliding along the collider when stepping up.
            if (correctionDirection.IsZero() == false &&
                HasCollisionsWithinExtent(stage.OverlapInfo, ECollisionType.Slope) == false)
            {
                var ray = new Ray(basePosition - desiredDelta * 2.0f, desiredDirection);
                var plane = new Plane(correctionDirection, targetPosition);

                if (plane.Raycast(ray, out var distance)) targetPosition = ray.GetPoint(distance);
            }

            var checkRadius = kcc.Settings.Radius - kcc.Settings.Extent;
            var checkPosition = targetPosition + new Vector3(0.0f, _stepHeight, 0.0f);

            // 1. Upward collision check.
            if (kcc.CapsuleOverlap(_overlapInfo, checkPosition, checkRadius, kcc.Settings.Height,
                    QueryTriggerInteraction.Ignore))
            {
                ProcessStepUpResult(kcc, data, false);
                return;
            }

            checkPosition += combinedCheckDirectionXZ * _stepDepth;

            // 2. Forward collision check. Forward = Combination of desired XZ direction and negative correction XZ direction - to eliminate stepping up along collider surface.
            if (kcc.CapsuleOverlap(_overlapInfo, checkPosition, checkRadius, kcc.Settings.Height,
                    QueryTriggerInteraction.Ignore))
            {
                ProcessStepUpResult(kcc, data, false);
                return;
            }

            var maxStepHeight = _stepHeight;
            bool highestPointFound = default;
            Vector3 highestPointNormal = default;

            if (_groundCheckRadiusScale < 1.0f)
            {
                // Ground check can be done with smaller radius to compensate normal on edges.
                checkRadius = kcc.Settings.Radius * _groundCheckRadiusScale;
                checkPosition += combinedCheckDirectionXZ * (kcc.Settings.Radius - kcc.Settings.Extent - checkRadius);
            }

            // 3. Downward collision check to get step height.
            if (kcc.SphereCast(_shapeCastInfo, checkPosition + new Vector3(0.0f, kcc.Settings.Radius, 0.0f),
                    checkRadius, Vector3.down, maxStepHeight + kcc.Settings.Radius, QueryTriggerInteraction.Ignore,
                    false))
            {
                var highestPoint = new Vector3(0.0f, float.MinValue, 0.0f);

                for (int i = 0, count = _shapeCastInfo.ColliderHitCount; i < count; ++i)
                {
                    var raycastHit = _shapeCastInfo.ColliderHits[i].RaycastHit;
                    var raycastHitPoint = raycastHit.point;

                    if (raycastHitPoint.y > targetPosition.y && raycastHitPoint.y > highestPoint.y)
                    {
                        highestPoint = raycastHitPoint;
                        highestPointNormal = raycastHit.normal;
                        highestPointFound = true;
                    }
                }

                if (highestPointFound)
                    maxStepHeight = Mathf.Clamp(highestPoint.y - targetPosition.y, 0.0f, _stepHeight);
            }

            // For initial attempt, do not try to step up on non-ground surfaces.
            if (_requireGroundTarget && data.IsSteppingUp == false && data.WasSteppingUp == false)
                if (highestPointFound)
                {
                    var minGroundDot = Mathf.Cos(Mathf.Clamp(data.MaxGroundAngle, 0.0f, 90.0f) * Mathf.Deg2Rad);
                    var highestPointUpDot = Vector3.Dot(highestPointNormal, Vector3.up);

                    if (highestPointUpDot < minGroundDot)
                    {
                        ProcessStepUpResult(kcc, data, false);
                        return;
                    }
                }

            // Project unapplied movement as step-up movement.
            var desiredDistance = Vector3.Distance(basePosition, desiredPosition);
            var traveledDistance = Vector3.Distance(basePosition, targetPosition);
            var remainingDistance = Mathf.Clamp((desiredDistance - traveledDistance) * _stepSpeed, 0.0f, maxStepHeight);

            remainingDistance *= Mathf.Clamp01(Vector3.Dot(desiredDirection, -correctionDirection));

            data.TargetPosition = targetPosition + new Vector3(0.0f, remainingDistance, 0.0f);

            // KCC remains grounded state while stepping up.
            data.IsGrounded = true;
            data.GroundNormal = Vector3.up;
            data.GroundDistance = kcc.Settings.Extent;
            data.GroundPosition = data.TargetPosition;
            data.GroundTangent = data.TransformDirection;

            if (_forceUpdateHits)
                // New position is set, refresh collision hits after the stage.
                stage.RequestUpdateHits(true);

            ProcessStepUpResult(kcc, data, true);
        }

        // StepUpProcessor INTERFACE

        protected virtual void OnStepUpBegin(KCC kcc, KCCData data)
        {
        }

        protected virtual void OnStepUpEnd(KCC kcc, KCCData data)
        {
        }

        // PRIVATE METHODS

        private void ProcessStepUpResult(KCC kcc, KCCData data, bool isSteppingUp)
        {
            data.IsSteppingUp = isSteppingUp;

            if (data.IsSteppingUp && data.WasSteppingUp == false)
            {
                OnStepUpBegin(kcc, data);
            }
            else if (data.IsSteppingUp == false && data.WasSteppingUp)
            {
                if (_clearDynamicVelocityOnEnd)
                    // Clearing dynamic velocity ensures that the movement from external sources and jump won't continue after hiting the edge.
                    data.DynamicVelocity = default;

                OnStepUpEnd(kcc, data);
            }
        }

        private static bool HasCollisionsWithinExtent(KCCOverlapInfo overlapInfo, ECollisionType collisionTypes)
        {
            for (var i = 0; i < overlapInfo.ColliderHitCount; ++i)
            {
                var hit = overlapInfo.ColliderHits[i];
                if (hit.IsWithinExtent && (collisionTypes & hit.CollisionType) != default)
                    return true;
            }

            return false;
        }
    }
}