using System;
using UnityEngine;

namespace Fusion.Addons.KCC
{
    // This file contains remote procedure calls.
    public partial class KCC
    {
        // PUBLIC METHODS

        /// <summary>
        ///     Teleport to a specific position with look rotation and immediately synchronize Transform.
        ///     This RPC is for input authority only, state authority should use <c>SetPosition()</c> and <c>SetLookRotation()</c>
        ///     instead.
        ///     <c>KCCSettings.AllowClientTeleports</c> must be set to <c>true</c> for this to work.
        /// </summary>
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void TeleportRPC(Vector3 position, float lookPitch, float lookYaw)
        {
            if (_settings.AllowClientTeleports == false)
                throw new InvalidOperationException(
                    $"{nameof(KCCSettings)}.{nameof(KCCSettings.AllowClientTeleports)} must be enabled to use {nameof(KCC)}.{nameof(TeleportRPC)}().");

            KCCUtility.ClampLookRotationAngles(ref lookPitch, ref lookYaw);

            RenderData.BasePosition = position;
            RenderData.DesiredPosition = position;
            RenderData.TargetPosition = position;
            RenderData.HasTeleported = true;
            RenderData.IsSteppingUp = false;
            RenderData.IsSnappingToGround = false;
            RenderData.LookPitch = lookPitch;
            RenderData.LookYaw = lookYaw;

            FixedData.BasePosition = position;
            FixedData.DesiredPosition = position;
            FixedData.TargetPosition = position;
            FixedData.HasTeleported = true;
            FixedData.IsSteppingUp = false;
            FixedData.IsSnappingToGround = false;
            FixedData.LookPitch = lookPitch;
            FixedData.LookYaw = lookYaw;

            SynchronizeTransform(FixedData, true, true, false);
        }
    }
}