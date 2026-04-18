using DG.Tweening;
using Tetris.Core.Events;
using Tetris.Core.Tetrominoes;
using Tetris.Presentation.Configs;
using UnityEngine;
using System;


namespace Tetris.Presentation.Views
{
    public sealed class OrbitalCamera : MonoBehaviour
    {
        private GameEvents _events = default!;
        private CameraSettingsSO _cameraSettings = default!;
        private DOTweenAnimationSettingsSO _animationSettings = default!;

        private float _yaw;
        private float _pitch;
        private float _distance;
        private Vector3 _target;

        private Vector3 _shakeOffset;
        private Tween? _shakeTween;

        private bool _isInitialized;

        private float _shakeRemainingTime;
        private float _shakeStrength;

        public void Initialize(
            GameEvents events,
            CameraSettingsSO cameraSettings,
            DOTweenAnimationSettingsSO animationSettings,
            Vector3 target)
        {
            if (_isInitialized) throw new InvalidOperationException("OrbitalCamera already initialized.");

            if (events == null) throw new System.ArgumentNullException(nameof(events));
            if (cameraSettings == null) throw new System.ArgumentNullException(nameof(cameraSettings));
            if (animationSettings == null) throw new System.ArgumentNullException(nameof(animationSettings));

            _events = events;
            _cameraSettings = cameraSettings;
            _animationSettings = animationSettings;
            _target = target + cameraSettings.TargetOffset;

            _yaw = cameraSettings.InitYaw;
            _pitch = cameraSettings.InitPitch;
            _distance = cameraSettings.InitDistance;
            _shakeOffset = Vector3.zero;

            _events.PieceHardDropped += HandlePieceHardDropped;

            ApplyTransformImmediate();

            _isInitialized = true;

        }

        private void OnDestroy()
        {
            if (_isInitialized)
            {
                _events.PieceHardDropped -= HandlePieceHardDropped;
            }

            _shakeTween?.Kill();
        }

        private void Update()
        {
            if (!_isInitialized) return;

            ReadInput();
            UpdateTransform();
        }

        private void ReadInput() 
        {
            if (Input.GetMouseButton(1))
            {
                var deltaX = Input.GetAxis("Mouse X");
                var deltaY = Input.GetAxis("Mouse Y");

                _yaw += deltaX * _cameraSettings.RotationSenditivity * 60f;
                _pitch -= deltaY * _cameraSettings.RotationSenditivity * 60f;

                _pitch = Mathf.Clamp(_pitch, _cameraSettings.MinPitch, _cameraSettings.MaxPitch);
            }

            var wheel = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(wheel) > 0.001f)
            {
                _distance -= wheel * _cameraSettings.ZoomSensitivity * 10f;
                _distance = Mathf.Clamp(_distance, _cameraSettings.MinDistance, _cameraSettings.MaxDistance);
            }
        }
        private void UpdateTransform() 
        {
            UpdateShake();

            var offset = ComputeOrbitOffset(_yaw, _pitch, _distance);
            var targetPosition = _target + offset + _shakeOffset;

            var damping = _cameraSettings.DampingFactor;
            transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - damping);

            transform.LookAt(_target + _shakeOffset);
        }

        private void UpdateShake()
        {
            if (_shakeRemainingTime <= 0f)
            {
                if (_shakeOffset != Vector3.zero)
                {
                    _shakeOffset = Vector3.zero;
                }
                return;
            }

            _shakeRemainingTime -= Time.deltaTime;

            var decay = Mathf.Clamp01(_shakeRemainingTime / _animationSettings.HardDropShakeDuration);
            _shakeOffset = UnityEngine.Random.insideUnitSphere * _shakeStrength * decay;

            if (_shakeRemainingTime <= 0f)
            {
                _shakeOffset = Vector3.zero;
            }
        }

        private void HandlePieceHardDropped(Piece piece, int cellsDropped) 
        {
            var baseStrength = _animationSettings.HardDropShakeStrength;
            var cellsFactor = Mathf.Clamp(cellsDropped / 10f, 0.3f, 2f);

            _shakeStrength = baseStrength * cellsFactor;
            _shakeRemainingTime = _animationSettings.HardDropShakeDuration;

        }

        private void ApplyTransformImmediate()
        {
            var offset = ComputeOrbitOffset(_yaw, _pitch, _distance);
            transform.position = _target + offset;
            transform.LookAt(_target);
        }

        private static Vector3 ComputeOrbitOffset(float yaw, float pitch, float distance)
        {
            var yawRad = yaw * Mathf.Deg2Rad;
            var pitchRad = pitch * Mathf.Deg2Rad;

            var x = distance * Mathf.Cos(pitchRad) * Mathf.Sin(yawRad);
            var y = distance * Mathf.Sin(pitchRad);
            var z = distance * Mathf.Cos(pitchRad) * Mathf.Cos(yawRad);

            return new Vector3(x, y, z);
        }
    }
}
