using System;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using com.unity.shadergraph.defs;

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
                    primitive = GraphType.Primitive.Float,
                    precision = GraphType.Precision.Fixed,
                    height = GraphType.Height.One,
                    length = GraphType.Length.Three,
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
    class PreviewTestFixture
    {
        HeadlessPreviewManager m_PreviewManager = new ();

        Registry m_RegistryInstance = new ();

        GraphHandler m_InterpreterTestsGraph;

        [OneTimeSetUp]
        public void Setup()
        {
            m_RegistryInstance.Register<GraphType>();
            m_RegistryInstance.Register<GraphTypeAssignment>();
            m_RegistryInstance.Register<AddNode>();
            m_RegistryInstance.Register<TestDescriptor>();

            // Setup a separate graph for the interpreter tests
            m_InterpreterTestsGraph = new GraphHandler();
            var node = m_InterpreterTestsGraph.AddNode<AddNode>("Add1", m_RegistryInstance);
            var port = node.GetPort("In1");
            var typeField = port.GetTypeField();
            var c0 = typeField.GetSubField<float>("c0");
            c0.SetData(1f); //(1,0,0,0)
            m_InterpreterTestsGraph.AddNode<AddNode>("Add2", m_RegistryInstance).GetPort("In2").GetTypeField().GetSubField<float>("c1").SetData(1f); //(0,1,0,0)
            m_InterpreterTestsGraph.AddNode<AddNode>("Add3", m_RegistryInstance);
            m_InterpreterTestsGraph.TryConnect("Add1", "Out", "Add3", "In1", m_RegistryInstance);
            m_InterpreterTestsGraph.TryConnect("Add2", "Out", "Add3", "In2", m_RegistryInstance); //should be (1,1,0,0)
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
            var graphHandler = new GraphHandler(); 

            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // Create an add node on the graph
            graphHandler.AddNode<AddNode>("Add1", m_RegistryInstance);
            var nodeWriter = graphHandler.GetNodeWriter("Add1");

            // Set the X component of the A input to 1, which makes the value Red
            nodeWriter.SetPortField("In1", "c0", 1.0f);
            m_PreviewManager.SetLocalProperty("Add1", "In1", 1);

            // Seems weird we need to cast down for this...
            graphHandler.AddContextNode(Registry.ResolveKey<TestDescriptor>(), m_RegistryInstance);

            // Verify context node is not null
            var contextNode = graphHandler.GetNodeReader("TestContextDescriptor");
            Assert.IsNotNull(contextNode);

            // Connect output of the Add node to the context node
            graphHandler.TryConnect("Add1", "Out", "TestContextDescriptor", "BaseColor", m_RegistryInstance);
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
            var graphHandler = new GraphHandler();

            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // Create an add node on the graph
            graphHandler.AddNode<AddNode>("Add1", m_RegistryInstance);
            var nodeWriter = graphHandler.GetNodeWriter("Add1");

            // Set the X component of the A input to 1, which makes the value Red
            nodeWriter.SetPortField("In1", "c0", 1f);
            m_PreviewManager.SetLocalProperty("Add1", "In1", 1);

            // Set the Y component of the B input to 1, which makes the value Green
            nodeWriter.SetPortField("In2", "c1", 1f);
            m_PreviewManager.SetLocalProperty("Add1", "In2", 1);

            graphHandler.AddContextNode(Registry.ResolveKey<TestDescriptor>(), m_RegistryInstance);

            // Verify context node is not null
            var contextNode = graphHandler.GetNodeReader("TestContextDescriptor");
            Assert.IsNotNull(contextNode);

            // Connect output of the Add node to the context node
            graphHandler.TryConnect("Add1", "Out", "TestContextDescriptor", "BaseColor", m_RegistryInstance);
            m_PreviewManager.NotifyNodeFlowChanged("TestContextDescriptor");

            // Request master preview material once the graph has been setup correctly
            m_PreviewManager.RequestMasterPreviewMaterial(400, 400, out var masterPreviewMaterial, out var shaderMessages);
            Assert.AreEqual(new Color(1, 1, 0, 1), SampleMaterialColor(masterPreviewMaterial));
        }

        [Test]
        public void MasterPreview_SubtractTwoColors()
        {
            // Instantiate a graph
            var graphHandler = new GraphHandler();

            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // Create an add node on the graph
            graphHandler.AddNode<AddNode>("Add1", m_RegistryInstance);
            var nodeWriter = graphHandler.GetNodeWriter("Add1");

            // Set the X component of the A input to 1
            nodeWriter.SetPortField("In1", "c0", 1f);
            m_PreviewManager.SetLocalProperty("Add1", "In1", 1);

            // Set the X component of the B input to -1
            nodeWriter.SetPortField("In2", "c0", -1f);
            m_PreviewManager.SetLocalProperty("Add1", "In2", -1);

            graphHandler.AddContextNode(Registry.ResolveKey<TestDescriptor>(), m_RegistryInstance);

            // Verify context node is not null
            var contextNode = graphHandler.GetNodeReader("TestContextDescriptor");
            Assert.IsNotNull(contextNode);

            // Connect output of the Add node to the context node
            graphHandler.TryConnect("Add1", "Out", "TestContextDescriptor", "BaseColor", m_RegistryInstance);
            m_PreviewManager.NotifyNodeFlowChanged("TestContextDescriptor");

            // Request master preview material once the graph has been setup correctly
            m_PreviewManager.RequestMasterPreviewMaterial(400, 400, out var masterPreviewMaterial, out var shaderMessages);
            Assert.AreEqual(Color.black, SampleMaterialColor(masterPreviewMaterial));
        }

        [Test]
        public void NodePreview_SingleColorMaterial()
        {
            // Instantiate a graph
            var graphHandler = new GraphHandler();

            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // Create an add node on the graph
            graphHandler.AddNode<AddNode>("Add1", m_RegistryInstance);
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
            var graphHandler = new GraphHandler();

            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // Create an add node on the graph
            graphHandler.AddNode<AddNode>("Add1", m_RegistryInstance);
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
            var graphHandler = new GraphHandler();

            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // Create an add node on the graph
            graphHandler.AddNode<AddNode>("Add1", m_RegistryInstance);
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
            var graphHandler = new GraphHandler();

            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // Create an add node on the graph
            graphHandler.AddNode<AddNode>("Add1", m_RegistryInstance);
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

        [Test]
        public void NodePreview_ValidShaderCode()
        {
            // Instantiate a graph
            var graphHandler = new GraphHandler();

            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // Create an add node on the graph
            graphHandler.AddNode<AddNode>("Add1", m_RegistryInstance);
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
            var graphHandler = new GraphHandler();

            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // Create an add node on the graph
            graphHandler.AddNode<AddNode>("Add1", m_RegistryInstance);
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
            var graphHandler = new GraphHandler();

            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // Create an add node on the graph
            graphHandler.AddNode<AddNode>("Add1", m_RegistryInstance);
            var nodeWriter = graphHandler.GetNodeWriter("Add1");

            graphHandler.AddContextNode(Registry.ResolveKey<TestDescriptor>(), m_RegistryInstance);

            // Verify context node is not null
            var contextNode = graphHandler.GetNodeReader("TestContextDescriptor");
            Assert.IsNotNull(contextNode);

            // Connect output of the Add node to the context node - throws exception
            graphHandler.TryConnect("Add1", "Out", "TestContextDescriptor", "BaseColor", m_RegistryInstance);

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
            var graphHandler = new GraphHandler();

            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // Create an add node on the graph
            graphHandler.AddNode<AddNode>("Add1", m_RegistryInstance);
            var nodeWriter = graphHandler.GetNodeWriter("Add1");

            // Set the X component of the A input to 1, which makes the value Red
            nodeWriter.SetPortField("In1", "c0", 1f);

            // Set the Y component of the B input to 1, which makes the value Green
            nodeWriter.SetPortField("In2", "c1", 1f);

            graphHandler.AddContextNode(Registry.ResolveKey<TestDescriptor>(), m_RegistryInstance);

            // Verify context node is not null
            var contextNode = graphHandler.GetNodeReader("TestContextDescriptor");
            Assert.IsNotNull(contextNode);

            // Connect output of the Add node to the context node - throws exception
            graphHandler.TryConnect("Add1", "Out", "TestContextDescriptor", "BaseColor", m_RegistryInstance);

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

        static object[] interpreterTestCases = new object[]
        {
            ("Add1", new Color(1,0,0,1)), //Colors with Alpha 1 since target is opaque
            ("Add2", new Color(0,1,0,1)),
            ("Add3", new Color(1,1,0,1)),
        };

        [Test]
        [TestCaseSource("interpreterTestCases")]
        public void InterpreterTests((string nodeToCompile, Color expectedColor) input)
        {
            m_PreviewManager.SetActiveGraph(m_InterpreterTestsGraph);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            var shaderString = m_PreviewManager.RequestNodePreviewShaderCode(m_InterpreterTestsGraph.GetNodeReader(input.nodeToCompile).GetName(), out var shaderMessages);
            bool tmp = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            Shader shaderObject = ShaderUtil.CreateShaderAsset(shaderString, true);
            ShaderUtil.allowAsyncCompilation = tmp;
            var rt = DrawShaderToTexture(shaderObject);
            Assert.AreEqual(input.expectedColor, rt.GetPixel(0, 0));
        }
        [Test]
        public void NodePreview_GetFuncAndBlockShaderCode()
        {
            FunctionDescriptor passThroughFD = new FunctionDescriptor(1, "PassThrough",
                "Out = In.x;",
                new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out),
                new ParameterDescriptor("In", TYPE.Float, GraphType.Usage.In));

            var graphHandler = new GraphHandler();
            var registry = new Registry();
            var previewMgr = new HeadlessPreviewManager();

            registry.Register<GraphType>();
            registry.Register<GraphTypeAssignment>();
            var passKey = registry.Register(passThroughFD);

            previewMgr.SetActiveGraph(graphHandler);
            previewMgr.SetActiveRegistry(registry);

            var passWriter = graphHandler.AddNode(passKey, "PassNodeInst", registry);

            passWriter.SetPortField("In", "c0", 1f);
            previewMgr.SetLocalProperty("PassNodeInst", "In", 1f);

            previewMgr.RequestNodePreviewShaderCodeStrings("PassNodeInst", out _, out _, out var blockCode, out var funcCode);

            // this block code is generated by the interpreter and is welcome to be modified if expectations of the interpreter change.
            string expectedBlock = "void PassThrough(out float1 Out, float1 In)\n{\nOut = In.x;\n}\nShaderGraphBlockBlock::ShaderGraphBlockOutput SYNTAX_PassNodeInstMain(ShaderGraphBlockBlock::ShaderGraphBlockInput In)\n{\nfloat1 SYNTAX_PassNodeInst_Out = float1(0);\nPassThrough(SYNTAX_PassNodeInst_Out, float1(1));\nShaderGraphBlockBlock::ShaderGraphBlockOutput output;\noutput.BaseColor = float3(SYNTAX_PassNodeInst_Out, SYNTAX_PassNodeInst_Out, SYNTAX_PassNodeInst_Out);\nreturn output;\n}\n";
            string expectedFunc = "void PassThrough(out float1 Out, float1 In)\n{\nOut = In.x;\n}\n";

            Assert.AreEqual(expectedFunc, funcCode);
            Assert.AreEqual(expectedBlock, blockCode);
        }

        [Test]
        public void FunctionDescriptor_PreviewOutputTypes()
        {
            FunctionDescriptor truncate = new(1, "Truncate",
                "Out = In.x;",
                new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out),
                new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In));

            FunctionDescriptor make = new FunctionDescriptor(1, "Make",
                "Out.x = X; Out.y = Y;",
                new ParameterDescriptor("Out", TYPE.Vec2, GraphType.Usage.Out),
                new ParameterDescriptor("X", TYPE.Float, GraphType.Usage.In),
                new ParameterDescriptor("Y", TYPE.Float, GraphType.Usage.In));

            FunctionDescriptor append = new(1, "Append",
                "Out.xy = In; Out.z = Z;",
                new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out),
                new ParameterDescriptor("In", TYPE.Vec2, GraphType.Usage.In),
                new ParameterDescriptor("Z", TYPE.Float, GraphType.Usage.In));

            var graphHandler = new GraphHandler();
            var registry = new Registry();
            var previewMgr = new HeadlessPreviewManager();

            registry.Register<GraphType>();
            registry.Register<GraphTypeAssignment>();
            var makeKey = registry.Register(make);
            var appendKey = registry.Register(append);
            var scalarKey = registry.Register(truncate);

            previewMgr.SetActiveGraph(graphHandler);
            previewMgr.SetActiveRegistry(registry);

            var scalarWriter = graphHandler.AddNode(scalarKey, "TruncateNodeInstance", registry);
            var makeWriter = graphHandler.AddNode(makeKey, "MakeNodeInstance", registry);
            var appendWriter = graphHandler.AddNode(appendKey, "AppendNodeInstance", registry);

            // White, float output duplicates across all components
            scalarWriter.SetPortField("In", "c0", 1f);
            previewMgr.SetLocalProperty("TruncateNodeInstance", "In", 1f);
            var nodePreviewMaterial = previewMgr.RequestNodePreviewMaterial("TruncateNodeInstance");
            Assert.AreEqual(new Color(1, 1, 1, 1), SampleMaterialColor(nodePreviewMaterial));

            // Yellow, X set to 1, Y comes from the output of the truncate node.
            makeWriter.SetPortField("X", "c0", 1f);
            previewMgr.SetLocalProperty("MakeNodeInstance", "X", 1f);
            graphHandler.TryConnect("TruncateNodeInstance", "Out", "MakeNodeInstance", "Y", registry);
            previewMgr.NotifyNodeFlowChanged("AppendNodeInstance");
            nodePreviewMaterial = previewMgr.RequestNodePreviewMaterial("MakeNodeInstance");
            Assert.AreEqual(new Color(1,1,0,1), SampleMaterialColor(nodePreviewMaterial));

            // White, Z is set to 1, XY comes from the vec2 output of the make node.
            appendWriter.SetPortField("Z", "c0", 1f);
            previewMgr.SetLocalProperty("AppendNodeInstance", "Z", 1f);
            graphHandler.TryConnect("MakeNodeInstance","Out","AppendNodeInstance", "In", registry);
            previewMgr.NotifyNodeFlowChanged("AppendNodeInstance");
            nodePreviewMaterial = previewMgr.RequestNodePreviewMaterial("AppendNodeInstance");
            Assert.AreEqual(new Color(1, 1, 1, 1), SampleMaterialColor(nodePreviewMaterial));
        }
        
        [Test]
        public void Gradients_TestAll()
        {
            var graphHandler = new GraphHandler();
            var registry = new Registry();
            var previewMgr = new HeadlessPreviewManager();
        
            registry.Register<GraphType>();
            registry.Register<GraphTypeAssignment>();
            registry.Register<GradientType>();
            registry.Register<GradientNode>();
            registry.Register<SampleGradientNode>();

            previewMgr.SetActiveGraph(graphHandler);
            previewMgr.SetActiveRegistry(registry);

            var nodeWriter = graphHandler.AddNode<SampleGradientNode>("SampleGradientNode", registry);
            previewMgr.NotifyNodeFlowChanged("SampleGradientNode");

            // Default 0 time color on a gradient is black.
            var nodePreviewMaterial = previewMgr.RequestNodePreviewMaterial("SampleGradientNode");
            Assert.AreEqual(new Color(0, 0, 0, 1), SampleMaterialColor(nodePreviewMaterial));

            // default 1 time color is white.
            nodeWriter.SetPortField(SampleGradientNode.kTime, "c0", 1f);
            previewMgr.SetLocalProperty("SampleGradientNode", SampleGradientNode.kTime, 1f);
            nodePreviewMaterial = previewMgr.RequestNodePreviewMaterial("SampleGradientNode");
            Assert.AreEqual(new Color(1, 1, 1, 1), SampleMaterialColor(nodePreviewMaterial));

            // our gradient comes from a connection now, let's pick a fun color (time is still 1).
            var gradientNode = graphHandler.AddNode<GradientNode>("GradientNode", registry);
            var portField = gradientNode.GetPort(GradientNode.kInlineStatic).GetTypeField();

            // Setup the end color to be yellow.
            var gradient = new Gradient();
            gradient.mode = GradientMode.Blend;
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0,0,0), 0),
                    new GradientColorKey(new Color(1,1,0), 1)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1, 0),
                    new GradientAlphaKey(1, 1)
                });

            GradientTypeHelpers.SetGradient(portField, gradient);

            graphHandler.TryConnect("GradientNode", "Out", "SampleGradientNode", "Gradient", registry);
            previewMgr.NotifyNodeFlowChanged("SampleGradientNode");
            nodePreviewMaterial = previewMgr.RequestNodePreviewMaterial("SampleGradientNode");

            previewMgr.RequestNodePreviewShaderCodeStrings("SampleGradientNode", out _, out _, out string block, out _);
            Assert.AreEqual(new Color(1, 1, 0, 1), SampleMaterialColor(nodePreviewMaterial));

            // TODO: split these tests up into fixtures and also move these sort of tests out of PreviewTests.cs
        }
    }
}
