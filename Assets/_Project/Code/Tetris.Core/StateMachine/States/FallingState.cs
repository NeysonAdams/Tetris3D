using Tetris.Core.Commands;
using Tetris.Core.Tetrominoes;
using UnityEngine;

namespace Tetris.Core.StateMachine.States
{
    public sealed class FallingState : IGameState
    {
        public void Enter(GameContext context) 
        {
            if (context.CurrentPiece == null)
            {
                throw new System.InvalidOperationException(
                    "FallingState entered without CurrentPiece. State transitions are broken.");
            }
        }
        public void Exit(GameContext context) { }
        public IGameState? Tick(GameContext context, float deltaTime)
        {
            if (context.CurrentPiece != null) return null;

            var interval = GetCurrentInterval(context);

            context.Gravity.AccumulateTime += deltaTime;

            var felledThisTick = false;
            while (context.Gravity.AccumulateTime >= interval)
            {
                var candidate = context.CurrentPiece.Value.MoveBy(Vector3Int.down);
                if (!context.Field.CanPlace(candidate))
                {
                    break;
                }

                context.CurrentPiece = candidate;
                context.Gravity.AccumulateTime -= interval;
                context.Gravity.LockDelayTimer = 0f;
                felledThisTick = true;

                context.Events.InvokePieceFell(candidate);
                RecalculateGhost(context);
            }

            if (!felledThisTick)
            {
                context.Gravity.LockDelayTimer += deltaTime;
                if (context.Gravity.LockDelayTimer >= context.GravitySettings.LockDelay)
                {
                    context.RequestTransition<LockingState>();
                }
            }

            return null;
        }
        public IGameState? HandleCommand(GameContext context, ICommand command)
        {
            if (context.CurrentPiece == null) return null;

            switch (command)
            {
                case MoveCommand move:
                    TryMove(context, move.Direction);
                    break;

                case RotateCommand rotate:
                    TryRotate(context, rotate.Axis, rotate.Direction);
                    break;

                case SoftDropCommand softDrop:
                    context.Gravity.IsSoftDropping = softDrop.Pressed;
                    break;

                case HardDropCommand:
                    PerfomHardDrop(context);
                    break;

                case PauseCommand:
                    context.RequestTransition<PausedState>();
                    break;
            }
            return null;
        }

        //Helpers:
        private static void TryMove (GameContext context, Vector3Int direction) 
        {
            var candidate = context.CurrentPiece!.Value.MoveBy(direction);
            if (!context.Field.CanPlace(candidate))
            {
                return;
            }

            context.CurrentPiece = candidate;
            context.Gravity.LockDelayTimer = 0f;
            context.Events.InvokePieceMoved(candidate);
            RecalculateGhost(context);
        }

        private static void TryRotate (GameContext context, RotationAxis axis, int direction) 
        {
            var candidate = context.CurrentPiece!.Value.Rotate(axis, direction);
            if (!context.Field.CanPlace(candidate)) return;

            context.CurrentPiece = candidate;
            context.Gravity.LockDelayTimer = 0f;
            context.Events.InvokePieceRotated(candidate);
            RecalculateGhost(context);
        }

        private static void PerfomHardDrop(GameContext context) 
        {
            var piece = context.CurrentPiece!.Value;
            var landingPiece = GhostCalculator.Calculate(piece, context.Field);
            var cellsDropped = piece.Position.y - landingPiece.Position.y;

            context.CurrentPiece = landingPiece;
            context.Events.InvokePiceHardDropped(landingPiece, cellsDropped);

            context.RequestTransition<LockingState>();
        }

        private static void RecalculateGhost(GameContext context) 
        {
            if (context.CurrentPiece != null) return;

            var ghost = GhostCalculator.Calculate(context.CurrentPiece.Value, context.Field);
            context.Ghost = ghost;
            context.Events.InvokedGhostPieceChanged(ghost);
        }

        private static float GetCurrentInterval(GameContext context)
        {
            var intervals = context.GravitySettings.IntervalsByLevel;
            var baseInterval = intervals.Length > 0
                ? intervals[Mathf.Clamp(context.Score.Level, 0, intervals.Length - 1)]
                : 1.0f;

            return context.Gravity.IsSoftDropping
                ? baseInterval * context.GravitySettings.SoftDropMultiplier
                : baseInterval;
        }
    }
}