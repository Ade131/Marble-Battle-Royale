using UnityEngine;

namespace Fusion.Addons.KCC
{
    /// <summary>
    ///     Main data structure used for movement calculations. Stores data which require rollback, network synchronization +
    ///     various metadata.
    /// </summary>
    public sealed partial class KCCData
    {
        /// <summary>
        ///     Collection of networked collisions. Represents colliders the KCC interacts with.
        ///     Only objects with NetworkObject component are stored for compatibility with local prediction.
        /// </summary>
        public readonly KCCCollisions Collisions = new();

        /// <summary>
        ///     Collection of colliders/triggers the KCC overlaps (radius + extent).
        ///     This collection is not synchronized over the network! NetworkObject component is not needed, only local history is
        ///     supported.
        /// </summary>
        public readonly KCCHits Hits = new();

        /// <summary>
        ///     Collection of ignored colliders.
        ///     Only objects with NetworkObject component are stored for compatibility with local prediction.
        /// </summary>
        public readonly KCCIgnores Ignores = new();

        /// <summary>
        ///     Collection of manually registered modifiers (for example processors) the KCC interacts with.
        ///     Only objects with NetworkObject component are stored for compatibility with local prediction.
        /// </summary>
        public readonly KCCModifiers Modifiers = new();

        private Vector3 _lookDirection;
        private bool _lookDirectionCalculated;

        // PRIVATE MEMBERS

        private float _lookPitch;
        private Quaternion _lookRotation;
        private bool _lookRotationCalculated;
        private float _lookYaw;
        private Vector3 _transformDirection;
        private bool _transformDirectionCalculated;
        private Quaternion _transformRotation;
        private bool _transformRotationCalculated;

        /// <summary>
        ///     Relative position of the time between two fixed times. Valid range is &lt;0.0f, 1.0f&gt;
        /// </summary>
        public float Alpha;

        /// <summary>
        ///     Base position, initialized with TargetPosition at the start of each KCC step.
        /// </summary>
        public Vector3 BasePosition;

        /// <summary>
        ///     Partial delta time, variable if CCD is active. Valid range is &lt;<c>0.0f, UpdateDeltaTime</c>&gt;, but can be
        ///     altered.
        /// </summary>
        public float DeltaTime;

        /// <summary>
        ///     Desired position before depenetration and post-processing.
        /// </summary>
        public Vector3 DesiredPosition;

        /// <summary>
        ///     Velocity calculated from <c>Gravity</c>, <c>ExternalVelocity</c>, <c>ExternalAcceleration</c>,
        ///     <c>ExternalImpulse</c>, <c>ExternalForce</c> and <c>JumpImpulse</c>.
        /// </summary>
        public Vector3 DynamicVelocity;

        /// <summary>
        ///     Acceleration from external sources, continuous effect - value remains same for subsequent applications in render,
        ///     ignoring <c>Mass</c>, example usage - escalator.
        /// </summary>
        public Vector3 ExternalAcceleration;

        /// <summary>
        ///     Absolute position delta which is consumed by single move. It can also be set from ProcessPhysicsQuery and still
        ///     consumed by currently executed move (useful for depenetration corrections).
        /// </summary>
        public Vector3 ExternalDelta;

        /// <summary>
        ///     Force from external sources, continuous effect - value remains same for subsequent applications in render, affected
        ///     by Mass, example usage - attractor.
        /// </summary>
        public Vector3 ExternalForce;

        /// <summary>
        ///     Impulse from external sources, one-time effect - reseted on the end of <c>Move()</c> call to prevent subsequent
        ///     applications in render, affected by <c>Mass</c>, example usage - explosion.
        /// </summary>
        public Vector3 ExternalImpulse;

        /// <summary>
        ///     Velocity from external sources, one-time effect - reseted on the end of <c>Move()</c> call to prevent subsequent
        ///     applications in render, ignoring <c>Mass</c>, example usage - jump pad.
        /// </summary>
        public Vector3 ExternalVelocity;
        // PUBLIC MEMBERS

