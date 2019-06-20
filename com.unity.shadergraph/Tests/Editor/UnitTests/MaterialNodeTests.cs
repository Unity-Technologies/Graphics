using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    class MaterialNodeTests
    {
        private GraphData m_Graph;
        private TestNode m_NodeA;

        class TestNode : AbstractMaterialNode
        {
            public const int V1Out = 0;
            public const int V1In = 1;

            public TestNode()
            {
                AddSlot(new Vector1MaterialSlot(V1Out, "V1Out", "V1Out", SlotType.Output, 0));
                AddSlot(new Vector1MaterialSlot(V1In, "V1In", "V1In", SlotType.Input, 0));
            }
        }

        class NotAMaterialSlot : ISlot
        {
            public bool Equals(ISlot other)
            {
                throw new NotImplementedException();
            }

            public int id { get; }
            public string displayName { get; set; }
            public bool isInputSlot { get; }
            public bool isOutputSlot { get; }
            public int priority { get; set; }
            public SlotReference slotReference { get; }
            public AbstractMaterialNode owner { get; set; }
            public bool hidden { get; set; }
        }

        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            Debug.unityLogger.logHandler = new ConsoleLogHandler();
        }

        [SetUp]
        public void TestSetUp()
        {
            m_Graph = new GraphData();
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
            Assert.Throws<ArgumentException>(() => m_NodeA.AddSlot(new NotAMaterialSlot()));
        }

        [Test]
        public void ReplacingMaterialSlotPreservesTheOldCurrentValue()
        {
            m_NodeA.AddSlot(new Vector1MaterialSlot(TestNode.V1In, "V1In", "V1In", SlotType.Input, 1));
            Assert.AreEqual(2, m_NodeA.GetSlots<MaterialSlot>().Count());
            Assert.AreEqual(1, m_NodeA.GetInputSlots<MaterialSlot>().Count());

            var slot = m_NodeA.GetInputSlots<Vector1MaterialSlot>().FirstOrDefault();
            Assert.AreEqual(1, slot.defaultValue);
            Assert.AreEqual(0, slot.value);
        }

        [Test]
        public void CanConvertConcreteSlotValueTypeToOutputChunkProperly()
        {
            Assert.AreEqual("float", NodeUtils.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, ConcreteSlotValueType.Vector1));
            Assert.AreEqual("float", NodeUtils.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, ConcreteSlotValueType.Boolean));
            Assert.AreEqual("float2", NodeUtils.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, ConcreteSlotValueType.Vector2));
            Assert.AreEqual("float3", NodeUtils.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, ConcreteSlotValueType.Vector3));
            Assert.AreEqual("float4", NodeUtils.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, ConcreteSlotValueType.Vector4));
            Assert.AreEqual("Texture2D", NodeUtils.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, ConcreteSlotValueType.Texture2D));
            Assert.AreEqual("float2x2", NodeUtils.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, ConcreteSlotValueType.Matrix2));
            Assert.AreEqual("float3x3", NodeUtils.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, ConcreteSlotValueType.Matrix3));
            Assert.AreEqual("float4x4", NodeUtils.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, ConcreteSlotValueType.Matrix4));
            Assert.AreEqual("SamplerState", NodeUtils.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, ConcreteSlotValueType.SamplerState));
            Assert.AreEqual("Cubemap", NodeUtils.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, ConcreteSlotValueType.Cubemap));
        }

        [Test]
        public void CanGetDefaultInputsFromNodeAsPreviewProperties()
        {
            var properties = new List<PreviewProperty>();
            m_NodeA.CollectPreviewMaterialProperties(properties);
            var slot = m_NodeA.GetInputSlots<Vector1MaterialSlot>().FirstOrDefault();

            Assert.AreEqual(1, properties.Count);
            var pp = properties.FirstOrDefault();

            Assert.AreEqual(m_NodeA.GetVariableNameForSlot(slot.id), pp.name);
            Assert.AreEqual(PropertyType.Vector1, pp.propType);
            Assert.AreEqual(slot.value, pp.floatValue);
        }

        [Test]
        public void CanGetDefaultSlotValueWhenNoEdgesConnected()
        {
            string expected = string.Format("{0}", m_NodeA.GetVariableNameForSlot(TestNode.V1In));

            var slot = m_NodeA.GetInputSlots<MaterialSlot>().FirstOrDefault();
            var result = m_NodeA.GetSlotValue(slot.id, GenerationMode.Preview);
            Assert.AreEqual(expected, result);
        }

        /*       [Test]
               public void NodeGenerateCorrectPreviewPropertyUsages()
               {
                   string expected = string.Format("{0} {1};{2}", AbstractMaterialNode.OutputPrecision.@fixed, m_NodeA.GetVariableNameForSlot(TestNode.V1In), Environment.NewLine);
                   var visitor = new ShaderGenerator();
                   m_NodeA.precision = AbstractMaterialNode.OutputPrecision.@fixed;
                   m_NodeA.GeneratePropertyUsages(visitor, GenerationMode.Preview);
                   Assert.AreEqual(expected, visitor.GetShaderString(0));

                   expected = string.Format("{0} {1};{2}", AbstractMaterialNode.OutputPrecision.@float, m_NodeA.GetVariableNameForSlot(TestNode.V1In), Environment.NewLine);
                   visitor = new ShaderGenerator();
                   m_NodeA.precision = AbstractMaterialNode.OutputPrecision.@float;
                   m_NodeA.GeneratePropertyUsages(visitor, GenerationMode.Preview);
                   Assert.AreEqual(expected, visitor.GetShaderString(0));

                   expected = string.Format("{0} {1};{2}", AbstractMaterialNode.OutputPrecision.half, m_NodeA.GetVariableNameForSlot(TestNode.V1In), Environment.NewLine);
                   visitor = new ShaderGenerator();
                   m_NodeA.precision = AbstractMaterialNode.OutputPrecision.half;
                   m_NodeA.GeneratePropertyUsages(visitor, GenerationMode.Preview);
                   Assert.AreEqual(expected, visitor.GetShaderString(0));
               }*/
    }
}
