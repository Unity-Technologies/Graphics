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

        SubGraphNode FindSubgraphNode(GraphData graphData, string subGraphNodeName)
        {
            var subGraphNodes = graphData.GetNodes<SubGraphNode>();
            foreach (var subGraphNode in subGraphNodes)
            {
                if (subGraphNode.name == subGraphNodeName)
                    return subGraphNode;
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
            var subGraphNode = FindSubgraphNode(graphData, subGraphnodeName);
            if(subGraphNode == null)
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
    }
}
