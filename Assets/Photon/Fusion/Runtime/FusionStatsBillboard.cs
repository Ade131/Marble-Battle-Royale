using UnityEngine;

namespace Fusion
{
  /// <summary>
  ///     Companion component for <see cref="FusionStats" />, which automatically faces this GameObject toward the supplied
  ///     Camera. If Camera == null, will face towards Camera.main.
  /// </summary>
  [ScriptHelp(BackColor = ScriptHeaderBackColor.Olive)]
    [ExecuteAlways]
    public class FusionStatsBillboard : Behaviour
    {
        // Camera find is expensive, so do it once per update for ALL implementations
        private static float _lastCameraFindTime;
        private static Camera _currentCam;

        /// <summary>
        ///     Force a particular camera to billboard this object toward. Leave null to use Camera.main.
        /// </summary>
        [InlineHelp] public Camera Camera;

        private FusionStats _fusionStats;

        private Camera MainCamera
        {
            set => _currentCam = value;
            get
            {
                var time = Time.time;
                // Only look for the camera once per Update.
                if (time == _lastCameraFindTime)
                    return _currentCam;

                _lastCameraFindTime = time;
                var cam = Camera.main;
                _currentCam = cam;
                return cam;
            }
        }

        private void Awake()
        {
            _fusionStats = GetComponent<FusionStats>();
        }

        private void LateUpdate()
        {
            UpdateLookAt();
        }

        private void OnEnable()
        {
            UpdateLookAt();
        }

        private void OnDisable()
        {
            transform.localRotation = default;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            LateUpdate();
        }
#endif

        public void UpdateLookAt()
        {
            // Save the CPU here if our FusionStats is in overlay. Billboarding does nothing.
            if (_fusionStats && _fusionStats.CanvasType == FusionStats.StatCanvasTypes.Overlay) return;

            var cam = Camera ? Camera : MainCamera;

            if (cam)
                if (enabled)
                    //var armOffset = transform.position - cam.transform.position;
                    //if (_canvasT == null) {
                    //  _canvasT = GetComponentInChildren<Canvas>()?.transform;
                    //  if (_canvasT) {
                    //    _canvasT.localPosition = Offset;
                    //  }
                    //} else {
                    //  _canvasT.localPosition = Offset;
                    //}
                    transform.rotation = cam.transform.rotation;
            //transform.LookAt(transform.position + armOffset, cam.transform.up);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _currentCam = default;
            _lastCameraFindTime = default;
        }
    }
}