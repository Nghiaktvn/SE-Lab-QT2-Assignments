using Microsoft.Data.SqlClient;
using Wholesale.Domain;
namespace Wholesale.AdminWinForms;

internal sealed partial class SqlAdminStore
{
    public void RegisterAgent(AgentRow agent, string username, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(agent.Name) || string.IsNullOrWhiteSpace(agent.Address) || string.IsNullOrWhiteSpace(agent.Phone)) throw new ArgumentException("Company name, address, and phone are required.");
        if (string.IsNullOrWhiteSpace(username) || username.Length > 80 || email.Length > 254 || !System.Net.Mail.MailAddress.TryCreate(email, out _)) throw new ArgumentException("Enter a username and a valid email address.");
        var hash = PasswordHash.Create(password);
        using var c = Open(); using var tx = c.BeginTransaction();
        using var cmd = new SqlCommand("INSERT dbo.Agent(AgentName,Address,Phone) OUTPUT INSERTED.AgentID VALUES(@n,@a,@p)", c, tx);
        cmd.Parameters.AddWithValue("@n", agent.Name); cmd.Parameters.AddWithValue("@a", agent.Address); cmd.Parameters.AddWithValue("@p", agent.Phone);
        var id = (int)cmd.ExecuteScalar()!;
        using var user = new SqlCommand("INSERT dbo.Users(UserName,Email,PasswordHash,Role,AgentID) VALUES(@u,@e,@h,'Agent',@id)", c, tx);
        user.Parameters.AddWithValue("@u", username.Trim()); user.Parameters.AddWithValue("@e", email.Trim()); user.Parameters.AddWithValue("@h", hash); user.Parameters.AddWithValue("@id", id); user.ExecuteNonQuery(); tx.Commit();
    }
    public void UnlockUser(string username)
    {
        using var c = Open(); using var cmd = new SqlCommand("UPDATE dbo.Users SET FailedLoginCount=0,IsLocked=0 WHERE UserName=@u AND Role='Agent'", c); cmd.Parameters.AddWithValue("@u", username.Trim());
        if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("No matching Agent account found.");
    }
    public string ReportFiltered(int? agentId, DateTime from, DateTime to)
    {
        using var c = Open(); using var cmd = new SqlCommand("SELECT TOP 10 i.ItemName,SUM(d.Quantity) Units FROM dbo.OrderDetail d JOIN dbo.[Order] o ON o.OrderID=d.OrderID JOIN dbo.Item i ON i.ItemID=d.ItemID WHERE o.OrderStatus<>'Cancelled' AND (@a IS NULL OR o.AgentID=@a) AND o.OrderDate>=@f AND o.OrderDate<@t GROUP BY i.ItemName ORDER BY Units DESC,i.ItemName; SELECT ItemName,StockQuantity FROM dbo.Item WHERE IsActive=1 AND StockQuantity<=10 ORDER BY StockQuantity; SELECT a.AgentName,COALESCE(SUM(d.Quantity*d.UnitAmount),0) Revenue FROM dbo.Agent a LEFT JOIN dbo.[Order] o ON o.AgentID=a.AgentID AND o.OrderStatus='Shipped' AND o.OrderDate>=@f AND o.OrderDate<@t LEFT JOIN dbo.OrderDetail d ON d.OrderID=o.OrderID WHERE (@a IS NULL OR a.AgentID=@a) GROUP BY a.AgentName ORDER BY Revenue DESC", c);
        cmd.Parameters.Add("@a", System.Data.SqlDbType.Int).Value = (object?)agentId ?? DBNull.Value; cmd.Parameters.AddWithValue("@f", from.Date); cmd.Parameters.AddWithValue("@t", to.Date.AddDays(1));
        using var r = cmd.ExecuteReader(); var b = new System.Text.StringBuilder(); b.AppendLine($"SALES PERFORMANCE   |   {from:dd MMM yyyy} — {to:dd MMM yyyy}\n"); b.AppendLine("BEST SELLERS · NON-CANCELLED ORDERS\n"); while (r.Read()) b.AppendLine($"  {r.GetString(0),-32} {r.GetInt32(1),8:N0} units");
        b.AppendLine("\nLOW STOCK · CURRENT ACTIVE INVENTORY (≤ 10)\n"); r.NextResult(); while (r.Read()) b.AppendLine($"  {r.GetString(0),-32} {r.GetInt32(1),8:N0} units");
        b.AppendLine("\nREVENUE BY AGENT · SHIPPED ORDERS ONLY\n"); r.NextResult(); while (r.Read()) b.AppendLine($"  {r.GetString(0),-32} {r.GetDecimal(1),15:N0} VND"); return b.ToString();
    }
    public string ManifestHtml(int id)
    {
        using var c = Open(); using var cmd = new SqlCommand("SELECT o.OrderDate,o.OrderStatus,a.AgentName,a.Address,a.Phone,i.ItemName,d.Quantity,d.UnitAmount FROM dbo.[Order] o JOIN dbo.Agent a ON a.AgentID=o.AgentID JOIN dbo.OrderDetail d ON d.OrderID=o.OrderID JOIN dbo.Item i ON i.ItemID=d.ItemID WHERE o.OrderID=@id ORDER BY d.ID", c); cmd.Parameters.AddWithValue("@id", id); using var r = cmd.ExecuteReader();
        static string H(string s) => System.Net.WebUtility.HtmlEncode(s);
        var b = new System.Text.StringBuilder(); decimal total = 0; bool first = true;
        while (r.Read())
        {
            if (first) { b.Append($"<header><b>✳ NORTHSTAR WHOLESALE</b><span>SHIPPING MANIFEST #{id:D5}</span></header><h1>Ready for dispatch.</h1><p><b>{H(r.GetString(2))}</b><br>{H(r.GetString(3))}<br>Phone: {H(r.GetString(4))}</p><p>Placed: {r.GetDateTime(0):dd MMM yyyy HH:mm} UTC · Status: {H(r.GetString(1))}</p><table><thead><tr><th>Product</th><th>Quantity</th><th>Unit price</th><th>Amount</th></tr></thead><tbody>"); first = false; }
            var amount = r.GetInt32(6) * r.GetDecimal(7); total += amount; b.Append($"<tr><td>{H(r.GetString(5))}</td><td>{r.GetInt32(6)}</td><td>{r.GetDecimal(7):N0}</td><td>{amount:N0} VND</td></tr>");
        }
        if (first) throw new InvalidOperationException("Order not found.");
        b.Append($"</tbody></table><h2>Total: {total:N0} VND</h2><footer>Prepared by: __________________ &nbsp; Dispatched by: __________________<br><br>Received by: __________________ &nbsp; Date: __________________</footer>");
        return "<!doctype html><html lang='en'><meta charset='utf-8'><title>Northstar shipping manifest</title><style>body{max-width:950px;margin:50px auto;font:15px Segoe UI,Arial;color:#193b3b;padding:25px}header{display:flex;justify-content:space-between;border-bottom:2px solid;padding:20px 0}header span{font-size:12px}h1{font:40px Georgia}p{line-height:1.8}table{width:100%;border-collapse:collapse;margin:35px 0}th,td{padding:15px;border-bottom:1px solid #ddd;text-align:left}th{background:#edf3ec}h2{text-align:right}footer{margin-top:70px;font-size:12px}button{background:#24715d;color:white;border:0;padding:12px 20px;cursor:pointer}@media print{body{margin:0;padding:0}button{display:none}thead{display:table-header-group}tr,footer{break-inside:avoid}@page{size:A4;margin:15mm}}</style><button onclick='window.print()'>Print / Save as PDF</button>" + b + "</html>";
    }
}
