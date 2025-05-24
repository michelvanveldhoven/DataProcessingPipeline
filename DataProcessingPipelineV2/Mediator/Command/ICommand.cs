using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataProcessingPipelineV2.Mediator.Command;

public interface ICommand
{
}

public interface ICommandHandler
{
}
public interface ICommandHandler<T> : ICommandHandler
    where T : ICommand
{
    Task HandleAsync(T command, CancellationToken cancellationToken = default);
}
