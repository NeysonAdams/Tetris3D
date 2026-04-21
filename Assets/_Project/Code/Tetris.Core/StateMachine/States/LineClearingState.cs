using Tetris.Core.Commands;
using Tetris.Core.Tetrominoes;
using System;
using UnityEngine;

namespace Tetris.Core.StateMachine.States
{
    public sealed class LineClearingState : IGameState
    {

        private float _elapsedTime;
        private int[] _clearingLayers = Array.Empty<int>();
        private TetrominoType _clearingType;

        public void Enter(GameContext context)
        {
            Debug.Log("[State] LineClearingState.Enter");
            var cominfFromPause = context.PreviousStateType == typeof(PausedState);
            if (cominfFromPause)
            {
                return;
            }

            _elapsedTime = 0f;
            _clearingLayers = context.Field.GetFullLayers();
            _clearingType = context.LastLockedType;

            if (_clearingLayers.Length > 0)
            {
                context.Events.InvokedLayerClearing(_clearingLayers, _clearingType);
            }
        }
        public void Exit(GameContext context) { }
        public IGameState? Tick(GameContext context, float deltaTime)
        {
            _elapsedTime += deltaTime;

            var duration = context.ScoreSettings.LineClearAnimation;
            if (_elapsedTime < duration) return null;

            CompleteClear(context);
            context.RequestTransition<SpawningState>();

            return null;
        }
        public IGameState? HandleCommand(GameContext context, ICommand command)
        {
            if (command is PauseCommand)
            {
                context.RequestTransition<PausedState>();
            }
            return null;
        }

        private void CompleteClear(GameContext context)
        {
            var clearedCount = _clearingLayers.Length;
            var oldLevel = context.Score.Level;

            context.Field.RemoveLayers(_clearingLayers);

            var newScore = context.Scoring.CalculateClear(context.Score, clearedCount);
            context.Score = newScore;

            context.Events.InvokedLayersCleared(newScore, clearedCount);
            context.Events.InvokeScoreChanged(newScore);

            if (newScore.Level != oldLevel)
            {
                context.Events.InvokeLevelChanged(newScore.Level);
            }
        }
    }
}