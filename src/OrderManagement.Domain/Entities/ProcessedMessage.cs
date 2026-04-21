namespace OrderManagement.Domain.Entities;

public class ProcessedMessage
{
    public Guid Id { get; set; }

    public string MessageId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;

    public DateTimeOffset ProcessedAt { get; set; }
}

