using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXCustomAttributeInvalidationTest
    {
        [TearDown]
        public void CleanUp()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [Test]
        public void Adding_Custom_Attribute_Dirties_Expression_Graph()
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();
            graph.SetExpressionGraphDirty(false);
            Assert.IsFalse(graph.IsExpressionGraphDirty());

            Assert.IsTrue(graph.TryAddCustomAttribute("custom", VFXValueType.Float3, string.Empty, false, out _));
            Assert.IsTrue(graph.IsExpressionGraphDirty());
        }

        [Test]
        public void Renaming_Custom_Attribute_Dirties_Expression_Graph()
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();
            Assert.IsTrue(graph.TryAddCustomAttribute("custom", VFXValueType.Float3, string.Empty, false, out _));
            graph.SetExpressionGraphDirty(false);
            Assert.IsFalse(graph.IsExpressionGraphDirty());

            Assert.IsTrue(graph.TryRenameCustomAttribute("custom", "renamed"));
            Assert.IsTrue(graph.IsExpressionGraphDirty());
        }

        [Test]
        public void Adding_Custom_Attribute_Refreshes_Custom_HLSL_Node()
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var update = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            var hlslBlock = ScriptableObject.CreateInstance<Block.CustomHLSL>();
            graph.AddChild(update);
            update.AddChild(hlslBlock);

            hlslBlock.SetSettingValue("m_HLSLCode",
                "void TestFunction(inout VFXAttributes attributes, in float param)\n" +
                "{\n" +
                "  attributes.position = attributes.custom * param;\n" +
                "}");
            hlslBlock.Invalidate(VFXModel.InvalidationCause.kSettingChanged);

            // "custom" is unresolved, so it never lands in the parsed attributes.
            Assert.IsFalse(hlslBlock.attributes.Any(x => x.attrib.name == "custom"));

            Assert.IsTrue(graph.TryAddCustomAttribute("custom", VFXValueType.Float3, string.Empty, false, out _));

            Assert.IsTrue(hlslBlock.attributes.Any(x => x.attrib.name == "custom"));
        }

        [Test]
        public void Changing_Custom_Attribute_Type_Refreshes_Custom_HLSL_Node()
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var update = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            var hlslBlock = ScriptableObject.CreateInstance<Block.CustomHLSL>();
            graph.AddChild(update);
            update.AddChild(hlslBlock);

            Assert.IsTrue(graph.TryAddCustomAttribute("custom", VFXValueType.Float, string.Empty, false, out _));

            hlslBlock.SetSettingValue("m_HLSLCode",
                "void TestFunction(inout VFXAttributes attributes, in float param)\n" +
                "{\n" +
                "  attributes.position = attributes.custom * param;\n" +
                "}");
            hlslBlock.Invalidate(VFXModel.InvalidationCause.kSettingChanged);
            Assert.AreEqual(VFXValueType.Float, hlslBlock.attributes.Single(x => x.attrib.name == "custom").attrib.type);

            Assert.IsTrue(graph.TryUpdateCustomAttribute("custom", Block.CustomAttributeUtility.GetSignature(VFXValueType.Float3), string.Empty));

            Assert.AreEqual(VFXValueType.Float3, hlslBlock.attributes.Single(x => x.attrib.name == "custom").attrib.type);
        }
    }
}
