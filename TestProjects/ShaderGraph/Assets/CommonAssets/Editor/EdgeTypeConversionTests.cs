using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Graphing;
using UnityEditor.Rendering;
using UnityEngine;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    internal class EdgeTypeConversionTests
    {
        static string kGraphName = "Assets/CommonAssets/Graphs/SlotConv.shadergraph";
        GraphData m_Graph;
        CustomFunctionNode m_CFNode;

        [SetUp]
        public void LoadGraph()
        {
            List<PropertyCollector.TextureInfo> lti;
            var lsadp = new List<string>();
            ShaderGraphImporter.GetShaderText(kGraphName, out lti, lsadp, out m_Graph);
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
    }
}
