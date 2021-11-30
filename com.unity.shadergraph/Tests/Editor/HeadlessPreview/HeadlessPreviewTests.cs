using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ContextLayeredDataStorage;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.Registry.Defs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Types = UnityEditor.ShaderGraph.Registry.Types;

namespace UnityEditor.ShaderGraph.HeadlessPreview.UnitTests
{
    // TODO: Move to preview manager and then rely on the name from the registry key for the context node/master preview data name
    class TestDescriptor : IContextDescriptor
    {
        public IReadOnlyCollection<IContextDescriptor.ContextEntry> GetEntries()
        {
            return new List<IContextDescriptor.ContextEntry>()
            {
                new ()
                {
                    fieldName = "BaseColor",
                    primitive = Registry.Types.GraphType.Primitive.Float,
                    height = 1,
                    length = 3,
                    precision = Registry.Types.GraphType.Precision.Fixed,
                    isFlat = true
                }
            };
        }

        public RegistryFlags GetRegistryFlags()
        {
           return RegistryFlags.Base;
        }

        public RegistryKey GetRegistryKey()
        {
            // Defines the name of the context node on the graph
            return new RegistryKey() { Name = "TestContextDescriptor", Version = 1 };
        }
    }


    // TODO: Consider async compile/rendering, need to wait until thread is done and results are returned
    // https://forum.unity.com/threads/support-for-async-await-in-tests.787853/#post-6838493

    [TestFixture]
    class HeadlessPreviewTestFixture
    {
        HeadlessPreviewManager m_PreviewManager = new ();

        Registry.Registry m_RegistryInstance = new ();

        [OneTimeSetUp]
        public void Setup()
        {
            m_RegistryInstance.Register<TestDescriptor>();
            m_RegistryInstance.Register<Types.GraphType>();
            m_RegistryInstance.Register<Types.AddNode>();
            m_RegistryInstance.Register<Types.GraphTypeAssignment>();
        }

        [TearDown]
        public void TestCleanup()
        {
            // Consider flushing the cached state of the preview manager between tests, depending on the test in question
            // And/or having a separate test fixture for the contiguous/standalone tests
        }

