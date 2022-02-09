using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ContextLayeredDataStorage;
using UnityEditor.ShaderGraph.Generation;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.Registry.Defs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Windows;
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

        static Texture2D DrawShaderToTexture(Shader shader)
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

        static Texture2D DrawRTToTexture(RenderTexture renderTexture)
        {
            var prevActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Texture2D output = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
            Rect readPixels = new Rect(0, 0, output.width, output.height);
            output.ReadPixels(readPixels, 0, 0);
            output.Apply();
            RenderTexture.active = prevActive;
            renderTexture.Release();
            return output;
        }


        static Color SampleMaterialColor(Material material)
        {
            var outputTexture = DrawShaderToTexture(material.shader);
            try
            {
                return outputTexture.GetPixel(0, 0);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        static Color SampleImageColor(Texture image)
        {
            try
            {
                var renderTexture = image as RenderTexture;
                var outputTexture = DrawRTToTexture(renderTexture);
                return outputTexture.GetPixel(0, 0);
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

            // Set the X component of the A input to 1, which makes the value Red
            nodeWriter.SetPortField("In1", "c0", 1.0f);
            m_PreviewManager.SetLocalProperty("Add1", "In1", 1);

            // Seems weird we need to cast down for this...
            var graphDelta = graphHandler as GraphDelta.GraphDelta;
            graphDelta.SetupContextNodes(new List<IContextDescriptor>() { new TestDescriptor() }, m_RegistryInstance);

            // Verify context node is not null
            var contextNode = graphHandler.GetNodeReader("TestContextDescriptor");
            Assert.IsNotNull(contextNode);

            // Connect output of the Add node to the context node
            graphDelta.TryConnect("Add1", "Out", "TestContextDescriptor", "BaseColor", m_RegistryInstance);
            m_PreviewManager.NotifyNodeFlowChanged("TestContextDescriptor");

            // Request master preview material once the graph has been setup correctly
            m_PreviewManager.RequestMasterPreviewMaterial(400, 400, out var masterPreviewMaterial, out var shaderMessages);
            Assert.IsNotNull(masterPreviewMaterial);
            Assert.AreEqual(Color.red, SampleMaterialColor(masterPreviewMaterial));
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

            // Set the X component of the A input to 1, which makes the value Red
            nodeWriter.SetPortField("In1", "c0", 1f);
            m_PreviewManager.SetLocalProperty("Add1", "In1", 1);

            // Set the Y component of the B input to 1, which makes the value Green
            nodeWriter.SetPortField("In2", "c1", 1f);
            m_PreviewManager.SetLocalProperty("Add1", "In2", 1);

            // Seems weird we need to cast down for this...
            var graphDelta = graphHandler as GraphDelta.GraphDelta;
            graphDelta.SetupContextNodes(new List<Registry.Defs.IContextDescriptor>() { new TestDescriptor() }, m_RegistryInstance);

            // Verify context node is not null
            var contextNode = graphHandler.GetNodeReader("TestContextDescriptor");
            Assert.IsNotNull(contextNode);

            // Connect output of the Add node to the context node
            graphDelta.TryConnect("Add1", "Out", "TestContextDescriptor", "BaseColor", m_RegistryInstance);
            m_PreviewManager.NotifyNodeFlowChanged("TestContextDescriptor");

            // Request master preview material once the graph has been setup correctly
            m_PreviewManager.RequestMasterPreviewMaterial(400, 400, out var masterPreviewMaterial, out var shaderMessages);
            Assert.AreEqual(new Color(1, 1, 0, 1), SampleMaterialColor(masterPreviewMaterial));
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

            // Set the X component of the A input to 1
            nodeWriter.SetPortField("In1", "c0", 1f);
            m_PreviewManager.SetLocalProperty("Add1", "In1", 1);

            // Set the X component of the B input to -1
            nodeWriter.SetPortField("In2", "c0", -1f);
            m_PreviewManager.SetLocalProperty("Add1", "In2", -1);

            // Seems weird we need to cast down for this...
            var graphDelta = graphHandler as GraphDelta.GraphDelta;
            graphDelta.SetupContextNodes(new List<Registry.Defs.IContextDescriptor>() { new TestDescriptor() }, m_RegistryInstance);

            // Verify context node is not null
            var contextNode = graphHandler.GetNodeReader("TestContextDescriptor");
            Assert.IsNotNull(contextNode);

            // Connect output of the Add node to the context node
            graphDelta.TryConnect("Add1", "Out", "TestContextDescriptor", "BaseColor", m_RegistryInstance);
            m_PreviewManager.NotifyNodeFlowChanged("TestContextDescriptor");

            // Request master preview material once the graph has been setup correctly
            m_PreviewManager.RequestMasterPreviewMaterial(400, 400, out var masterPreviewMaterial, out var shaderMessages);
            Assert.AreEqual(Color.black, SampleMaterialColor(masterPreviewMaterial));
        }

        [Test]
        public void NodePreview_SingleColorMaterial()
        {
            // Instantiate a graph
            var graphHandler = GraphUtil.CreateGraph();

            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // Create an add node on the graph
            graphHandler.AddNode<Types.AddNode>("Add1", m_RegistryInstance);
            var nodeWriter = graphHandler.GetNodeWriter("Add1");

            // Set the X component of the A input to 1, which makes the value Red
            nodeWriter.SetPortField("In1", "c0", 1f);
            m_PreviewManager.SetLocalProperty("Add1", "In1", 1);

            // Request node preview material once the graph has been setup correctly
            var nodePreviewMaterial = m_PreviewManager.RequestNodePreviewMaterial("Add1");
            Assert.IsNotNull(nodePreviewMaterial);
            Assert.AreEqual(Color.red, SampleMaterialColor(nodePreviewMaterial));
        }

        [Test]
        public void NodePreview_SingleColorImage()
        {
            // Instantiate a graph
            var graphHandler = GraphUtil.CreateGraph();

            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // Create an add node on the graph
            graphHandler.AddNode<Types.AddNode>("Add1", m_RegistryInstance);
            var nodeWriter = graphHandler.GetNodeWriter("Add1");

            // Set the X component of the A input to 1, which makes the value Red
            nodeWriter.SetPortField("In1", "c0", 1.0f);
            m_PreviewManager.SetLocalProperty("Add1", "In1", 1);

            // Request node preview image once the graph has been setup correctly
            m_PreviewManager.RequestNodePreviewImage("Add1", out var nodeRenderOutput, out var shaderMessages);
            Assert.IsNotNull(nodeRenderOutput);
            Assert.AreEqual(Color.red, SampleImageColor(nodeRenderOutput));
        }

        [Test]
        public void NodePreview_AddTwoColors()
        {
            // Instantiate a graph
            var graphHandler = GraphUtil.CreateGraph();

            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // Create an add node on the graph
            graphHandler.AddNode<Types.AddNode>("Add1", m_RegistryInstance);
            var nodeWriter = graphHandler.GetNodeWriter("Add1");

            // Set the X component of the A input to 1, which makes the value Red
            nodeWriter.SetPortField("In1", "c0", 1f);
            m_PreviewManager.SetLocalProperty("Add1", "In1", 1);

            // Set the Y component of the B input to 1, which makes the value Green
            nodeWriter.SetPortField("In2", "c1", 1f);
            m_PreviewManager.SetLocalProperty("Add1", "In2", 1);

            // Request node preview material once the graph has been setup correctly
            var nodePreviewMaterial = m_PreviewManager.RequestNodePreviewMaterial("Add1");
            Assert.IsNotNull(nodePreviewMaterial);
            Assert.AreEqual(new Color(1, 1, 0, 1), SampleMaterialColor(nodePreviewMaterial));
        }

        [Test]
        public void NodePreview_SubtractTwoColors()
        {
            // Instantiate a graph
            var graphHandler = GraphUtil.CreateGraph();

            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // Create an add node on the graph
            graphHandler.AddNode<Types.AddNode>("Add1", m_RegistryInstance);
            var nodeWriter = graphHandler.GetNodeWriter("Add1");

            // Set the X component of the A input to 1
            nodeWriter.SetPortField("In1", "c0", 1f);
            m_PreviewManager.SetLocalProperty("Add1", "In1", 1);

            // Set the Y component of the B input to -1
            nodeWriter.SetPortField("In2", "c0", -1f);
            m_PreviewManager.SetLocalProperty("Add1", "I2", -1);

            // Request node preview material once the graph has been setup correctly
            var nodePreviewMaterial = m_PreviewManager.RequestNodePreviewMaterial("Add1");
            Assert.IsNotNull(nodePreviewMaterial);
            Assert.AreEqual(Color.black, SampleMaterialColor(nodePreviewMaterial));
        }

        // TODO: Same tests as above but testing the output texture/image instead of the material
        [Test]
        public void NodePreview_ValidShaderCode()
        {
            // Instantiate a graph
            var graphHandler = GraphUtil.CreateGraph();

            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // Create an add node on the graph
            graphHandler.AddNode<Types.AddNode>("Add1", m_RegistryInstance);
            var nodeWriter = graphHandler.GetNodeWriter("Add1");

            // Set the X component of the A input to 1
            nodeWriter.SetPortField("In1", "c0", 1f);

            // Set the Y component of the B input to -1
            nodeWriter.SetPortField("In2", "c0", -1f);

            // Request node preview material once the graph has been setup correctly
            var nodePreviewShaderCode = m_PreviewManager.RequestNodePreviewShaderCode("Add1", out var shaderMessages);
            Shader testShaderOutput = ShaderUtil.CreateShaderAsset(nodePreviewShaderCode, true);

            Assert.IsFalse(ShaderUtil.ShaderHasError(testShaderOutput), shaderMessages.ToString());
        }

        [Test]
        public void NodePreview_CodeChange()
        {
            // Instantiate a graph
            var graphHandler = GraphUtil.CreateGraph();

            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // Create an add node on the graph
            graphHandler.AddNode<Types.AddNode>("Add1", m_RegistryInstance);
            var nodeWriter = graphHandler.GetNodeWriter("Add1");

            // Get code for node, first version
            var nodeCodev1 = m_PreviewManager.RequestNodePreviewShaderCode("Add1", out var shaderMessages1);
            Assert.IsTrue(nodeCodev1 != String.Empty);

            // Make some change
            nodeWriter.SetPortField("In1", "c0", 1f);
            m_PreviewManager.SetLocalProperty("Add1", "In1", 1);

            // Get code for node, second version
            var nodeCodev2 = m_PreviewManager.RequestNodePreviewShaderCode("Add1", out var shaderMessages2);
            Assert.IsTrue(nodeCodev2 != String.Empty);

            // Code generated should be different after a change was made
            Assert.IsTrue(nodeCodev1 != nodeCodev2);
        }

        [Test]
        public void MasterPreview_CodeChange()
        {
            // Instantiate a graph
            var graphHandler = GraphUtil.CreateGraph();

            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // Create an add node on the graph
            graphHandler.AddNode<Types.AddNode>("Add1", m_RegistryInstance);
            var nodeWriter = graphHandler.GetNodeWriter("Add1");

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
            m_PreviewManager.RequestMasterPreviewShaderCode(out var shaderCodev1, out var shaderMessages1);
            Assert.IsTrue(shaderCodev1 != String.Empty);

            // Make some change
            nodeWriter.SetPortField("In1", "c0", 1f);
            m_PreviewManager.SetLocalProperty("Add1", "In1", 1);

            // Request master preview material once the graph has been setup correctly
            m_PreviewManager.RequestMasterPreviewShaderCode(out var shaderCodev2, out var shaderMessages2);
            Assert.IsTrue(shaderCodev2 != String.Empty);

            // Code generated should be different after a change was made
            Assert.IsTrue(shaderCodev1 != shaderCodev2);
        }


        [Test]
        public void MasterPreview_ValidShaderCode()
        {
            // Instantiate a graph
            var graphHandler = GraphUtil.CreateGraph();

            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // Create an add node on the graph
            graphHandler.AddNode<Types.AddNode>("Add1", m_RegistryInstance);
            var nodeWriter = graphHandler.GetNodeWriter("Add1");

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
            m_PreviewManager.RequestMasterPreviewShaderCode(out var shaderCode, out var shaderMessages);
            Shader testShaderOutput = ShaderUtil.CreateShaderAsset(shaderCode, true);

            Assert.IsFalse(ShaderUtil.ShaderHasError(testShaderOutput), shaderMessages.ToString());

            //var previewShader = m_PreviewManager.RequestMasterPreviewShaderCode();
            // Q) Possible test ideas:
            //      5) Testing if the material being generated and its shader code matches the cached shader code
            //      6) Testing if the material render output (graph & node) matches the desired state after making some property changes
            //      7) Testing if a given property exists in the material property block?
            //      8) Testing if a property is at a given value in the MPB after setting the property
        }
    }
}
