using System.Text.Json;
using Microsoft.Data.SqlClient;
using SmartTicketing.Domain;
using SmartTicketing.WinForms;

var root = Path.GetFullPath(args.FirstOrDefault() ?? Path.Combine(AppContext.BaseDirectory, "../../../../.."));
using var settings = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "src/SmartTicketing.WinForms/appsettings.json")));
var configured = settings.RootElement.GetProperty("ConnectionStrings").GetProperty("Ticketing").GetString();
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Ticketing") ?? configured ?? throw new InvalidOperationException("Missing ticket database connection.");
var store = new TicketStore(connectionString, true);
var checks = new List<string>();
void Check(bool passed, string name)
{
    if (!passed) throw new InvalidOperationException("FAIL: " + name);
    checks.Add("PASS: " + name);
    Console.WriteLine(checks[^1]);
}

async Task<int> Count(string? reference = null)
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = new SqlCommand(reference is null ? "SELECT COUNT(*) FROM dbo.TicketTransaction" : "SELECT COUNT(*) FROM dbo.TicketTransaction WHERE PaymentReference=@reference", connection);
    if (reference is not null) command.Parameters.AddWithValue("@reference", reference);
    return (int)(await command.ExecuteScalarAsync())!;
}

var before = await Count();
string? paymentReference = null;
try
{
    var routes = await store.Routes();
    Check(routes.Count > 0 && routes.All(route => route.IsActive && route.Fare > 0), "SQL provides active routes with positive fares");
    var transaction = new TicketTransaction(routes[0], PaymentMethod.BankQr);
    paymentReference = transaction.IdempotencyKey;
    transaction.MarkPaid("SIM-" + paymentReference);
    var ticket = transaction.IssueTicket(DateTimeOffset.Now.AddDays(1));
    await store.Save(transaction);
    Check(await Count(paymentReference) == 1, "Confirmed ticket persists once");
    await store.Save(transaction);
    Check(await Count(paymentReference) == 1 && ReferenceEquals(ticket, transaction.IssueTicket(DateTimeOffset.Now.AddDays(1))), "Retry keeps the same SQL row and ticket identity");
    await store.Update(transaction, "Failed");
    transaction.PrinterFailed();
    await store.Update(transaction, "Failed", true);
    transaction.Refund();
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    await using var query = new SqlCommand("SELECT Status,PrintStatus,Barcode,Fare FROM dbo.TicketTransaction WHERE PaymentReference=@reference", connection);
    query.Parameters.AddWithValue("@reference", paymentReference);
    await using var reader = await query.ExecuteReaderAsync();
    Check(await reader.ReadAsync() && reader.GetString(0) == "Refunded" && reader.GetString(1) == "Failed" && reader.GetString(2) == ticket.Barcode && reader.GetDecimal(3) == transaction.FareSnapshot, "Refund retains the original fare, barcode and failed print state");
}
finally
{
    if (paymentReference is not null)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("DELETE FROM dbo.TicketTransaction WHERE PaymentReference=@reference", connection);
        command.Parameters.AddWithValue("@reference", paymentReference);
        await command.ExecuteNonQueryAsync();
    }
    Check(await Count() == before, "Fixture cleanup restores the ticket count");
    var evidence = Path.Combine(root, "docs/evidence");
    Directory.CreateDirectory(evidence);
    await File.WriteAllLinesAsync(Path.Combine(evidence, "ticket-sql-integration.txt"), checks.Append($"TOTAL: {checks.Count} passed; baseline/final ticket count = {before}/{await Count()}"));
}

Console.WriteLine($"Completed {checks.Count} ticket SQL integration checks.");
