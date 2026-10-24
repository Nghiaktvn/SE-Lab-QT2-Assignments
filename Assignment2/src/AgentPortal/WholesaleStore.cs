using System.Data;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Wholesale.Domain;

namespace AgentPortal;

public sealed record CatalogItem(int Id, string Name, string Size, decimal Price, int Stock);
public sealed record OrderSummary(int Id, DateTime Date, string Status, string PaymentStatus, decimal Total);
public sealed record InvoiceLine(string ItemName, int Quantity, decimal UnitAmount);
public sealed record OrderInvoice(int Id, DateTime Date, string Status, string PaymentStatus, string? PaymentReference, DateTime? PaidAtUtc, string AgentName, string AgentAddress, string AgentPhone, List<InvoiceLine> Lines)
{
    public decimal Total => Lines.Sum(line => line.Quantity * line.UnitAmount);
}
public sealed class OrderException(string message) : Exception(message);

public sealed class WholesaleStore(IConfiguration configuration)
{
    private SqlConnection Open() => new(configuration.GetConnectionString("Wholesale"));

    public async Task<(string UserName, int AgentId)?> AuthenticateAgentAsync(string email, string password)
    {
        await using var connection = Open();
        await connection.OpenAsync();
        await using var tx = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        await using var query = new SqlCommand("SELECT u.UserID,u.UserName,u.Role,u.AgentID,u.PasswordHash,u.IsLocked,u.FailedLoginCount FROM dbo.Users u WITH(UPDLOCK,HOLDLOCK) LEFT JOIN dbo.Agent a ON a.AgentID=u.AgentID WHERE u.Email=@email AND (u.Role<>'Agent' OR a.IsActive=1)", connection, tx);
        query.Parameters.AddWithValue("@email", email);
        await using var reader = await query.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        var userId = reader.GetInt32(0);
        var userName = reader.GetString(1);
        var role = reader.GetString(2);
        var agentId = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
        var hash = reader.GetString(4);
        var locked = reader.GetBoolean(5);
        await reader.DisposeAsync();
        if (locked || role != "Agent" || agentId == 0) return null;
        if (!PasswordHash.Matches(password, hash))
        {
            await using var fail = new SqlCommand("UPDATE dbo.Users SET FailedLoginCount=FailedLoginCount+1, IsLocked=CASE WHEN FailedLoginCount+1>=5 THEN 1 ELSE 0 END WHERE UserID=@id", connection, tx);
            fail.Parameters.AddWithValue("@id", userId);
            await fail.ExecuteNonQueryAsync();
            await tx.CommitAsync();
            return null;
        }
        await using var success = new SqlCommand("UPDATE dbo.Users SET FailedLoginCount=0 WHERE UserID=@id", connection, tx);
        success.Parameters.AddWithValue("@id", userId);
        await success.ExecuteNonQueryAsync();
        await tx.CommitAsync();
        return (userName, agentId);
    }

    public async Task<List<CatalogItem>> GetCatalogAsync()
    {
        await using var connection = Open(); await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT ItemID,ItemName,Size,Price,StockQuantity FROM dbo.Item WHERE IsActive=1 ORDER BY ItemName", connection);
        await using var reader = await command.ExecuteReaderAsync();
        var result = new List<CatalogItem>();
        while (await reader.ReadAsync()) result.Add(new(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetDecimal(3), reader.GetInt32(4)));
        return result;
    }

