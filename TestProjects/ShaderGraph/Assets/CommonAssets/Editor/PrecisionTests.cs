using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Graphing;


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
            ShaderGraphImporterLegacy.GetShaderText(kGraphName, out lti, assetCollection, out m_Graph);
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
            // this test checks if a node using inherited precision can correctly inherit precision from a source node set to single precision
            // basically:    single ==> inherit (single)
            m_Graph.ValidateGraph();

            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            PreviewNode testNode;
            if(!m_TestNodes.TryGetValue(testName, out testNode))
                throw new Exception($"Failed to find test node for {testName}");

            Assert.AreEqual(Precision.Inherit, testNode.precision);
            Assert.AreEqual(GraphPrecision.Single, testNode.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Single, testNode.concretePrecision);

            var parent = NodeUtils.GetParentNodes(testNode).First();
            Assert.AreEqual(GraphPrecision.Single, parent.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Single, parent.concretePrecision);
        }

        [Test]
        public void CanSetNodeToHalf()
        {
            // this test checks if a node using inherited precision can correctly inherit precision from a source node set to half precision
            // half ==> inherit (half)
            m_Graph.ValidateGraph();

            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            PreviewNode testNode;
            if(!m_TestNodes.TryGetValue(testName, out testNode))
                throw new Exception($"Failed to find test node for {testName}");

            Assert.AreEqual(Precision.Inherit, testNode.precision);
            Assert.AreEqual(GraphPrecision.Half, testNode.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Half, testNode.concretePrecision);

            var parent = NodeUtils.GetParentNodes(testNode).First();
            Assert.AreEqual(GraphPrecision.Half, parent.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Half, parent.concretePrecision);
        }

        [Test]
        public void CanCastToSingle()
        {
            // testing multiple overrides in a chain, requiring an upcast to single
            // half ==> single ==> inherit (single)
            m_Graph.ValidateGraph();

            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            PreviewNode testNode;
            if(!m_TestNodes.TryGetValue(testName, out testNode))
                throw new Exception($"Failed to find test node for {testName}");

            Assert.AreEqual(Precision.Inherit, testNode.precision);
            Assert.AreEqual(GraphPrecision.Single, testNode.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Single, testNode.concretePrecision);

            var parent = NodeUtils.GetParentNodes(testNode).First();
            Assert.AreEqual(GraphPrecision.Single, parent.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Single, parent.concretePrecision);

            var grandparent = NodeUtils.GetParentNodes(parent).First();
            Assert.AreEqual(GraphPrecision.Half, grandparent.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Half, grandparent.concretePrecision);
        }

        [Test]
        public void CanCastToHalf()
        {
            // testing multiple overrides in a chain, requiring a downcast to half
            // single ==> half ==> inherit (half)
            m_Graph.ValidateGraph();

            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            PreviewNode testNode;
            if(!m_TestNodes.TryGetValue(testName, out testNode))
                throw new Exception($"Failed to find test node for {testName}");

            Assert.AreEqual(Precision.Inherit, testNode.precision);
            Assert.AreEqual(GraphPrecision.Half, testNode.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Half, testNode.concretePrecision);

            var parent = NodeUtils.GetParentNodes(testNode).First();
            Assert.AreEqual(GraphPrecision.Half, parent.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Half, parent.concretePrecision);

            var grandparent = NodeUtils.GetParentNodes(parent).First();
            Assert.AreEqual(GraphPrecision.Single, grandparent.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Single, grandparent.concretePrecision);
        }

        [Test]
        public void CanSetPropToSingle()
        {
            // tests that a property set to single precision is correctly inherited by a node using inherit precision
            // single property ==> inherit (single)
            m_Graph.ValidateGraph();

            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            PreviewNode testNode;
            if(!m_TestNodes.TryGetValue(testName, out testNode))
                throw new Exception($"Failed to find test node for {testName}");

            Assert.AreEqual(Precision.Inherit, testNode.precision);
            Assert.AreEqual(GraphPrecision.Single, testNode.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Single, testNode.concretePrecision);

            var parent = NodeUtils.GetParentNodes(testNode).First();
            Assert.AreEqual(GraphPrecision.Single, parent.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Single, parent.concretePrecision);
        }

        [Test]
        public void CanSetPropToHalf()
        {
            // half property ==> inherit (half)
            m_Graph.ValidateGraph();

            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            PreviewNode testNode;
            if(!m_TestNodes.TryGetValue(testName, out testNode))
                throw new Exception($"Failed to find test node for {testName}");

            Assert.AreEqual(Precision.Inherit, testNode.precision);
            Assert.AreEqual(GraphPrecision.Half, testNode.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Half, testNode.concretePrecision);

            var parent = NodeUtils.GetParentNodes(testNode).First();
            Assert.AreEqual(GraphPrecision.Half, parent.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Half, parent.concretePrecision);
        }

        [Test]
        public void CanSetPropToGraph()
        {
            // graph property ==> inherit node (should match graph setting)
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            PreviewNode testNode;
            if (!m_TestNodes.TryGetValue(testName, out testNode))
                throw new Exception($"Failed to find test node for {testName}");
            var parent = NodeUtils.GetParentNodes(testNode).First();

            // set graph to single, test that node matches
            m_Graph.SetGraphDefaultPrecision(GraphPrecision.Single);
            m_Graph.ValidateGraph();

            Assert.AreEqual(Precision.Inherit, testNode.precision);
            Assert.AreEqual(GraphPrecision.Graph, testNode.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Single, testNode.concretePrecision);

            Assert.AreEqual(GraphPrecision.Graph, parent.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Single, parent.concretePrecision);

            // set graph to half, test that node matches
            m_Graph.SetGraphDefaultPrecision(GraphPrecision.Half);
            m_Graph.ValidateGraph();

            Assert.AreEqual(Precision.Inherit, testNode.precision);
            Assert.AreEqual(GraphPrecision.Graph, testNode.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Half, testNode.concretePrecision);

            Assert.AreEqual(GraphPrecision.Graph, parent.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Half, parent.concretePrecision);
        }

        [Test]
        public void CanInheritPropFromGraph()
        {
            // inherit property ==> inherit node (should match graph setting)
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            PreviewNode testNode;
            if (!m_TestNodes.TryGetValue(testName, out testNode))
                throw new Exception($"Failed to find test node for {testName}");
            var parent = NodeUtils.GetParentNodes(testNode).First();

            // set graph to single, test that node matches
            m_Graph.SetGraphDefaultPrecision(GraphPrecision.Single);
            m_Graph.ValidateGraph();

            Assert.AreEqual(Precision.Inherit, testNode.precision);
            Assert.AreEqual(GraphPrecision.Graph, testNode.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Single, testNode.concretePrecision);

            Assert.AreEqual(GraphPrecision.Graph, parent.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Single, parent.concretePrecision);

            // set graph to half, test that node matches
            m_Graph.SetGraphDefaultPrecision(GraphPrecision.Half);
            m_Graph.ValidateGraph();

            Assert.AreEqual(Precision.Inherit, testNode.precision);
            Assert.AreEqual(GraphPrecision.Graph, testNode.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Half, testNode.concretePrecision);

            Assert.AreEqual(GraphPrecision.Graph, parent.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Half, parent.concretePrecision);
        }

        [Test]
        public void CanDefaultNodeToGraph()
        {
            // inherit (no input ports) ==> inherit  (should both be using graph precision)
            m_Graph.ValidateGraph();

            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            PreviewNode testNode;
            if(!m_TestNodes.TryGetValue(testName, out testNode))
                throw new Exception($"Failed to find test node for {testName}");
            var parent = NodeUtils.GetParentNodes(testNode).First();

            // set graph to single, test that node matches
            m_Graph.SetGraphDefaultPrecision(GraphPrecision.Single);
            m_Graph.ValidateGraph();

            Assert.AreEqual(Precision.Inherit, testNode.precision);
            Assert.AreEqual(GraphPrecision.Graph, testNode.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Single, testNode.concretePrecision);

            Assert.AreEqual(GraphPrecision.Graph, parent.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Single, parent.concretePrecision);

            // set graph to half, test that node matches
            m_Graph.SetGraphDefaultPrecision(GraphPrecision.Half);
            m_Graph.ValidateGraph();

            Assert.AreEqual(Precision.Inherit, testNode.precision);
            Assert.AreEqual(GraphPrecision.Graph, testNode.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Half, testNode.concretePrecision);

            Assert.AreEqual(GraphPrecision.Graph, parent.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Half, parent.concretePrecision);
        }

        [Test]
        public void CanDefaultPortToGraph()
        {
            // inherit with unassigned input port ==> inherit (should both be using graph precision)
            m_Graph.ValidateGraph();

            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            PreviewNode testNode;
            if (!m_TestNodes.TryGetValue(testName, out testNode))
                throw new Exception($"Failed to find test node for {testName}");
            var parent = NodeUtils.GetParentNodes(testNode).First();

            // set graph to single, test that node matches
            m_Graph.SetGraphDefaultPrecision(GraphPrecision.Single);
            m_Graph.ValidateGraph();

            Assert.AreEqual(Precision.Inherit, testNode.precision);
            Assert.AreEqual(GraphPrecision.Graph, testNode.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Single, testNode.concretePrecision);

            Assert.AreEqual(GraphPrecision.Graph, parent.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Single, parent.concretePrecision);

            // set graph to half, test that node matches
            m_Graph.SetGraphDefaultPrecision(GraphPrecision.Half);
            m_Graph.ValidateGraph();

            Assert.AreEqual(Precision.Inherit, testNode.precision);
            Assert.AreEqual(GraphPrecision.Graph, testNode.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Half, testNode.concretePrecision);

            Assert.AreEqual(GraphPrecision.Graph, parent.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Half, parent.concretePrecision);
        }

        public struct TestCase
        {
            public TestCase(GraphPrecision graphPrecision, Precision parentPrecision0, Precision parentPrecision1, GraphPrecision resultGraph, ConcretePrecision resultConcrete)
            {
                this.graphPrecision = graphPrecision;
                this.parentPrecision0 = parentPrecision0;
                this.parentPrecision1 = parentPrecision1;
                this.resultGraph = resultGraph;
                this.resultConcrete = resultConcrete;
            }

            // inputs
            public readonly GraphPrecision graphPrecision;
            public readonly Precision parentPrecision0;
            public readonly Precision parentPrecision1;

            // outputs
            public readonly GraphPrecision resultGraph;
            public readonly ConcretePrecision resultConcrete;
        }

        [Test]
        public void CanResolveInheritance()
        {
            // test combinations of the precision values.  i.e.   half + single ==> inherit (single)
            TestCase[] cases =
            {
                // inputs                                                           outputs
                new TestCase(GraphPrecision.Single, Precision.Single,  Precision.Single,     GraphPrecision.Single,  ConcretePrecision.Single ),
                new TestCase(GraphPrecision.Single, Precision.Half,    Precision.Half,       GraphPrecision.Half,    ConcretePrecision.Half   ),
                new TestCase(GraphPrecision.Single, Precision.Graph,   Precision.Graph,      GraphPrecision.Graph,   ConcretePrecision.Single ),
                new TestCase(GraphPrecision.Single, Precision.Inherit, Precision.Inherit,    GraphPrecision.Graph,   ConcretePrecision.Single ),
                new TestCase(GraphPrecision.Single, Precision.Half,    Precision.Single,     GraphPrecision.Single,  ConcretePrecision.Single ),
                new TestCase(GraphPrecision.Single, Precision.Single,  Precision.Half,       GraphPrecision.Single,  ConcretePrecision.Single ),
                new TestCase(GraphPrecision.Single, Precision.Inherit, Precision.Single,     GraphPrecision.Single,  ConcretePrecision.Single ),
                new TestCase(GraphPrecision.Single, Precision.Half,    Precision.Inherit,    GraphPrecision.Graph,   ConcretePrecision.Single ),
                new TestCase(GraphPrecision.Single, Precision.Graph,   Precision.Half,       GraphPrecision.Graph,   ConcretePrecision.Single ),
                new TestCase(GraphPrecision.Single, Precision.Inherit, Precision.Graph,      GraphPrecision.Graph,   ConcretePrecision.Single ),
                new TestCase(GraphPrecision.Single, Precision.Single,  Precision.Graph,      GraphPrecision.Single,  ConcretePrecision.Single ),

                new TestCase(GraphPrecision.Half,   Precision.Single,  Precision.Single,     GraphPrecision.Single,  ConcretePrecision.Single ),
                new TestCase(GraphPrecision.Half,   Precision.Half,    Precision.Half,       GraphPrecision.Half,    ConcretePrecision.Half   ),
                new TestCase(GraphPrecision.Half,   Precision.Graph,   Precision.Graph,      GraphPrecision.Graph,   ConcretePrecision.Half   ),
                new TestCase(GraphPrecision.Half,   Precision.Inherit, Precision.Inherit,    GraphPrecision.Graph,   ConcretePrecision.Half   ),
                new TestCase(GraphPrecision.Half,   Precision.Half,    Precision.Single,     GraphPrecision.Single,  ConcretePrecision.Single ),
                new TestCase(GraphPrecision.Half,   Precision.Single,  Precision.Half,       GraphPrecision.Single,  ConcretePrecision.Single ),
                new TestCase(GraphPrecision.Half,   Precision.Inherit, Precision.Single,     GraphPrecision.Single,  ConcretePrecision.Single ),
                new TestCase(GraphPrecision.Half,   Precision.Half,    Precision.Inherit,    GraphPrecision.Graph,   ConcretePrecision.Half   ),
                new TestCase(GraphPrecision.Half,   Precision.Graph,   Precision.Half,       GraphPrecision.Graph,   ConcretePrecision.Half   ),
                new TestCase(GraphPrecision.Half,   Precision.Inherit, Precision.Graph,      GraphPrecision.Graph,   ConcretePrecision.Half   ),
                new TestCase(GraphPrecision.Half,   Precision.Single,  Precision.Graph,      GraphPrecision.Single,  ConcretePrecision.Single ),
            };

            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            PreviewNode testNode;
            if (!m_TestNodes.TryGetValue(testName, out testNode))
                throw new Exception($"Failed to find test node for {testName}");
            var parents = NodeUtils.GetParentNodes(testNode);
            var grandparents = NodeUtils.GetParentNodes(parents.First());
            Assert.AreEqual(grandparents.Count, 2);
            var parent0 = grandparents[0];
            var parent1 = grandparents[1];

            int testIndex = 0;
            foreach (var test in cases)
            {
                // set graph to half, test that node matches
                m_Graph.SetGraphDefaultPrecision(test.graphPrecision);
                parent0.precision = test.parentPrecision0;
                parent1.precision = test.parentPrecision1;
                m_Graph.ValidateGraph();

                Assert.AreEqual(test.resultGraph, testNode.graphPrecision, "GraphPrecision Mismatch on TestCase " + testIndex);
                Assert.AreEqual(test.resultConcrete, testNode.concretePrecision, "ConcretePrecision Mismatch on TestCase " + testIndex);
                testIndex++;
            }
        }

        [Test]
        public void SubGraphSingle()
        {
            // single subgraph node ==> inherit (single)
            m_Graph.ValidateGraph();

            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            PreviewNode testNode;
            if (!m_TestNodes.TryGetValue(testName, out testNode))
                throw new Exception($"Failed to find test node for {testName}");

            Assert.AreEqual(Precision.Inherit, testNode.precision);
            Assert.AreEqual(GraphPrecision.Single, testNode.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Single, testNode.concretePrecision);

            var parent = NodeUtils.GetParentNodes(testNode).First();
            Assert.AreEqual(GraphPrecision.Single, parent.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Single, parent.concretePrecision);
        }

        [Test]
        public void SubGraphHalf()
        {
            // half subgraph node ==> inherit (half)
            m_Graph.ValidateGraph();

            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            PreviewNode testNode;
            if (!m_TestNodes.TryGetValue(testName, out testNode))
                throw new Exception($"Failed to find test node for {testName}");

            Assert.AreEqual(Precision.Inherit, testNode.precision);
            Assert.AreEqual(GraphPrecision.Half, testNode.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Half, testNode.concretePrecision);

            var parent = NodeUtils.GetParentNodes(testNode).First();
            Assert.AreEqual(GraphPrecision.Half, parent.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Half, parent.concretePrecision);
        }

        [Test]
        public void SubGraphInherit()
        {
            // half subgraph node ==> inherit (half)
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            PreviewNode testNode;
            if (!m_TestNodes.TryGetValue(testName, out testNode))
                throw new Exception($"Failed to find test node for {testName}");
            var parent = NodeUtils.GetParentNodes(testNode).First();

            // set graph to single, test that node matches
            m_Graph.SetGraphDefaultPrecision(GraphPrecision.Single);
            m_Graph.ValidateGraph();

            Assert.AreEqual(Precision.Inherit, testNode.precision);
            Assert.AreEqual(GraphPrecision.Graph, testNode.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Single, testNode.concretePrecision);

            Assert.AreEqual(GraphPrecision.Graph, parent.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Single, parent.concretePrecision);

            // set graph to half, test that node matches
            m_Graph.SetGraphDefaultPrecision(GraphPrecision.Half);
            m_Graph.ValidateGraph();

            Assert.AreEqual(Precision.Inherit, testNode.precision);
            Assert.AreEqual(GraphPrecision.Graph, testNode.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Half, testNode.concretePrecision);

            Assert.AreEqual(GraphPrecision.Graph, parent.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Half, parent.concretePrecision);

            // set subgraph NODE to override half, and graph to Single
            m_Graph.SetGraphDefaultPrecision(GraphPrecision.Single);
            parent.precision = Precision.Half;
            m_Graph.ValidateGraph();

            Assert.AreEqual(Precision.Inherit, testNode.precision);
            Assert.AreEqual(GraphPrecision.Half, testNode.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Half, testNode.concretePrecision);

            Assert.AreEqual(GraphPrecision.Half, parent.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Half, parent.concretePrecision);

            // set subgraph NODE to graph, and graph to Single
            m_Graph.SetGraphDefaultPrecision(GraphPrecision.Single);
            parent.precision = Precision.Graph;
            m_Graph.ValidateGraph();

            Assert.AreEqual(Precision.Inherit, testNode.precision);
            Assert.AreEqual(GraphPrecision.Graph, testNode.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Single, testNode.concretePrecision);

            Assert.AreEqual(GraphPrecision.Graph, parent.graphPrecision);
            Assert.AreEqual(ConcretePrecision.Single, parent.concretePrecision);
        }

    }
}
