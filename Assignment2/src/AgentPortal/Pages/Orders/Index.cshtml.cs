using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AgentPortal.Pages.Orders;

public class IndexModel(WholesaleStore store) : PageModel
{
    public List<OrderSummary> Orders { get; private set; } = [];
    public string? Error { get; private set; }
    public async Task<IActionResult> OnGetAsync()
    {
        var agentId = HttpContext.Session.GetInt32("agentId");
        if (agentId is null) return RedirectToPage("/Login");
        try { Orders = await store.GetOrdersAsync(agentId.Value); }
        catch (Microsoft.Data.SqlClient.SqlException) { Error = "Could not load your orders. Check SQL Server."; }
        return Page();
    }
}
