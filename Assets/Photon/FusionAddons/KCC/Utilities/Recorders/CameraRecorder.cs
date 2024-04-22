using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.KCC
{
	/// <summary>
	///     Records <c>Camera.main</c> position and rotation.
	/// </summary>
	[DefaultExecutionOrder(31502)]
    public class CameraRecorder : StatsRecorder
    {
        // StatsRecorder INTERFACE

        protected override void GetHeaders(ERecorderType recorderType, List<string> headers)
        {
            headers.Add("Camera Position X");
            headers.Add("Camera Position Y");
            headers.Add("Camera Position Z");

            headers.Add("Camera Rotation X");
            headers.Add("Camera Rotation Y");
            headers.Add("Camera Rotation Z");
        }

        protected override bool AddValues(ERecorderType recorderType, StatsWriter writer)
        {
            var camera = Camera.main;
            if (camera == null)
                return false;

            var cameraPosition = camera.transform.position;
            var cameraRotation = camera.transform.rotation.eulerAngles;

            writer.Add($"{cameraPosition.x:F4}");
            writer.Add($"{cameraPosition.y:F4}");
            writer.Add($"{cameraPosition.z:F4}");

            writer.Add($"{cameraRotation.x:F4}");
            writer.Add($"{cameraRotation.y:F4}");
            writer.Add($"{cameraRotation.z:F4}");

            return true;
        }
    }
}