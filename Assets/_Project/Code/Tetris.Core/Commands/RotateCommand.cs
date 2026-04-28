using Tetris.Core.Tetrominoes;

namespace Tetris.Core.Commands
{
    public readonly record struct RotateCommand(RotationAxis Axis, int Direction) : ICommand;
}
