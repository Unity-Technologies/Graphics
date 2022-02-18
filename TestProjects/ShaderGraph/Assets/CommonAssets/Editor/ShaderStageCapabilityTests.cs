using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Serialization;


namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    internal class ShaderStageCapabilityTests
    {
        static string targetUnityDirectoryPath => "Assets/Testing/ShaderStageCapabilityGraphs";

        [OneTimeSetUp]
        public void Setup()
        {
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
        }

        NodeType FindFirstNodeOfType<NodeType>(GraphData graphData, string nodeName = null) where NodeType : AbstractMaterialNode
        {
            var nodes = graphData.GetNodes<NodeType>();
            foreach (var node in nodes)
            {
                if (nodeName == null || node.name == nodeName)
                    return node;
            }
            return null;
        }

        MaterialSlot FindNamedSlot(AbstractMaterialNode node, string slotName)
        {
            foreach (var slot in node.GetSlots<MaterialSlot>())
            {
                if (slot.RawDisplayName() == slotName)
                    return slot;
            }
            return null;
        }

        [Test]
        public void SubGraphDescendentsTests()
        {
            var graphPath = targetUnityDirectoryPath + "/ShaderStageCapability_Graph.shadergraph";

            string fileContents = File.ReadAllText(graphPath);
            var graphGuid = AssetDatabase.AssetPathToGUID(graphPath);
            var messageManager = new MessageManager();
            GraphData graphData = new GraphData() { assetGuid = graphGuid, messageManager = messageManager };
            MultiJson.Deserialize(graphData, fileContents);
            graphData.OnEnable();
            graphData.ValidateGraph();

            var subGraphnodeName = "ShaderStageCapability_SubGraph";
            var subGraphNode = FindFirstNodeOfType<SubGraphNode>(graphData, subGraphnodeName);
            if (subGraphNode == null)
            {
                Assert.Fail("Failed to find sub graph node for {0}", subGraphnodeName);
                return;
            }

            var expectedSlotCapabilities = new Dictionary<string, ShaderStageCapability>
            {
                { "NotConnectedOut", ShaderStageCapability.All },
                { "NotConnectedInput", ShaderStageCapability.All },
                { "InternalVertexLockedOut", ShaderStageCapability.Vertex },
                { "InternalFragmentLockedOut", ShaderStageCapability.Fragment },
                { "InternalBothLockedOut", ShaderStageCapability.None },
                { "InternalVertexLockedInput", ShaderStageCapability.Vertex },
                { "InternalFragmentLockedInput", ShaderStageCapability.Fragment },
                { "InternalBothLockedInput", ShaderStageCapability.None },
                // Output A is connected to InputA which is attached to a vertex locked node in the parent graph
                { "OutputA", ShaderStageCapability.Vertex },
                // Output B is connected to InputB which is attached to a fragment locked node in the parent graph
                { "OutputB", ShaderStageCapability.Fragment },
                // OutputAB is connected to InputA and InputB
                { "OutputAB", ShaderStageCapability.None },
                // InputC is connected to OutputC which is hooked up to the vertex output in the parent graph
                { "InputC", ShaderStageCapability.Vertex },
                // InputD is connected to OutputD which is hooked up to the fragment output in the parent graph
                { "InputD", ShaderStageCapability.Fragment },
                // InputEF is split into OutputE and OutputF which are hooked up to vertex and fragment outputs in the parent graph
                { "InputEF", ShaderStageCapability.None },
            };

            var slotNameToId = new Dictionary<string, MaterialSlot>();
            var slots = subGraphNode.GetSlots<MaterialSlot>();
            foreach (var slot in slots)
                slotNameToId[slot.RawDisplayName()] = slot;

            foreach (var expectedSlotResult in expectedSlotCapabilities)
            {
                var slotName = expectedSlotResult.Key;
                var expectedSlotValue = expectedSlotResult.Value;
                if (slotNameToId.TryGetValue(slotName, out var slot))
                {
                    var capabilities = NodeUtils.GetEffectiveShaderStageCapability(slot, true) & NodeUtils.GetEffectiveShaderStageCapability(slot, false);
                    Assert.AreEqual(capabilities, expectedSlotValue, "Slot {0} expected shader capability {1} but was {2}", slotName, expectedSlotValue, capabilities);
                }
                else
                    Assert.Fail("Expected slot {0} wasn't found", slotName);
            }
        }

        [Test]
        public void InvalidConnectionsTest()
        {
            var graphPath = targetUnityDirectoryPath + "/SubShaderInvalidCapabilities_Graph.shadergraph";

            string fileContents = File.ReadAllText(graphPath);
            var graphGuid = AssetDatabase.AssetPathToGUID(graphPath);
            var messageManager = new MessageManager();
            GraphData graphData = new GraphData() { assetGuid = graphGuid, messageManager = messageManager };
            MultiJson.Deserialize(graphData, fileContents);
            graphData.OnEnable();

            void ValidateSlotError(MaterialSlot slotA, MaterialSlot slotB, AbstractMaterialNode nodeWithError)
            {
                Assert.IsNotNull(slotA, "Expected slotA to not be null");
                Assert.IsNotNull(slotB, "Expected slotB to not be null");
                var edge = graphData.Connect(slotA.slotReference, slotB.slotReference);

                bool foundNode = false;
                foreach (var message in graphData.messageManager.GetNodeMessages())
                {
                    if (message.Key.Equals(nodeWithError.objectId))
                    {
                        foundNode = true;
                        break;
                    }
                }
                Assert.IsTrue(foundNode, $"Expected node {nodeWithError.name} didn't have an error");

                // Put the graph back in a clean state
                graphData.messageManager.ClearAll();
                graphData.RemoveEdge(edge);
            }

            var subGraphNode = FindFirstNodeOfType<SubGraphNode>(graphData, "SubShaderInvalidCapabilities_SubGraph");
            var vertexIdNode = FindFirstNodeOfType<VertexIDNode>(graphData);
            var sampleTextureNode = FindFirstNodeOfType<SampleTexture2DNode>(graphData);
            var baseColorNode = FindFirstNodeOfType<BlockNode>(graphData, $"{BlockFields.SurfaceDescription.BaseColor.tag}.{BlockFields.SurfaceDescription.BaseColor.name}");
            var positionNode = FindFirstNodeOfType<BlockNode>(graphData, $"{BlockFields.VertexDescription.Position.tag}.{BlockFields.VertexDescription.Position.name}");

            var vertexLockedSlot = FindNamedSlot(subGraphNode, "VertexLocked_Out");
            var fragmentLockedSlot = FindNamedSlot(subGraphNode, "FragmentLocked_Out");
            var outputA = FindNamedSlot(subGraphNode, "OutputA");
            var baseColorSlot = FindNamedSlot(baseColorNode, "Base Color");
            var positionSlot = FindNamedSlot(positionNode, "Position");

            // Hook up a (internal) vertex locked slot to a fragment output. The error should be on the sub graph node
            ValidateSlotError(vertexLockedSlot, baseColorSlot, subGraphNode);
            // Hook up a (internal) fragment locked slot to a vertex output. The error should be on the sub graph node
            ValidateSlotError(fragmentLockedSlot, positionSlot, subGraphNode);
            // Hook up: Sample Texture -> Add -> SubGraph -> Position (out). Error should be on the SampleTexture node.
            ValidateSlotError(outputA, positionSlot, sampleTextureNode);
            // Hook up: VertexId -> Add -> SubGraph -> Position (out). Error should be on the VertexId node.
            ValidateSlotError(outputA, baseColorSlot, vertexIdNode);
        }
    }
}
