namespace Wholesale.Domain;

public sealed record OrderLine(int ItemId, string ItemName, int Quantity, decimal UnitAmount);
public sealed record StockItem(int ItemId, string Name, int StockQuantity, decimal Price);

public static class OrderRules
{
    public static decimal CalculateTotal(IEnumerable<OrderLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var materialized = lines.ToList();
        if (materialized.Count == 0) throw new ArgumentException("An order must contain at least one line.", nameof(lines));
        if (materialized.Any(x => x.ItemId <= 0 || x.Quantity <= 0 || x.UnitAmount <= 0)) throw new ArgumentException("Order lines require valid item, quantity, and price values.", nameof(lines));
        return materialized.Sum(x => x.Quantity * x.UnitAmount);
    }

    public static bool HasSufficientStock(StockItem item, int orderedQuantity) => orderedQuantity > 0 && item.StockQuantity >= orderedQuantity;

    public static StockItem DeductStock(StockItem item, int orderedQuantity)
    {
        if (!HasSufficientStock(item, orderedQuantity)) throw new InvalidOperationException($"Insufficient stock for {item.Name}.");
        return item with { StockQuantity = item.StockQuantity - orderedQuantity };
    }

    public static bool ShouldLockAccount(int failedAttempts, int maximumAttempts = 5) => failedAttempts >= maximumAttempts;

    public static bool IsValidStatusTransition(string current, string next) => (current, next) switch
    {
        ("Pending", "Processing" or "Cancelled") => true,
        ("Processing", "Shipped" or "Cancelled") => true,
        _ => false
    };
}
