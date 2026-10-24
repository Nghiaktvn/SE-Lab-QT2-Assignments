using System.Security.Cryptography;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;
using Wholesale.Domain;

namespace Wholesale.AdminWinForms;

public sealed record ItemRow(int Id, string Name, string Size, decimal Price, int Stock, byte[]? Version = null);
public sealed record AgentRow(int Id, string Name, string Address, string Phone);
public sealed record OrderRow(int Id, string Agent, DateTime Date, string Status, string PaymentStatus, decimal Total);

internal sealed partial class SqlAdminStore
{
    private readonly string connectionString;
    internal SqlAdminStore(string configuredConnectionString) => connectionString = configuredConnectionString;
    public SqlAdminStore()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "Wholesale.AdminWinForms.dll.config");
        if (!File.Exists(configPath)) configPath = Path.Combine(AppContext.BaseDirectory, "App.config");
        var doc = XDocument.Load(configPath); connectionString = doc.Descendants("add").First(x => (string?)x.Attribute("name") == "Wholesale").Attribute("connectionString")!.Value;
    }
    private SqlConnection Open() { var c = new SqlConnection(connectionString); c.Open(); return c; }
    public bool AuthenticateAdmin(string username, string password)
    {
        using var c = Open(); using var tx = c.BeginTransaction(System.Data.IsolationLevel.Serializable);
        using var cmd = new SqlCommand("SELECT UserID,PasswordHash,IsLocked,Role FROM dbo.Users WITH(UPDLOCK,HOLDLOCK) WHERE UserName=@u", c, tx); cmd.Parameters.AddWithValue("@u", username);
        using var r = cmd.ExecuteReader(); if (!r.Read() || r.GetBoolean(2) || r.GetString(3) != "Admin") return false;
        var id = r.GetInt32(0); var ok = PasswordHash.Matches(password, r.GetString(1)); r.Close();
        using var update = new SqlCommand(ok ? "UPDATE dbo.Users SET FailedLoginCount=0 WHERE UserID=@id AND IsLocked=0" : "UPDATE dbo.Users SET FailedLoginCount=FailedLoginCount+1,IsLocked=CASE WHEN FailedLoginCount+1>=5 THEN 1 ELSE 0 END WHERE UserID=@id", c, tx);
        update.Parameters.AddWithValue("@id", id); update.ExecuteNonQuery(); tx.Commit(); return ok;
    }
    public List<ItemRow> GetItems() { using var c = Open(); using var cmd = new SqlCommand("SELECT ItemID,ItemName,Size,Price,StockQuantity,RowVersion FROM dbo.Item WHERE IsActive=1 ORDER BY ItemName", c); using var r = cmd.ExecuteReader(); var list = new List<ItemRow>(); while (r.Read()) list.Add(new(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetDecimal(3), r.GetInt32(4), (byte[])r[5])); return list; }
    public List<AgentRow> GetAgents() { using var c = Open(); using var cmd = new SqlCommand("SELECT AgentID,AgentName,Address,Phone FROM dbo.Agent WHERE IsActive=1 ORDER BY AgentName", c); using var r = cmd.ExecuteReader(); var list = new List<AgentRow>(); while (r.Read()) list.Add(new(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3))); return list; }
    public List<OrderRow> GetOrders() { using var c = Open(); using var cmd = new SqlCommand("SELECT o.OrderID,a.AgentName,o.OrderDate,o.OrderStatus,o.PaymentStatus,COALESCE(SUM(d.Quantity*d.UnitAmount),0) FROM dbo.[Order] o JOIN dbo.Agent a ON a.AgentID=o.AgentID LEFT JOIN dbo.OrderDetail d ON d.OrderID=o.OrderID GROUP BY o.OrderID,a.AgentName,o.OrderDate,o.OrderStatus,o.PaymentStatus ORDER BY o.OrderDate DESC", c); using var r = cmd.ExecuteReader(); var list = new List<OrderRow>(); while (r.Read()) list.Add(new(r.GetInt32(0), r.GetString(1), r.GetDateTime(2), r.GetString(3), r.GetString(4), r.GetDecimal(5))); return list; }
    public void SaveItem(ItemRow item)
    {
        if (item.Id > 0 && item.Version is null) throw new ArgumentException("Reload the product before saving its current version.");
        if (string.IsNullOrWhiteSpace(item.Name) || string.IsNullOrWhiteSpace(item.Size) || item.Price <= 0 || item.Stock < 0) throw new ArgumentException("Enter an item name, size, positive price, and non-negative stock.");
        using var c = Open(); using var cmd = new SqlCommand(item.Id == 0 ?
            "INSERT dbo.Item(ItemName,Size,Price,StockQuantity) VALUES(@name,@size,@price,@stock)" :
            "UPDATE dbo.Item SET ItemName=@name,Size=@size,Price=@price,StockQuantity=@stock WHERE ItemID=@id AND IsActive=1 AND (@version IS NULL OR RowVersion=@version)", c);
        cmd.Parameters.AddWithValue("@name", item.Name.Trim()); cmd.Parameters.AddWithValue("@size", item.Size.Trim()); cmd.Parameters.AddWithValue("@price", item.Price); cmd.Parameters.AddWithValue("@stock", item.Stock); cmd.Parameters.AddWithValue("@id", item.Id);
        cmd.Parameters.Add("@version", System.Data.SqlDbType.VarBinary, 8).Value = (object?)item.Version ?? DBNull.Value;
        if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("This item changed while you were editing. Refresh before saving to protect current stock.");
    }
    public void ChangeStock(int id, int delta) { using var c = Open(); using var cmd = new SqlCommand("UPDATE dbo.Item SET StockQuantity=StockQuantity+@d WHERE ItemID=@id AND StockQuantity+@d>=0", c); cmd.Parameters.AddWithValue("@d", delta); cmd.Parameters.AddWithValue("@id", id); if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Stock update would create a negative quantity."); }
    public void DeactivateItem(int id) { using var c = Open(); using var cmd = new SqlCommand("UPDATE dbo.Item SET IsActive=0 WHERE ItemID=@id", c); cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery(); }
    public void SaveAgent(AgentRow agent)
    {
        if (string.IsNullOrWhiteSpace(agent.Name) || string.IsNullOrWhiteSpace(agent.Address) || string.IsNullOrWhiteSpace(agent.Phone)) throw new ArgumentException("Enter an agent name, address, and phone.");
        using var c = Open(); using var cmd = new SqlCommand(agent.Id == 0 ?
            "INSERT dbo.Agent(AgentName,Address,Phone) VALUES(@name,@address,@phone)" :
            "UPDATE dbo.Agent SET AgentName=@name,Address=@address,Phone=@phone WHERE AgentID=@id AND IsActive=1", c);
        cmd.Parameters.AddWithValue("@name", agent.Name.Trim()); cmd.Parameters.AddWithValue("@address", agent.Address.Trim()); cmd.Parameters.AddWithValue("@phone", agent.Phone.Trim()); cmd.Parameters.AddWithValue("@id", agent.Id);
        if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Agent no longer exists. Refresh and try again.");
    }
    public void DeactivateAgent(int id) { using var c = Open(); using var cmd = new SqlCommand("UPDATE dbo.Agent SET IsActive=0 WHERE AgentID=@id AND NOT EXISTS(SELECT 1 FROM dbo.[Order] WHERE AgentID=@id AND OrderStatus IN ('Pending','Processing'))", c); cmd.Parameters.AddWithValue("@id", id); if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Agent has an open order and cannot be deactivated."); }
    public void AdvanceOrder(int id, string current, string next)
    {
        if (!OrderRules.IsValidStatusTransition(current, next)) throw new InvalidOperationException($"The order cannot move from {current} to {next}.");
        using var c = Open(); using var tx = c.BeginTransaction(System.Data.IsolationLevel.Serializable);
        using var cmd = new SqlCommand("UPDATE dbo.[Order] SET OrderStatus=@next,PaymentStatus=CASE WHEN @next='Cancelled' AND PaymentStatus='Paid' THEN 'Refunded' ELSE PaymentStatus END WHERE OrderID=@id AND OrderStatus=@current", c, tx);
        cmd.Parameters.AddWithValue("@next", next); cmd.Parameters.AddWithValue("@id", id); cmd.Parameters.AddWithValue("@current", current);
        if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Order status changed; refresh and try again.");
        if (next == "Cancelled")
        {
            using var restore = new SqlCommand("UPDATE i SET StockQuantity=i.StockQuantity+d.Quantity FROM dbo.Item i JOIN dbo.OrderDetail d ON d.ItemID=i.ItemID WHERE d.OrderID=@id", c, tx);
            restore.Parameters.AddWithValue("@id", id); restore.ExecuteNonQuery();
        }
        tx.Commit();
    }
    public string Report()
    {
        using var c = Open(); using var cmd = new SqlCommand("SELECT TOP 5 i.ItemName,SUM(d.Quantity) Units FROM dbo.OrderDetail d JOIN dbo.[Order] o ON o.OrderID=d.OrderID JOIN dbo.Item i ON i.ItemID=d.ItemID WHERE o.OrderStatus<>'Cancelled' GROUP BY i.ItemName ORDER BY Units DESC; SELECT ItemName,StockQuantity FROM dbo.Item WHERE StockQuantity<=10 ORDER BY StockQuantity; SELECT a.AgentName,COALESCE(SUM(d.Quantity*d.UnitAmount),0) Revenue FROM dbo.Agent a LEFT JOIN dbo.[Order] o ON o.AgentID=a.AgentID AND o.OrderStatus<>'Cancelled' LEFT JOIN dbo.OrderDetail d ON d.OrderID=o.OrderID GROUP BY a.AgentName ORDER BY Revenue DESC", c); using var r = cmd.ExecuteReader(); var text = "WHOLESALE PERFORMANCE\n\nBEST-SELLING ITEMS\n"; while (r.Read()) text += $"{r.GetString(0),-28} {r.GetInt32(1),6} units\n"; text += "\nLOW STOCK (<= 10)\n"; r.NextResult(); while (r.Read()) text += $"{r.GetString(0),-28} {r.GetInt32(1),6} units\n"; text += "\nREVENUE BY AGENT\n"; r.NextResult(); while (r.Read()) text += $"{r.GetString(0),-28} {r.GetDecimal(1),12:N0} VND\n"; return text;
    }
    public string Manifest(int id)
    {
        using var c = Open(); using var cmd = new SqlCommand("SELECT o.OrderDate,o.OrderStatus,a.AgentName,i.ItemName,d.Quantity,d.UnitAmount FROM dbo.[Order] o JOIN dbo.Agent a ON a.AgentID=o.AgentID JOIN dbo.OrderDetail d ON d.OrderID=o.OrderID JOIN dbo.Item i ON i.ItemID=d.ItemID WHERE o.OrderID=@id", c); cmd.Parameters.AddWithValue("@id", id); using var r = cmd.ExecuteReader(); var text = $"SHIPPING MANIFEST #{id}\n\n"; decimal total = 0; while (r.Read()) { var amount = r.GetInt32(4) * r.GetDecimal(5); total += amount; text += $"Agent: {r.GetString(2)}\nOrder date: {r.GetDateTime(0):g}\nStatus: {r.GetString(1)}\n{r.GetInt32(4),4} x {r.GetString(3),-24} {amount,12:N0} VND\n\n"; }
        return text + $"TOTAL: {total:N0} VND\n\nWarehouse dispatch copy";
    }
}
