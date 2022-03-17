using NUnit.Framework;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Misc
{
    [TestFixture]
    public class MathExpressionParserTests
    {
        static string Format(IExpression n) => n.ToString();

        [TestCase("float2(1,3)", "#float2(1, 3)")]
        [TestCase("float4(normalize(x + 2) + y,1,2,3 )", "#float4((#normalize(($x + 2)) + $y), 1, 2, 3)")]
        [TestCase("float2(normalize(x) + y,1)", "#float2((#normalize($x) + $y), 1)")]
        [TestCase("float4(normalize(x + 2),1,2,3 )", "#float4(#normalize(($x + 2)), 1, 2, 3)")]
        [TestCase("float4(0,1,2,3 )", "#float4(0, 1, 2, 3)")]
        [TestCase("(1)", "1")]
        [TestCase("abs(abs(x)/b+(a))", "#abs(((#abs($x) / $b) + $a))")]
        [TestCase("abs(x/b+(a))", "#abs((($x / $b) + $a))")]
        [TestCase("abs((a))", "#abs($a)")]
        [TestCase("ABS((a))", "#abs($a)")]
        [TestCase("abs(((a)))", "#abs($a)")]
        [TestCase("1 * (2 + 3)", "(1 * (2 + 3))")]
        [TestCase("(1) * ((2) + (3))", "(1 * (2 + 3))")]
        [TestCase("(1 + 2) * 3", "((1 + 2) * 3)")]
        [TestCase("(sin(a+b)/(c%3))", "(#sin(($a + $b)) / ($c % 3))")]
        [TestCase("1.2 + 3.45", "(1.2 + 3.45)")]
        public void Parse(string input, string formatted)
        {
            var n = MathExpressionParser.Parse(input, out var err);
            Assert.IsNull(err, $"The expression was expected to be valid, but failed to parse: '{err}'");
            Assert.AreEqual(formatted, Format(n));
        }

        [TestCase("float4(normalize(x + 2) + y,9,9,9")]
        [TestCase("((1)")]
        [TestCase("(1))")]
        [TestCase("(1")]
        [TestCase("1)")]
        [TestCase("abs(abs(x)/b+(a)))")]
        [TestCase("1.2.3")]
        public void InvalidExpressions(string input)
        {
            var n = MathExpressionParser.Parse(input, out var err);
            Assert.IsNotNull(err, $"The expression was expected to be invalid, but was parsed: '{n}'");
        }
    }
}
