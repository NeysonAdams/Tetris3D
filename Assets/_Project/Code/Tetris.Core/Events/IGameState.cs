using Tetris.Core.Commands;

namespace Tetris.Core.StateMachine
{
    public interface IGameState
    {
        void Enter(GameContext context);
        void Exit(GameContext context);
        IGameState? Tick (GameContext context, float deltaTime);
        IGameState? HandleCommand (GameContext context, ICommand command);
    }
}