        /// <summary>
        ///     Frame number, equals to <c>Time.frameCount</c>.
        /// </summary>
        public int Frame;

        /// <summary>
        ///     Gravitational acceleration.
        /// </summary>
        public Vector3 Gravity;

        /// <summary>
        ///     Difference between ground normal and up direction in degrees.
        /// </summary>
        public float GroundAngle;

        /// <summary>
        ///     Distance from ground.
        /// </summary>
        public float GroundDistance;

        /// <summary>
        ///     Combined normal of all touching colliders. Normals less distant from up direction have bigger impacton final
        ///     normal.
        /// </summary>
        public Vector3 GroundNormal;

        /// <summary>
        ///     Position of the KCC collider surface touching the ground collider.
        /// </summary>
        public Vector3 GroundPosition;

        /// <summary>
        ///     Tangent to <c>GroundNormal</c>, can be calculated from <c>DesiredVelocity</c> or <c>TargetRotation</c> if
        ///     <c>GroundNormal</c> and up direction is same.
        /// </summary>
        public Vector3 GroundTangent;

        /// <summary>
        ///     Flag that indicates KCC has teleported in current tick.
        /// </summary>
        public bool HasTeleported;

        /// <summary>
        ///     Non-interpolated world space input direction - based on Keyboard / Joystick / NavMesh / ...
        /// </summary>
        public Vector3 InputDirection;

        /// <summary>
        ///     Controls execution of the KCC.
        /// </summary>
        public bool IsActive = true;

        /// <summary>
        ///     Flag that indicates KCC is touching a collider with normal angle lower than <c>MaxGroundAngle</c>.
        /// </summary>
        public bool IsGrounded;

        /// <summary>
        ///     Indicates the KCC temporarily lost grounded state and is snapping to ground.
        /// </summary>
        public bool IsSnappingToGround;

        /// <summary>
        ///     Indicates the KCC is stepping up.
        /// </summary>
        public bool IsSteppingUp;

        /// <summary>
        ///     Counter in how many frames the JumpImpulse was applied in a row.
        ///     Fixed update have 1 at max. In render update value higher than 1 indicates that jump happened in earlier render
        ///     frame and should be processed next fixed update as well.
        /// </summary>
        public int JumpFrames;

        /// <summary>
        ///     One-time world space jump impulse based on input.
        /// </summary>
        public Vector3 JumpImpulse;

        /// <summary>
        ///     Desired kinematic direction, based on <c>InputDirection</c> and other factors, used to calculate
        ///     <c>KinematicVelocity</c>.
        /// </summary>
        public Vector3 KinematicDirection;

        /// <summary>
        ///     Speed used to calculate <c>KinematicVelocity</c>.
        /// </summary>
        public float KinematicSpeed;

        /// <summary>
        ///     Calculated kinematic tangent, based on <c>KinematicDirection</c> and affected by other factors like ground normal,
        ///     used to calculate <c>KinematicVelocity</c>.
        /// </summary>
        public Vector3 KinematicTangent;

        /// <summary>
        ///     Velocity calculated from <c>InputDirection</c>, <c>KinematicDirection</c>, <c>KinematicTangent</c> and
        ///     <c>KinematicSpeed</c>.
        /// </summary>
        public Vector3 KinematicVelocity;

        /// <summary>
        ///     Maximum angle between KCC up direction and ground normal (depenetration vector) in degrees. Valid range is &lt;0,
        ///     90&gt;. Default is 75.
        /// </summary>
        public float MaxGroundAngle;

        /// <summary>
        ///     Maximum angle between KCC up direction and hang surface (perpendicular to depenetration vector) in degrees. Valid
        ///     range is &lt;MaxWallAngle, 90&gt; Default is 30.
        /// </summary>
        public float MaxHangAngle;

        /// <summary>
        ///     Single Move/CCD step is split into multiple smaller sub-steps which results in higher overall depenetration
        ///     quality.
        /// </summary>
        public int MaxPenetrationSteps;

        /// <summary>
        ///     Maximum angle between KCC up direction and wall surface (perpendicular to depenetration vector) in degrees. Valid
        ///     range is &lt;0, MaxGroundAngle&gt; Default is 5.
        /// </summary>
        public float MaxWallAngle;

