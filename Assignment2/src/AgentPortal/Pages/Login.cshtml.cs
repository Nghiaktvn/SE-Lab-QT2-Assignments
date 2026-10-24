using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AgentPortal.Pages;

public class LoginModel(WholesaleStore store) : PageModel
{
    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    public string? Error { get; set; }
    public IActionResult OnGet() => HttpContext.Session.GetString("role") == "Agent" ? RedirectToPage("/Catalog") : Page();
    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var user = await store.AuthenticateAgentAsync(Email.Trim(), Password);
            if (user is null) { Error = "Sign in failed. Check your credentials and account status."; return Page(); }
            HttpContext.Session.Clear();
            HttpContext.Session.SetString("role", "Agent");
            HttpContext.Session.SetString("user", user.Value.UserName);
            HttpContext.Session.SetInt32("agentId", user.Value.AgentId);
            return !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ? LocalRedirect(ReturnUrl) : RedirectToPage("/Catalog");
        }
        catch (Microsoft.Data.SqlClient.SqlException)
        {
            Error = "Database unavailable. Check the SQL Server connection and run the seed script.";
            return Page();
        }
    }
    public IActionResult OnPostLogout()
    {
        HttpContext.Session.Clear();
        return RedirectToPage("/Login");
    }
}