    public async Task<int> PlaceOrderAsync(int agentId, IReadOnlyCollection<(int ItemId, int Quantity)> quantities, Guid? requestKey = null)
    {
        if (quantities.Count == 0 || quantities.Count > 100 || quantities.Any(x => x.ItemId <= 0 || x.Quantity <= 0) || quantities.Select(x => x.ItemId).Distinct().Count() != quantities.Count)
            throw new OrderException("Select one or more distinct products with positive quantities.");
        await using var connection = Open(); await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            await using var active = new SqlCommand("SELECT COUNT(*) FROM dbo.Agent a WITH(HOLDLOCK) JOIN dbo.Users u ON u.AgentID=a.AgentID WHERE a.AgentID=@id AND a.IsActive=1 AND u.Role='Agent' AND u.IsLocked=0", connection, transaction);
            active.Parameters.AddWithValue("@id", agentId);
            if ((int)(await active.ExecuteScalarAsync())! == 0) throw new OrderException("Your agent account is unavailable. Contact the warehouse administrator.");
            if (requestKey.HasValue)
            {
                await using var previous = new SqlCommand("SELECT OrderID FROM dbo.[Order] WITH(UPDLOCK,HOLDLOCK) WHERE RequestKey=@key AND AgentID=@agent", connection, transaction);
                previous.Parameters.AddWithValue("@key", requestKey.Value); previous.Parameters.AddWithValue("@agent", agentId);
                var existing = await previous.ExecuteScalarAsync();
                if (existing is int existingId) { await transaction.CommitAsync(); return existingId; }
            }
            var lines = new List<OrderLine>();
            foreach (var selected in quantities.OrderBy(x => x.ItemId))
            {
                await using var lookup = new SqlCommand("SELECT ItemName,Price,StockQuantity FROM dbo.Item WITH(UPDLOCK,HOLDLOCK) WHERE ItemID=@id AND IsActive=1", connection, transaction);
                lookup.Parameters.AddWithValue("@id", selected.ItemId);
                await using var reader = await lookup.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) throw new OrderException($"Product #{selected.ItemId} is unavailable.");
                var name = reader.GetString(0); var price = reader.GetDecimal(1); var stock = reader.GetInt32(2);
                if (!OrderRules.HasSufficientStock(new StockItem(selected.ItemId, name, stock, price), selected.Quantity))
                    throw new OrderException($"Insufficient stock for {name}. Available: {stock}.");
                lines.Add(new OrderLine(selected.ItemId, name, selected.Quantity, price));
            }
            _ = OrderRules.CalculateTotal(lines);
            await using var master = new SqlCommand("INSERT dbo.[Order](AgentID,OrderStatus,RequestKey) OUTPUT INSERTED.OrderID VALUES(@agent,'Pending',@key)", connection, transaction);
            master.Parameters.AddWithValue("@agent", agentId);
            master.Parameters.Add("@key", SqlDbType.UniqueIdentifier).Value = (object?)requestKey ?? DBNull.Value;
            var orderId = (int)(await master.ExecuteScalarAsync())!;
            foreach (var line in lines)
            {
                await using var detail = new SqlCommand("INSERT dbo.OrderDetail(OrderID,ItemID,Quantity,UnitAmount) VALUES(@o,@i,@q,@p); UPDATE dbo.Item SET StockQuantity=StockQuantity-@q WHERE ItemID=@i AND StockQuantity>=@q; IF @@ROWCOUNT<>1 THROW 50001,'Stock changed while creating order',1;", connection, transaction);
                detail.Parameters.AddWithValue("@o", orderId); detail.Parameters.AddWithValue("@i", line.ItemId); detail.Parameters.AddWithValue("@q", line.Quantity); detail.Parameters.AddWithValue("@p", line.UnitAmount);
                await detail.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();
            return orderId;
        }
        catch
        {
            try { await transaction.RollbackAsync(); } catch { /* Preserve the original failure. */ }
            throw;
        }
    }

    public async Task<string> ConfirmDemoPaymentAsync(int orderId, int agentId, Guid requestKey)
    {
        if (orderId <= 0 || agentId <= 0 || requestKey == Guid.Empty)
            throw new OrderException("A valid order and payment reference are required.");

        var reference = "DEMO-" + requestKey.ToString("N").ToUpperInvariant();
        await using var connection = Open();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            await using var lookup = new SqlCommand("SELECT PaymentStatus,OrderStatus,PaymentReference FROM dbo.[Order] WITH(UPDLOCK,HOLDLOCK) WHERE OrderID=@order AND AgentID=@agent", connection, transaction);
            lookup.Parameters.AddWithValue("@order", orderId);
            lookup.Parameters.AddWithValue("@agent", agentId);
            await using var reader = await lookup.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) throw new OrderException("This invoice is not available to your account.");
            var paymentStatus = reader.GetString(0);
            var orderStatus = reader.GetString(1);
            var existingReference = reader.IsDBNull(2) ? null : reader.GetString(2);
            await reader.DisposeAsync();

            if (paymentStatus == "Paid")
            {
                await transaction.CommitAsync();
                return existingReference ?? throw new InvalidOperationException("Paid order is missing its payment reference.");
            }
            if (paymentStatus == "Refunded") throw new OrderException("This demo payment has already been refunded.");
            if (paymentStatus != "Unpaid" || orderStatus == "Cancelled")
                throw new OrderException("This order can no longer be paid.");

            await using var update = new SqlCommand("UPDATE dbo.[Order] SET PaymentStatus='Paid',PaidAtUtc=SYSUTCDATETIME(),PaymentReference=@reference WHERE OrderID=@order AND AgentID=@agent AND PaymentStatus='Unpaid' AND OrderStatus<>'Cancelled'", connection, transaction);
            update.Parameters.AddWithValue("@reference", reference);
            update.Parameters.AddWithValue("@order", orderId);
            update.Parameters.AddWithValue("@agent", agentId);
            if (await update.ExecuteNonQueryAsync() != 1) throw new OrderException("Payment state changed. Refresh the invoice and try again.");

            await transaction.CommitAsync();
            return reference;
        }
        catch
        {
            try { await transaction.RollbackAsync(); } catch { /* Preserve the original failure. */ }
            throw;
        }
    }

    public async Task<bool> IsAgentActiveAsync(int agentId, string? userName = null)
    {
        await using var connection = Open(); await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT COUNT(*) FROM dbo.Agent a JOIN dbo.Users u ON u.AgentID=a.AgentID WHERE a.AgentID=@id AND a.IsActive=1 AND u.Role='Agent' AND u.IsLocked=0 AND (@user IS NULL OR u.UserName=@user)", connection);
        command.Parameters.AddWithValue("@id", agentId);
        command.Parameters.Add("@user", SqlDbType.NVarChar, 80).Value = (object?)userName ?? DBNull.Value;
        return (int)(await command.ExecuteScalarAsync())! > 0;
    }

    public async Task<List<OrderSummary>> GetOrdersAsync(int agentId)
    {
        await using var connection = Open(); await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT o.OrderID,o.OrderDate,o.OrderStatus,o.PaymentStatus,SUM(d.Quantity*d.UnitAmount) FROM dbo.[Order] o JOIN dbo.OrderDetail d ON d.OrderID=o.OrderID WHERE o.AgentID=@a GROUP BY o.OrderID,o.OrderDate,o.OrderStatus,o.PaymentStatus ORDER BY o.OrderDate DESC", connection);
        command.Parameters.AddWithValue("@a", agentId);
        await using var reader = await command.ExecuteReaderAsync();
        var result = new List<OrderSummary>();
        while (await reader.ReadAsync()) result.Add(new(reader.GetInt32(0), reader.GetDateTime(1), reader.GetString(2), reader.GetString(3), reader.GetDecimal(4)));
        return result;
    }

    public async Task<OrderInvoice?> GetInvoiceAsync(int orderId, int agentId)
    {
        await using var connection = Open(); await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT o.OrderDate,o.OrderStatus,o.PaymentStatus,o.PaymentReference,o.PaidAtUtc,a.AgentName,a.Address,a.Phone,i.ItemName,d.Quantity,d.UnitAmount FROM dbo.[Order] o JOIN dbo.Agent a ON a.AgentID=o.AgentID JOIN dbo.OrderDetail d ON d.OrderID=o.OrderID JOIN dbo.Item i ON i.ItemID=d.ItemID WHERE o.OrderID=@o AND o.AgentID=@a ORDER BY d.ID", connection);
        command.Parameters.AddWithValue("@o", orderId); command.Parameters.AddWithValue("@a", agentId);
        await using var reader = await command.ExecuteReaderAsync();
        var lines = new List<InvoiceLine>(); DateTime date = default; DateTime? paidAt = null; string status = "", paymentStatus = "", agentName = "", agentAddress = "", agentPhone = ""; string? paymentReference = null;
        while (await reader.ReadAsync())
        {
            date = reader.GetDateTime(0); status = reader.GetString(1); paymentStatus = reader.GetString(2);
            paymentReference = reader.IsDBNull(3) ? null : reader.GetString(3); paidAt = reader.IsDBNull(4) ? null : reader.GetDateTime(4); agentName = reader.GetString(5); agentAddress = reader.GetString(6); agentPhone = reader.GetString(7);
            lines.Add(new InvoiceLine(reader.GetString(8), reader.GetInt32(9), reader.GetDecimal(10)));
        }
        return lines.Count == 0 ? null : new OrderInvoice(orderId, date, status, paymentStatus, paymentReference, paidAt, agentName, agentAddress, agentPhone, lines);
    }
}
