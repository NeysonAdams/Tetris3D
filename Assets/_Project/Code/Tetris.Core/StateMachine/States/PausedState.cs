using Tetris.Core.Commands;
using UnityEngine;

namespace Tetris.Core.StateMachine.States
{
    public sealed class PausedState : IGameState
    {
        public void Enter(GameContext context)
        {
            Debug.Log("[State] PausedState.Enter");
            context.Events.InvokeGamePaused();
        }
        public void Exit(GameContext context) 
        {
            context.Events.InvokeGameResumed();
        }
        public IGameState? Tick(GameContext context, float deltaTime) => null;
        public IGameState? HandleCommand(GameContext context, ICommand command)
        {
            switch (command)
            {
                case ResumeCommand:
                case PauseCommand:
                    ResumeToPreviousState(context);
                    break;
                case QuitCommand:
                    context.RequestTransition<MainMenuState>(); 
                    break;
            }

            return null;
        }

        private static void ResumeToPreviousState(GameContext context)
        {
            var previous = context.PreviousStateType;
            if (previous != null) 
                context.RequestTransition(previous);
            else
                context.RequestTransition<MainMenuState>();
        }

    }
}