using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.Rendering;
using UnityEngine;

namespace UnityEditor.ShaderGraph.UnitTests
{
    internal class TestGraphObject : GraphObject
    {
        public void Initialize(string graphPath)
        {
            hideFlags = HideFlags.HideAndDontSave;

            var textGraph = File.ReadAllText(graphPath, Encoding.UTF8);
            graph = JsonUtility.FromJson<GraphData>(textGraph);
            graph.messageManager = new MessageManager();
            graph.assetGuid = AssetDatabase.AssetPathToGUID(graphPath);
            graph.OnEnable();
            graph.ValidateGraph();
        }

        public override void RegisterCompleteObjectUndo(string actionName)
        {
            // Do nothing
        }
    }

    [TestFixture]
    internal class EdgeTypeConversionTests
    {
        static string kGraphName = "Assets/CommonAssets/Graphs/SlotConv.shadergraph";
        TestGraphObject m_Object;
        GraphData m_Graph;
        CustomFunctionNode m_CFNode;

        [SetUp]
        public void LoadGraph()
        {
            m_Object = ScriptableObject.CreateInstance<TestGraphObject>();
            m_Object.Initialize(kGraphName);
            m_Graph = m_Object.graph;

            Assert.NotNull(m_Graph, $"Invalid graph data found for {kGraphName}");
            m_CFNode = m_Graph.GetNodes<CustomFunctionNode>().FirstOrDefault();
            Assert.NotNull(m_CFNode, $"No CustomFunctionNode found in {kGraphName}.");
        }

        [Test]
        public void TestAllCombos()
        {
            foreach (var slot in m_CFNode.GetInputSlots<MaterialSlot>())
            {
                // Should only be one edge per slot on this graph
                var edge = m_Graph.GetEdges(slot.slotReference).FirstOrDefault();
                if (edge == null) continue;

                var outputNode = m_Graph.GetNodeFromGuid(edge.outputSlot.nodeGuid);
                var outputSlot = outputNode.GetOutputSlots<MaterialSlot>().First(s => s.id == edge.outputSlot.slotId);
                var curOutputType = outputSlot.valueType.ToConcreteSlotValueType();

                foreach (var slotValType in (SlotValueType[]) Enum.GetValues(typeof(SlotValueType)))
                {
                    var newSlot = MaterialSlot.CreateMaterialSlot(slotValType, slot.id, slot.displayName, slot.shaderOutputName,
                        slot.slotType, Vector4.zero);
                    m_CFNode.AddSlot(newSlot);
                    m_Graph.ValidateGraph();

                    // Verify all errors are expected
                    foreach (var message in m_Graph.messageManager.GetNodeMessages())
                    {
                        if (message.Key.Equals(m_CFNode.guid) && message.Value.Exists(msg =>
                            msg.severity == ShaderCompilerMessageSeverity.Error))
                        {
                            Assert.IsFalse(SlotValueHelper.AreCompatible(slotValType, curOutputType),
                                $"{curOutputType} failed to convert to {slotValType} but that conversion is supported. Error: {message.Value.First().message}");
                        }
                        else
                        {
                            Assert.IsTrue(SlotValueHelper.AreCompatible(slotValType, curOutputType),
                                $"{curOutputType} successfully connected to {slotValType} but that conversion is not supported.");
                        }
                    }

                    m_Graph.ClearErrorsForNode(m_CFNode);
                }

                // Replace original slot
                m_CFNode.AddSlot(slot);
            }
        }

        [Test]
        public void RedirectNodes_DoNotAffectOutput()
        {
            foreach (var slot in m_CFNode.GetInputSlots<MaterialSlot>())
            {
                var originalValue = m_CFNode.GetSlotValue(slot.id, GenerationMode.ForReals);

                // Should only be one edge per slot on this graph
                var edge = m_Graph.GetEdges(slot.slotReference).FirstOrDefault();
                if (edge == null) continue;

                var outputNode = m_Graph.GetNodeFromGuid(edge.outputSlot.nodeGuid);
                var outputSlot = outputNode.GetOutputSlots<MaterialSlot>().First(s => s.id == edge.outputSlot.slotId);

                RedirectNodeData.Create(m_Graph, outputSlot.valueType, Vector2.zero, edge.inputSlot, edge.outputSlot, Guid.Empty);

                m_Graph.ValidateGraph();

                // Verify all errors are expected
                foreach (var message in m_Graph.messageManager.GetNodeMessages())
                {
                    if (message.Key.Equals(m_CFNode.guid) && message.Value.Exists(msg =>
                        msg.severity == ShaderCompilerMessageSeverity.Error))
                    {
                        Assert.Fail(message.Value.FirstOrDefault().message);
                    }
                }

                var redirectedValue = m_CFNode.GetSlotValue(slot.id, GenerationMode.ForReals);

                Assert.AreEqual(originalValue, redirectedValue, $"Value of slot {slot.displayName} changed with redirect node");

                m_Graph.ClearErrorsForNode(m_CFNode);
            }
        }
    }
}
