namespace OrderManagement.Worker;

public sealed class OrderProcessingOptions
{
    public int TransitionDelaySeconds { get; init; } = 5;

    public static OrderProcessingOptions From(IConfiguration configuration)
    {
        var raw = configuration["ORDER_STATUS_DELAY_SECONDS"];
        if (!string.IsNullOrWhiteSpace(raw) && int.TryParse(raw, out var parsed) && parsed >= 0)
        {
            return new OrderProcessingOptions { TransitionDelaySeconds = parsed };
        }

        return new OrderProcessingOptions();
    }
}

