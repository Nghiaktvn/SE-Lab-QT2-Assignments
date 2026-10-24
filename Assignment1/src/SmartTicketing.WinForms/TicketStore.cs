using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using SmartTicketing.Domain;
namespace SmartTicketing.WinForms;

public sealed class TicketStore
{
    private readonly string connectionString;
    public bool Persist { get; }
    public TicketStore()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "appsettings.json")));
        connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Ticketing") ?? doc.RootElement.GetProperty("ConnectionStrings").GetProperty("Ticketing").GetString()!;
        Persist = doc.RootElement.GetProperty("Demo").GetProperty("PersistToSqlServer").GetBoolean();
    }
    public TicketStore(string connectionString, bool persist)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        this.connectionString = connectionString;
        Persist = persist;
    }
    public async Task<List<Destination>> Routes()
    {
        if (!Persist) return [new("D01", "Ben Thanh", 7000, true), new("D02", "Tan Cang", 12000, true), new("D03", "Suoi Tien", 20000, true)];
        await using var c = new SqlConnection(connectionString); await c.OpenAsync();
        await using var cmd = new SqlCommand("SELECT DestinationCode,DestinationName,Fare FROM dbo.Destination WHERE IsActive=1 ORDER BY DestinationName", c);
        await using var r = await cmd.ExecuteReaderAsync(); var result = new List<Destination>();
        while (await r.ReadAsync()) result.Add(new(r.GetString(0), r.GetString(1), r.GetDecimal(2), true));
        if (result.Count == 0) throw new InvalidOperationException("No active routes. Run the database setup script.");
        return result;
    }
    public async Task Save(TicketTransaction t)
    {
        if (!Persist) return;
        await using var c = new SqlConnection(connectionString); await c.OpenAsync();
        await using var tx = (SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable);
        await using var query = new SqlCommand("SELECT Barcode FROM dbo.TicketTransaction WITH(UPDLOCK,HOLDLOCK) WHERE PaymentReference=@r", c, tx);
        query.Parameters.AddWithValue("@r", t.IdempotencyKey);
        var existing = await query.ExecuteScalarAsync();
        if (existing is string barcode) { if (barcode != t.Ticket!.Barcode) throw new InvalidOperationException("Conflicting payment reference. Contact the operator."); await tx.CommitAsync(); return; }
        await using var cmd = new SqlCommand("INSERT dbo.TicketTransaction(DestinationCode,DestinationName,Fare,PaymentMethod,PaymentReference,Barcode,Status,ValidUntilUtc) VALUES(@c,@n,@f,@m,@r,@b,'Paid',@v)", c, tx);
        cmd.Parameters.AddWithValue("@c", t.Destination.Code); cmd.Parameters.AddWithValue("@n", t.Destination.Name); cmd.Parameters.AddWithValue("@f", t.FareSnapshot); cmd.Parameters.AddWithValue("@m", t.PaymentMethod.ToString()); cmd.Parameters.AddWithValue("@r", t.IdempotencyKey); cmd.Parameters.AddWithValue("@b", t.Ticket!.Barcode); cmd.Parameters.AddWithValue("@v", t.Ticket.ValidUntil.UtcDateTime);
        await cmd.ExecuteNonQueryAsync(); await tx.CommitAsync();
    }
    public async Task Update(TicketTransaction t, string printStatus, bool refund = false)
    {
        if (!Persist) return;
        await using var c = new SqlConnection(connectionString); await c.OpenAsync();
        await using var cmd = new SqlCommand("UPDATE dbo.TicketTransaction SET PrintStatus=@p,Status=CASE WHEN @refund=1 THEN 'Refunded' ELSE Status END WHERE PaymentReference=@r", c);
        cmd.Parameters.AddWithValue("@p", printStatus); cmd.Parameters.AddWithValue("@refund", refund); cmd.Parameters.AddWithValue("@r", t.IdempotencyKey);
        if (await cmd.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("Ticket record missing. Contact the operator.");
    }
}
