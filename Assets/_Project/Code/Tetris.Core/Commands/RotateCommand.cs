using Tetris.Core.Tetrominoes;
using UnityEditor.Experimental.GraphView;

namespace Tetris.Core.Commands
{
    public readonly record struct RotateCommand(RotationAxis Axis, int Direction) : ICommand;
}
