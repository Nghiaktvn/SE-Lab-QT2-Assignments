using System.ComponentModel.DataAnnotations;
using Lab.Intelligence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AgentPortal.Pages;
public class AssistantModel(WholesaleStore store) : PageModel
{
    [BindProperty, Required, StringLength(300)] public string Question { get; set; } = "";
    public IActionResult OnGet() => RedirectToPage("/Index");
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return new JsonResult(new { reply = "Ask a short question, up to 300 characters.", items = Array.Empty<object>() });
        KnowledgeEntry[] knowledge = [
            new("order", "place order checkout buy dat hang mua hang gio hang", "Choose quantities in the catalog, review the live total, then select Place order. Stock and prices are confirmed together."),
            new("history", "my orders order status history track lich su don hang trang thai", "Open My orders to follow your own orders and view or print their invoices."),
            new("cancel", "cancel order refund huy don hang hoan tien", "Contact your warehouse administrator to cancel an unshipped order. Reserved stock is returned once. Shipped orders cannot be cancelled here."),
            new("account", "account login locked password tai khoan dang nhap khoa mat khau", "After five incorrect passwords the account locks. Ask your warehouse administrator to unlock it."),
            new("print", "print invoice pdf in hoa don", "Open an order invoice, then choose Print invoice / Save PDF. The paper view keeps products and totals."),
            new("stock", "low stock inventory available ton kho con hang sap het", "Availability is checked again when you place an order. The following low-stock products may need an earlier order."),
            new("help", "help hello hi xin chao tro giup", "Hi, I'm Nova. Ask about a product, a budget, stock, ordering or your invoices. For example: cà phê dưới 250k.")
        ];
        var answer = KnowledgeAssistant.Ask(Question, knowledge);
        var agentId = HttpContext.Session.GetInt32("agentId");
        if (agentId is null) return new JsonResult(new { reply = answer.Topic == "unknown" ? "Sign in as an Agent for live product recommendations. I can also explain ordering, invoices and account access." : answer.Answer, items = Array.Empty<object>() });
        if (answer.Topic is not ("unknown" or "stock" or "history"))
            return new JsonResult(new { reply = answer.Answer, items = Array.Empty<object>() });
        try
        {
            if (answer.Topic == "history")
            {
                var orders = (await store.GetOrdersAsync(agentId.Value)).Take(3).ToList();
                var text = orders.Count == 0 ? "You haven't placed an order yet." : string.Join(" · ", orders.Select(o => $"#{o.Id}: {o.Status}, {o.Total:N0} VND"));
                return new JsonResult(new { reply = text, link = "/orders", linkText = "Open my orders", items = Array.Empty<object>() });
            }
            var products = (await store.GetCatalogAsync()).Select(p => new AssistantProduct(p.Id, p.Name, p.Price, p.Stock)).ToList();
            var recommendations = KnowledgeAssistant.Recommend(Question, products);
            if (recommendations.Count == 0 && answer.Topic == "stock")
                recommendations = products.Where(p => p.Stock > 0 && p.Stock <= 10).OrderBy(p => p.Stock).Take(3).ToList();
            if (recommendations.Count > 0) return new JsonResult(new { reply = answer.Topic == "stock" ? answer.Answer : "These available products match your request. Prices are per pack; checkout confirms current stock.", items = recommendations.Select(p => new { p.Name, p.Price, p.Stock, link = "/catalog?search=" + Uri.EscapeDataString(p.Name) }) });
            return new JsonResult(new { reply = answer.Topic == "unknown" ? "I couldn't find an available product matching that request. Try a product name or a different budget, such as coffee under 250k." : answer.Answer, items = Array.Empty<object>() });
        }
        catch (Microsoft.Data.SqlClient.SqlException)
        {
            return new JsonResult(new { reply = "Live inventory is temporarily unavailable. Please retry shortly. " + (answer.Topic == "unknown" ? "" : answer.Answer), items = Array.Empty<object>() });
        }
    }
}
