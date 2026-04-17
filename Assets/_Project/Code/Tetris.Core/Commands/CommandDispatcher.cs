using System;

namespace Tetris.Core.Commands
{
    public sealed class CommandDispatcher : ICommandDispatcher
    {
        private readonly ICommandHandler _handler;

        public CommandDispatcher(ICommandHandler handler)
        {
            if (handler == null) 
                throw new ArgumentNullException(nameof(handler));
            
            _handler = handler;
        }

        public void Dispatch(ICommand command)
        {
            if (command == null) 
                throw new ArgumentNullException(nameof(command));

            _handler.Handle(command);
        }
    }
}
