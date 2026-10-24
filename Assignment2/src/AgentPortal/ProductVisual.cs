namespace AgentPortal;

public static class ProductVisual
{
    public static string Category(string name) => name.Contains("Coffee", StringComparison.OrdinalIgnoreCase) || name.Contains("Tea", StringComparison.OrdinalIgnoreCase) ? "Coffee & tea" : name.Contains("Coconut", StringComparison.OrdinalIgnoreCase) ? "Drinks" : name.Contains("Mango", StringComparison.OrdinalIgnoreCase) || name.Contains("Cashew", StringComparison.OrdinalIgnoreCase) ? "Snacks" : "Pantry";
    public static string Photo(string name)
    {
        var n = name.ToLowerInvariant();
        var file = n.Contains("coffee") ? "coffee.jpg" : n.Contains("tea") ? "tea.jpg" : n.Contains("cashew") ? "cashew.jpg" : n.Contains("mango") ? "mango.jpg" : n.Contains("coconut") ? "coconut.jpg" : n.Contains("noodle") ? "noodles.jpg" : n.Contains("oil") ? "oil.jpg" : n.Contains("sugar") ? "sugar.jpg" : n.Contains("salt") ? "salt.jpg" : n.Contains("rice") ? "rice.jpg" : null;
        return file is null ? "/media/northstar-3d-hero.png" : "/media/products/" + file;
    }
}
