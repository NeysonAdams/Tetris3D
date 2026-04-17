namespace Tetris.Core.Scoring
{
    public readonly record struct ScoreState(int Score, int Level, int LinesCleared)
    {
        public static ScoreState Initial => default;
    }
}
