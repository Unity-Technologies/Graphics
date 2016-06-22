using NUnit.Framework;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.UnitTests
{
    [TestFixture]
    public class ShaderGeneratorTests
    {
        [TestFixtureSetUp]
        public void RunBeforeAnyTests()
        {
            Debug.logger.logHandler = new ConsoleLogHandler();
        }

        [Test]
        public void TestTexturePropertyChunkGeneratesValidPropertyStringNotHiddenNotModifiable()
        {
            var chunk = new TexturePropertyChunk("TexProp", "Description", null, TextureType.White, false, false);
            Assert.AreEqual("[NonModifiableTextureData] TexProp(\"Description\", 2D) = \"white\" {}" , chunk.GetPropertyString());
            Assert.IsFalse(chunk.modifiable);
            Assert.IsFalse(chunk.hidden);
        }

        [Test]
        public void TestTexturePropertyChunkGeneratesValidPropertyStringHiddenNotModifiable()
        {
            var chunk = new TexturePropertyChunk("TexProp", "Description", null, TextureType.White, true, false);
            Assert.AreEqual("[HideInInspector] [NonModifiableTextureData] TexProp(\"Description\", 2D) = \"white\" {}", chunk.GetPropertyString());
            Assert.IsFalse(chunk.modifiable);
            Assert.IsTrue(chunk.hidden);
        }

        [Test]
        public void TestColorPropertyChunkGeneratesValidPropertyString()
        {
            var chunk = new ColorPropertyChunk("ColProp", "Description", Color.green, false);
            Assert.AreEqual("ColProp(\"Description\", Color) = (0,1,0,1)", chunk.GetPropertyString());
        }

        [Test]
        public void TestFloatPropertyChunkGeneratesValidPropertyString()
        {
            var chunk = new FloatPropertyChunk("FloatProp", "Description", 0.3f, false);
            Assert.AreEqual("FloatProp(\"Description\", Float) = 0.3", chunk.GetPropertyString());
        }

        [Test]
        public void VectorPropertyChunkGeneratesValidPropertyString()
        {
            var chunk = new VectorPropertyChunk("VectorProp", "Description", new Vector4(0, 0, 1, 0), false);
            Assert.AreEqual("VectorProp(\"Description\", Vector) = (0,0,1,0)", chunk.GetPropertyString());
        }

        class TestNode : AbstractMaterialNode
        {
            public TestNode()
            {
                AddSlot(new MaterialSlot("V1Out", "V1Out", SlotType.Output, 0, SlotValueType.Vector1, Vector4.zero));
                AddSlot(new MaterialSlot("V2Out", "V2Out", SlotType.Output, 1, SlotValueType.Vector2, Vector4.zero));
                AddSlot(new MaterialSlot("V3Out", "V3Out", SlotType.Output, 2, SlotValueType.Vector3, Vector4.zero));
                AddSlot(new MaterialSlot("V4Out", "V4Out", SlotType.Output, 3, SlotValueType.Vector4, Vector4.zero));
            }
        }

        [Test]
        public void AdaptNodeOutput1To1Works()
        {
            var node = new TestNode();
            var slot = node.FindOutputSlot<MaterialSlot>("V1Out");
            var result = ShaderGenerator.AdaptNodeOutput(node, slot, ConcreteSlotValueType.Vector1);
            Assert.AreEqual(string.Format("{0}", node.GetOutputVariableNameForSlot(slot)), result);
        }

        [Test]
        public void AdaptNodeOutput1To2Works()
        {
            var node = new TestNode();
            var slot = node.FindOutputSlot<MaterialSlot>("V1Out");
            var result = ShaderGenerator.AdaptNodeOutput(node, slot, ConcreteSlotValueType.Vector2);
            Assert.AreEqual(string.Format("({0})", node.GetOutputVariableNameForSlot(slot)), result);
        }

        [Test]
        public void AdaptNodeOutput1To3Works()
        {
            var node = new TestNode();
            var slot = node.FindOutputSlot<MaterialSlot>("V1Out");
            var result = ShaderGenerator.AdaptNodeOutput(node, slot, ConcreteSlotValueType.Vector3);
            Assert.AreEqual(string.Format("({0})", node.GetOutputVariableNameForSlot(slot)), result);
        }

        [Test]
        public void AdaptNodeOutput1To4Works()
        {
            var node = new TestNode();
            var slot = node.FindOutputSlot<MaterialSlot>("V1Out");
            var result = ShaderGenerator.AdaptNodeOutput(node, slot, ConcreteSlotValueType.Vector4);
            Assert.AreEqual(string.Format("({0})", node.GetOutputVariableNameForSlot(slot)), result);
        }

        [Test]
        public void AdaptNodeOutput2To1Works()
        {
            var node = new TestNode();
            var slot = node.FindOutputSlot<MaterialSlot>("V2Out");
            var result = ShaderGenerator.AdaptNodeOutput(node, slot, ConcreteSlotValueType.Vector1);
            Assert.AreEqual(string.Format("({0}).x", node.GetOutputVariableNameForSlot(slot)), result);
        }

        [Test]
        public void AdaptNodeOutput2To2Works()
        {
            var node = new TestNode();
            var slot = node.FindOutputSlot<MaterialSlot>("V2Out");
            var result = ShaderGenerator.AdaptNodeOutput(node, slot, ConcreteSlotValueType.Vector2);
            Assert.AreEqual(string.Format("{0}", node.GetOutputVariableNameForSlot(slot)), result);
        }

        [Test]
        public void AdaptNodeOutput2To3Fails()
        {
            var node = new TestNode();
            var slot = node.FindOutputSlot<MaterialSlot>("V2Out");
            var result = ShaderGenerator.AdaptNodeOutput(node, slot, ConcreteSlotValueType.Vector3);
            Assert.AreEqual("ERROR!", result);
        }

        [Test]
        public void AdaptNodeOutput2To4Fails()
        {
            var node = new TestNode();
            var slot = node.FindOutputSlot<MaterialSlot>("V2Out");
            var result = ShaderGenerator.AdaptNodeOutput(node, slot, ConcreteSlotValueType.Vector4);
            Assert.AreEqual("ERROR!", result);
        }

        [Test]
        public void AdaptNodeOutput3To1Works()
        {
            var node = new TestNode();
            var slot = node.FindOutputSlot<MaterialSlot>("V3Out");
            var result = ShaderGenerator.AdaptNodeOutput(node, slot, ConcreteSlotValueType.Vector1);
            Assert.AreEqual(string.Format("({0}).x", node.GetOutputVariableNameForSlot(slot)), result);
        }

        [Test]
        public void AdaptNodeOutput3To2Works()
        {
            var node = new TestNode();
            var slot = node.FindOutputSlot<MaterialSlot>("V3Out");
            var result = ShaderGenerator.AdaptNodeOutput(node, slot, ConcreteSlotValueType.Vector2);
            Assert.AreEqual(string.Format("({0}.xy)", node.GetOutputVariableNameForSlot(slot)), result);
        }

        [Test]
        public void AdaptNodeOutput3To3Works()
        {
            var node = new TestNode();
            var slot = node.FindOutputSlot<MaterialSlot>("V3Out");
            var result = ShaderGenerator.AdaptNodeOutput(node, slot, ConcreteSlotValueType.Vector3);
            Assert.AreEqual(string.Format("{0}", node.GetOutputVariableNameForSlot(slot)), result);
        }

        [Test]
        public void AdaptNodeOutput3To4Fails()
        {
            var node = new TestNode();
            var slot = node.FindOutputSlot<MaterialSlot>("V3Out");
            var result = ShaderGenerator.AdaptNodeOutput(node, slot, ConcreteSlotValueType.Vector4);
            Assert.AreEqual("ERROR!", result);
        }

        [Test]
        public void AdaptNodeOutput4To1Works()
        {
            var node = new TestNode();
            var slot = node.FindOutputSlot<MaterialSlot>("V4Out");
            var result = ShaderGenerator.AdaptNodeOutput(node, slot, ConcreteSlotValueType.Vector1);
            Assert.AreEqual(string.Format("({0}).x", node.GetOutputVariableNameForSlot(slot)), result);
        }

        [Test]
        public void AdaptNodeOutput4To2Works()
        {
            var node = new TestNode();
            var slot = node.FindOutputSlot<MaterialSlot>("V4Out");
            var result = ShaderGenerator.AdaptNodeOutput(node, slot, ConcreteSlotValueType.Vector2);
            Assert.AreEqual(string.Format("({0}.xy)", node.GetOutputVariableNameForSlot(slot)), result);
        }

        [Test]
        public void AdaptNodeOutput4To3Works()
        {
            var node = new TestNode();
            var slot = node.FindOutputSlot<MaterialSlot>("V4Out");
            var result = ShaderGenerator.AdaptNodeOutput(node, slot, ConcreteSlotValueType.Vector3);
            Assert.AreEqual(string.Format("({0}.xyz)", node.GetOutputVariableNameForSlot(slot)), result);
        }

        [Test]
        public void AdaptNodeOutput4To4Works()
        {
            var node = new TestNode();
            var slot = node.FindOutputSlot<MaterialSlot>("V4Out");
            var result = ShaderGenerator.AdaptNodeOutput(node, slot, ConcreteSlotValueType.Vector4);
            Assert.AreEqual(string.Format("{0}", node.GetOutputVariableNameForSlot(slot)), result);
        }



    }
}
