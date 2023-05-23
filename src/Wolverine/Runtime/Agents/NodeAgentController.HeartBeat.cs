namespace Wolverine.Runtime.Agents;

public partial class NodeAgentController : IInternalHandler<CheckAgentHealth>
{
    public async IAsyncEnumerable<object> HandleAsync(CheckAgentHealth message)
    {
        if (_cancellation.IsCancellationRequested) yield break;
        if (_tracker.Self == null) yield break;
        
        // write health check regardless
        await _persistence.MarkHealthCheckAsync(_tracker.Self.Id);
        
        if (_tracker.Self.IsLeader())
        {
            // check health of each node. Look for stale agents, and initiate a separate command to kick them off.
            // verify that all agents are assigned? Run assignment through the agents? How do we know when to trip off?
        }
        else
        {
            // potentially trigger leadership if leader appears to be offline
        }

        yield break;
    }
    
    // Separate command to initiate kicking off a stale node, so that gets unit tested
    // TODO -- whole new command to check up on current assignments
}