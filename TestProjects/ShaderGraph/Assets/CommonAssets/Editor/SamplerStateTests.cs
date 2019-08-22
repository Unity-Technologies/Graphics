using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    class SamplerStateTests
    {
        const string kSubGraphPath = "Assets/CommonAssets/Graphs/SamplerStateSubGraph.shadersubgraph";
        const string kGraphPath = "Assets/CommonAssets/Graphs/SamplerState.shadergraph";
        SubGraphAsset m_SubGraph;
        GraphData m_Graph;

        [OneTimeSetUp]
        public void LoadGraph()
        {
            m_SubGraph = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(kSubGraphPath);

            ShaderGraphImporter.GetShaderText(kGraphPath, out var textures, new List<string>(), out m_Graph);
            Assert.NotNull(m_Graph, $"Invalid graph data found for {kGraphPath}");
        }

        [Test]
        public void SubGraphSamplerInputsAreAdded()
        {
            foreach (var i in m_SubGraph.inputs.Where(i => i.IsAnyTextureType()))
            {
                var samplerName = $"sampler{i.referenceName}";
                var samplerInput = m_SubGraph.inputs.FirstOrDefault(p => p is SamplerStateShaderProperty && p.referenceName == samplerName);
                Assert.IsNotNull(samplerInput);
                Assert.IsTrue(samplerInput.hidden);

                var samplerProperty = m_SubGraph.nodeProperties.FirstOrDefault(p => p is SamplerStateShaderProperty && p.referenceName == samplerName);
                Assert.IsNull(samplerProperty);
            }
        }

        [Test]
        public void SubGraphSamplerPropertiesAreRemoved()
        {
            foreach (var i in m_SubGraph.inputs.Where(i => i.IsAnyTextureType()))
            {
                var samplerName = $"sampler{i.referenceName}";
                var samplerProperty = m_SubGraph.nodeProperties.FirstOrDefault(p => p is SamplerStateShaderProperty && p.referenceName == samplerName);
                Assert.IsNull(samplerProperty);
            }
        }

        [Test]
        public void MainGraphSendsSamplerStateForConnectedTextureSlots()
        {
            var subGraphNode = m_Graph.GetNodes<SubGraphNode>().FirstOrDefault();
            Assert.IsNotNull(subGraphNode != null);

            var inputSlots = new List<MaterialSlot>();
            subGraphNode.GetInputSlots(inputSlots);
            foreach (var input in inputSlots.Where(slot => slot is SamplerStateMaterialSlot))
            {
                var samplerSlot = input as SamplerStateMaterialSlot;
                Assert.IsTrue(samplerSlot.textureSlotId != -1);
                var textureSlot = subGraphNode.FindInputSlot<MaterialSlot>(samplerSlot.textureSlotId);
                if (textureSlot.isConnected)
                {
                    var textureValue = subGraphNode.GetSlotValue(samplerSlot.textureSlotId, GenerationMode.ForReals);
                    var samplerValue = subGraphNode.GetSlotValue(input.id, GenerationMode.ForReals);
                    Assert.AreEqual($"sampler{textureValue}", samplerValue);
                }
            }
        }
    }
}
