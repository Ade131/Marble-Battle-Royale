using UnityEngine;

namespace Fusion.Addons.KCC
{
    // This file contains API which manipulates with KCCData or KCCSettings.
    public partial class KCC
    {
        // PUBLIC METHODS

        /// <summary>
        ///     Controls execution of the KCC.
        /// </summary>
        public void SetActive(bool isActive)
        {
            if (CheckSpawned() == false)
                return;

            RenderData.IsActive = isActive;

            if (IsInFixedUpdate) FixedData.IsActive = isActive;
        }

        /// <summary>
        ///     Set non-interpolated world space input direction. Vector with magnitude greater than 1.0f is normalized.
        ///     Changes done in render will vanish with next fixed update.
        /// </summary>
        public void SetInputDirection(Vector3 direction, bool clampToNormalized = true)
        {
            if (clampToNormalized) direction.ClampToNormalized();

            RenderData.InputDirection = direction;

            if (IsInFixedUpdate) FixedData.InputDirection = direction;
        }

        /// <summary>
        ///     Returns current look rotation.
        /// </summary>
        public Vector2 GetLookRotation(bool pitch = true, bool yaw = true)
        {
            return Data.GetLookRotation(pitch, yaw);
        }

        /// <summary>
        ///     Add pitch and yaw look rotation. Resulting values are clamped to &lt;-90, 90&gt; (pitch) and &lt;-180, 180&gt;
        ///     (yaw).
        ///     Changes done in render will vanish with next fixed update.
        /// </summary>
        public void AddLookRotation(float pitchDelta, float yawDelta)
        {
            var data = RenderData;
            data.AddLookRotation(pitchDelta, yawDelta);

            if (IsInFixedUpdate)
            {
                data = FixedData;
                data.AddLookRotation(pitchDelta, yawDelta);
            }

            SynchronizeTransform(data, false, true, false);
        }

        /// <summary>
        ///     Add pitch and yaw look rotation. Resulting values are clamped to &lt;minPitch, maxPitch&gt; (pitch) and &lt;-180,
        ///     180&gt; (yaw).
        ///     Changes done in render will vanish with next fixed update.
        /// </summary>
        public void AddLookRotation(float pitchDelta, float yawDelta, float minPitch, float maxPitch)
        {
            var data = RenderData;
            data.AddLookRotation(pitchDelta, yawDelta, minPitch, maxPitch);

            if (IsInFixedUpdate)
            {
                data = FixedData;
                data.AddLookRotation(pitchDelta, yawDelta, minPitch, maxPitch);
            }

            SynchronizeTransform(data, false, true, false);
        }

        /// <summary>
        ///     Add pitch (x) and yaw (y) look rotation. Resulting values are clamped to &lt;-90, 90&gt; (pitch) and &lt;-180, 180
        ///     &gt; (yaw).
        ///     Changes done in render will vanish with next fixed update.
        /// </summary>
        public void AddLookRotation(Vector2 lookRotationDelta)
        {
            AddLookRotation(lookRotationDelta.x, lookRotationDelta.y);
        }

        /// <summary>
        ///     Add pitch (x) and yaw (y) look rotation. Resulting values are clamped to &lt;minPitch, maxPitch&gt; (pitch) and
        ///     &lt;-180, 180&gt; (yaw).
        ///     Changes done in render will vanish with next fixed update.
        /// </summary>
        public void AddLookRotation(Vector2 lookRotationDelta, float minPitch, float maxPitch)
        {
            AddLookRotation(lookRotationDelta.x, lookRotationDelta.y, minPitch, maxPitch);
        }

        /// <summary>
        ///     Set pitch and yaw look rotation. Values are clamped to &lt;-90, 90&gt; (pitch) and &lt;-180, 180&gt; (yaw).
        ///     Changes done in render will vanish with next fixed update.
        /// </summary>
        public void SetLookRotation(float pitch, float yaw)
        {
            var data = RenderData;
            data.SetLookRotation(pitch, yaw);

            if (IsInFixedUpdate)
            {
                data = FixedData;
                data.SetLookRotation(pitch, yaw);
            }

            SynchronizeTransform(data, false, true, false);
        }

        /// <summary>
        ///     Set pitch and yaw look rotation. Values are clamped to &lt;-90, 90&gt; (pitch) and &lt;-180, 180&gt; (yaw).
        ///     Changes done in render will vanish with next fixed update.
        /// </summary>
        public void SetLookRotation(Vector2 lookRotation)
        {
            SetLookRotation(lookRotation.x, lookRotation.y);
        }

