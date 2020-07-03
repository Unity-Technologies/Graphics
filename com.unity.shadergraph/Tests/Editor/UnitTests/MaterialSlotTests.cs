using NUnit.Framework;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    class MaterialSlotTests
    {
        private GraphData m_Graph;
        private TestNode m_NodeA;

        class TestNode : AbstractMaterialNode
        {
            public const int V1In = 1;
            public const int V2In = 2;
            public const int V3In = 3;
            public const int V4In = 4;

            public readonly Vector1MaterialSlot slot1;
            public readonly Vector2MaterialSlot slot2;
            public readonly Vector3MaterialSlot slot3;
            public readonly Vector4MaterialSlot slot4;

            public TestNode()
            {
                slot1 = new Vector1MaterialSlot(V1In, "V1In", "V1In", SlotType.Input, 1);
                AddSlot(slot1);

                slot2 = new Vector2MaterialSlot(V2In, "V2In", "V2In", SlotType.Input, Vector2.one);
                AddSlot(slot2);

                slot3 = new Vector3MaterialSlot(V3In, "V3In", "V3In", SlotType.Input, Vector3.one);
                AddSlot(slot3);

                slot4 = new Vector4MaterialSlot(V4In, "V4In", "V4In", SlotType.Input, Vector4.one);
                AddSlot(slot4);
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
            m_NodeA.precision = Precision.Half;
            m_Graph.AddNode(m_NodeA);
        }

        [Test]
        public void SlotDisplayNameIsCorrect()
        {
            Assert.AreEqual("V1In(1)", m_NodeA.slot1.displayName);

            Assert.AreEqual("V2In(2)", m_NodeA.slot2.displayName);

            Assert.AreEqual("V3In(3)", m_NodeA.slot3.displayName);

            Assert.AreEqual("V4In(4)", m_NodeA.slot4.displayName);
        }

        [Test]
        public void CanUpdateMaterialSlotDefaultValue()
        {
            var slot = m_NodeA.slot1;
            slot.value = 1;
            Assert.AreEqual(1, slot.defaultValue);
        }

        [Test]
        public void CanUpdateMaterialSlotCurrentValue()
        {
            var slot = m_NodeA.slot1;
            slot.value = 1;
            Assert.AreEqual(1, 1);
        }

        /*     [Test]
             public void MaterialSlotCanGeneratePropertyUsagesForPreview()
             {
                 string expected = string.Format("{0} {1};{2}", m_NodeA.precision, m_NodeA.GetVariableNameForSlot(TestNode.V1In), Environment.NewLine);

                 var slot = m_NodeA.slot;
                 var visitor = new ShaderGenerator();
                 slot.GeneratePropertyUsages(visitor, GenerationMode.Preview);
                 Assert.AreEqual(expected, visitor.GetShaderString(0));
             }*/

        [Test]
        public void MaterialSlotReturnsValidDefaultValue()
        {
            string expected = string.Format("{0}", m_NodeA.GetVariableNameForSlot(TestNode.V1In));

            var result = m_NodeA.slot1.GetDefaultValue(GenerationMode.Preview);
            Assert.AreEqual(expected, result);

            m_NodeA.slot1.value = 6;
            result = m_NodeA.slot1.GetDefaultValue(GenerationMode.ForReals);
            Assert.AreEqual("6", result);

            m_NodeA.slot2.value = new Vector4(6, 6, 6, 1);
            result = m_NodeA.slot2.GetDefaultValue(GenerationMode.ForReals, ConcretePrecision.Half);
            Assert.AreEqual("half2 (6, 6)", result);

            m_NodeA.slot3.value = new Vector4(6, 6, 6, 1);
            result = m_NodeA.slot3.GetDefaultValue(GenerationMode.ForReals, ConcretePrecision.Half);
            Assert.AreEqual("half3 (6, 6, 6)", result);

            m_NodeA.slot4.value = new Vector4(6, 6, 6, 1);
            result = m_NodeA.slot4.GetDefaultValue(GenerationMode.ForReals, ConcretePrecision.Half);
            Assert.AreEqual("half4 (6, 6, 6, 1)", result);
        }

        /* [Test]
         public void MaterialSlotThrowsWhenNoOwner()
         {
             var slot = new MaterialSlot(0, string.Empty, string.Empty, SlotType.Input, SlotValueType.Vector1, Vector4.zero);
             Assert.Throws<Exception>(() => slot.GeneratePropertyUsages(new ShaderGenerator(), GenerationMode.Preview));
             Assert.Throws<Exception>(() => slot.GetDefaultValue(GenerationMode.Preview));
         }*/
    }
}
