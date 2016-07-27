using NUnit.Framework;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.UnitTests
{
    [TestFixture]
    public class DynamicSlotTests
    {
        private PixelGraph m_Graph;
        private DynamicNode m_DynamicNode;
        private TestNode m_StaticNode;

        class DynamicNode : AbstractMaterialNode
        {
            public const int DynIn1 = 0;
            public const int DynIn2 = 1;
            public const int DynOut = 2;

            public DynamicNode()
            {
                AddSlot(new MaterialSlot(DynIn1, "DynIn1", "DynIn1", SlotType.Input, SlotValueType.Dynamic, Vector4.zero));
                AddSlot(new MaterialSlot(DynIn2, "DynIn2", "DynIn2", SlotType.Input, SlotValueType.Dynamic, Vector4.zero));
                AddSlot(new MaterialSlot(DynOut, "DynOut", "DynOut", SlotType.Output, SlotValueType.Dynamic, Vector4.zero));
            }
        }

        class TestNode : AbstractMaterialNode
        {
            public const int V1Out = 0;
            public const int V2Out = 1;
            public const int V3Out = 2;
            public const int V4Out = 3;

            public TestNode()
            {
                AddSlot(new MaterialSlot(V1Out, "V1Out", "V1Out", SlotType.Output, SlotValueType.Vector1, Vector4.zero));
                AddSlot(new MaterialSlot(V2Out, "V2Out", "V2Out", SlotType.Output, SlotValueType.Vector2, Vector4.zero));
                AddSlot(new MaterialSlot(V3Out, "V3Out", "V3Out", SlotType.Output, SlotValueType.Vector3, Vector4.zero));
                AddSlot(new MaterialSlot(V4Out, "V4Out", "V4Out", SlotType.Output, SlotValueType.Vector4, Vector4.zero));
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
            m_DynamicNode = new DynamicNode();
            m_Graph.AddNode(m_DynamicNode);
            m_StaticNode = new TestNode();
            m_Graph.AddNode(m_StaticNode);
        }

        [Test]
        public void DynamicInputsV1NoneWorks()
        {
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V1Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn1));
            var dynOut1 = m_DynamicNode.FindOutputSlot<MaterialSlot>(DynamicNode.DynOut);
            Assert.AreEqual(ConcreteSlotValueType.Vector1, dynOut1.concreteValueType);
        }

        [Test]
        public void DynamicInputsV1V1Works()
        {
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V1Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn1));
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V1Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn2));
            var dynOut1 = m_DynamicNode.FindOutputSlot<MaterialSlot>(DynamicNode.DynOut);
            Assert.AreEqual(ConcreteSlotValueType.Vector1, dynOut1.concreteValueType);
        }

        [Test]
        public void DynamicInputsV1V2Works()
        {
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V1Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn1));
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V2Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn2));
            var dynOut1 = m_DynamicNode.FindOutputSlot<MaterialSlot>(DynamicNode.DynOut);
            Assert.AreEqual(ConcreteSlotValueType.Vector2, dynOut1.concreteValueType);
        }

        [Test]
        public void DynamicInputsV1V3Works()
        {
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V1Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn1));
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V3Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn2));
            var dynOut1 = m_DynamicNode.FindOutputSlot<MaterialSlot>(DynamicNode.DynOut);
            Assert.AreEqual(ConcreteSlotValueType.Vector3, dynOut1.concreteValueType);
        }

        [Test]
        public void DynamicInputsV1V4Works()
        {
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V1Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn1));
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V4Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn2));
            var dynOut1 = m_DynamicNode.FindOutputSlot<MaterialSlot>(DynamicNode.DynOut);
            Assert.AreEqual(ConcreteSlotValueType.Vector4, dynOut1.concreteValueType);
        }

        [Test]
        public void DynamicInputsV2NoneWorks()
        {
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V2Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn1));
            var dynOut1 = m_DynamicNode.FindOutputSlot<MaterialSlot>(DynamicNode.DynOut);
            Assert.AreEqual(ConcreteSlotValueType.Vector2, dynOut1.concreteValueType);
        }

        [Test]
        public void DynamicInputsV2V1Works()
        {
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V2Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn1));
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V1Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn2));
            var dynOut1 = m_DynamicNode.FindOutputSlot<MaterialSlot>(DynamicNode.DynOut);
            Assert.AreEqual(ConcreteSlotValueType.Vector2, dynOut1.concreteValueType);
        }

        [Test]
        public void DynamicInputsV2V2Works()
        {
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V2Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn1));
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V2Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn2));
            var dynOut1 = m_DynamicNode.FindOutputSlot<MaterialSlot>(DynamicNode.DynOut);
            Assert.AreEqual(ConcreteSlotValueType.Vector2, dynOut1.concreteValueType);
        }

        [Test]
        public void DynamicInputsV2V3Works()
        {
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V2Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn1));
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V3Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn2));
            var dynOut1 = m_DynamicNode.FindOutputSlot<MaterialSlot>(DynamicNode.DynOut);
            Assert.AreEqual(ConcreteSlotValueType.Vector2, dynOut1.concreteValueType);
        }

        [Test]
        public void DynamicInputsV2V4Works()
        {
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V2Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn1));
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V4Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn2));
            var dynOut1 = m_DynamicNode.FindOutputSlot<MaterialSlot>(DynamicNode.DynOut);
            Assert.AreEqual(ConcreteSlotValueType.Vector2, dynOut1.concreteValueType);
        }

        [Test]
        public void DynamicInputsV3NoneWorks()
        {
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V3Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn1));
            var dynOut1 = m_DynamicNode.FindOutputSlot<MaterialSlot>(DynamicNode.DynOut);
            Assert.AreEqual(ConcreteSlotValueType.Vector3, dynOut1.concreteValueType);
        }

        [Test]
        public void DynamicInputsV3V1Works()
        {
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V3Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn1));
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V1Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn2));
            var dynOut1 = m_DynamicNode.FindOutputSlot<MaterialSlot>(DynamicNode.DynOut);
            Assert.AreEqual(ConcreteSlotValueType.Vector3, dynOut1.concreteValueType);
        }

        [Test]
        public void DynamicInputsV3V2Works()
        {
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V3Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn1));
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V2Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn2));
            var dynOut1 = m_DynamicNode.FindOutputSlot<MaterialSlot>(DynamicNode.DynOut);
            Assert.AreEqual(ConcreteSlotValueType.Vector2, dynOut1.concreteValueType);
        }

        [Test]
        public void DynamicInputsV3V3Works()
        {
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V3Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn1));
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V3Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn2));
            var dynOut1 = m_DynamicNode.FindOutputSlot<MaterialSlot>(DynamicNode.DynOut);
            Assert.AreEqual(ConcreteSlotValueType.Vector3, dynOut1.concreteValueType);
        }

        [Test]
        public void DynamicInputsV3V4Works()
        {
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V3Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn1));
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V4Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn2));
            var dynOut1 = m_DynamicNode.FindOutputSlot<MaterialSlot>(DynamicNode.DynOut);
            Assert.AreEqual(ConcreteSlotValueType.Vector3, dynOut1.concreteValueType);
        }

        [Test]
        public void DynamicInputsV4NoneWorks()
        {
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V4Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn1));
            var dynOut1 = m_DynamicNode.FindOutputSlot<MaterialSlot>(DynamicNode.DynOut);
            Assert.AreEqual(ConcreteSlotValueType.Vector4, dynOut1.concreteValueType);
        }

        [Test]
        public void DynamicInputsV4V1Works()
        {
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V4Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn1));
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V1Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn2));
            var dynOut1 = m_DynamicNode.FindOutputSlot<MaterialSlot>(DynamicNode.DynOut);
            Assert.AreEqual(ConcreteSlotValueType.Vector4, dynOut1.concreteValueType);
        }

        [Test]
        public void DynamicInputsV4V2Works()
        {
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V4Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn1));
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V2Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn2));
            var dynOut1 = m_DynamicNode.FindOutputSlot<MaterialSlot>(DynamicNode.DynOut);
            Assert.AreEqual(ConcreteSlotValueType.Vector2, dynOut1.concreteValueType);
        }

        [Test]
        public void DynamicInputsV4V3Works()
        {
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V4Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn1));
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V3Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn2));
            var dynOut1 = m_DynamicNode.FindOutputSlot<MaterialSlot>(DynamicNode.DynOut);
            Assert.AreEqual(ConcreteSlotValueType.Vector3, dynOut1.concreteValueType);
        }

        [Test]
        public void DynamicInputsV4V4Works()
        {
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V4Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn1));
            m_Graph.Connect(m_StaticNode.GetSlotReference(TestNode.V4Out), m_DynamicNode.GetSlotReference(DynamicNode.DynIn2));
            var dynOut1 = m_DynamicNode.FindOutputSlot<MaterialSlot>(DynamicNode.DynOut);
            Assert.AreEqual(ConcreteSlotValueType.Vector4, dynOut1.concreteValueType);
        }
    }
}
