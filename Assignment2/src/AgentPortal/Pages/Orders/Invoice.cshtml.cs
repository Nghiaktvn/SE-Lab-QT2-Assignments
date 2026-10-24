using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AgentPortal.Pages.Orders;

public class InvoiceModel(WholesaleStore store) : PageModel
{
    public OrderInvoice? Invoice { get; private set; }
    public Guid PaymentRequestKey { get; private set; }
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var agentId = HttpContext.Session.GetInt32("agentId");
        if (agentId is null) return RedirectToPage("/Login");
        try { Invoice = await store.GetInvoiceAsync(id, agentId.Value); }
        catch (Microsoft.Data.SqlClient.SqlException)
        {
            TempData["OrderNotice"] = "The invoice is temporarily unavailable. Please retry when the database connection returns.";
            return RedirectToPage("/Orders/Index");
        }
        if (Invoice is { PaymentStatus: "Unpaid", Status: not "Cancelled" }) PaymentRequestKey = Guid.NewGuid();
        return Invoice is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostConfirmDemoPaymentAsync(int id, Guid paymentRequestKey)
    {
        var agentId = HttpContext.Session.GetInt32("agentId");
        if (agentId is null) return RedirectToPage("/Login");
        try
        {
            if (paymentRequestKey == Guid.Empty) throw new OrderException("Refresh the invoice before confirming payment.");
            var reference = await store.ConfirmDemoPaymentAsync(id, agentId.Value, paymentRequestKey);
            TempData["PaymentConfirmed"] = $"Payment confirmed for this classroom demo. Reference {reference}; no real money was transferred.";
        }
        catch (OrderException ex) { TempData["PaymentError"] = ex.Message; }
        catch (Microsoft.Data.SqlClient.SqlException) { TempData["PaymentError"] = "Payment confirmation could not be verified. Refresh this invoice before retrying; the same order will not be charged twice."; }
        return RedirectToPage("/Orders/Invoice", new { id });
    }
}
