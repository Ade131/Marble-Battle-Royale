using System;

namespace Fusion.Addons.KCC
{
	/// <summary>
	///     Defines update behavior for KCC with input and state authority.
	///     <list type="bullet">
	///         <item>
	///             <description>
	///                 Predict Fixed | Interpolate Render - Full processing/prediction in fixed update, interpolation
	///                 between last two predicted fixed update states in render update.
	///             </description>
	///         </item>
	///         <item>
	///             <description>
	///                 Predict Fixed | Predict Render - Full processing/prediction in fixed update, full
	///                 processing/prediction in render update.
	///             </description>
	///         </item>
	///     </list>
	/// </summary>
	public enum EKCCAuthorityBehavior
    {
        PredictFixed_InterpolateRender = 0,
        PredictFixed_PredictRender = 1
    }

	/// <summary>
	///     Defines interpolation behavior. Objects predicted in fixed update are always fully interpolated.
	///     <list type="bullet">
	///         <item>
	///             <description>
	///                 Full - Interpolates all networked properties in KCCSettings and KCCData, synchronizes
	///                 Transform and Rigidbody components when interpolation is triggered.
	///             </description>
	///         </item>
	///         <item>
	///             <description>
	///                 Transform - Interpolates only position and rotation and synchronizes Transform component. This
	///                 mode is fastest, but KCCSettings and KCCData properties won't be synchronized. Use with caution!
	///             </description>
	///         </item>
	///     </list>
	/// </summary>
	public enum EKCCInterpolationMode
    {
        Full = 0,
        Transform = 1
    }

	/// <summary>
	///     Defines KCC physics behavior.
	///     <list type="bullet">
	///         <item>
	///             <description>None - Skips internal physics query, collider is despawned.</description>
	///         </item>
	///         <item>
	///             <description>Capsule - Full physics processing, Capsule collider spawned.</description>
	///         </item>
	///     </list>
	/// </summary>
	public enum EKCCShape
    {
        None = 0,
        Capsule = 1
    }

    public enum EKCCFeature
    {
        None = 0,
        CCD = 1,
        AntiJitter = 2,
        PredictionCorrection = 3
    }

    [Flags]
    public enum EKCCFeatures
    {
        None = 0,
        CCD = 1 << EKCCFeature.CCD,
        AntiJitter = 1 << EKCCFeature.AntiJitter,
        PredictionCorrection = 1 << EKCCFeature.PredictionCorrection,
        All = -1
    }

    public enum EColliderType
    {
        None = 0,
        Sphere = 1,
        Capsule = 2,
        Box = 3,
        Mesh = 4,
        Terrain = 5
    }

    /// <summary>
    ///     Defines collision type between KCC and overlapping collider surface calculated from depenetration or trigger.
    ///     <list type="bullet">
    ///         <item>
    ///             <description>None - Default.</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Ground - Angle between Up and normalized depenetration vector is between 0 and
    ///                 KCCData.MaxGroundAngle.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Slope - Angle between Up and normalized depenetration vector is between KCCData.MaxGroundAngle
    ///                 and (90 - KCCData.MaxWallAngle).
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Wall - Angle between Back and normalized depenetration vector is between -KCCData.MaxWallAngle
    ///                 and KCCData.MaxWallAngle.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Hang - Angle between Back and normalized depenetration vector is between -30 and
    ///                 -KCCData.MaxWallAngle.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>Top - Angle between Back and normalized depenetration vector is lower than -30.</description>
    ///         </item>
    ///         <item>
    ///             <description>Trigger - Overlapping collider - trigger. Penetration is unknown.</description>
    ///         </item>
    ///     </list>
    /// </summary>
    public enum ECollisionType
    {
        None = 0,
        Ground = 1,
        Slope = 1 << 1,
        Wall = 1 << 2,
        Hang = 1 << 3,
        Top = 1 << 4,
        Trigger = 1 << 5
    }

    /// <summary>
    ///     Controls execution of overlap queries when updating collision hits.
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
    /// </summary>
    public enum EKCCHitsOverlapQuery
    {
        Default = 0,
        Reuse = 1,
        New = 2
    }

    public enum EKCCLogType
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    /// <summary>
    ///     Used for interpolation of networked data.
    /// </summary>
    public ref struct KCCInterpolationInfo
    {
        public NetworkBehaviourBuffer FromBuffer;
        public NetworkBehaviourBuffer ToBuffer;
        public int Offset;
        public float Alpha;
    }

    public static class KCCTypes
    {
        public static readonly Type IBeginMove = typeof(IBeginMove);
        public static readonly Type BeginMove = typeof(BeginMove);

        public static readonly Type IPrepareData = typeof(IPrepareData);
        public static readonly Type PrepareData = typeof(PrepareData);

        public static readonly Type IAfterMoveStep = typeof(IAfterMoveStep);
        public static readonly Type AfterMoveStep = typeof(AfterMoveStep);

        public static readonly Type IEndMove = typeof(IEndMove);
        public static readonly Type EndMove = typeof(EndMove);
    }
}