namespace Tetris.Core.Commands
{
    public interface ICommandHandler
    {
        void Handle(ICommand command);
    }
}
