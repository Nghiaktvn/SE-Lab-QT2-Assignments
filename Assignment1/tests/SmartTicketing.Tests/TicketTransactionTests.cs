using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartTicketing.Domain;
namespace SmartTicketing.Tests;

[TestClass]
public class TicketTransactionTests
{
    private static TicketTransaction New() => new(new("D01", "Central", 7000, true), PaymentMethod.BankQr);
    [TestMethod] public void CannotBuyInactiveRoute() => Assert.ThrowsExactly<ArgumentException>(() => new TicketTransaction(new("D01", "Central", 7000, false), PaymentMethod.Card));
    [TestMethod] public void CannotBuyZeroFare() => Assert.ThrowsExactly<ArgumentException>(() => new TicketTransaction(new("D01", "Central", 0, true), PaymentMethod.Card));
    [TestMethod] public void CannotIssueBeforeApproval() => Assert.ThrowsExactly<InvalidOperationException>(() => New().IssueTicket(DateTimeOffset.UtcNow.AddHours(1)));
    [TestMethod] public void ApprovedPaymentIssuesOneTicket() { var t = New(); t.MarkPaid("PAY-1"); var a = t.IssueTicket(DateTimeOffset.UtcNow.AddHours(1)); Assert.AreSame(a, t.IssueTicket(DateTimeOffset.UtcNow.AddHours(1))); }
    [TestMethod] public void DuplicateCallbackIsIdempotent() { var t = New(); t.MarkPaid("PAY-1"); t.MarkPaid("PAY-1"); Assert.AreEqual(TransactionStatus.Paid, t.Status); }
    [TestMethod] public void ConflictingCallbackIsRejected() { var t = New(); t.MarkPaid("PAY-1"); Assert.ThrowsExactly<InvalidOperationException>(() => t.MarkPaid("PAY-2")); }
    [TestMethod] public void TimeoutCannotIssueTicket() { var t = New(); t.MarkUnknown(); Assert.ThrowsExactly<InvalidOperationException>(() => t.IssueTicket(DateTimeOffset.UtcNow.AddHours(1))); }
    [TestMethod] public void TimeoutCanBeReconciled() { var t = New(); t.MarkUnknown(); t.MarkPaid("PAY-1"); Assert.AreEqual(TransactionStatus.Completed, Issue(t)); }
    private static TransactionStatus Issue(TicketTransaction t) { t.IssueTicket(DateTimeOffset.UtcNow.AddHours(1)); return t.Status; }
    [TestMethod] public void DeclinedPaymentCannotIssueTicket() { var t = New(); t.MarkDeclined(); Assert.ThrowsExactly<InvalidOperationException>(() => t.IssueTicket(DateTimeOffset.UtcNow.AddHours(1))); }
    [TestMethod] public void UnknownPaymentCannotBeSilentlyCancelled() { var t = New(); t.MarkUnknown(); Assert.ThrowsExactly<InvalidOperationException>(t.Cancel); }
    [TestMethod] public void CancelledPaymentCannotBeCharged() { var t = New(); t.Cancel(); Assert.ThrowsExactly<InvalidOperationException>(() => t.MarkPaid("PAY-1")); }
    [TestMethod] public void PrinterRecoveryRetainsTicketIdentity() { var t = New(); t.MarkPaid("PAY-1"); var barcode = t.IssueTicket(DateTimeOffset.UtcNow.AddHours(1)).Barcode; t.PrinterFailed(); t.Reprint(); Assert.AreEqual(barcode, t.Ticket!.Barcode); }
    [TestMethod] public void PrinterFailureCanBeRefunded() { var t = New(); t.MarkPaid("PAY-1"); Issue(t); t.PrinterFailed(); t.Refund(); Assert.AreEqual(TransactionStatus.Refunded, t.Status); }
    [TestMethod] public void UniqueKeysAcrossKiosks() { var a = New(); var b = New(); Assert.AreNotEqual(a.IdempotencyKey, b.IdempotencyKey); }
}
