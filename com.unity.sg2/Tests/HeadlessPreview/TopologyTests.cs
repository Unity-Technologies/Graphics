using NUnit.Framework;
using UnityEngine;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.HeadlessPreview.UnitTests;

namespace UnityEditor.ShaderGraph.HeadlessPreview.NodeTests
{
    [TestFixture]
    class TopologyPreviewTests
    {
        ShaderGraphRegistry SGR;

        [OneTimeSetUp]
        public void StartUp()
        {
            SGR = new();
            SGR.InitializeDefaults();
        }

        static readonly Color BadImageResults = new(1, 0, 1, 1);
        static readonly RegistryKey SampleTex2d = new RegistryKey { Name = "SampleTexture2D", Version = 1 };
        static readonly RegistryKey lerp = new RegistryKey { Name = "Lerp", Version = 1 };
        static readonly RegistryKey add = new RegistryKey { Name = "Add", Version = 1 };

        GraphHandler m_Graph;
        PreviewService m_Preview;

        [SetUp]
        public void Setup()
        {
            m_Graph = new(SGR.Registry);

            m_Preview = new();
            m_Preview.SetActiveRegistry(SGR.Registry);
            m_Preview.SetActiveGraph(m_Graph);
            m_Preview.Initialize("ThisDontMatter", new UnityEngine.Vector2(125, 125));
        }

        [TearDown]
        public void TearDown()
        {
            m_Preview.Cleanup();
        }

        // The following topology tests that topological sorting where upstream dependencies
        // have dependencies on each other-- it's important that any generated variable names
        // don't conflict. This test covers 2 separate configurations that have caused issues.
        // In each case, the output port of the node needs to be repeated across the various downstream inputs.
        /*

          The Triangle:
           A----->D
            \     ^
             v   /
               C

        The Diamond:
               B
             ^   \
            /     v
           A      D
            \     ^
             v   /
               C


        Triangle + Diamond can be tested at the same time using a node w/3 inputs, eg. Lerp.
               B
             ^   \
            /     v
           A----->D
            \     ^
             v   /
               C
         */
        [Test]
        public void TestDiamondAndTriangle()
        {

            m_Graph.AddNode(lerp, "lerpA");
            m_Graph.AddNode(lerp, "lerpB");
            m_Graph.AddNode(lerp, "lerpC");
            m_Graph.AddNode(lerp, "lerpD");

            // A->B
            m_Graph.TryConnect("lerpA", "Out", "lerpB", "A");
            m_Graph.TryConnect("lerpA", "Out", "lerpB", "B");
            m_Graph.TryConnect("lerpA", "Out", "lerpB", "T");

            // A->C
            m_Graph.TryConnect("lerpA", "Out", "lerpC", "A");
            m_Graph.TryConnect("lerpA", "Out", "lerpC", "B");
            m_Graph.TryConnect("lerpA", "Out", "lerpC", "T");

            // A,B,C->D
            m_Graph.TryConnect("lerpA", "Out", "lerpD", "B");
            m_Graph.TryConnect("lerpB", "Out", "lerpD", "A");
            m_Graph.TryConnect("lerpC", "Out", "lerpD", "T");

            m_Graph.ReconcretizeAll();
            var material = m_Preview.RequestNodePreviewMaterial("lerpD");
            var value = PreviewTestFixture.SampleMaterialColor(material);
            Assert.AreNotEqual(BadImageResults, value);
        }


        // If two nodes (esp. of the same type) would promote properties,
        // they could have name conflicts-- this is just a simple sanity test to make sure that they are
        // not causing compilation issues.
        [Test]
        public void TestDoublePromotion()
        {
            m_Graph.AddNode(SampleTex2d, "texA");
            m_Graph.AddNode(SampleTex2d, "texB");
            m_Graph.AddNode(add, "result");

            // A->B
            m_Graph.TryConnect("texA", "RGB", "result", "A");
            m_Graph.TryConnect("texB", "RGB", "result", "B");


            m_Graph.ReconcretizeAll();
            var material = m_Preview.RequestNodePreviewMaterial("result");
            var value = PreviewTestFixture.SampleMaterialColor(material);
            Assert.AreNotEqual(BadImageResults, value);
        }
    }
}
