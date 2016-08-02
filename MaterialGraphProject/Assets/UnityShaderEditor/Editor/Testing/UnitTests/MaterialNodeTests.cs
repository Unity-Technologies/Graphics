using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.UnitTests
{
    [TestFixture]
    public class MaterialNodeTests
    {
        private PixelGraph m_Graph;
        private TestNode m_NodeA;

        class TestNode : AbstractMaterialNode
        {
            public const int V1Out = 0;
            public const int V1In = 1;

            public TestNode()
            {
                AddSlot(new MaterialSlot(V1Out, "V1Out", "V1Out", SlotType.Output, SlotValueType.Vector1, Vector4.zero));
                AddSlot(new MaterialSlot(V1In, "V1In", "V1In", SlotType.Input, SlotValueType.Vector1, Vector4.zero));
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
        public void GetVariableNameForSlotThrowsWhenInvalid()
        {
            Assert.Throws<ArgumentException>(() => m_NodeA.GetVariableNameForSlot(666));
        }

        [Test]
        public void AddingNonMaterialSlotToNodeThrows()
        {
            Assert.Throws<ArgumentException>(() => m_NodeA.AddSlot(new SerializableSlot(0, string.Empty, SlotType.Input)));
        }

        [Test]
        public void ReplacingMaterialSlotPreservesTheOldCurrentValue()
        {
            m_NodeA.AddSlot(new MaterialSlot(TestNode.V1In, "V1In", "V1In", SlotType.Input, SlotValueType.Vector1, Vector4.one));
            Assert.AreEqual(2, m_NodeA.GetSlots<MaterialSlot>().Count());
            Assert.AreEqual(1, m_NodeA.GetInputSlots<MaterialSlot>().Count());

            var slot = m_NodeA.GetInputSlots<MaterialSlot>().FirstOrDefault();
            Assert.AreEqual(Vector4.one, slot.defaultValue);
            Assert.AreEqual(Vector4.zero, slot.currentValue);
        }

        [Test]
        public void CanConvertConcreteSlotValueTypeToOutputChunkProperly()
        {
            Assert.AreEqual(string.Empty, AbstractMaterialNode.ConvertConcreteSlotValueTypeToString(ConcreteSlotValueType.Vector1));
            Assert.AreEqual("2", AbstractMaterialNode.ConvertConcreteSlotValueTypeToString(ConcreteSlotValueType.Vector2));
            Assert.AreEqual("3", AbstractMaterialNode.ConvertConcreteSlotValueTypeToString(ConcreteSlotValueType.Vector3));
            Assert.AreEqual("4", AbstractMaterialNode.ConvertConcreteSlotValueTypeToString(ConcreteSlotValueType.Vector4));
            Assert.AreEqual("Error", AbstractMaterialNode.ConvertConcreteSlotValueTypeToString(ConcreteSlotValueType.Error));
        }

        [Test]
        public void CanGetDefaultInputsFromNodeAsPreviewProperties()
        {
            var properties = new List<PreviewProperty>();
            m_NodeA.CollectPreviewMaterialProperties(properties);
            var slot = m_NodeA.GetInputSlots<MaterialSlot>().FirstOrDefault();

            Assert.AreEqual(1, properties.Count);
            var pp = properties.FirstOrDefault();

            Assert.AreEqual(m_NodeA.GetVariableNameForSlot(slot.id), pp.m_Name);
            Assert.AreEqual(PropertyType.Vector4, pp.m_PropType);
            Assert.AreEqual(slot.currentValue, pp.m_Vector4);
        }

        [Test]
        public void CanGetDefaultSlotValueWhenNoEdgesConnected()
        {
            string expected = string.Format("{0}", m_NodeA.GetVariableNameForSlot(TestNode.V1In));

            var slot = m_NodeA.GetInputSlots<MaterialSlot>().FirstOrDefault();
            var result = m_NodeA.GetSlotValue(slot.id, GenerationMode.Preview2D);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void NodeGenerateCorrectPreviewPropertyUsages()
        {
            string expected = string.Format("{0} {1};{2}", AbstractMaterialNode.OutputPrecision.@fixed, m_NodeA.GetVariableNameForSlot(TestNode.V1In), Environment.NewLine);
            var visitor = new ShaderGenerator();
            m_NodeA.precision = AbstractMaterialNode.OutputPrecision.@fixed;
            m_NodeA.GeneratePropertyUsages(visitor, GenerationMode.Preview2D);
            Assert.AreEqual(expected, visitor.GetShaderString(0));

            expected = string.Format("{0} {1};{2}", AbstractMaterialNode.OutputPrecision.@float, m_NodeA.GetVariableNameForSlot(TestNode.V1In), Environment.NewLine);
            visitor = new ShaderGenerator();
            m_NodeA.precision = AbstractMaterialNode.OutputPrecision.@float;
            m_NodeA.GeneratePropertyUsages(visitor, GenerationMode.Preview2D);
            Assert.AreEqual(expected, visitor.GetShaderString(0));

            expected = string.Format("{0} {1};{2}", AbstractMaterialNode.OutputPrecision.half, m_NodeA.GetVariableNameForSlot(TestNode.V1In), Environment.NewLine);
            visitor = new ShaderGenerator();
            m_NodeA.precision = AbstractMaterialNode.OutputPrecision.half;
            m_NodeA.GeneratePropertyUsages(visitor, GenerationMode.Preview2D);
            Assert.AreEqual(expected, visitor.GetShaderString(0));
        }
    }
}
