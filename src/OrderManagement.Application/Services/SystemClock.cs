using OrderManagement.Application.Abstractions;

namespace OrderManagement.Application.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

