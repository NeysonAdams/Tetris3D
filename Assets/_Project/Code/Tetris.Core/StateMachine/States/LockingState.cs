using Tetris.Core.Commands;

namespace Tetris.Core.StateMachine.States
{
    public sealed class LockingState : IGameState
    {
        public void Enter(GameContext context) 
        { 
            if (context.CurrentPiece == null)
            {
                throw new System.InvalidOperationException(
                    "LockingState entered without CurrentPiece set. This is a bug in state transitions.");
            }

            var piece = context.CurrentPiece.Value;
            var type = piece.Shape.Type;

            context.Field.WriteCells(piece, type);
            context.LastLockedType = type;
            context.Events.InvokePieceLocked(piece, type);

            var fulllayers = context.Field.GetFullLayers();
            if (fulllayers.Length > 0)
            {
                context.RequestTransition<LineClearingState>();
            }
            else
            {
                context.RequestTransition<SpawningState>();
            }
        }
        public void Exit(GameContext context) { }
        public IGameState? Tick(GameContext context, float deltaTime) => null;
        public IGameState? HandleCommand(GameContext context, ICommand command) => null;
    }
}