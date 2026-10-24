namespace SmartTicketing.Domain;

public enum TransactionStatus { Pending, PaymentUnknown, Declined, Paid, Completed, CompensationRequired, Refunded, Cancelled }
public enum PaymentMethod { Card, BankQr, Wallet }
public enum PrintStatus { Pending, Printed, Failed }
public sealed record Destination(string Code, string Name, decimal Fare, bool IsActive);

public sealed class TicketTransaction
{
    public Guid Id { get; } = Guid.NewGuid();
    public string IdempotencyKey { get; } = Guid.NewGuid().ToString("N");
    public Destination Destination { get; }
    public string DestinationCode => Destination.Code;
    public decimal FareSnapshot => Destination.Fare;
    public PaymentMethod PaymentMethod { get; }
    public TransactionStatus Status { get; private set; } = TransactionStatus.Pending;
    public string? GatewayReference { get; private set; }
    public Ticket? Ticket { get; private set; }
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;

    public TicketTransaction(Destination destination, PaymentMethod method)
    {
        if (!destination.IsActive || destination.Fare <= 0 || string.IsNullOrWhiteSpace(destination.Code))
            throw new ArgumentException("Select an active destination with a positive fare.");
        if (!Enum.IsDefined(method)) throw new ArgumentOutOfRangeException(nameof(method));
        Destination = destination; PaymentMethod = method;
    }
    public void MarkPaid(string reference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        if (GatewayReference == reference && Status is TransactionStatus.Paid or TransactionStatus.Completed) return;
        if (Status is not (TransactionStatus.Pending or TransactionStatus.PaymentUnknown)) throw new InvalidOperationException("Payment cannot be approved in this state.");
        GatewayReference = reference; Status = TransactionStatus.Paid;
    }
    public void MarkUnknown() { Require(TransactionStatus.Pending); Status = TransactionStatus.PaymentUnknown; }
    public void MarkDeclined()
    {
        if (Status is not (TransactionStatus.Pending or TransactionStatus.PaymentUnknown)) throw new InvalidOperationException("Payment cannot be declined in this state.");
        Status = TransactionStatus.Declined;
    }
    public void Cancel() { Require(TransactionStatus.Pending); Status = TransactionStatus.Cancelled; }
    public Ticket IssueTicket(DateTimeOffset serviceDayEnd)
    {
        if (Status == TransactionStatus.Completed && Ticket is not null) return Ticket;
        Require(TransactionStatus.Paid);
        if (serviceDayEnd <= CreatedAt) throw new ArgumentException("Ticket expiry must be after purchase.");
        Ticket = new Ticket(Guid.NewGuid(), "TKT-" + Id.ToString("N").ToUpperInvariant(), serviceDayEnd, PrintStatus.Pending);
        Status = TransactionStatus.Completed; return Ticket;
    }
    public void PrinterFailed() { Require(TransactionStatus.Completed); Ticket = Ticket! with { PrintStatus = PrintStatus.Failed }; Status = TransactionStatus.CompensationRequired; }
    public void Reprint() { Require(TransactionStatus.CompensationRequired); Ticket = Ticket! with { PrintStatus = PrintStatus.Pending }; Status = TransactionStatus.Completed; }
    public void Refund() { Require(TransactionStatus.CompensationRequired); Status = TransactionStatus.Refunded; }
    private void Require(TransactionStatus expected) { if (Status != expected) throw new InvalidOperationException($"Expected {expected}; current state is {Status}."); }
}

public sealed record Ticket(Guid TicketId, string Barcode, DateTimeOffset ValidUntil, PrintStatus PrintStatus);
public sealed record PaymentRequest(Guid TransactionId, decimal Amount, string IdempotencyKey, PaymentMethod Method);
public sealed record PaymentResult(bool Approved, string? Reference, string StatusCode);
public interface IPaymentGateway { Task<PaymentResult> Authorize(PaymentRequest request, CancellationToken token = default); Task<PaymentResult> Reconcile(string reference, CancellationToken token = default); }
public interface ITicketRepository { Task Save(TicketTransaction transaction, CancellationToken token = default); Task<TicketTransaction?> Find(Guid transactionId, CancellationToken token = default); }
public interface ITicketPrinter { Task<PrintStatus> Print(Ticket ticket, CancellationToken token = default); }
