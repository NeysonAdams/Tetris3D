using Tetris.Core.Commands;

namespace Tetris.Core.StateMachine.States
{
    public sealed class MainMenuState : IGameState
    {
        public void Enter(GameContext context) 
        { 
            context.ResetRuntimeState();
        }

        public void Exit(GameContext context) { }

        public IGameState? Tick(GameContext context, float deltaTime) 
        { return null; }

        public IGameState? HandleCommand(GameContext context, ICommand command) 
        {
            if (command is StartCommand)
            {
                context.RequestTransition<SpawningState>();
            }
            return null;
        }
    }
}