using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wholesale.Domain;
namespace Wholesale.Tests;

[TestClass]
public class PasswordHashTests
{
    [TestMethod] public void CorrectPasswordMatches() => Assert.IsTrue(PasswordHash.Matches("Demo123!", PasswordHash.Create("Demo123!")));
    [TestMethod] public void WrongPasswordDoesNotMatch() => Assert.IsFalse(PasswordHash.Matches("wrong-pass", PasswordHash.Create("Demo123!")));
    [TestMethod] public void SaltMakesHashesUnique() => Assert.AreNotEqual(PasswordHash.Create("Demo123!"), PasswordHash.Create("Demo123!"));
    [TestMethod] public void RejectsEmptyHashPayload() => Assert.IsFalse(PasswordHash.Matches("wrong", "pbkdf2$120000$$"));
    [TestMethod] public void RejectsMalformedBase64() => Assert.IsFalse(PasswordHash.Matches("wrong", "pbkdf2$120000$!!!!!$!!!!"));
    [TestMethod] public void RejectsUnboundedIterations() => Assert.IsFalse(PasswordHash.Matches("wrong", "pbkdf2$2147483647$AAAA$AAAA"));
    [TestMethod] public void RejectsShortPasswordAtCreation() => Assert.ThrowsExactly<ArgumentException>(() => PasswordHash.Create("short"));
}
