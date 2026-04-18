using UnityEngine;

namespace Tetris.Input.Configs
{

    [CreateAssetMenu(
        fileName = "KeyRepeatSettings",
        menuName = "Tetris/Input/Key Repeat Settings",
        order = 41)]
    public sealed class KeyRepeatSettingsSO : ScriptableObject
    {
        private const float MinDelay = 0.01f;

        [SerializeField]
        private float _initialDelay = 0.2f;

        [SerializeField]
        private float _repeatInterval = 0.05f;

        public float InitialDElay => _initialDelay;
        public float RepeatInterbal => _repeatInterval;

        private void OnValidate()
        {
            _initialDelay = Mathf.Max(MinDelay, _initialDelay);
            _repeatInterval = Mathf.Max(MinDelay, _repeatInterval);
        }
    }
}
