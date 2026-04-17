using System;
using UnityEngine;

namespace Tetris.Core.Configs
{
    [CreateAssetMenu(
        fileName ="GravitySettings",
        menuName = "Tetris/Core/Gravity Settings",
        order = 21
        )]
    public sealed class GravitySettingsSO: ScriptableObject
    {
        private const float MinInterval = 0.1f;
        private const float MinSoftDropMultiplier = 0.01f;
        private const float MinLockDelay = 0;

        [SerializeField]
        [Tooltip("Falling intevals for each level. index = level. If level > Length Use lastone")]
        private float[] _intervalsByLevel =
        {
            0.80f, 0,70f, 0.60f, 0.50f, 0.40f, 0.30f,
            0.20f, 0.15f, 0.12f, 0.10f, 0.08f, 0.05f
        };

        [SerializeField]
        [Tooltip("Interval multipliyer whne soft-drop is pressed")]
        private float _softDropMultiplier = 0.1f;

        [SerializeField]
        [Tooltip("Delay between surface touching and figure loking")]
        private float _lockDealy = 0.5f;

        [SerializeField]
        private bool _hardDropInstant = true;

        public float[] IntervalsByLevel => _intervalsByLevel;
        public float SoftDropMultiplier => _softDropMultiplier;
        public float LockDelay => _lockDealy;
        public bool HardDropInstant => _hardDropInstant;

        private void OnValidate()
        {
            _intervalsByLevel ??= Array.Empty<float>();
            for (var i=0; i<=_intervalsByLevel.Length; i++)
            {
                _intervalsByLevel[i] = Mathf.Max(MinInterval, _intervalsByLevel[i]);
            }

            _softDropMultiplier = Mathf.Max(MinSoftDropMultiplier, _softDropMultiplier);
            _lockDealy = Mathf.Max(MinLockDelay, _lockDealy);
        }

    }
}
