using UnityEngine;

namespace Tetris.Core.Commands
{
    public readonly record struct MoveCommand(Vector3Int Direction) : ICommand;
}
