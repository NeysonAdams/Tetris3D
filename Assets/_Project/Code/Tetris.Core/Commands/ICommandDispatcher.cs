namespace Tetris.Core.Commands
{
    public interface ICommandDispatcher
    {
        void Dispatch(ICommand command);
    }
}