        /// <summary>
        ///     Set pitch and yaw look rotation. Roll is ignored (not supported). Values are clamped to &lt;-90, 90&gt; (pitch) and
        ///     &lt;-180, 180&gt; (yaw).
        ///     Changes done in render will vanish with next fixed update.
        /// </summary>
        public void SetLookRotation(Quaternion lookRotation, bool preservePitch = false, bool preserveYaw = false)
        {
            var data = RenderData;
            data.SetLookRotation(lookRotation, preservePitch, preserveYaw);

            if (IsInFixedUpdate)
            {
                data = FixedData;
                data.SetLookRotation(lookRotation, preservePitch, preserveYaw);
            }

            SynchronizeTransform(data, false, true, false);
        }

        /// <summary>
        ///     Add jump impulse, which should be propagated by processors to <c>KCCData.DynamicVelocity</c>.
        ///     Changes done in render will vanish with next fixed update.
        /// </summary>
        public void Jump(Vector3 impulse)
        {
            RenderData.JumpImpulse += impulse;

            if (IsInFixedUpdate) FixedData.JumpImpulse += impulse;
        }

        /// <summary>
        ///     Add velocity from external sources. Should propagate in processors to <c>KCCData.DynamicVelocity</c>.
        ///     Changes done in render will vanish with next fixed update.
        /// </summary>
        public void AddExternalVelocity(Vector3 velocity)
        {
            RenderData.ExternalVelocity += velocity;

            if (IsInFixedUpdate) FixedData.ExternalVelocity += velocity;
        }

        /// <summary>
        ///     Set velocity from external sources. Should propagate in processors to <c>KCCData.DynamicVelocity</c>.
        ///     Changes done in render will vanish with next fixed update.
        /// </summary>
        public void SetExternalVelocity(Vector3 velocity)
        {
            RenderData.ExternalVelocity = velocity;

            if (IsInFixedUpdate) FixedData.ExternalVelocity = velocity;
        }

        /// <summary>
        ///     Add acceleration from external sources. Should propagate in processors to <c>KCCData.DynamicVelocity</c>.
        ///     Changes done in render will vanish with next fixed update.
        /// </summary>
        public void AddExternalAcceleration(Vector3 acceleration)
        {
            RenderData.ExternalAcceleration += acceleration;

            if (IsInFixedUpdate) FixedData.ExternalAcceleration += acceleration;
        }

        /// <summary>
        ///     Set acceleration from external sources. Should propagate in processors to <c>KCCData.DynamicVelocity</c>.
        ///     Changes done in render will vanish with next fixed update.
        /// </summary>
        public void SetExternalAcceleration(Vector3 acceleration)
        {
            RenderData.ExternalAcceleration = acceleration;

            if (IsInFixedUpdate) FixedData.ExternalAcceleration = acceleration;
        }

        /// <summary>
        ///     Add impulse from external sources. Should propagate in processors to <c>KCCData.DynamicVelocity</c>.
        ///     Changes done in render will vanish with next fixed update.
        /// </summary>
        public void AddExternalImpulse(Vector3 impulse)
        {
            RenderData.ExternalImpulse += impulse;

            if (IsInFixedUpdate) FixedData.ExternalImpulse += impulse;
        }

        /// <summary>
        ///     Set impulse from external sources. Should propagate in processors to <c>KCCData.DynamicVelocity</c>.
        ///     Changes done in render will vanish with next fixed update.
        /// </summary>
        public void SetExternalImpulse(Vector3 impulse)
        {
            RenderData.ExternalImpulse = impulse;

            if (IsInFixedUpdate) FixedData.ExternalImpulse = impulse;
        }

        /// <summary>
        ///     Add force from external sources. Should propagate in processors to <c>KCCData.DynamicVelocity</c>.
        ///     Changes done in render will vanish with next fixed update.
        /// </summary>
        public void AddExternalForce(Vector3 force)
        {
            RenderData.ExternalForce += force;

            if (IsInFixedUpdate) FixedData.ExternalForce += force;
        }

        /// <summary>
        ///     Set force from external sources. Should propagate in processors to <c>KCCData.DynamicVelocity</c>.
        ///     Changes done in render will vanish with next fixed update.
        /// </summary>
        public void SetExternalForce(Vector3 force)
        {
            RenderData.ExternalForce = force;

            if (IsInFixedUpdate) FixedData.ExternalForce = force;
        }

        /// <summary>
        ///     Add position delta from external sources. Will be consumed by following update.
        ///     Changes done in render will vanish with next fixed update.
        /// </summary>
        public void AddExternalDelta(Vector3 delta)
        {
            RenderData.ExternalDelta += delta;

            if (IsInFixedUpdate) FixedData.ExternalDelta += delta;
        }

