using System.Threading.Tasks.Dataflow;
using JasperFx.Core;
using JasperFx.Core.Reflection;
using Microsoft.Extensions.Logging;
using Wolverine.Runtime;
using Wolverine.Runtime.Handlers;
using Wolverine.Util.Dataflow;

namespace Wolverine.RDBMS.Polling;

public class DatabaseBatcher : IAsyncDisposable
{
    private readonly ActionBlock<IDatabaseOperation[]> _executingBlock;
    private readonly BatchingBlock<IDatabaseOperation> _batchingBlock;
    private readonly IMessageDatabase _database;
    private readonly IWolverineRuntime _runtime;
    private readonly CancellationToken _cancellationToken;
    private readonly ILogger<DatabaseBatcher> _logger;
    private readonly IExecutor _executor;

    public DatabaseBatcher(IMessageDatabase database, IWolverineRuntime runtime,
        CancellationToken cancellationToken)
    {
        _database = database;
        _runtime = runtime;
        _cancellationToken = cancellationToken;
        _executingBlock = new ActionBlock<IDatabaseOperation[]>(processOperationsAsync, new ExecutionDataflowBlockOptions
        {
            EnsureOrdered = true,
            MaxDegreeOfParallelism = 1
        });

        _batchingBlock = new BatchingBlock<IDatabaseOperation>(250 ,_executingBlock, cancellationToken);

        _logger = _runtime.LoggerFactory.CreateLogger<DatabaseBatcher>();

        _executor = runtime.As<IExecutorFactory>().BuildFor(typeof(DatabaseOperationBatch));
    }

    public Task EnqueueAsync(IDatabaseOperation operation)
    {
        return _batchingBlock.SendAsync(operation);
    }

    private async Task processOperationsAsync(IDatabaseOperation[] operations)
    {
        try
        {
            await _executor.InvokeAsync(new DatabaseOperationBatch(_database, operations), new MessageBus(_runtime), _cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError("Error running database operations {Operations} against message database {Database}", operations.Select(x => x.Description).Join(", "), _database);
        }
    }


    public ValueTask DisposeAsync()
    {
        _batchingBlock.Dispose();
        
        return ValueTask.CompletedTask;
    }

    public async Task DrainAsync()
    {
        _batchingBlock.Complete();
        await _batchingBlock.Completion;

        _executingBlock.Complete();
        await _executingBlock.Completion;
    }
}
