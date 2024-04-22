using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.KCC
{
	/// <summary>
	///     Records <c>KCC</c> and <c>KCCData</c> properties.
	/// </summary>
	[DefaultExecutionOrder(31504)]
    [RequireComponent(typeof(KCC))]
    public class KCCRecorder : StatsRecorder
    {
        // PRIVATE MEMBERS

        private KCC _kcc;
        // PROTECTED MEMBERS

        protected KCC KCC
        {
            get
            {
                if (_kcc == null) _kcc = GetComponent<KCC>();
                return _kcc;
            }
        }

        // StatsRecorder INTERFACE

        protected override void GetHeaders(ERecorderType recorderType, List<string> headers)
        {
            headers.Add("KCC Frame");
            headers.Add("KCC Tick");
            headers.Add("KCC Alpha");
            headers.Add("KCC Time [s]");
            headers.Add("KCC Delta Time [ms]");
            headers.Add("KCC Target Position X");
            headers.Add("KCC Target Position Y");
            headers.Add("KCC Target Position Z");
            headers.Add("KCC Look Pitch");
            headers.Add("KCC Look Yaw");
            headers.Add("KCC Input Direction X");
            headers.Add("KCC Input Direction Y");
            headers.Add("KCC Input Direction Z");
            headers.Add("KCC Kinematic Speed");
            headers.Add("KCC Kinematic Velocity");
            headers.Add("KCC Dynamic Velocity");
            headers.Add("KCC Real Speed");
            headers.Add("KCC Jump Frames");
            headers.Add("KCC Is Grounded");
            headers.Add("KCC Is Stepping Up");
            headers.Add("KCC Is Snapping To Ground");
            headers.Add("KCC Ground Distance");
            headers.Add("KCC Ground Angle");
            headers.Add("KCC Collision Hits");

            if (recorderType != ERecorderType.SimulationTick) headers.Add("KCC Prediction Error");
        }

        protected override bool AddValues(ERecorderType recorderType, StatsWriter writer)
        {
            var data = KCC.Data;
            if (data == null)
                return false;

            writer.Add($"{data.Frame}");
            writer.Add($"{data.Tick}");
            writer.Add($"{data.Alpha:F4}");
            writer.Add($"{data.Time:F4}");
            writer.Add($"{data.DeltaTime * 1000.0f:F4}");
            writer.Add($"{data.TargetPosition.x:F4}");
            writer.Add($"{data.TargetPosition.y:F4}");
            writer.Add($"{data.TargetPosition.z:F4}");
            writer.Add($"{data.LookPitch:F4}");
            writer.Add($"{data.LookYaw:F4}");
            writer.Add($"{data.InputDirection.x:F4}");
            writer.Add($"{data.InputDirection.y:F4}");
            writer.Add($"{data.InputDirection.z:F4}");
            writer.Add($"{data.KinematicSpeed:F4}");
            writer.Add($"{data.KinematicVelocity.magnitude:F4}");
            writer.Add($"{data.DynamicVelocity.magnitude:F4}");
            writer.Add($"{data.RealSpeed:F4}");
            writer.Add($"{data.JumpFrames}");
            writer.Add($"{(data.IsGrounded ? 1 : 0)}");
            writer.Add($"{(data.IsSteppingUp ? 1 : 0)}");
            writer.Add($"{(data.IsSnappingToGround ? 1 : 0)}");
            writer.Add($"{data.GroundDistance:F4}");
            writer.Add($"{data.GroundAngle:F4}");
            writer.Add($"{data.Hits.Count}");

            if (recorderType != ERecorderType.SimulationTick) writer.Add($"{_kcc.PredictionError.magnitude:F4}");

            return true;
        }
    }
}