        /// <summary>
        ///     Set position delta from external sources. Will be consumed by following update.
        ///     Changes done in render will vanish with next fixed update.
        /// </summary>
        public void SetExternalDelta(Vector3 delta)
        {
            RenderData.ExternalDelta = delta;

            if (IsInFixedUpdate) FixedData.ExternalDelta = delta;
        }

        /// <summary>
        ///     Set <c>KCCData.DynamicVelocity</c>.
        ///     Changes done in render will vanish with next fixed update.
        /// </summary>
        public void SetDynamicVelocity(Vector3 velocity)
        {
            RenderData.DynamicVelocity = velocity;

            if (IsInFixedUpdate) FixedData.DynamicVelocity = velocity;
        }

        /// <summary>
        ///     Set <c>KCCData.KinematicVelocity</c>.
        ///     Changes done in render will vanish with next fixed update.
        /// </summary>
        public void SetKinematicVelocity(Vector3 velocity)
        {
            RenderData.KinematicVelocity = velocity;

            if (IsInFixedUpdate) FixedData.KinematicVelocity = velocity;
        }

        /// <summary>
        ///     Sets <c>KCCData.BasePosition</c>, <c>KCCData.DesiredPosition</c>, <c>KCCData.TargetPosition</c> and immediately
        ///     synchronize Transform and Rigidbody components.
        ///     Also sets <c>KCCData.HasTeleported</c> flag to <c>true</c> and clears <c>KCCData.IsSteppingUp</c> and
        ///     <c>KCCData.IsSnappingToGround</c>.
        ///     Calling this from within a processor stage effectively stops any pending move steps and forces KCC to update hits
        ///     with new overlap query.
        ///     Changes done in render will vanish with next fixed update.
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            var data = RenderData;

            data.BasePosition = position;
            data.DesiredPosition = position;
            data.TargetPosition = position;
            data.HasTeleported = true;
            data.IsSteppingUp = false;
            data.IsSnappingToGround = false;

            if (IsInFixedUpdate)
            {
                data = FixedData;

                data.BasePosition = position;
                data.DesiredPosition = position;
                data.TargetPosition = position;
                data.HasTeleported = true;
                data.IsSteppingUp = false;
                data.IsSnappingToGround = false;
            }

            SynchronizeTransform(data, true, false, false);
        }

        /// <summary>
        ///     Update <c>Shape</c>, <c>Radius</c> (optional), <c>Height</c> (optional) in settings and immediately synchronize
        ///     with Collider.
        ///     <list type="bullet">
        ///         <item>
        ///             <description>None - Skips internal physics query, collider is despawned.</description>
        ///         </item>
        ///         <item>
        ///             <description>Capsule - Full physics processing, Capsule collider spawned.</description>
        ///         </item>
        ///     </list>
        /// </summary>
        public void SetShape(EKCCShape shape, float radius = 0.0f, float height = 0.0f)
        {
            _settings.Shape = shape;

            if (radius > 0.0f) _settings.Radius = radius;
            if (height > 0.0f) _settings.Height = height;

            RefreshCollider();
        }

        /// <summary>
        ///     Update <c>IsTrigger</c> flag in settings and immediately synchronize with Collider.
        /// </summary>
        public void SetTrigger(bool isTrigger)
        {
            _settings.IsTrigger = isTrigger;

            RefreshCollider();
        }

        /// <summary>
        ///     Update <c>Radius</c> in settings and immediately synchronize with Collider.
        /// </summary>
        public void SetRadius(float radius)
        {
            if (radius <= 0.0f)
                return;

            _settings.Radius = radius;

            RefreshCollider();
        }

        /// <summary>
        ///     Update <c>Height</c> in settings and immediately synchronize with Collider.
        /// </summary>
        public void SetHeight(float height)
        {
            if (height <= 0.0f)
                return;

            _settings.Height = height;

            RefreshCollider();
        }

        /// <summary>
        ///     Update <c>ColliderLayer</c> in settings and immediately synchronize with Collider.
        /// </summary>
        public void SetColliderLayer(int layer)
        {
            _settings.ColliderLayer = layer;

            RefreshCollider();
        }

        /// <summary>
        ///     Update <c>CollisionLayerMask</c> in settings.
        /// </summary>
        public void SetCollisionLayerMask(LayerMask layerMask)
        {
            _settings.CollisionLayerMask = layerMask;
        }

        /// <summary>
        ///     Returns whether the KCC use Local or Remote timeframe for interpolation.
        /// </summary>
        public RenderTimeframe GetInterpolationTimeframe()
        {
            return Object.IsInSimulation ? RenderTimeframe.Local : RenderTimeframe.Remote;
        }
    }
}