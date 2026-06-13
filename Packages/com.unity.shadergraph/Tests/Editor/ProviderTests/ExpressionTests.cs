using System.Collections.Generic;
using NUnit.Framework;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.ProviderSystem.Tests
{
    [TestFixture]
    class ExpressionTests
    {
        // Adapter that bridges the shader-reserved keyword check to the production
        // ExpressionProvider.IKeywordFilter contract.
        sealed class ShaderKeywordFilter : ExpressionProvider.IKeywordFilter
        {
            public bool IsReserved(string name) =>
                NodeUtils.IsShaderLabKeyWord(name) || NodeUtils.IsShaderGraphKeyWord(name) || NodeUtils.IsHLSLKeyword(name);
        }

        static readonly ShaderKeywordFilter s_keywordFilter = new();

        private static void DoTest(string expression, string[] expectedParamNames, string expectedExpression = null)
        {
            List<IShaderField> fields = new();
            var type = new ShaderType("float");
            if (expectedParamNames != null)
                foreach (var name in expectedParamNames)
                    fields.Add(new ShaderField(name, true, false, type, null));

            if (string.IsNullOrEmpty(expectedExpression))
                expectedExpression = expression;

            // Hints not compared here — generateHints:false excludes them from the code comparison.
            var expected = new ShaderFunction("Test", null, fields, type, $"return {expectedExpression};", null);

            // this operation produces a 'cleaned' expression; for now, we're just stripping comments
            // For now, we allow keywords and illegal characters (even semicolons), though they ought to
            // result in compilation failure and give appropriate feedback to the user.
            var actual = ExpressionProvider.ExpressionToShaderFunction("Test", expression, "float", out var actualExpression, s_keywordFilter);

            Assert.AreEqual(expectedExpression, actualExpression);

            // the code generated from the actual/expected shader functions should be identical.
            string expectedCode = ShaderObjectUtils.GenerateCode(expected, false, false, false);
            string actualCode = ShaderObjectUtils.GenerateCode(actual, false, false, false);

            Assert.AreEqual(expectedCode, actualCode);

            // check that we get the appropriate number of slots.
            var node = new ExpressionNode();
            node.InitializeFromProvider(new ExpressionProvider());
            node.Expression = expression;
            List<MaterialSlot> slots = new();
            node.GetSlots(slots);

            int expectedSlots = 1; // +1 for return output.
            foreach (var i in expected.Parameters)
                ++expectedSlots;

            Assert.AreEqual(expectedSlots, slots.Count, $"Slots generated from {expression} was unexpected.");
        }


        [Test]
        public void ExpressionTest()
        {
            // some basic binary operations
            DoTest("a - b", new[] { "a", "b" });
            DoTest("a * b", new[] { "a", "b" });
            DoTest("a / b", new[] { "a", "b" });
            DoTest("a % b", new[] { "a", "b" });
            DoTest("a + b", new[] { "a", "b" });

            // float literal and function calls
            DoTest("asint(4.5f) + cos(45)", null);
            DoTest("asint(4.5f) + cos(pi)", new[] { "pi" });

            // check function call with multiple parametrs
            DoTest("dot(a, b*c)", new[] { "a", "b", "c" });

            // check that member access operators aren't treated as identifiers
            DoTest("a + b.xyz * c.xyz[0]", new[] { "a", "b", "c" });

            // check that comments are stripped
            DoTest("a +/* b */ c// d", new[] { "a", "c" }, "a + c");

            // check that keywords aren't interpreted as parameters
            DoTest("(float)a", new[] { "a" });

            // check that we deduplicate parameters
            DoTest("a + a + a + b + b", new[] { "a", "b" });
        }
    }
}
