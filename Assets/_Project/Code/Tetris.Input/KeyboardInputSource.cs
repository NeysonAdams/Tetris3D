using UnityEngine;
using Tetris.Core.Commands;
using Tetris.Input.Configs;
using System;
using Tetris.Core.Tetrominoes;

namespace Tetris.Input
{

    public sealed class KeyboardInputSource : IInputSource
    {
        private readonly ICommandDispatcher _dispatcher;
        private readonly KeyBindingsSO _bindings;
        private readonly KeyRepeatSettingsSO _repeatSettings;

        private MoveKeyState _moveNegX;
        private MoveKeyState _movePosX;
        private MoveKeyState _moveNegZ;
        private MoveKeyState _movePosZ;

        private bool _softDropPressed;

        public KeyboardInputSource (
            ICommandDispatcher disptcher,
            KeyBindingsSO bindigs,
            KeyRepeatSettingsSO repeatSettings)
        {
            if(disptcher == null) throw new ArgumentNullException (nameof (disptcher));
            if(bindigs == null) throw new ArgumentNullException (nameof (bindigs));
            if(repeatSettings == null) throw new ArgumentNullException (nameof (repeatSettings));

            _dispatcher = disptcher;
            _bindings = bindigs;
            _repeatSettings = repeatSettings;

            _moveNegX = new MoveKeyState { Key = bindigs.MoveNegativeX, Direction = Vector3Int.left };
            _movePosX = new MoveKeyState { Key = bindigs.MoveNegativeX, Direction = Vector3Int.right };
            _moveNegZ = new MoveKeyState { Key = bindigs.MoveNegativeX, Direction = Vector3Int.back };
            _movePosX = new MoveKeyState { Key = bindigs.MoveNegativeX, Direction = Vector3Int.forward };
        }

        public void Tick(float deltaTime)
        {
            TickMoves(deltaTime);
            TickRotations();
            TickDrops();
            TickPause();
        }

        public void OnApplicationFocusLost()
        {
            if(_softDropPressed)
            {
                _softDropPressed = false;
                _dispatcher.Dispatch(new SoftDropCommand(false));
            }

            ResetMoveState(ref _moveNegX);
            ResetMoveState(ref _movePosX);
            ResetMoveState(ref _moveNegZ);
            ResetMoveState(ref _movePosZ);
        }

        private static void ResetMoveState(ref MoveKeyState state)
        {
            state.Timer = 0f;
            state.IsInInitialWait = false;
        }

        private void TickMoves(float deltaTime) 
        {
            TickMoveKey(ref _moveNegX, deltaTime);
            TickMoveKey(ref _movePosX, deltaTime);
            TickMoveKey(ref _moveNegZ, deltaTime);
            TickMoveKey(ref _movePosZ, deltaTime);
        }
        private void TickMoveKey(ref MoveKeyState state, float deltaTime)
        {
            if (UnityEngine.Input.GetKeyDown(state.Key))
            {
                _dispatcher.Dispatch(new MoveCommand(state.Direction));
                state.Timer = 0f;
                state.IsInInitialWait = true;
                return;
            }

            if (!UnityEngine.Input.GetKey(state.Key))
            {
                state.Timer = 0f;
                state.IsInInitialWait = false;
                return;
            }

            state.Timer = deltaTime;

            if (state.IsInInitialWait)
            {
                if (state.Timer >= _repeatSettings.InitialDElay)
                {
                    state.IsInInitialWait = false;
                    state.Timer = 0f;
                }
            }
            else
            {
                while (state.Timer >= _repeatSettings.RepeatInterbal)
                {
                    _dispatcher.Dispatch(new MoveCommand(state.Direction));
                    state.Timer -= _repeatSettings.RepeatInterbal;
                }
            }
        }
        private void TickRotations() 
        {
            if (UnityEngine.Input.GetKeyDown(_bindings.RotateNegativeX))
            {
                _dispatcher.Dispatch(new RotateCommand(RotationAxis.X, -1));
            }

            if (UnityEngine.Input.GetKeyDown(_bindings.RotatePositiveX))
            {
                _dispatcher.Dispatch(new RotateCommand(RotationAxis.X, 1));
            }

            if (UnityEngine.Input.GetKeyDown(_bindings.RotateNegativeY))
            {
                _dispatcher.Dispatch(new RotateCommand(RotationAxis.Y, -1));
            }

            if (UnityEngine.Input.GetKeyDown(_bindings.RotatePositiveY))
            {
                _dispatcher.Dispatch(new RotateCommand(RotationAxis.Y, 1));
            }

            if (UnityEngine.Input.GetKeyDown(_bindings.RotateNegativeZ))
            {
                _dispatcher.Dispatch(new RotateCommand(RotationAxis.Z, -1));
            }

            if (UnityEngine.Input.GetKeyDown(_bindings.RotatePositiveZ))
            {
                _dispatcher.Dispatch(new RotateCommand(RotationAxis.Z, 1));
            }
        }
        private void TickDrops() 
        {
            if(UnityEngine.Input.GetKeyDown(_bindings.SoftDrop))
            {
                _softDropPressed = true;
                _dispatcher.Dispatch(new SoftDropCommand(true));
            }
            else if (UnityEngine.Input.GetKeyUp(_bindings.SoftDrop))
            {
                _softDropPressed = false;
                _dispatcher.Dispatch(new SoftDropCommand(false));
            }

            if (UnityEngine.Input.GetKeyDown(_bindings.HardDrop))
            {
                _dispatcher.Dispatch(new HardDropCommand());
            }
        }
        private void TickPause() 
        {
            if (UnityEngine.Input.GetKeyDown(_bindings.Pause))
            {
                _dispatcher.Dispatch(new PauseCommand());
            }
        }

        private struct MoveKeyState
        {
            public KeyCode Key;
            public Vector3Int Direction;
            public float Timer;
            public bool IsInInitialWait;
        }
    }

}