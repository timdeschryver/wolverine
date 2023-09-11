using Microsoft.Extensions.DependencyInjection;
using TestingSupport;
using Wolverine.Runtime.Handlers;
using Xunit;

namespace CoreTests.Compilation;

public class handler_within_class_name_that_ends_with_a_number
{
    [Fact]
    public async Task handler_within_class_name_that_ends_with_a_number_are_discovered()
    {
        using var host = WolverineHost.Basic();

        var bus = host.Services.GetRequiredService<IMessageBus>();
        await bus.InvokeAsync(new FixtureMessage());

        var graph = host.Services.GetRequiredService<HandlerGraph>();
        var chain = graph.ChainFor<FixtureMessage>();

        chain.Handler.ShouldNotBeNull();
    }
}

public record FixtureMessage(string Name = "FixtureMessage");

public class ClassNameEndingWithANumberHandler4
{
    public void Handle(FixtureMessage message)
    {
    }
}