        /// <summary>
        ///     Speed calculated from real position change.
        /// </summary>
        public float RealSpeed;

        /// <summary>
        ///     Velocity calculated from real position change.
        /// </summary>
        public Vector3 RealVelocity;

        /// <summary>
        ///     Calculated or explicitly set position which is propagated to <c>Transform</c>.
        /// </summary>
        public Vector3 TargetPosition;

        /// <summary>
        ///     Tick number, equals to <c>Simulation.Tick</c> or calculated fixed update frame count.
        /// </summary>
        public int Tick;

        /// <summary>
        ///     Current time, equals to <c>NetworkRunner.SimulationTime</c> or <c>NetworkRunner.SimulationRenderTime</c> or
        ///     variable if CCD is active.
        /// </summary>
        public float Time;

        /// <summary>
        ///     Delta time of full update/tick (CCD independent).
        ///     <list type="number">
        ///         <item>
        ///             <description>FixedUpdate => FixedUpdate</description>
        ///         </item>
        ///         <item>
        ///             <description>FixedUpdate => Render</description>
        ///         </item>
        ///         <item>
        ///             <description>Render => Render</description>
        ///         </item>
        ///     </list>
        /// </summary>
        public float UpdateDeltaTime;

        /// <summary>
        ///     Same as IsGrounded previous tick or physics query.
        /// </summary>
        public bool WasGrounded;

        /// <summary>
        ///     Same as IsSnappingToGround previous tick or physics query.
        /// </summary>
        public bool WasSnappingToGround;

        /// <summary>
        ///     Same as IsSteppingUp previous tick or physics query.
        /// </summary>
        public bool WasSteppingUp;

        /// <summary>
        ///     Explicitly set look pitch rotation, this should propagate to camera rotation.
        /// </summary>
        public float LookPitch
        {
            get => _lookPitch;
            set
            {
                if (_lookPitch != value)
                {
                    _lookPitch = value;
                    _lookRotationCalculated = false;
                    _lookDirectionCalculated = false;
                }
            }
        }

        /// <summary>
        ///     Explicitly set look yaw rotation, this should propagate to camera and transform rotation.
        /// </summary>
        public float LookYaw
        {
            get => _lookYaw;
            set
            {
                if (_lookYaw != value)
                {
                    _lookYaw = value;
                    _lookRotationCalculated = false;
                    _lookDirectionCalculated = false;
                    _transformRotationCalculated = false;
                    _transformDirectionCalculated = false;
                }
            }
        }

        /// <summary>
        ///     Combination of <c>LookPitch</c> and <c>LookYaw</c>.
        /// </summary>
        public Quaternion LookRotation
        {
            get
            {
                if (_lookRotationCalculated == false)
                {
                    _lookRotation = Quaternion.Euler(_lookPitch, _lookYaw, 0.0f);
                    _lookRotationCalculated = true;
                }

                return _lookRotation;
            }
        }

        /// <summary>
        ///     Calculated and cached look direction based on <c>LookRotation</c>.
        /// </summary>
        public Vector3 LookDirection
        {
            get
            {
                if (_lookDirectionCalculated == false)
                {
                    _lookDirection = LookRotation * Vector3.forward;
                    _lookDirectionCalculated = true;
                }

                return _lookDirection;
            }
        }

        /// <summary>
        ///     Calculated and cached transform rotation based on Yaw look rotation.
        /// </summary>
        public Quaternion TransformRotation
        {
            get
            {
                if (_transformRotationCalculated == false)
                {
                    _transformRotation = Quaternion.Euler(0.0f, _lookYaw, 0.0f);
                    _transformRotationCalculated = true;
                }

                return _transformRotation;
            }
        }

        /// <summary>
        ///     Calculated and cached transform direction based on <c>TransformRotation</c>.
        /// </summary>
        public Vector3 TransformDirection
        {
            get
            {
                if (_transformDirectionCalculated == false)
                {
                    _transformDirection = TransformRotation * Vector3.forward;
                    _transformDirectionCalculated = true;
                }

                return _transformDirection;
            }
        }

