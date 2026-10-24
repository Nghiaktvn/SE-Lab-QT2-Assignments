using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AgentPortal.Pages;

public class CatalogModel(WholesaleStore store) : PageModel
{
    public List<CatalogItem> Items { get; private set; } = [];
    public string? Error { get; private set; }
    [BindProperty] public Guid RequestKey { get; set; }
    public Dictionary<int, int> Quantities { get; } = [];
    public async Task<IActionResult> OnGetAsync()
    {
        if (HttpContext.Session.GetInt32("agentId") is null) return RedirectToPage("/Login", new { ReturnUrl = "/catalog" + Request.QueryString });
        RequestKey = Guid.NewGuid();
        try { Items = await store.GetCatalogAsync(); }
        catch (Microsoft.Data.SqlClient.SqlException) { Error = "Could not load the catalog. Check SQL Server and the seed script."; }
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        var agentId = HttpContext.Session.GetInt32("agentId");
        if (agentId is null) return RedirectToPage("/Login");
        if (RequestKey == Guid.Empty) Error = "The order form expired. Review your quantities and submit again.";
        var quantities = new List<(int ItemId, int Quantity)>();
        foreach (var entry in Request.Form)
        {
            if (!entry.Key.StartsWith("qty_", StringComparison.Ordinal)) continue;
            if (!int.TryParse(entry.Key[4..], out var itemId) || !int.TryParse(entry.Value.ToString(), out var qty) || qty < 0)
            { Error = "Enter whole, non-negative quantities."; break; }
            if (qty > 0) quantities.Add((itemId, qty));
            Quantities[itemId] = qty;
        }
        if (Error is null && quantities.Count == 0) Error = "Enter a quantity for at least one product.";
        if (Error is null)
        {
            try { var orderId = await store.PlaceOrderAsync(agentId.Value, quantities, RequestKey); TempData["OrderPlaced"] = "Your order has been confirmed. Stock has been reserved for dispatch."; return RedirectToPage("/Orders/Invoice", new { id = orderId }); }
            catch (OrderException ex) { Error = ex.Message; }
            catch (Microsoft.Data.SqlClient.SqlException) { Error = "Could not save the order. Check the database and try again."; }
        }
        try { Items = await store.GetCatalogAsync(); }
        catch (Microsoft.Data.SqlClient.SqlException) { Error ??= "Could not load the catalog."; }
        if (RequestKey == Guid.Empty) RequestKey = Guid.NewGuid();
        return Page();
    }
}
