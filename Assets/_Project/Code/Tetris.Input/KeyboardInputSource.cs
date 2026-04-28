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
        private readonly ICameraOrientationProvider _cameraOrientation;

        private MoveKeyState _moveLeft;
        private MoveKeyState _moveRight;
        private MoveKeyState _moveUp;
        private MoveKeyState _moveDown;

        private bool _softDropPressed;

        // Direction remapping table: [facing][screenDirection] -> worldDirection
        // Screen directions: 0=Left, 1=Right, 2=Up(forward), 3=Down(back)
        private static readonly Vector3Int[][] DirectionRemapTable = new Vector3Int[][]
        {
            // Front (yaw ~0°): camera looks at +Z face, no remapping needed
            new[] { Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back },
            // Right (yaw ~90°): camera looks at +X face
            new[] { Vector3Int.forward, Vector3Int.back, Vector3Int.right, Vector3Int.left },
            // Back (yaw ~180°): camera looks at -Z face
            new[] { Vector3Int.right, Vector3Int.left, Vector3Int.back, Vector3Int.forward },
            // Left (yaw ~270°): camera looks at -X face
            new[] { Vector3Int.back, Vector3Int.forward, Vector3Int.left, Vector3Int.right }
        };

        public KeyboardInputSource (
            ICommandDispatcher disptcher,
            KeyBindingsSO bindigs,
            KeyRepeatSettingsSO repeatSettings,
            ICameraOrientationProvider cameraOrientation)
        {
            if(disptcher == null) throw new ArgumentNullException (nameof (disptcher));
            if(bindigs == null) throw new ArgumentNullException (nameof (bindigs));
            if(repeatSettings == null) throw new ArgumentNullException (nameof (repeatSettings));
            if(cameraOrientation == null) throw new ArgumentNullException (nameof (cameraOrientation));

            _dispatcher = disptcher;
            _bindings = bindigs;
            _repeatSettings = repeatSettings;
            _cameraOrientation = cameraOrientation;

            // Screen-relative direction indices: 0=Left, 1=Right, 2=Up, 3=Down
            _moveLeft = new MoveKeyState { Key = bindigs.MoveNegativeX, DirectionIndex = 0 };
            _moveRight = new MoveKeyState { Key = bindigs.MovePositiveX, DirectionIndex = 1 };
            _moveUp = new MoveKeyState { Key = bindigs.MovePositiveZ, DirectionIndex = 2 };
            _moveDown = new MoveKeyState { Key = bindigs.MoveNegativeZ, DirectionIndex = 3 };
        }

        private Vector3Int RemapDirection(int screenDirectionIndex)
        {
            var facing = (int)_cameraOrientation.CurrentFacing;
            return DirectionRemapTable[facing][screenDirectionIndex];
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

            ResetMoveState(ref _moveLeft);
            ResetMoveState(ref _moveRight);
            ResetMoveState(ref _moveUp);
            ResetMoveState(ref _moveDown);
        }

        private static void ResetMoveState(ref MoveKeyState state)
        {
            state.Timer = 0f;
            state.IsInInitialWait = false;
        }

        private void TickMoves(float deltaTime)
        {
            TickMoveKey(ref _moveLeft, deltaTime);
            TickMoveKey(ref _moveRight, deltaTime);
            TickMoveKey(ref _moveUp, deltaTime);
            TickMoveKey(ref _moveDown, deltaTime);
        }

        private void TickMoveKey(ref MoveKeyState state, float deltaTime)
        {
            if (UnityEngine.Input.GetKeyDown(state.Key))
            {
                var worldDirection = RemapDirection(state.DirectionIndex);
                _dispatcher.Dispatch(new MoveCommand(worldDirection));
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
                    var worldDirection = RemapDirection(state.DirectionIndex);
                    _dispatcher.Dispatch(new MoveCommand(worldDirection));
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
            public int DirectionIndex; // 0=Left, 1=Right, 2=Up, 3=Down (screen-relative)
            public float Timer;
            public bool IsInInitialWait;
        }
    }
}
