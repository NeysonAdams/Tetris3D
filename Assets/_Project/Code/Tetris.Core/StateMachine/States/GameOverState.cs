using Tetris.Core.Commands;

namespace Tetris.Core.StateMachine.States
{
    public sealed class GameOverState : IGameState
    {
        public void Enter(GameContext context) 
        {
            context.Events.InvokeGameOver(context.Score);
        }
        public void Exit(GameContext context) { }
        public IGameState? Tick(GameContext context, float deltaTime) => null;
        public IGameState? HandleCommand(GameContext context, ICommand command)
        {
            switch (command)
            {
                case RestartCommand:
                    context.ResetRuntimeState();
                    context.RequestTransition<SpawningState>();
                    break;

                case QuitCommand:
                    context.RequestTransition<MainMenuState>();
                    break;
            }

            return null;
        }
    }
}