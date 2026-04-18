using System;
using Tetris.Core.Configs;

namespace Tetris.Core.Scoring
{
    public sealed class ScoringService : IScoringService
    {
        private readonly ScoreSettingsSO _settings;

        public ScoringService(ScoreSettingsSO settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            _settings = settings;
        }

        public ScoreState CalculateClear(ScoreState current, int layersCleared)
        {
            if (layersCleared <= 0) return current;

            var basePoint = GetBasePoint(layersCleared);
            var pointsEarned = basePoint * (current.Level + 1);

            if (layersCleared >=2)
            {
                pointsEarned = (int) (pointsEarned * _settings.ComboMultiplier);
            }

            var newLinesCleared = current.LinesCleared + layersCleared;
            var newLevel = newLinesCleared / Math.Max(1, _settings.LinesPerLevel);

            return current with
            {
                Score = current.Score + pointsEarned,
                LinesCleared = newLinesCleared,
                Level = (int)newLevel
            };
        }

        private int GetBasePoint(int layerCleared)
        {
            var table = _settings.PointByLayersCleared;
            if (table.Length == 0) return 0;

            var index = Math.Min(layerCleared -1, table.Length - 1);

            return table[index];
        }
    }
}
