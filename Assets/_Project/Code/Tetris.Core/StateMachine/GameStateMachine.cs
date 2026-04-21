using System;
using System.Collections.Generic;
using Tetris.Core.Commands;
using Tetris.Core.Scoring;
using Tetris.Core.StateMachine.States;
using UnityEngine;


namespace Tetris.Core.StateMachine
{
    public sealed class GameStateMachine: IGameStateReader, ICommandHandler
    {
        private const int MaxTransitionChain = 10;

        private readonly GameContext _context;
        private readonly Dictionary<Type, IGameState> _states;
        private IGameState _current;
        private Type? _pendingTransition;

        public GameStateMachine (
            GameContext context, 
            Dictionary<Type,IGameState> states, 
            Type initialStateType)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (states == null) throw new ArgumentNullException(nameof(states));
            if (initialStateType == null) throw new ArgumentNullException(nameof(initialStateType));

            if (!states.TryGetValue(initialStateType, out var initialState))
            {
                throw new ArgumentException(
                    $"Initial state type '{initialStateType.Name}' is not registered in states dictionary.",
                    nameof(initialStateType));
            }

            _context = context;
            _states = states;
            _pendingTransition = initialStateType;
            _current = states[initialStateType];

            _context.SetTransitionRequester(QueueTransition);

            _current.Enter(_context);
            ProcessPendingTransition();
        }

        public Type CurrentStateType => _current.GetType();
        public ScoreState Score => _context.Score;
        public bool IsPaused => _current is PausedState;


        public void Tick(float deltaTime)
        {
            _current.Tick(_context, deltaTime);
            ProcessPendingTransition();
        }

        public void Handle(ICommand command)
        {
            if(command == null) throw new ArgumentNullException(nameof(command));

            _current.HandleCommand(_context, command);
            ProcessPendingTransition();
        }

        private void QueueTransition(Type stateType)
        {
            if (stateType == null) throw new ArgumentNullException(nameof(stateType));

            if (!_states.ContainsKey(stateType))
            {
                throw new ArgumentException(
                    $"Cannot transition to '{stateType.Name}': state is not registered.",
                    nameof(stateType));
            }

            _pendingTransition = stateType;
        }

        private void ProcessPendingTransition()
        {
            var iterations = 0;

            while (_pendingTransition != null)
            {
                if(iterations >= MaxTransitionChain)
                {
                    throw new InvalidOperationException(
                        $"State transition chain exceeded {MaxTransitionChain} iterations — likely a loop. " +
                        $"Last pending: {_pendingTransition.Name}.");
                }

                var target = _pendingTransition;
                _pendingTransition = null;
                Transition(target);
                iterations++;
            }
        }

        private void Transition(Type stateType)
        {
            _current.Exit(_context);
            _context.PreviousStateType = _current.GetType();
            _current = _states[stateType];
            _current.Enter(_context);
        }
    }
}
