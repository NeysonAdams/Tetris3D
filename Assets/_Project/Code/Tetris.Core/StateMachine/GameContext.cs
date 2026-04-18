using System;
using Tetris.Core.Configs;
using Tetris.Core.Events;
using Tetris.Core.Fields;
using Tetris.Core.Randomization;
using Tetris.Core.Scoring;
using Tetris.Core.Spawning;
using Tetris.Core.States;
using Tetris.Core.Tetrominoes;

namespace Tetris.Core.StateMachine
{
    public sealed class GameContext
    {

        // Runtime states
        public Piece? CurrentPiece;
        public Piece? NextPiece;
        public Piece? Ghost;
        public ScoreState Score;
        public GravityState Gravity;

        public Type? PreviousStateType;
        public TetrominoType LastLockedType;

        //Core Dependencies
        public readonly GameField Field;
        public readonly IRandomizer Randomizer;
        public readonly ISpawner Spawner;
        public readonly IScoringService Scoring;
        public readonly ITetrominoCatalog Catalog;
        public readonly GameEvents Events;

        //Configs
        public readonly GravitySettingsSO GravitySettings;
        public readonly ScoreSettingsSO ScoreSettings;
        public readonly SpawnSettingsSO SpawnSettings;

        private Action<Type>? _transitionRequester;

        public GameContext(
            GameField field,
            IRandomizer randomizer,
            ISpawner spawner,
            IScoringService scoring,
            ITetrominoCatalog catalog,
            GameEvents events,
            GravitySettingsSO gravitySettings,
            ScoreSettingsSO scoreSettings,
            SpawnSettingsSO spawnSettings)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (randomizer == null) throw new ArgumentNullException(nameof(randomizer));
            if (spawner == null) throw new ArgumentNullException(nameof(spawner));
            if (scoring == null) throw new ArgumentNullException(nameof(scoring));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (events == null) throw new ArgumentNullException(nameof(events));
            if (gravitySettings == null) throw new ArgumentNullException(nameof(gravitySettings));
            if (scoreSettings == null) throw new ArgumentNullException(nameof(scoreSettings));
            if (spawnSettings == null) throw new ArgumentNullException(nameof(spawnSettings));

            Field = field;
            Randomizer = randomizer;
            Spawner = spawner;
            Scoring = scoring;
            Catalog = catalog;
            Events = events;
            GravitySettings = gravitySettings;
            ScoreSettings = scoreSettings;
            SpawnSettings = spawnSettings;

            CurrentPiece = null;
            NextPiece = null;
            Ghost = null;
            Score = ScoreState.Initial;
            Gravity = new GravityState(GetInitialInterval(gravitySettings));
        }

        public void SetTransitionRequester(Action<Type> requester)
        {
            if (requester == null) throw new ArgumentNullException(nameof(requester));
            _transitionRequester = requester;
        }

        public void RequestTransition(Type stateType)
        {
            if (stateType == null) throw new ArgumentNullException(nameof(stateType));

            if (_transitionRequester == null)
            {
                throw new InvalidOperationException(
                    $"Cannot request transition to {stateType.Name}: TransitionRequester not set. " +
                    "Bootstrap must call SetTransitionRequester before any state may request transitions.");
            }

            _transitionRequester(stateType);
        }

        public void RequestTransition<TState>() where TState : IGameState
        {
            RequestTransition(typeof(TState));
        }

        public void ResetRuntimeState()
        {
            Field.Clear();
            Randomizer.Reset();

            CurrentPiece = null;
            NextPiece = null;
            Ghost = null;
            Score = ScoreState.Initial;
            Gravity = new GravityState(GetInitialInterval(GravitySettings));
        }
        private static float GetInitialInterval(GravitySettingsSO settings)
        {
            const float fallbackInterval = 1.0f;
            var intervals = settings.IntervalsByLevel;
            return intervals.Length > 0 ? intervals[0] : fallbackInterval;
        }

    }
}