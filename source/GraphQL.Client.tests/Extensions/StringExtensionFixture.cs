using GraphQL.Client.Extensions;
using NUnit.Framework;

namespace GraphQL.Client.tests.Extensions
{
    public class StringExtensionFixture
    {

        [TestCase("InitialUpper", ExpectedResult = "initialUpper")]
        [TestCase("initialLower", ExpectedResult = "initialLower")]
        [TestCase("alllower", ExpectedResult = "alllower")]
        [TestCase("A", ExpectedResult = "a")]
        [TestCase("a", ExpectedResult = "a")]
        [TestCase("", ExpectedResult = "")]
        public string ToCamelCaseTest(string str) {
            return str.ToCamelCase();
        }

    }
}