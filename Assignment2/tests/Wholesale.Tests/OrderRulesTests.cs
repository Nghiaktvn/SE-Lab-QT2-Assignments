using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wholesale.Domain;

namespace Wholesale.Tests;

[TestClass]
public class OrderRulesTests
{
    [TestMethod] public void CalculatesMasterDetailTotal() => Assert.AreEqual(495m, OrderRules.CalculateTotal(new[] { new OrderLine(1, "A", 2, 120m), new OrderLine(2, "B", 3, 85m) }));
    [TestMethod] public void RejectsEmptyOrder() => Assert.ThrowsExactly<ArgumentException>(() => OrderRules.CalculateTotal(Array.Empty<OrderLine>()));
    [TestMethod] public void RejectsZeroQuantity() => Assert.ThrowsExactly<ArgumentException>(() => OrderRules.CalculateTotal(new[] { new OrderLine(1, "A", 0, 10m) }));
    [TestMethod] public void AllowsExactStockAndBlocksOversell() { var stock = new StockItem(1, "Tea", 4, 20m); Assert.IsTrue(OrderRules.HasSufficientStock(stock, 4)); Assert.IsFalse(OrderRules.HasSufficientStock(stock, 5)); }
    [TestMethod] public void DeductsStockWithoutMutatingOriginal() { var stock = new StockItem(1, "Tea", 9, 20m); Assert.AreEqual(6, OrderRules.DeductStock(stock, 3).StockQuantity); Assert.AreEqual(9, stock.StockQuantity); }
    [TestMethod] public void ThrowsWhenDeductionWouldGoNegative() => Assert.ThrowsExactly<InvalidOperationException>(() => OrderRules.DeductStock(new StockItem(1, "Tea", 2, 20m), 3));
    [TestMethod] public void LocksAtFiveFailedAttempts() { Assert.IsFalse(OrderRules.ShouldLockAccount(4)); Assert.IsTrue(OrderRules.ShouldLockAccount(5)); }
    [TestMethod] public void AllowsOnlyValidOrderTransitions() { Assert.IsTrue(OrderRules.IsValidStatusTransition("Pending", "Processing")); Assert.IsFalse(OrderRules.IsValidStatusTransition("Shipped", "Pending")); }
    [TestMethod] public void CannotCancelShippedOrder() => Assert.IsFalse(OrderRules.IsValidStatusTransition("Shipped", "Cancelled"));
    [TestMethod] public void CanCancelOnlyUnshippedOrders() { Assert.IsTrue(OrderRules.IsValidStatusTransition("Pending", "Cancelled")); Assert.IsTrue(OrderRules.IsValidStatusTransition("Processing", "Cancelled")); Assert.IsFalse(OrderRules.IsValidStatusTransition("Cancelled", "Cancelled")); }
    [TestMethod] public void RejectsNegativePrice() => Assert.ThrowsExactly<ArgumentException>(() => OrderRules.CalculateTotal([new(1, "A", 2, -10)]));
    [TestMethod] public void ZeroStockCannotBeDeducted() => Assert.ThrowsExactly<InvalidOperationException>(() => OrderRules.DeductStock(new(1, "A", 0, 10), 1));
}
