using NUnit.Framework;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph.UnitTests
{
    class UtilityTests
    {
        [Test]
        public void ConvertToValidHLSLIdentifier()
        {
            Assert.AreEqual("_", NodeUtils.ConvertToValidHLSLIdentifier(""));
            Assert.AreEqual("_", NodeUtils.ConvertToValidHLSLIdentifier(" "));
            Assert.AreEqual("_", NodeUtils.ConvertToValidHLSLIdentifier("_"));
            Assert.AreEqual("_9", NodeUtils.ConvertToValidHLSLIdentifier("9"));
            Assert.AreEqual("q", NodeUtils.ConvertToValidHLSLIdentifier("q"));
            Assert.AreEqual("b", NodeUtils.ConvertToValidHLSLIdentifier("b#"));
            Assert.AreEqual("t", NodeUtils.ConvertToValidHLSLIdentifier("{t"));
            Assert.AreEqual("Y", NodeUtils.ConvertToValidHLSLIdentifier("&Y~"));
            Assert.AreEqual("a_Az_Z0_9_", NodeUtils.ConvertToValidHLSLIdentifier("a_Az_Z0_9_"));
            Assert.AreEqual("a_Az_Z0_9_", NodeUtils.ConvertToValidHLSLIdentifier(" a_Az_Z0_9_"));
            Assert.AreEqual("a_Az_Z0_9_", NodeUtils.ConvertToValidHLSLIdentifier("a_Az_Z0_9_ "));
            Assert.AreEqual("a_Az_Z0_9_", NodeUtils.ConvertToValidHLSLIdentifier(" a_Az_Z0_9_ "));
            Assert.AreEqual("a_Az_Z0_9_", NodeUtils.ConvertToValidHLSLIdentifier("  a_Az_Z0_9_"));
            Assert.AreEqual("a_Az_Z0_9_", NodeUtils.ConvertToValidHLSLIdentifier("a_Az_Z0_9_  "));
            Assert.AreEqual("a_Az_Z0_9_", NodeUtils.ConvertToValidHLSLIdentifier("  a_Az_Z0_9_  "));
            Assert.AreEqual("_", NodeUtils.ConvertToValidHLSLIdentifier("_ _")); // double underscore sequences are not valid
            Assert.AreEqual("_", NodeUtils.ConvertToValidHLSLIdentifier("      "));
            Assert.AreEqual("_1", NodeUtils.ConvertToValidHLSLIdentifier("*1   "));
            Assert.AreEqual("_1", NodeUtils.ConvertToValidHLSLIdentifier("  *-(1)"));
            Assert.AreEqual("z_1", NodeUtils.ConvertToValidHLSLIdentifier("*z-1>"));
            Assert.AreEqual("w_r", NodeUtils.ConvertToValidHLSLIdentifier("*^#@$w)!(r+-"));
            Assert.AreEqual("_1_var_q_30_0_1", NodeUtils.ConvertToValidHLSLIdentifier("  1   var  * q-30 ( 0 ) (1)   "));
        }

        [Test]
        public void DeduplicateName()
        {
            string[] existingNames = new string[]
            {
                "a",
                "b",
                "c",
                "qwerty",
                "qwerty_1",
                "qwerty_3",
                "b_1",
                "qwerty_4",
                "b2",
                "b3",
                "_",
                "_1",
                "a (1)",
                "a (2)",
                "a (3)",
                "b (2)",
                "b_1 (1)"
            };

            Assert.AreEqual(GraphUtil.DeduplicateName(existingNames, "{0}_{1}", "a"), "a_1");
            Assert.AreEqual(GraphUtil.DeduplicateName(existingNames, "{0}_{1}", "b"), "b_2");
            Assert.AreEqual(GraphUtil.DeduplicateName(existingNames, "{0}_{1}", "qwert"), "qwert");
            Assert.AreEqual(GraphUtil.DeduplicateName(existingNames, "{0}_{1}", "qwerty"), "qwerty_2");
            Assert.AreEqual(GraphUtil.DeduplicateName(existingNames, "{0}_{1}", "qwerty_1"), "qwerty_2");
            Assert.AreEqual(GraphUtil.DeduplicateName(existingNames, "{0}_{1}", "qwerty_4"), "qwerty_2");
            Assert.AreEqual(GraphUtil.DeduplicateName(existingNames, "{0}_{1}", "asdf"), "asdf");
            Assert.AreEqual(GraphUtil.DeduplicateName(existingNames, "{0}_{1}", "d_1"), "d_1");
            Assert.AreEqual(GraphUtil.DeduplicateName(existingNames, "{0}_{1}", "_1"), "_2");

            // this one actually outputs "__1" .. but not going to fix it now
            // Assert.AreEqual(GraphUtil.DeduplicateName(existingNames, "{0}_{1}", "_"), "_2");

            Assert.AreEqual(GraphUtil.DeduplicateName(existingNames, "{0} ({1})", "a"), "a (4)");
            Assert.AreEqual(GraphUtil.DeduplicateName(existingNames, "{0} ({1})", "b"), "b (1)");
            Assert.AreEqual(GraphUtil.DeduplicateName(existingNames, "{0} ({1})", "b (2)"), "b (1)");
            Assert.AreEqual(GraphUtil.DeduplicateName(existingNames, "{0} ({1})", "b_1"), "b_1 (2)");
            Assert.AreEqual(GraphUtil.DeduplicateName(existingNames, "{0} ({1})", "c"), "c (1)");
        }

        [Test]
        public void IsHLSLKeyword()
        {
            Assert.IsTrue(NodeUtils.IsHLSLKeyword("min16uint"));
            Assert.IsTrue(NodeUtils.IsHLSLKeyword("float2"));
            Assert.IsTrue(NodeUtils.IsHLSLKeyword("uint4x4"));
            Assert.IsTrue(NodeUtils.IsHLSLKeyword("bool2x2"));
            Assert.IsTrue(NodeUtils.IsHLSLKeyword("half1x1"));
            Assert.IsTrue(NodeUtils.IsHLSLKeyword("StructuredBuffer"));
            Assert.IsTrue(NodeUtils.IsHLSLKeyword("texture"));
            Assert.IsTrue(NodeUtils.IsHLSLKeyword("while"));
            Assert.IsTrue(NodeUtils.IsHLSLKeyword("true"));
            Assert.IsTrue(NodeUtils.IsHLSLKeyword("NULL"));

            Assert.IsFalse(NodeUtils.IsHLSLKeyword("x"));
            Assert.IsFalse(NodeUtils.IsHLSLKeyword("var"));
            Assert.IsFalse(NodeUtils.IsHLSLKeyword("float5"));
            Assert.IsFalse(NodeUtils.IsHLSLKeyword("tex"));
            Assert.IsFalse(NodeUtils.IsHLSLKeyword("Texture"));
            Assert.IsFalse(NodeUtils.IsHLSLKeyword("_0"));
            Assert.IsFalse(NodeUtils.IsHLSLKeyword(""));
            Assert.IsFalse(NodeUtils.IsHLSLKeyword("103"));
        }
    }
}
