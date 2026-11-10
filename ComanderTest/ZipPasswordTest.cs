using Comander.View;
using NUnit.Framework;

namespace ComanderTest
{
    [TestFixture]
    public class ZipPasswordTest
    {
        [TestCase("", "", "")]
        [TestCase(null, null, "")]
        [TestCase("pw", "pw", "pw")]
        public void Valid(string password, string confirmation, string expected)
        {
            Assert.That(ZipWindow.ValidatePasswords(password, confirmation, out var error), Is.EqualTo(expected));
            Assert.That(error, Is.Null);
        }

        [TestCase("pw", "")]
        [TestCase("", "pw")]
        [TestCase("pw", "other")]
        public void Invalid_IsReportedNotDropped(string password, string confirmation)
        {
            Assert.That(ZipWindow.ValidatePasswords(password, confirmation, out var error), Is.Null);
            Assert.That(error, Is.Not.Null);
        }
    }
}
