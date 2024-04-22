using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fusion.Addons.KCC
{
    /// <summary>
    ///     Base settings for <c>KCC</c>, can be modified at runtime.
    /// </summary>
    [Serializable]
    public sealed partial class KCCSettings
    {
        // CONSTANTS

        public static readonly int MaxNestedStages = 32;
        public static readonly float ExtrapolationDeltaTimeThreshold = 0.00005f;

        // PUBLIC MEMBERS

        [Header("Networked Settings")]
        [Tooltip("Defines KCC physics behavior.\n" +
                 "• None - Skips internal physics query, collider is despawned.\n" +
                 "• Capsule - Full physics processing, Capsule collider spawned.")]
        public EKCCShape Shape = EKCCShape.Capsule;

        [Tooltip("Sets collider isTrigger.")] public bool IsTrigger;

        [Tooltip("Sets collider radius.")] public float Radius = 0.35f;

        [Tooltip("Sets collider height.")] public float Height = 1.8f;

        [Tooltip(
            "Defines additional radius extent for ground detection and processors tracking. Recommended range is 10-20% of radius.\n" +
            "• Low value decreases stability and has potential performance impact when executing additional checks.\n" +
            "• High value increases stability at the cost of increased sustained performance impact.")]
        public float Extent = 0.035f;

        [Tooltip("Sets layer of collider game object.")] [KCCLayer]
        public int ColliderLayer;

        [Tooltip("Layer mask the KCC collides with.")]
        public LayerMask CollisionLayerMask = 1;

        [Tooltip("Default KCC features.")] public EKCCFeatures Features = EKCCFeatures.All;

        [Tooltip(
            "Defines update behavior for KCC with input authority. This has priority over State Authority Behavior\n" +
            "• Predict Fixed | Interpolate Render - Full processing/prediction in fixed update, interpolation between last two predicted fixed update states in render update.\n" +
            "• Predict Fixed | Predict Render - Full processing/prediction in fixed update, full processing/prediction in render update.")]
        public EKCCAuthorityBehavior InputAuthorityBehavior = EKCCAuthorityBehavior.PredictFixed_InterpolateRender;

        [Tooltip("Defines update behavior for KCC with state authority.\n" +
                 "• Predict Fixed | Interpolate Render - Full processing/prediction in fixed update, interpolation between last two predicted fixed update states in render update.\n" +
                 "• Predict Fixed | Predict Render - Full processing/prediction in fixed update, full processing/prediction in render update.")]
        public EKCCAuthorityBehavior StateAuthorityBehavior = EKCCAuthorityBehavior.PredictFixed_InterpolateRender;

        [Tooltip("Defines interpolation behavior. Proxies predicted in fixed update are always fully interpolated.\n" +
                 "• Full - Interpolates all networked properties in KCCSettings and KCCData, synchronizes Transform and Rigidbody components everytime interpolation is triggered.\n" +
                 "• Transform - Interpolates only position and rotation and synchronizes Transform component. This mode is fastest, but most KCCSettings and KCCData properties won't be synchronized. Use with caution!")]
        public EKCCInterpolationMode ProxyInterpolationMode = EKCCInterpolationMode.Full;

        [Tooltip("Allows input authority to call Teleport RPC. Use with caution.")]
        public bool AllowClientTeleports;

        [Header("Local Settings")]
        [KCCProcessorReference]
        [Tooltip("Default processors, propagated to KCC.LocalProcessors upon initialization.")]
        public Object[] Processors;

        [Tooltip(
            "Defines minimum distance the KCC must move in a single tick to treat the movement as instant (teleport). Affects interpolation and other KCC features.")]
        public float TeleportThreshold = 1.0f;

        [Tooltip(
            "Single Move/CCD step is split into multiple smaller sub-steps which results in higher overall depenetration quality.")]
        [Range(1, 16)]
        public int MaxPenetrationSteps = 8;

        [Tooltip(
            "Controls maximum distance the KCC moves in a single CCD step. Valid range is 25% - 75% of the radius. Use lower values if the character passes through geometry.\n" +
            "This setting is valid only when EKCCFeature.CCD is enabled. CCD Max Step Distance = Radius * CCD Radius Multiplier")]
        [Range(0.25f, 0.75f)]
        public float CCDRadiusMultiplier = 0.75f;

        [Tooltip(
            "Defines render position distance tolerance to smooth out jitter. Higher values may introduce noticeable delay when switching move direction.\n" +
            "• X = Horizontal axis.\n" +
            "• Y = Vertical axis.")]
        public Vector2 AntiJitterDistance = new(0.025f, 0.01f);

        [Tooltip("How fast prediction error interpolates towards zero.")]
        public float PredictionCorrectionSpeed = 30.0f;

        [Tooltip(
            "Maximum interactions synchronized over network. Includes Collisions, Modifiers and Ignores from KCCData.")]
        public int NetworkedInteractions = 8;

        [Tooltip(
            "Reduces network traffic by synchronizing compressed position at the cost of precision. Useful for non-player characters.")]
        public bool CompressNetworkPosition;

        [Tooltip(
            "Perform single overlap query during move. Hits are tracked on position before depenetration. This is a performance optimization for non-player characters at the cost of possible errors in movement.")]
        public bool ForceSingleOverlapQuery;

        [Tooltip(
            "This skips look rotation interpolation in render for local character and can be used for extra look responsiveness with other properties being interpolated.")]
        public bool ForcePredictedLookRotation;

        [Tooltip(
            "Enable to always check collisions against non-convex mesh colliders to prevent ghost collisions and incorrect penetration vectors.")]
        public bool SuppressConvexMeshColliders;


        // PUBLIC METHODS

        public void CopyFromOther(KCCSettings other)
        {
            Shape = other.Shape;
            IsTrigger = other.IsTrigger;
            Radius = other.Radius;
            Height = other.Height;
            Extent = other.Extent;
            ColliderLayer = other.ColliderLayer;
            CollisionLayerMask = other.CollisionLayerMask;
            Features = other.Features;
            InputAuthorityBehavior = other.InputAuthorityBehavior;
            StateAuthorityBehavior = other.StateAuthorityBehavior;
            ProxyInterpolationMode = other.ProxyInterpolationMode;
            AllowClientTeleports = other.AllowClientTeleports;

            if (other.Processors != null && other.Processors.Length > 0)
            {
                Processors = new Object[other.Processors.Length];
                Array.Copy(other.Processors, Processors, Processors.Length);
            }
            else
            {
                Processors = null;
            }

            TeleportThreshold = other.TeleportThreshold;
            MaxPenetrationSteps = other.MaxPenetrationSteps;
            CCDRadiusMultiplier = other.CCDRadiusMultiplier;
            AntiJitterDistance = other.AntiJitterDistance;
            PredictionCorrectionSpeed = other.PredictionCorrectionSpeed;
            NetworkedInteractions = other.NetworkedInteractions;
            CompressNetworkPosition = other.CompressNetworkPosition;
            ForceSingleOverlapQuery = other.ForceSingleOverlapQuery;
            ForcePredictedLookRotation = other.ForcePredictedLookRotation;
            SuppressConvexMeshColliders = other.SuppressConvexMeshColliders;

            CopyUserSettingsFromOther(other);
        }

        // PARTIAL METHODS

        partial void CopyUserSettingsFromOther(KCCSettings other);
    }
}