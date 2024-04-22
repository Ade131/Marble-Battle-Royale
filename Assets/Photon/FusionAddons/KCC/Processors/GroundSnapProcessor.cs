using UnityEngine;

namespace Fusion.Addons.KCC
{
	/// <summary>
	///     This processor snaps character down after losing grounded state.
	/// </summary>
	public class GroundSnapProcessor : KCCProcessor, IAfterMoveStep
    {
        // CONSTANTS

        public static readonly int DefaultPriority = -2000;

        // PRIVATE MEMBERS

        [SerializeField] [Tooltip("Maximum ground check distance for snapping.")]
        private float _snapDistance = 0.25f;

        [SerializeField] [Tooltip("Ground snapping speed per second.")]
        private float _snapSpeed = 4.0f;

        [SerializeField] [Tooltip("Force extra update of collision hits if the snapping is active and moves the KCC.")]
        private bool _forceUpdateHits;

        private readonly KCCOverlapInfo _overlapInfo = new();

        // KCCProcessor INTERFACE

        public override float GetPriority(KCC kcc)
        {
            return DefaultPriority;
        }

        // IAfterMoveStep INTERFACE

        public virtual void Execute(AfterMoveStep stage, KCC kcc, KCCData data)
        {
            if (_snapDistance <= 0.0f)
                return;

            // Ground snapping activates only if ground is lost and there's no jump or step-up active.
            if (data.IsGrounded || data.WasGrounded == false || data.JumpFrames > 0 || data.IsSteppingUp ||
                data.WasSteppingUp)
                return;

            // Ignore ground snapping if there is a force pushing the character upwards.
            if (data.DynamicVelocity.y > 0.0f)
                return;

            var maxPenetrationDistance = _snapDistance;
            var maxStepPenetrationDelta = kcc.Settings.Radius * 0.25f;
            var penetrationSteps = Mathf.CeilToInt(maxPenetrationDistance / maxStepPenetrationDelta);
            var penetrationDelta = maxPenetrationDistance / penetrationSteps;
            var overlapRadius = kcc.Settings.Radius * 1.5f;

            // Make a bigger overlap to correctly resolve penetrations along the way down.
            kcc.CapsuleOverlap(_overlapInfo, data.TargetPosition - new Vector3(0.0f, _snapDistance, 0.0f),
                overlapRadius, kcc.Settings.Height + _snapDistance, QueryTriggerInteraction.Ignore);

            if (_overlapInfo.ColliderHitCount == 0)
                return;

            var targetGroundedPosition = data.TargetPosition;
            var penetrationPositionDelta = new Vector3(0.0f, -penetrationDelta, 0.0f);

            // Checking collisions with full snap distance could lead to incorrect collision type (ground/slope/wall) detection.
            // So we split the downward movenent into more steps and move by 1/4 of radius at max in single step.
            for (var i = 0; i < penetrationSteps; ++i)
            {
                // Resolve penetration on new candidate position.
                targetGroundedPosition = kcc.ResolvePenetration(_overlapInfo, data, targetGroundedPosition,
                    targetGroundedPosition + penetrationPositionDelta, false, 0, 0, false);

                if (data.IsGrounded)
                {
                    // We found the ground, now move the KCC towards the grounded position.

                    var maxSnapDelta = _snapSpeed * data.UpdateDeltaTime;
                    var positionOffset = targetGroundedPosition - data.TargetPosition;
                    Vector3 targetSnappedPosition;

                    if (data.WasSnappingToGround == false)
                        // First max snap delta is reduced by half to smooth out the snapping.
                        maxSnapDelta *= 0.5f;

                    if (positionOffset.sqrMagnitude <= maxSnapDelta * maxSnapDelta)
                        targetSnappedPosition = targetGroundedPosition;
                    else
                        targetSnappedPosition = data.TargetPosition + positionOffset.normalized * maxSnapDelta;

                    kcc.Debug.DrawGroundSnapping(data.TargetPosition, targetGroundedPosition, targetSnappedPosition,
                        kcc.IsInFixedUpdate);

                    data.TargetPosition = targetSnappedPosition;
                    data.GroundDistance = Mathf.Max(0.0f, targetSnappedPosition.y - targetGroundedPosition.y);
                    data.IsSnappingToGround = true;

                    if (_forceUpdateHits)
                        // New position is set, refresh collision hits after the stage.
                        stage.RequestUpdateHits(true);

                    break;
                }
            }
        }
    }
}