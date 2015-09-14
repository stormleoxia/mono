using System.Web.RegularExpressions;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class AspCodeRegexTest
    {
        [Test]
        public void CheckBasicAspWorks()
        {
            AspCodeRegex regex = new AspCodeRegex();
            var responseWriteHelloWord = "<% response.write(\"Hello Word\") %>";
            var match = regex.Match(responseWriteHelloWord);
            var capture = match.Captures;
            var value = match.Value;
            Assert.AreEqual(responseWriteHelloWord, value);
        }

        [Test]
        public void CheckNonAspIsNotDetected()
        {
            AspCodeRegex regex = new AspCodeRegex();
            var responseWriteHelloWord = "<% response.write(\"Hello Word\") ";
            var match = regex.Match(responseWriteHelloWord);
            var capture = match.Captures;
            var value = match.Value;
            Assert.AreEqual(string.Empty, value);
        }
    }

}
