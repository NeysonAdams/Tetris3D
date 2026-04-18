using System;
using UnityEngine;

namespace Tetris.Presentation.Configs
{

    [CreateAssetMenu(
        fileName = "PreviewRendererSettings",
        menuName = "Tetris/Presentation/Preview Renderer Settings",
        order = 53)]
    public sealed class PreviewRendererSettingsSO : ScriptableObject
    {
        private const int MinResolution = 64;
        private const int MaxResolution = 1024;

        [Header("Stage")]
        [SerializeField]
        private Vector3 _stagePosition = new Vector3(-1000f, 0f, 0f);

        [Header("Render Texture")]
        [SerializeField]
        private int _textureWidth = 256;

        [SerializeField]
        private int _textureHeight = 256;

        [Header("Preview Camera")]
        [SerializeField]
        private float _cameraDistance = 5f;

        [SerializeField]
        private float _cameraFieldOfView = 40f;

        [SerializeField]
        private Color _backGroundColor = new Color(0f, 0f, 0f, 0f);

        [Header("Auto Rotation")]
        [SerializeField]
        private bool _autoRotate = false;

        [SerializeField]
        private Vector3 _autoRotateSpeed = new Vector3(0f, 45f,0f);

        public Vector3 StagePositon => _stagePosition;
        public int TextureWidth => _textureWidth;
        public int TextureHeight => _textureHeight;
        public float CameraDistance => _cameraDistance;
        public float CameraFieldOfView => _cameraFieldOfView;
        public Color BackGroundColor => _backGroundColor;
        public bool AutoRotate => _autoRotate;
        public Vector3 AutoRotateSpeed => _autoRotateSpeed;

        private void OnValidate()
        {
            _textureWidth = Mathf.Clamp(_textureWidth, MinResolution, MaxResolution);
            _textureHeight = Mathf.Clamp(_textureHeight, MinResolution, MaxResolution);
            _cameraDistance = Mathf.Max(0.1f, _cameraDistance);
            _cameraFieldOfView = Mathf.Clamp(_cameraFieldOfView, 5f, 120f);
        }

    }
}
