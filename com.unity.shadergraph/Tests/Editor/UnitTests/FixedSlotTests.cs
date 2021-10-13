using NUnit.Framework;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    class FixedSlotTests
    {
        private GraphData m_Graph;
        private TestNode m_NodeA;
        private TestNode m_NodeB;

        class TestNode : AbstractMaterialNode
        {
            public const int V1Out = 0;
            public const int V2Out = 1;
            public const int V3Out = 2;
            public const int V4Out = 3;
            public const int BoolOut = 8;
            public const int TexOut = 10;

            public const int V1In = 4;
            public const int V2In = 5;
            public const int V3In = 6;
            public const int V4In = 7;
            public const int BoolIn = 9;

            public TestNode()
            {
                AddSlot(new Vector1MaterialSlot(V1Out, "V1Out", "V1Out", SlotType.Output, 0));
                AddSlot(new Vector2MaterialSlot(V2Out, "V2Out", "V2Out", SlotType.Output, Vector4.zero));
                AddSlot(new Vector3MaterialSlot(V3Out, "V3Out", "V3Out", SlotType.Output, Vector4.zero));
                AddSlot(new Vector4MaterialSlot(V4Out, "V4Out", "V4Out", SlotType.Output, Vector4.zero));
                AddSlot(new BooleanMaterialSlot(BoolOut, "BOut", "BOut", SlotType.Output, false));
                AddSlot(new Texture2DMaterialSlot(TexOut, "TOut", "TOut", SlotType.Output));

                AddSlot(new Vector1MaterialSlot(V1In, "V1In", "V1In", SlotType.Input, 0));
                AddSlot(new Vector2MaterialSlot(V2In, "V2In", "V2In", SlotType.Input, Vector4.zero));
                AddSlot(new Vector3MaterialSlot(V3In, "V3In", "V3In", SlotType.Input, Vector4.zero));
                AddSlot(new Vector4MaterialSlot(V4In, "V4In", "V4In", SlotType.Input, Vector4.zero));
                AddSlot(new BooleanMaterialSlot(BoolIn, "BIn", "BIn", SlotType.Input, false));
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
            m_NodeA = new TestNode();
            m_NodeB = new TestNode();
            m_Graph.AddNode(m_NodeA);
            m_Graph.AddNode(m_NodeB);
        }

        [Test]
        public void ConnectV1ToV1Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.V1Out), m_NodeB.GetSlotReference(TestNode.V1In));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectBoolToV1Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.BoolOut), m_NodeB.GetSlotReference(TestNode.V1In));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV1ToV2Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.V1Out), m_NodeB.GetSlotReference(TestNode.V2In));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectBoolToV2Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.BoolOut), m_NodeB.GetSlotReference(TestNode.V2In));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV1ToV3Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.V1Out), m_NodeB.GetSlotReference(TestNode.V3In));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectBoolToV3Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.BoolOut), m_NodeB.GetSlotReference(TestNode.V3In));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV1ToV4Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.V1Out), m_NodeB.GetSlotReference(TestNode.V4In));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectBoolToV4Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.BoolOut), m_NodeB.GetSlotReference(TestNode.V4In));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV2ToV1Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.V2Out), m_NodeB.GetSlotReference(TestNode.V1In));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV2ToBoolWorks()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.V2Out), m_NodeB.GetSlotReference(TestNode.BoolIn));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV2ToV2Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.V2Out), m_NodeB.GetSlotReference(TestNode.V2In));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV2ToV3Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.V2Out), m_NodeB.GetSlotReference(TestNode.V3In));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV2ToV4Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.V2Out), m_NodeB.GetSlotReference(TestNode.V4In));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV3ToV1Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.V3Out), m_NodeB.GetSlotReference(TestNode.V1In));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV3ToBoolWorks()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.V3Out), m_NodeB.GetSlotReference(TestNode.BoolIn));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV3ToV2Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.V3Out), m_NodeB.GetSlotReference(TestNode.V2In));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV3ToV3Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.V3Out), m_NodeB.GetSlotReference(TestNode.V3In));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV3ToV4Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.V3Out), m_NodeB.GetSlotReference(TestNode.V4In));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV4ToV1Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.V4Out), m_NodeB.GetSlotReference(TestNode.V1In));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV4ToBoolWorks()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.V4Out), m_NodeB.GetSlotReference(TestNode.BoolIn));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV4ToV2Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.V4Out), m_NodeB.GetSlotReference(TestNode.V2In));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV4ToV3Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.V4Out), m_NodeB.GetSlotReference(TestNode.V3In));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectV4ToV4Works()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.V4Out), m_NodeB.GetSlotReference(TestNode.V4In));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectBoolToBoolWorks()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.BoolOut), m_NodeB.GetSlotReference(TestNode.BoolIn));
            Assert.IsFalse(m_NodeB.hasError);
        }

        [Test]
        public void ConnectTexToBoolFails()
        {
            m_Graph.Connect(m_NodeA.GetSlotReference(TestNode.TexOut), m_NodeB.GetSlotReference(TestNode.BoolIn));
            Assert.IsTrue(m_NodeB.hasError);
        }

    }
}