        /// <summary>
        ///     Final calculated velocity used for position change, combined <c>KinematicVelocity</c> and <c>DynamicVelocity</c>.
        /// </summary>
        public Vector3 DesiredVelocity => KinematicVelocity + DynamicVelocity;

        /// <summary>
        ///     Flag that indicates KCC has jumped in current tick/frame.
        /// </summary>
        public bool HasJumped => JumpFrames == 1;

        /// <summary>
        ///     Indicates the KCC temporarily or permanently lost grounded state.
        /// </summary>
        public bool IsOnEdge => IsGrounded == false && WasGrounded;

        // PUBLIC METHODS

        /// <summary>
        ///     Returns the look rotation vector with selected axes.
        /// </summary>
        public Vector2 GetLookRotation(bool pitch = true, bool yaw = true)
        {
            Vector2 lookRotation = default;

            if (pitch) lookRotation.x = _lookPitch;
            if (yaw) lookRotation.y = _lookYaw;

            return lookRotation;
        }

        /// <summary>
        ///     Add pitch and yaw look rotation. Resulting values are clamped to &lt;-90, 90&gt; (pitch) and &lt;-180, 180&gt;
        ///     (yaw).
        ///     Changes are not propagated to Transform component.
        /// </summary>
        public void AddLookRotation(float pitchDelta, float yawDelta)
        {
            if (pitchDelta != 0.0f) LookPitch = Mathf.Clamp(LookPitch + pitchDelta, -90.0f, 90.0f);

            if (yawDelta != 0.0f)
            {
                var lookYaw = LookYaw + yawDelta;
                while (lookYaw > 180.0f) lookYaw -= 360.0f;
                while (lookYaw < -180.0f) lookYaw += 360.0f;

                LookYaw = lookYaw;
            }
        }

        /// <summary>
        ///     Add pitch and yaw look rotation. Resulting values are clamped to &lt;minPitch, maxPitch&gt; (pitch) and &lt;-180,
        ///     180&gt; (yaw).
        ///     Changes are not propagated to Transform component.
        /// </summary>
        public void AddLookRotation(float pitchDelta, float yawDelta, float minPitch, float maxPitch)
        {
            if (pitchDelta != 0.0f)
            {
                if (minPitch < -90.0f) minPitch = -90.0f;
                if (maxPitch > 90.0f) maxPitch = 90.0f;

                if (maxPitch < minPitch) maxPitch = minPitch;

                LookPitch = Mathf.Clamp(LookPitch + pitchDelta, minPitch, maxPitch);
            }

            if (yawDelta != 0.0f)
            {
                var lookYaw = LookYaw + yawDelta;
                while (lookYaw > 180.0f) lookYaw -= 360.0f;
                while (lookYaw < -180.0f) lookYaw += 360.0f;

                LookYaw = lookYaw;
            }
        }

        /// <summary>
        ///     Add pitch (x) and yaw (y) look rotation. Resulting values are clamped to &lt;-90, 90&gt; (pitch) and &lt;-180, 180
        ///     &gt; (yaw).
        ///     Changes are not propagated to Transform component.
        /// </summary>
        public void AddLookRotation(Vector2 lookRotationDelta)
        {
            AddLookRotation(lookRotationDelta.x, lookRotationDelta.y);
        }

        /// <summary>
        ///     Add pitch (x) and yaw (y) look rotation. Resulting values are clamped to &lt;minPitch, maxPitch&gt; (pitch) and
        ///     &lt;-180, 180&gt; (yaw).
        ///     Changes are not propagated to Transform component.
        /// </summary>
        public void AddLookRotation(Vector2 lookRotationDelta, float minPitch, float maxPitch)
        {
            AddLookRotation(lookRotationDelta.x, lookRotationDelta.y, minPitch, maxPitch);
        }

