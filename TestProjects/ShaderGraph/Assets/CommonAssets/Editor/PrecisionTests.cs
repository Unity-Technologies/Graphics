using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    internal class PrecisionTests
    {
        static string kGraphName = "Assets/CommonAssets/Graphs/Precision.shadergraph";
        GraphData m_Graph;

        Dictionary<string, PreviewNode> m_TestNodes = new Dictionary<string, PreviewNode>();

        [OneTimeSetUp]
        public void LoadGraph()
        {
            List<PropertyCollector.TextureInfo> lti;
            var assetCollection = new AssetCollection();
            ShaderGraphImporter.GetShaderText(kGraphName, out lti, assetCollection, out m_Graph);
            Assert.NotNull(m_Graph, $"Invalid graph data found for {kGraphName}");

            List<PreviewNode> previewNodes = m_Graph.GetNodes<PreviewNode>().ToList();
            Assert.AreNotEqual(0, previewNodes.Count, $"No PreviewNode found in {kGraphName}.");
            foreach(PreviewNode node in previewNodes)
            {
                GroupData group = m_Graph.groups.Where(x => x == node.group).FirstOrDefault();
                Assert.NotNull(m_Graph, $"Invalid group data found for PreviewNode with guid {node.objectId}");
                m_TestNodes.Add(group.title, node);
            }
        }

        [Test]
        public void CanSetNodeToSingle()
        {
            m_Graph.ValidateGraph();

            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            PreviewNode testNode;
            if(!m_TestNodes.TryGetValue(testName, out testNode))
                throw new Exception($"Failed to find test node for {testName}");

            Assert.AreEqual(ConcretePrecision.Single, testNode.concretePrecision);
        }

        [Test]
        public void CanSetNodeToHalf()
        {
            m_Graph.ValidateGraph();

            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            PreviewNode testNode;
            if(!m_TestNodes.TryGetValue(testName, out testNode))
                throw new Exception($"Failed to find test node for {testName}");

            Assert.AreEqual(ConcretePrecision.Half, testNode.concretePrecision);
        }

        [Test]
        public void CanCastToSingle()
        {
            m_Graph.ValidateGraph();

            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            PreviewNode testNode;
            if(!m_TestNodes.TryGetValue(testName, out testNode))
                throw new Exception($"Failed to find test node for {testName}");

            Assert.AreEqual(ConcretePrecision.Single, testNode.concretePrecision);
        }

        [Test]
        public void CanCastToHalf()
        {
            m_Graph.ValidateGraph();

            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            PreviewNode testNode;
            if(!m_TestNodes.TryGetValue(testName, out testNode))
                throw new Exception($"Failed to find test node for {testName}");

            Assert.AreEqual(ConcretePrecision.Half, testNode.concretePrecision);
        }

        [Test]
        public void CanSetPropToSingle()
        {
            m_Graph.ValidateGraph();

            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            PreviewNode testNode;
            if(!m_TestNodes.TryGetValue(testName, out testNode))
                throw new Exception($"Failed to find test node for {testName}");

            Assert.AreEqual(ConcretePrecision.Single, testNode.concretePrecision);
        }

        [Test]
        public void CanSetPropToHalf()
        {
            m_Graph.ValidateGraph();

            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            PreviewNode testNode;
            if(!m_TestNodes.TryGetValue(testName, out testNode))
                throw new Exception($"Failed to find test node for {testName}");

            Assert.AreEqual(ConcretePrecision.Half, testNode.concretePrecision);
        }

        [Test]
        public void CanInheritPropFromGraph()
        {
            m_Graph.ValidateGraph();

            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            PreviewNode testNode;
            if(!m_TestNodes.TryGetValue(testName, out testNode))
                throw new Exception($"Failed to find test node for {testName}");

            Assert.AreEqual(ConcretePrecision.Half, testNode.concretePrecision);
        }

        [Test]
        public void CanDefaultNodeToGraph()
        {
            m_Graph.ValidateGraph();

            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            PreviewNode testNode;
            if(!m_TestNodes.TryGetValue(testName, out testNode))
                throw new Exception($"Failed to find test node for {testName}");

            Assert.AreEqual(ConcretePrecision.Half, testNode.concretePrecision);
        }

        [Test]
        public void CanDefaultPortToGraph()
        {
            m_Graph.ValidateGraph();

            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            PreviewNode testNode;
            if(!m_TestNodes.TryGetValue(testName, out testNode))
                throw new Exception($"Failed to find test node for {testName}");

            Assert.AreEqual(ConcretePrecision.Half, testNode.concretePrecision);
        }

        [Test]
        public void CanResolveInheritance()
        {
            m_Graph.ValidateGraph();

            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            PreviewNode testNode;
            if(!m_TestNodes.TryGetValue(testName, out testNode))
                throw new Exception($"Failed to find test node for {testName}");

            Assert.AreEqual(ConcretePrecision.Single, testNode.concretePrecision);
        }
    }
}
