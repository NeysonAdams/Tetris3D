using Tetris.Core.Commands;
using Tetris.Core.States;
using Tetris.Core.Tetrominoes;
using UnityEngine;

namespace Tetris.Core.StateMachine.States
{
    public sealed class SpawningState : IGameState
    {
        public void Enter(GameContext context)
        {
            Debug.Log("[State] SpawningState.Enter");

            var isFirstSpawn = context.NextPiece == null;
            if (isFirstSpawn)
            {
                context.Events.InvokeGamesStarted();
                var firstType = context.Randomizer.Next();
                var firstPiece = context.Spawner.TrySpawn(context.Field, firstType);
                if (firstPiece == null)
                {
                    context.RequestTransition<GameOverState>();
                    return;
                }

                context.CurrentPiece = firstPiece.Value;
            }
            else
            {
                context.CurrentPiece = context.NextPiece;
                if (!context.Field.CanPlace(context.CurrentPiece.Value))
                {
                    context.RequestTransition<GameOverState>();
                    return;
                }
            }

            context.Events.InvokePieceSwaped(context.CurrentPiece.Value);

            var nextType = context.Randomizer.Next();
            var nextPiece = context.Spawner.TrySpawn(context.Field, nextType);
            context.NextPiece = nextPiece;

            if (nextPiece != null)
            {
                context.Events.InvokedNextPieceChanged(nextPiece.Value.Shape);
            }

            var ghost = GhostCalculator.Calculate(context.CurrentPiece.Value, context.Field);
            context.Ghost = ghost;
            context.Events.InvokedGhostPieceChanged(ghost);

            var interval = GetIntervalForLevel(context);
            context.Gravity = new GravityState(interval);

            context.RequestTransition<FallingState>();
        }

        public void Exit(GameContext context) { }
        public IGameState? Tick(GameContext context, float deltaTime) => null;
        public IGameState? HandleCommand(GameContext context, ICommand command) => null;


        private static float GetIntervalForLevel(GameContext context)
        {
            var intervals = context.GravitySettings.IntervalsByLevel;
            if (intervals.Length == 0) return 0;
            var levelIndex = Mathf.Clamp(context.Score.Level, 0, intervals.Length - 1);
            return intervals[levelIndex];
        }
    }
}
