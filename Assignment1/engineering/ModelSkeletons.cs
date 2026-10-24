// Forward generated from domain-model.json. Review before using.
namespace GeneratedModel;
public class Destination
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public decimal Fare { get; set; }
    public bool IsActive { get; set; }
}

public class TicketTransaction
{
    public System.Guid Id { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? DestinationCode { get; set; }
    public decimal FareSnapshot { get; set; }
}

public class Ticket
{
    public System.Guid TicketId { get; set; }
    public string? Barcode { get; set; }
    public System.DateTimeOffset ValidUntil { get; set; }
}
