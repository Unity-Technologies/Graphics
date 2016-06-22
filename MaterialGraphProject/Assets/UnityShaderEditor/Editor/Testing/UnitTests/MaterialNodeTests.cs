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
        private TestNode m_NodeB;

        class TestNode : AbstractMaterialNode
        {
            public TestNode()
            {
                AddSlot(new MaterialSlot("V1Out", "V1Out", SlotType.Output, 0, SlotValueType.Vector1, Vector4.zero));
                AddSlot(new MaterialSlot("V2Out", "V2Out", SlotType.Output, 1, SlotValueType.Vector2, Vector4.zero));
                AddSlot(new MaterialSlot("V3Out", "V3Out", SlotType.Output, 2, SlotValueType.Vector3, Vector4.zero));
                AddSlot(new MaterialSlot("V4Out", "V4Out", SlotType.Output, 3, SlotValueType.Vector4, Vector4.zero));

                AddSlot(new MaterialSlot("V1In", "V1In", SlotType.Input, 0, SlotValueType.Vector1, Vector4.zero));
                AddSlot(new MaterialSlot("V2In", "V2In", SlotType.Input, 1, SlotValueType.Vector2, Vector4.zero));
                AddSlot(new MaterialSlot("V3In", "V3In", SlotType.Input, 2, SlotValueType.Vector3, Vector4.zero));
                AddSlot(new MaterialSlot("V4In", "V4In", SlotType.Input, 3, SlotValueType.Vector4, Vector4.zero));
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
            m_NodeB = new TestNode();
            m_Graph.AddNode(m_NodeA);
            m_Graph.AddNode(m_NodeB);
        }

        [Test]
        public void ConnectV1ToV1Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference("V1Out"), m_NodeB.GetSlotReference("V1In"));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV1ToV2Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference("V1Out"), m_NodeB.GetSlotReference("V2In"));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV1ToV3Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference("V1Out"), m_NodeB.GetSlotReference("V3In"));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV1ToV4Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference("V1Out"), m_NodeB.GetSlotReference("V4In"));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV2ToV1Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference("V2Out"), m_NodeB.GetSlotReference("V1In"));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV2ToV2Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference("V2Out"), m_NodeB.GetSlotReference("V2In"));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV2ToV3Fails()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference("V2Out"), m_NodeB.GetSlotReference("V3In"));
            Assert.IsTrue(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV2ToV4Fails()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference("V2Out"), m_NodeB.GetSlotReference("V4In"));
            Assert.IsTrue(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV3ToV1Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference("V3Out"), m_NodeB.GetSlotReference("V1In"));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV3ToV2Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference("V3Out"), m_NodeB.GetSlotReference("V2In"));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV3ToV3Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference("V3Out"), m_NodeB.GetSlotReference("V3In"));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV3ToV4Fails()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference("V3Out"), m_NodeB.GetSlotReference("V4In"));
            Assert.IsTrue(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV4ToV1Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference("V4Out"), m_NodeB.GetSlotReference("V1In"));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV4ToV2Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference("V4Out"), m_NodeB.GetSlotReference("V2In"));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV4ToV3Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference("V4Out"), m_NodeB.GetSlotReference("V3In"));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV4ToV4Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference("V4Out"), m_NodeB.GetSlotReference("V4In"));
            Assert.IsFalse(m_NodeB.hasError);
        }
    }
}
