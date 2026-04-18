using UnityEngine;

namespace Tetris.Presentation.Configs
{

    [CreateAssetMenu(
        fileName = "FieldLayoutSettings",
        menuName = "Tetris/Presentation/Field Layout Settings",
        order = 50)]
    public class FieldLayoutSettingsSO : ScriptableObject
    {
        private const float MinBlockSize = 0.1f;

        [SerializeField]
        private float _blockSize = 1f;

        [SerializeField]
        private Vector3 _worldOrigin = Vector3.zero;

        public float BlockSize => _blockSize;
        public Vector3 WorldOrigin => _worldOrigin;

        private void OnValidate()
        {
            _blockSize = Mathf.Max(MinBlockSize, _blockSize);
        }
    }
}