        private static Texture2D DrawToTex(Shader shader)
        {
            var rt = RenderTexture.GetTemporary(4,4,0,RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            Graphics.Blit(null, rt, new Material(shader));
            Texture2D output = new Texture2D(4, 4, TextureFormat.ARGB32, false);
            output.ReadPixels(new Rect(0, 0, 4, 4), 0, 0);
            RenderTexture.active = prevActive;
            rt.Release();
            return output;
        }
        bool DoesMaterialMatchColor(Material testMaterial, Color expectedColor)
        {
            var renderTarget = DrawToTex(testMaterial.shader);
            try
            {
                var outputColor = renderTarget.GetPixel(0, 0);
                return outputColor == expectedColor;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }

        bool DoesMaterialMatchImage(Material testMaterial, Texture expectedImage)
        {
            var renderTarget = DrawToTex(testMaterial.shader);
            try
            {
                var outputColor = renderTarget.GetPixel(0, 0);
                var texture = expectedImage as Texture2D;
                var expectedColor = texture.GetPixel(0, 0);
                return outputColor == expectedColor;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [Test]
        public void MasterPreview_SingleColor()
        {
            // Instantiate a graph
            var graphHandler = GraphUtil.CreateGraph();

            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // Create an add node on the graph
            graphHandler.AddNode<Types.AddNode>("Add1", m_RegistryInstance);
            var nodeWriter = graphHandler.GetNodeWriter("Add1");

            // By default every Add node has vector4 inputs

            // Set the X component of the A input to 1, which makes the value Red
            nodeWriter.SetPortField("In1", "c0", 1f);

            // Seems weird we need to cast down for this...
            var graphDelta = graphHandler as GraphDelta.GraphDelta;
            // Throws an exception right now
            graphDelta.SetupContextNodes(new List<IContextDescriptor>() { new TestDescriptor() }, m_RegistryInstance);

            // Verify context node is not null
            var contextNode = graphHandler.GetNodeReader("TestContextDescriptor");
            Assert.IsNotNull(contextNode);

            // Connect output of the Add node to the context node - throws exception
            graphDelta.TryConnect("Add1", "Out", "TestContextDescriptor", "BaseColor", m_RegistryInstance);

            // Request master preview material once the graph has been setup correctly
            m_PreviewManager.RequestMasterPreviewMaterial(400, 400, out var masterPreviewMaterial, out var shaderMessages);
            Assert.IsNotNull(masterPreviewMaterial);
            Assert.IsTrue(DoesMaterialMatchColor(masterPreviewMaterial, Color.red));
        }


        [Test]
        public void MasterPreview_AddTwoColors()
        {
            // Instantiate a graph
            var graphHandler = GraphUtil.CreateGraph();

            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // Create an add node on the graph
            graphHandler.AddNode<Types.AddNode>("Add1", m_RegistryInstance);
            var nodeWriter = graphHandler.GetNodeWriter("Add1");

            // By default every Add node has vector4 inputs

            // Set the X component of the A input to 1, which makes the value Red
            nodeWriter.SetPortField("In1", "c0", 1f);

            // Set the Y component of the B input to 1, which makes the value Green
            nodeWriter.SetPortField("In2", "c1", 1f);

            // Seems weird we need to cast down for this...
            var graphDelta = graphHandler as GraphDelta.GraphDelta;
            // Throws an exception right now
            graphDelta.SetupContextNodes(new List<Registry.Defs.IContextDescriptor>() { new TestDescriptor() }, m_RegistryInstance);

            // Verify context node is not null
            var contextNode = graphHandler.GetNodeReader("TestContextDescriptor");
            Assert.IsNotNull(contextNode);

            // Connect output of the Add node to the context node - throws exception
            graphDelta.TryConnect("Add1", "Out", "TestContextDescriptor", "BaseColor", m_RegistryInstance);

            // Request master preview material once the graph has been setup correctly
            m_PreviewManager.RequestMasterPreviewMaterial(400, 400, out var masterPreviewMaterial, out var shaderMessages);
            Assert.IsTrue(DoesMaterialMatchColor(masterPreviewMaterial, new Color(1, 1, 0, 1)));
        }

        [Test]
        public void MasterPreview_SubtractTwoColors()
        {
            // Instantiate a graph
            var graphHandler = GraphUtil.CreateGraph();

            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // Create an add node on the graph
            graphHandler.AddNode<Types.AddNode>("Add1", m_RegistryInstance);
            var nodeWriter = graphHandler.GetNodeWriter("Add1");

            // By default every Add node has vector4 inputs

            // Set the X component of the A input to 1
            nodeWriter.SetPortField("In1", "c0", 1f);

            // Set the X component of the B input to -1
            nodeWriter.SetPortField("In2", "c0", -1f);

            // Seems weird we need to cast down for this...
            var graphDelta = graphHandler as GraphDelta.GraphDelta;
            // Throws an exception right now
            graphDelta.SetupContextNodes(new List<Registry.Defs.IContextDescriptor>() { new TestDescriptor() }, m_RegistryInstance);

            // Verify context node is not null
            var contextNode = graphHandler.GetNodeReader("TestContextDescriptor");
            Assert.IsNotNull(contextNode);

            // Connect output of the Add node to the context node - throws exception
            graphDelta.TryConnect("Add1", "Out", "TestContextDescriptor", "BaseColor", m_RegistryInstance);

            // Request master preview material once the graph has been setup correctly
            m_PreviewManager.RequestMasterPreviewMaterial(400, 400, out var masterPreviewMaterial, out var shaderMessages);
            Assert.IsTrue(DoesMaterialMatchColor(masterPreviewMaterial, Color.black));
        }

        // TODO: Same test as above but testing the output texture/image instead of the material

        public void MasterPreview_MasterPreviewShaderTest()
        {
            // Graph with node network setup as expected
            var assetPath = "Assets/CommonAssets/Graphs/Preview/MasterPreviewCode";
            var graphHandler = GraphUtil.OpenGraph(assetPath) as IGraphHandler;
            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            //var previewShader = m_PreviewManager.RequestMasterPreviewShaderCode();
            // Q) Possible test ideas:
            //      1) Check if the shader code provided by the preview manager matches up to the shader code provided by Interpreter directly?
            //      2) Testing the caching behavior, i.e. if changes are made or not made, the shader code object returned should match expected state
            //      3) Testing if the shader code output is valid
            //      4) Testing if shader code output compiles
            //      5) Testing if the material being generated and its shader code matches the cached shader code
            //      6) Testing if the material render output (graph & node) matches the desired state after making some property changes
            //      7) Testing if a given property exists in the material property block?
            //      8) Testing if a property is at a given value in the MPB after setting the property
        }
    }
}
