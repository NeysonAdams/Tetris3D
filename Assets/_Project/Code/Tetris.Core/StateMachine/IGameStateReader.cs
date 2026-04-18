using System;
using Tetris.Core.Scoring;

namespace Tetris.Core.StateMachine
{

    public interface IGameStateReader
    {
        Type CurrentStateType { get; }
        ScoreState Score {  get; }
        bool IsPaused { get; }
    }
}
