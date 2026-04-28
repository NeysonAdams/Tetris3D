using UnityEngine;

namespace Tetris.Presentation.Configs
{
    [CreateAssetMenu(
        fileName = "CameraSettings", 
        menuName = "Tetris/Presentation/Camera Settings",
        order = 52)]
    public sealed class CameraSettingsSO : ScriptableObject
    {
        [Header("Initial Orbit")]
        [SerializeField]
        [Tooltip ("Horizontal Rotataing Angle")]
        private float _intialYaw = 30f;

        [SerializeField]
        private float _initialPitch = 25f;

        [SerializeField]
        private float _initialDistance = 15f;

        [Header("Input Sensivity")]
        [SerializeField]
        private float _rotationSensitivity = 0.3f;

        [SerializeField]
        private float _zoomSensitivity = 2f;

        [Header("Constrains")]
        [SerializeField]
        private float _minPitch = -80f;

        [SerializeField]
        private float _maxPitch = 80f;

        [SerializeField]
        private float _minDistance = 5f;

        [SerializeField]
        private float _maxDistance = 20f;

        [Header("Smoothing")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _dampingFactor = 0.15f;

        [Header("Target")]
        [SerializeField]
        private Vector3 _targetOffset = Vector3.zero;

        //Initial orbit
        public float InitYaw => _intialYaw;
        public float InitPitch => _initialPitch;
        public float InitDistance => _initialDistance;

        //Sensitivity
        public float RotationSenditivity => _rotationSensitivity;
        public float ZoomSensitivity => _zoomSensitivity;

        //Constrain
        public float MinPitch => _minPitch;
        public float MinDistance => _minDistance;
        public float MaxPitch => _maxPitch;
        public float MaxDistance => _maxDistance;

        //Smoothing
        public float DampingFactor => _dampingFactor;

        //Target 
        public Vector3 TargetOffset => _targetOffset;

        private void OnValidate()
        {
            _maxPitch = Mathf.Max(_minPitch + 1f, _maxPitch);
            _maxDistance = Mathf.Max(_minDistance + 1f, _maxDistance);

            _initialPitch = Mathf.Clamp(_initialPitch, _minPitch, _maxPitch);
            _initialDistance = Mathf.Clamp(InitDistance, _minDistance, _maxDistance);

            _rotationSensitivity = Mathf.Max(0.01f, _rotationSensitivity);
            _zoomSensitivity = Mathf.Max(0.01f, _zoomSensitivity);
        }

    }
}
