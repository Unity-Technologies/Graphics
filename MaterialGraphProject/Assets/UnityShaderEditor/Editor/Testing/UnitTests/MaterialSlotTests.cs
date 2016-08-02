using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.UnitTests
{
    [TestFixture]
    public class MaterialSlotTests
    {
        private PixelGraph m_Graph;
        private TestNode m_NodeA;

        class TestNode : AbstractMaterialNode
        {
            public const int V1In = 1;

            public readonly MaterialSlot slot;

            public TestNode()
            {
                slot = new MaterialSlot(V1In, "V1In", "V1In", SlotType.Input, SlotValueType.Vector1, Vector4.zero);
                AddSlot(slot);
            }
        }

        [TestFixtureSetUp]
        public void RunBeforeAnyTests()
        {
            Debug.logger.logHandler = new ConsoleLogHandler();
        }

        [SetUp]
        public void TestSetUp()
        {
            m_Graph = new PixelGraph();
            m_NodeA = new TestNode();
            m_Graph.AddNode(m_NodeA);
        }

        [Test]
        public void CanUpdateMaterialSlotDisplayName()
        {
            var displayName = "New Display Name";

            var slot = m_NodeA.slot;
            slot.displayName = displayName;

            slot.valueType = SlotValueType.Vector1;
            Assert.AreEqual(displayName + "(1)", slot.displayName);

            slot.valueType = SlotValueType.Vector2;
            Assert.AreEqual(displayName + "(2)", slot.displayName);

            slot.valueType = SlotValueType.Vector3;
            Assert.AreEqual(displayName + "(3)", slot.displayName);

            slot.valueType = SlotValueType.Vector4;
            Assert.AreEqual(displayName + "(4)", slot.displayName);

            slot.valueType = SlotValueType.Dynamic;
            Assert.AreEqual(displayName + "(4)", slot.displayName);
        }

        [Test]
        public void CanUpdateMaterialSlotDefaultValue()
        {
            var slot = m_NodeA.slot;
            slot.defaultValue = Vector4.one;
            Assert.AreEqual(Vector4.one, slot.defaultValue);
        }

        [Test]
        public void CanUpdateMaterialSlotCurrentValue()
        {
            var slot = m_NodeA.slot;
            slot.currentValue = Vector4.one;
            Assert.AreEqual(Vector4.one, slot.currentValue);
        }

        [Test]
        public void CanUpdateMaterialSlotValueType()
        {
            var slot = m_NodeA.slot;
            slot.valueType = SlotValueType.Vector3;
            Assert.AreEqual(SlotValueType.Vector3, slot.valueType);
        }

        [Test]
        public void CanUpdateMaterialShaderOutputName()
        {
            var shaderName = "NewShaderOutputName";

            var slot = m_NodeA.slot;
            slot.shaderOutputName = shaderName;
            Assert.AreEqual(shaderName, slot.shaderOutputName);
        }

        [Test]
        public void MaterialSlotCanGeneratePropertyUsagesForPreview()
        {
            string expected = string.Format("{0} {1};{2}", m_NodeA.precision, m_NodeA.GetVariableNameForSlot(TestNode.V1In), Environment.NewLine);

            var slot = m_NodeA.slot;
            var visitor = new ShaderGenerator();
            slot.GeneratePropertyUsages(visitor, GenerationMode.Preview2D);
            Assert.AreEqual(expected, visitor.GetShaderString(0));
        }

        [Test]
        public void MaterialSlotReturnsValidDefaultValue()
        {
            string expected = string.Format("{0}", m_NodeA.GetVariableNameForSlot(TestNode.V1In));

            var slot = m_NodeA.slot;
            var result = slot.GetDefaultValue(GenerationMode.Preview2D);
            Assert.AreEqual(expected, result);

            slot.valueType = SlotValueType.Vector1;
            slot.currentValue = new Vector4(6, 6, 6, 1);
            result = slot.GetDefaultValue(GenerationMode.SurfaceShader);
            Assert.AreEqual("6", result);

            slot.valueType = SlotValueType.Vector2;
            slot.currentValue = new Vector4(6, 6, 6, 1);
            result = slot.GetDefaultValue(GenerationMode.SurfaceShader);
            Assert.AreEqual("half2 (6,6)", result);

            slot.valueType = SlotValueType.Vector3;
            slot.currentValue = new Vector4(6, 6, 6, 1);
            result = slot.GetDefaultValue(GenerationMode.SurfaceShader);
            Assert.AreEqual("half3 (6,6,6)", result);

            slot.valueType = SlotValueType.Vector4;
            slot.currentValue = new Vector4(6, 6, 6, 1);
            result = slot.GetDefaultValue(GenerationMode.SurfaceShader);
            Assert.AreEqual("half4 (6,6,6,1)", result);
        }

        [Test]
        public void MaterialSlotThrowsWhenNoOwner()
        {
            var slot = new MaterialSlot(0, string.Empty, string.Empty, SlotType.Input, SlotValueType.Vector1, Vector4.zero);
            Assert.Throws<Exception>(() => slot.GeneratePropertyUsages(new ShaderGenerator(), GenerationMode.Preview2D));
            Assert.Throws<Exception>(() => slot.GetDefaultValue(GenerationMode.Preview2D));
        }
    }
}
