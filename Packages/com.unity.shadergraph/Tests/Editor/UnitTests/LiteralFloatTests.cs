using NUnit.Framework;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    class LiteralFloatTests
    {
        private GraphData m_Graph;
        private LiteralFloatNode m_LiteralFloatNode;
        private PropertyNode m_FloatPropertyNode;
        class LiteralFloatNode : AbstractMaterialNode
        {
            public const int LitFloatIn = 0;
            public const int NonLitFloatOut = 1;

            public LiteralFloatNode()
            {
                AddSlot(new Vector1MaterialSlot(LitFloatIn, "LiteralFloatIn", "LiteralFloatIn", SlotType.Input, 0, literal: true));
                AddSlot(new Vector1MaterialSlot(NonLitFloatOut, "NonLiteralFloatOut", "NonLiteralFloatOut", SlotType.Output, 1, literal: false));
            }
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
            m_LiteralFloatNode = new LiteralFloatNode();
            m_Graph.AddNode(m_LiteralFloatNode);
            m_FloatPropertyNode = new PropertyNode { property = new Vector1ShaderProperty(){LiteralFloatMode = false} };
            // required so property node isn't auto-converted to Vector1Nodes
            m_Graph.AddGraphInput(m_FloatPropertyNode.property);
            m_Graph.AddNode(m_FloatPropertyNode);

        }

        [Test]
        public void PropertyInputDoesNotGenerateLiteralFloat()
        {
            Assert.IsFalse(m_FloatPropertyNode.GetSlotReference(PropertyNode.OutputSlotId).slot.IsCompatibleWith(m_LiteralFloatNode.GetSlotReference(LiteralFloatNode.LitFloatIn).slot));
            m_Graph.isSubGraph = true;
            m_Graph.ValidateGraph();
            Assert.IsFalse(m_FloatPropertyNode.GetSlotReference(PropertyNode.OutputSlotId).slot.IsCompatibleWith(m_LiteralFloatNode.GetSlotReference(LiteralFloatNode.LitFloatIn).slot));
        }

        [Test]
        public void RedirectPropagatesLiteral()
        {
            m_Graph.owner = ScriptableObject.CreateInstance<GraphObject>();
            (m_FloatPropertyNode.property as Vector1ShaderProperty).LiteralFloatMode = true;
            var outputSlotRef = m_FloatPropertyNode.GetSlotReference(PropertyNode.OutputSlotId);
            var inputSlotRef = m_LiteralFloatNode.GetSlotReference(LiteralFloatNode.LitFloatIn);
            GroupData group = null;
            if (outputSlotRef.slot.owner.group == inputSlotRef.slot.owner.group)
            {
                group = inputSlotRef.slot.owner.group;
            }
            var redirectNode = RedirectNodeData.Create(
                m_Graph, ConcreteSlotValueType.Vector1, Vector2.zero,
                inputSlotRef,
                outputSlotRef,
                group);

            Assert.IsTrue(redirectNode.GetSlotReference(RedirectNodeData.kOutputSlotID).slot.IsCompatibleWith(inputSlotRef.slot));
            (m_FloatPropertyNode.property as Vector1ShaderProperty).LiteralFloatMode = false;
            // trigger propagation
            m_Graph.ValidateGraph();
            // properties in non-subgraphs are _always non-literal_
            Assert.IsFalse(redirectNode.GetSlotReference(RedirectNodeData.kOutputSlotID).slot.IsCompatibleWith(inputSlotRef.slot));

            m_Graph.isSubGraph = true;
            m_Graph.ValidateGraph();
            Assert.IsFalse(redirectNode.GetSlotReference(RedirectNodeData.kOutputSlotID).slot.IsCompatibleWith(inputSlotRef.slot));

            (m_FloatPropertyNode.property as Vector1ShaderProperty).LiteralFloatMode = true;
            m_Graph.ValidateGraph();
            Assert.IsTrue(redirectNode.GetSlotReference(RedirectNodeData.kOutputSlotID).slot.IsCompatibleWith(inputSlotRef.slot));
        }

        [Test]
        public void NonLiteralTriggersError()
        {
            var literalFloatNode = new LiteralFloatNode();
            m_Graph.AddNode(literalFloatNode);
            m_Graph.Connect(literalFloatNode.GetSlotReference(LiteralFloatNode.NonLitFloatOut), m_LiteralFloatNode.GetSlotReference(LiteralFloatNode.LitFloatIn));
            Assert.IsTrue(m_LiteralFloatNode.hasError);
        }
    }
}

