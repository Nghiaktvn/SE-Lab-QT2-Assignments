using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lab.Intelligence;
namespace Wholesale.Tests;
[TestClass]
public class AssistantTests
{
 private static readonly KnowledgeEntry[] Knowledge=[new("stock","stock inventory available ton kho con hang","Stock is checked again at checkout."),new("payment","payment card wallet thanh toan the vi","Payment is simulated."),new("print","print invoice receipt in hoa don","Open the print preview.")];
 [TestMethod] public void UnderstandsVietnameseWithoutAccentMismatch()=>Assert.AreEqual("payment",KnowledgeAssistant.Ask("Thanh toán bằng thẻ?",Knowledge).Topic);
 [TestMethod] public void AnswersStockFromSuppliedKnowledge()=>Assert.AreEqual("stock",KnowledgeAssistant.Ask("available stock",Knowledge).Topic);
 [TestMethod] public void UnknownQuestionDoesNotInventAnswer()=>Assert.AreEqual("unknown",KnowledgeAssistant.Ask("quantum astronomy",Knowledge).Topic);
 [TestMethod] public void EmptyQuestionReturnsSafeFallback()=>Assert.AreEqual("unknown",KnowledgeAssistant.Ask("",Knowledge).Topic);
 [TestMethod] public void RecommendationsRespectBudget()=>Assert.AreEqual(1,KnowledgeAssistant.Recommend("coffee under 200k",[new(1,"Robusta Coffee",185000,4),new(2,"Arabica Coffee",245000,5)]).Count);
 [TestMethod] public void NeverRecommendsUnavailableStock()=>Assert.AreEqual(0,KnowledgeAssistant.Recommend("coffee",[new(1,"Robusta Coffee",185000,0)]).Count);
 [TestMethod] public void VietnameseProductRecommendationUsesRealPrice()=>Assert.AreEqual(2,KnowledgeAssistant.Recommend("cà phê dưới 250k",[new(1,"Robusta Coffee",185000,4),new(2,"Arabica Coffee",245000,5)]).Count);
}
