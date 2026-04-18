namespace Tetris.Core.Scoring
{
    public interface IScoringService
    {
        ScoreState CalculateClear(ScoreState state, int layersCleared);
    }
}
