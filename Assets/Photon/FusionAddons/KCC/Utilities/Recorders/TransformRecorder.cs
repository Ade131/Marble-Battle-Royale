using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.KCC
{
	/// <summary>
	///     Records <c>Transform</c> position and rotation.
	/// </summary>
	[DefaultExecutionOrder(31501)]
    public class TransformRecorder : StatsRecorder
    {
        // StatsRecorder INTERFACE

        protected override void GetHeaders(ERecorderType recorderType, List<string> headers)
        {
            headers.Add($"{name} Position X");
            headers.Add($"{name} Position Y");
            headers.Add($"{name} Position Z");

            headers.Add($"{name} Rotation X");
            headers.Add($"{name} Rotation Y");
            headers.Add($"{name} Rotation Z");
        }

        protected override bool AddValues(ERecorderType recorderType, StatsWriter writer)
        {
            var position = transform.position;
            var rotation = transform.rotation.eulerAngles;

            writer.Add($"{position.x:F4}");
            writer.Add($"{position.y:F4}");
            writer.Add($"{position.z:F4}");

            writer.Add($"{rotation.x:F4}");
            writer.Add($"{rotation.y:F4}");
            writer.Add($"{rotation.z:F4}");

            return true;
        }
    }
}