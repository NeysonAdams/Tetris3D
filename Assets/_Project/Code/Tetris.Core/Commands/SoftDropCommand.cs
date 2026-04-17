namespace Tetris.Core.Commands
{
    public readonly record struct SoftDropCommand(bool Pressed) : ICommand;
}
