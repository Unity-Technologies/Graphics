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
            Registry Registry = SGR.Registry;
            GraphHandler graph = new(Registry);

            PreviewService Preview = new();
            Preview.SetActiveRegistry(Registry);
            Preview.SetActiveGraph(graph);
            Preview.Initialize("ThisDontMatter", new UnityEngine.Vector2(125, 125));

            graph.AddNode(lerp, "lerpA");
            graph.AddNode(lerp, "lerpB");
            graph.AddNode(lerp, "lerpC");
            graph.AddNode(lerp, "lerpD");

            // A->B
            graph.TryConnect("lerpA", "Out", "lerpB", "A");
            graph.TryConnect("lerpA", "Out", "lerpB", "B");
            graph.TryConnect("lerpA", "Out", "lerpB", "T");

            // A->C
            graph.TryConnect("lerpA", "Out", "lerpC", "A");
            graph.TryConnect("lerpA", "Out", "lerpC", "B");
            graph.TryConnect("lerpA", "Out", "lerpC", "T");

            // A,B,C->D
            graph.TryConnect("lerpA", "Out", "lerpD", "B");
            graph.TryConnect("lerpB", "Out", "lerpD", "A");
            graph.TryConnect("lerpC", "Out", "lerpD", "T");

            graph.ReconcretizeAll();
            var material = Preview.RequestNodePreviewMaterial("lerpD");
            var value = PreviewTestFixture.SampleMaterialColor(material);
            Assert.AreNotEqual(BadImageResults, value);
        }


        // If two nodes (esp. of the same type) would promote properties,
        // they could have name conflicts-- this is just a simple sanity test to make sure that they are
        // not causing compilation issues.
        [Test]
        public void TestDoublePromotion()
        {
            Registry Registry = SGR.Registry;
            GraphHandler graph = new(Registry);

            PreviewService Preview = new();
            Preview.SetActiveRegistry(Registry);
            Preview.SetActiveGraph(graph);
            Preview.Initialize("ThisDontMatter", new UnityEngine.Vector2(125, 125));

            graph.AddNode(SampleTex2d, "texA");
            graph.AddNode(SampleTex2d, "texB");
            graph.AddNode(add, "result");

            // A->B
            graph.TryConnect("texA", "RGB", "result", "A");
            graph.TryConnect("texB", "RGB", "result", "B");


            graph.ReconcretizeAll();
            var material = Preview.RequestNodePreviewMaterial("result");
            var value = PreviewTestFixture.SampleMaterialColor(material);
            Assert.AreNotEqual(BadImageResults, value);
        }
    }
}