        /// <summary>
        ///     Set pitch and yaw look rotation. Values are clamped to &lt;-90, 90&gt; (pitch) and &lt;-180, 180&gt; (yaw).
        ///     Changes are not propagated to Transform component.
        /// </summary>
        public void SetLookRotation(float pitch, float yaw)
        {
            KCCUtility.ClampLookRotationAngles(ref pitch, ref yaw);

            LookPitch = pitch;
            LookYaw = yaw;
        }

        /// <summary>
        ///     Set pitch and yaw look rotation. Roll is ignored (not supported). Values are clamped to &lt;-90, 90&gt; (pitch) and
        ///     &lt;-180, 180&gt; (yaw).
        ///     Changes are not propagated to Transform component.
        /// </summary>
        public void SetLookRotation(Quaternion lookRotation, bool preservePitch = false, bool preserveYaw = false)
        {
            KCCUtility.GetClampedLookRotationAngles(lookRotation, out var pitch, out var yaw);

            if (preservePitch == false) LookPitch = pitch;
            if (preserveYaw == false) LookYaw = yaw;
        }

        /// <summary>
        ///     Clear all properties which should not propagate to next tick/frame if IsActive == false.
        /// </summary>
        public void ClearTransientProperties()
        {
            JumpFrames = default;
            JumpImpulse = default;
            ExternalVelocity = default;
            ExternalAcceleration = default;
            ExternalImpulse = default;
            ExternalForce = default;

            ClearUserTransientProperties();
        }

        public void Clear()
        {
            ClearUserData();

            Collisions.Clear();
            Modifiers.Clear();
            Ignores.Clear();
            Hits.Clear();
        }

        public void CopyFromOther(KCCData other)
        {
            Frame = other.Frame;
            Tick = other.Tick;
            Alpha = other.Alpha;
            Time = other.Time;
            DeltaTime = other.DeltaTime;
            UpdateDeltaTime = other.UpdateDeltaTime;
            IsActive = other.IsActive;
            BasePosition = other.BasePosition;
            DesiredPosition = other.DesiredPosition;
            TargetPosition = other.TargetPosition;
            LookPitch = other.LookPitch;
            LookYaw = other.LookYaw;

            InputDirection = other.InputDirection;
            JumpImpulse = other.JumpImpulse;
            Gravity = other.Gravity;
            MaxGroundAngle = other.MaxGroundAngle;
            MaxWallAngle = other.MaxWallAngle;
            MaxHangAngle = other.MaxHangAngle;
            MaxPenetrationSteps = other.MaxPenetrationSteps;
            ExternalVelocity = other.ExternalVelocity;
            ExternalAcceleration = other.ExternalAcceleration;
            ExternalImpulse = other.ExternalImpulse;
            ExternalForce = other.ExternalForce;
            ExternalDelta = other.ExternalDelta;

            KinematicSpeed = other.KinematicSpeed;
            KinematicTangent = other.KinematicTangent;
            KinematicDirection = other.KinematicDirection;
            KinematicVelocity = other.KinematicVelocity;
            DynamicVelocity = other.DynamicVelocity;

            RealSpeed = other.RealSpeed;
            RealVelocity = other.RealVelocity;
            JumpFrames = other.JumpFrames;
            HasTeleported = other.HasTeleported;
            IsGrounded = other.IsGrounded;
            WasGrounded = other.WasGrounded;
            IsSteppingUp = other.IsSteppingUp;
            WasSteppingUp = other.WasSteppingUp;
            IsSnappingToGround = other.IsSnappingToGround;
            WasSnappingToGround = other.WasSnappingToGround;
            GroundNormal = other.GroundNormal;
            GroundTangent = other.GroundTangent;
            GroundPosition = other.GroundPosition;
            GroundDistance = other.GroundDistance;
            GroundAngle = other.GroundAngle;

            Collisions.CopyFromOther(other.Collisions);
            Modifiers.CopyFromOther(other.Modifiers);
            Ignores.CopyFromOther(other.Ignores);
            Hits.CopyFromOther(other.Hits);

            CopyUserDataFromOther(other);
        }

        // PARTIAL METHODS

        partial void ClearUserTransientProperties();
        partial void ClearUserData();
        partial void CopyUserDataFromOther(KCCData other);
    }
}