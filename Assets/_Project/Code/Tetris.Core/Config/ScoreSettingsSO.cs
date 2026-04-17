using System;
using UnityEngine;

namespace Tetris.Core.Configs
{
    [CreateAssetMenu(
        fileName = "ScoreSettings",
        menuName = "Tetris/Core/Score Settings",
        order = 20
        )]
    public sealed class ScoreSettingsSO: ScriptableObject
    {
        private const int MinLinesPerLevel = 1;
        private const float MinAnimationDuration = 0;
        private const float MinComboMultiplier = 1f;

        [SerializeField]
        [Tooltip("Score for claring N layers")]
        private int[] _pointsByLayersCleared = { 100, 300, 500, 800 };

        [SerializeField]
        [Tooltip("Multyply Score for without interrpton clearing")]
        private float _comboMultiplier = 2f;

        [SerializeField]
        [Tooltip("How many layers need to clear for getting the next level")]
        private int _linesPerLevel = 10;

        [SerializeField]
        [Tooltip("Pause duration for layer clearing animation")]
        private float _lineClearAnimationDuration = 0.4f;

        public int[] PointByLayersCleared => _pointsByLayersCleared;
        public float ComboMultiplier => _comboMultiplier;
        public float LinesPerLevel => _linesPerLevel;
        public float LineClearAnimation => _lineClearAnimationDuration;

        private void OnValidate()
        {
            _pointsByLayersCleared ??= Array.Empty<int>();
            _linesPerLevel = Mathf.Max( MinLinesPerLevel, _linesPerLevel );
            _lineClearAnimationDuration = Mathf.Max(MinAnimationDuration, _lineClearAnimationDuration);
            _comboMultiplier = Mathf.Max(MinComboMultiplier, _comboMultiplier );
        }
    }
}
