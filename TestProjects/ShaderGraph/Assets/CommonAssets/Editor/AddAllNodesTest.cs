using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.UnitTests
{
    class AddAllNodesTest
    {
        static string kGraphName = "Assets/CommonAssets/Graphs/Blank.shadergraph";
        GraphData m_Graph;

        readonly List<string> m_NotAllowedNodes = new List<string>{"PropertyNode", "KeywordNode", "SubGraphNode", "SubGraphOutputNode", "CustomFunctionNode"};

        [OneTimeSetUp]
        public void LoadGraph()
        {
            List<PropertyCollector.TextureInfo> lti;
            var assetCollection = new AssetCollection();
            ShaderGraphImporter.GetShaderText(kGraphName, out lti, assetCollection, out m_Graph);
            Assert.NotNull(m_Graph, $"Invalid graph data found for {kGraphName}");
        }

        [SetUp]
        public void ClearGraph()
        {
            if (m_Graph != null)
            {
                var allNodes = m_Graph.GetNodes<AbstractMaterialNode>().ToArray();
                foreach (var node in allNodes)
                {
                    if(m_Graph.outputNode != node)
                        m_Graph.RemoveNode(node);
                }
            }
        }

        [Test]
        public void CreateAllNodes()
        {
            foreach (var materialNode in GetAllNodes())
            {
                string nType = materialNode.GetType().ToString().Split('.').Last();
                if (!m_NotAllowedNodes.Contains(nType))
                {
                    m_Graph.AddNode(materialNode);
                }
            }
        }

        [Test]
        public void CreateAllNodeViews()
        {
            foreach (var materialNode in GetAllNodes())
            {
                string nType = materialNode.GetType().ToString().Split('.').Last();
                if (!m_NotAllowedNodes.Contains(nType))
                {
                    m_Graph.AddNode(materialNode);

                    if (materialNode is PropertyNode propertyNode)
                    {
                        var tokenNode = new PropertyNodeView(propertyNode, null);
                    }
                    else
                    {
                        var materialNodeView = new MaterialNodeView {userData = materialNode};
                    }
                }
            }
        }

        static IEnumerable<AbstractMaterialNode> GetAllNodes()
        {
            IEnumerable<AbstractMaterialNode> nodes = typeof(AbstractMaterialNode)
                .Assembly.GetTypes()
                .Where(x => x.IsSubclassOf(typeof(AbstractMaterialNode)) && !x.IsAbstract)
                .Select(x => (AbstractMaterialNode)Activator.CreateInstance(x));

            return nodes;
        }
    }
}
