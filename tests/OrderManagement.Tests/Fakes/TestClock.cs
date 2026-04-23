using OrderManagement.Application.Abstractions;

namespace OrderManagement.Tests.Fakes;

public sealed class TestClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = now;

    public void Set(DateTimeOffset next) => UtcNow = next;
}

