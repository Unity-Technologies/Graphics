using System;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEditor.ShaderGraph.Defs;

namespace UnityEditor.ShaderGraph.HeadlessPreview.UnitTests
{
    // TODO: Move to preview manager and then rely on the name from the registry key for the context node/master preview data name
    class TestDescriptor : IContextDescriptor
    {
        public IEnumerable<ContextEntry> GetEntries()
        {
            return new List<ContextEntry>()
            {
                new ()
                {
                    fieldName = "BaseColor",
                    primitive = GraphType.Primitive.Float,
                    precision = GraphType.Precision.Single,
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
        HeadlessPreviewManager m_PreviewManager = new();

        Registry m_RegistryInstance = new();

        GraphHandler m_InterpreterTestsGraph;

        // This value needs to match the default specified in MainPreviewData in ShaderGraphAssetModel.cs
        int mainPreviewSize = 125;

        string testContextDescriptor => new TestDescriptor().GetRegistryKey().Name;

        [OneTimeSetUp]
        public void Setup()
        {
            m_RegistryInstance.Register<GraphType>();
            m_RegistryInstance.Register<GraphTypeAssignment>();
            m_RegistryInstance.Register<TestAddNode>();
            m_RegistryInstance.Register<TestDescriptor>();

            m_PreviewManager.Initialize(testContextDescriptor, new Vector2(mainPreviewSize, mainPreviewSize));

            // Setup a separate graph for the interpreter tests
            m_InterpreterTestsGraph = new GraphHandler(m_RegistryInstance);
            var node = m_InterpreterTestsGraph.AddNode<TestAddNode>("Add1");
            var port = node.GetPort("In1");
            var typeField = port.GetTypeField();
            var c0 = typeField.GetSubField<float>("c0");
            c0.SetData(1f); //(1,0,0,0)
            m_InterpreterTestsGraph.AddNode<TestAddNode>("Add2").GetPort("In2").GetTypeField().GetSubField<float>("c1").SetData(1f); //(0,1,0,0)
            m_InterpreterTestsGraph.AddNode<TestAddNode>("Add3");
            m_InterpreterTestsGraph.TryConnect("Add1", "Out", "Add3", "In1", m_RegistryInstance);
            m_InterpreterTestsGraph.TryConnect("Add2", "Out", "Add3", "In2", m_RegistryInstance); //should be (1,1,0,0)
        }

        [TearDown]
        public void TestCleanup()
        {
            // Consider flushing the cached state of the preview manager between tests, depending on the test in question
            // And/or having a separate test fixture for the contiguous/standalone tests
        }

        static Texture2D DrawMaterialToTexture(Material material)
        {
            var rt = RenderTexture.GetTemporary(4, 4, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            Graphics.Blit(null, rt, material);
            Texture2D output = new(4, 4, TextureFormat.ARGB32, false);
            output.ReadPixels(new Rect(0, 0, 4, 4), 0, 0);
            RenderTexture.active = prevActive;
            rt.Release();
            return output;
        }

        static Texture2D DrawShaderToTexture(Shader shader)
        {
            var rt = RenderTexture.GetTemporary(4, 4, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            Graphics.Blit(null, rt, new Material(shader));
            Texture2D output = new(4, 4, TextureFormat.ARGB32, false);
            output.ReadPixels(new Rect(0, 0, 4, 4), 0, 0);
            RenderTexture.active = prevActive;
            rt.Release();
            return output;
        }

        static Texture2D DrawRTToTexture(RenderTexture renderTexture)
        {
            var prevActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Texture2D output = new(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
            Rect readPixels = new(0, 0, output.width, output.height);
            output.ReadPixels(readPixels, 0, 0);
            output.Apply();
            RenderTexture.active = prevActive;
            renderTexture.Release();
            return output;
        }


        static Color SampleMaterialColor(Material material)
        {
            var outputTexture = DrawMaterialToTexture(material);
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

        static Color SampleTextureColor(Texture image, int width = 0, int height = 0)
        {
            try
            {
                switch (image)
                {
                    case RenderTexture renderTexture:
                        var outputTexture = DrawRTToTexture(renderTexture);
                        return outputTexture.GetPixel(width/2, height/2);
                    case Texture2D texture2D:
                        return texture2D.GetPixel(width/2, height/2);
                    default:
                        return Color.black;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        static GraphHandler SetupGraphWithAddNode(HeadlessPreviewManager previewManager, Registry registryInstance)
        {
            // Instantiate a graph
            var graphHandler = new GraphHandler(registryInstance);

            previewManager.SetActiveGraph(graphHandler);
            previewManager.SetActiveRegistry(registryInstance);

            // Create an add node on the graph
            graphHandler.AddNode<TestAddNode>("Add1");

            // Add context node to the graph
            var contextNodeRegistryKey = Registry.ResolveKey<TestDescriptor>();
            graphHandler.AddContextNode(contextNodeRegistryKey);

            // Verify context node is not null
            var contextNodeName = contextNodeRegistryKey.Name;
            var contextNode = graphHandler.GetNode(contextNodeName);
            Assert.IsNotNull(contextNode);

            // Connect output of the Add node to the context node
            graphHandler.TryConnect("Add1", "Out", contextNodeName, "BaseColor", registryInstance);
            previewManager.NotifyNodeFlowChanged(contextNodeName);

            return graphHandler;
        }


    [Test]
        public void MainPreview_SingleColor_Material()
        {
            // Instantiate the graph
            var graphHandler = SetupGraphWithAddNode(m_PreviewManager, m_RegistryInstance);

            var nodeWriter = graphHandler.GetNode("Add1");
            // Set the X component of the A input to 1, which makes the value Red
            nodeWriter.SetPortField("In1", "c0", 1.0f);
            m_PreviewManager.SetLocalProperty("Add1", "In1", 1);

            // Request master preview material once the graph has been setup correctly
            m_PreviewManager.RequestMainPreviewMaterial(mainPreviewSize, mainPreviewSize, out var MainPreviewMaterial, out var _);
            Assert.IsNotNull(MainPreviewMaterial);
            Assert.AreEqual(Color.red, SampleMaterialColor(MainPreviewMaterial));
        }

        [Test]
        public void MainPreview_AddTwoColors_Material()
        {
            // Instantiate the graph
            var graphHandler = SetupGraphWithAddNode(m_PreviewManager, m_RegistryInstance);

            var nodeWriter = graphHandler.GetNode("Add1");

            // Set the X component of the A input to 1, which makes the value Red
            nodeWriter.SetPortField("In1", "c0", 1f);
            m_PreviewManager.SetLocalProperty("Add1", "In1", 1);

            // Set the Y component of the B input to 1, which makes the value Green
            nodeWriter.SetPortField("In2", "c1", 1f);
            m_PreviewManager.SetLocalProperty("Add1", "In2", 1);

            // Request master preview material once the graph has been setup correctly
            m_PreviewManager.RequestMainPreviewMaterial(mainPreviewSize, mainPreviewSize, out var mainPreviewMaterial, out var _);
            Assert.IsNotNull(mainPreviewMaterial);
            Assert.AreEqual(new Color(1, 1, 0, 1), SampleMaterialColor(mainPreviewMaterial));
        }

        [Test]
        public void MainPreview_SubtractTwoColors_Material()
        {
            // Instantiate the graph
            var graphHandler = SetupGraphWithAddNode(m_PreviewManager, m_RegistryInstance);

            var nodeWriter = graphHandler.GetNode("Add1");

            // Set the X component of the A input to 1
            nodeWriter.SetPortField("In1", "c0", 1f);
            m_PreviewManager.SetLocalProperty("Add1", "In1", 1);

            // Set the X component of the B input to -1
            nodeWriter.SetPortField("In2", "c0", -1f);
            m_PreviewManager.SetLocalProperty("Add1", "In2", -1);

            // Request master preview material once the graph has been setup correctly
            m_PreviewManager.RequestMainPreviewMaterial(mainPreviewSize, mainPreviewSize, out var mainPreviewMaterial, out var _);
            Assert.IsNotNull(mainPreviewMaterial);
            Assert.AreEqual(Color.black, SampleMaterialColor(mainPreviewMaterial));
        }

        // This is currently known to fail due to the main preview outputting a lit render
        // TODO: (Sai) When we support unlit, this should change the graph to use the unlit subtarget
        [Test]
        public void MainPreview_AddTwoColors_Texture()
        {
            // Instantiate the graph
            var graphHandler = SetupGraphWithAddNode(m_PreviewManager, m_RegistryInstance);

            var nodeWriter = graphHandler.GetNode("Add1");

            // Set the X component of the A input to 1, which makes the value Red
            nodeWriter.SetPortField("In1", "c0", 1f);
            m_PreviewManager.SetLocalProperty("Add1", "In1", 1);

            // Set the Y component of the B input to 1, which makes the value Green
            nodeWriter.SetPortField("In2", "c1", 1f);
            m_PreviewManager.SetLocalProperty("Add1", "In2", 1);

            Mesh sphereMesh = Resources.GetBuiltinResource(typeof(Mesh), $"Sphere.fbx") as Mesh;

            // Request master preview texture
            m_PreviewManager.RequestMainPreviewTexture(
                mainPreviewSize,
                mainPreviewSize,
                sphereMesh,
                1.0f,
                false,
                Quaternion.identity,
                out var mainPreviewTexture,
                out _);
            Assert.IsNotNull(mainPreviewTexture);
            Assert.AreEqual(new Color(1, 1, 0, 1), SampleTextureColor(mainPreviewTexture, mainPreviewSize, mainPreviewSize));
        }

        [Test]
        public void NodePreview_SingleColor_Material()
        {
            // Instantiate the graph
            var graphHandler = SetupGraphWithAddNode(m_PreviewManager, m_RegistryInstance);

            var nodeWriter = graphHandler.GetNode("Add1");

            // Set the X component of the A input to 1, which makes the value Red
            nodeWriter.SetPortField("In1", "c0", 1f);
            m_PreviewManager.SetLocalProperty("Add1", "In1", 1);

            // Request node preview material once the graph has been setup correctly
            var nodePreviewMaterial = m_PreviewManager.RequestNodePreviewMaterial("Add1");
            Assert.IsNotNull(nodePreviewMaterial);
            Assert.AreEqual(Color.red, SampleMaterialColor(nodePreviewMaterial));
        }

        // This is currently known to fail due to the main preview outputting a lit render
        // TODO: (Sai) When we support unlit, this should change the graph to use the unlit subtarget
        [Test]
        public void NodePreview_SingleColor_Texture()
        {
            // Instantiate the graph
            var graphHandler = SetupGraphWithAddNode(m_PreviewManager, m_RegistryInstance);

            var nodeWriter = graphHandler.GetNode("Add1");

            // Set the X component of the A input to 1, which makes the value Red
            nodeWriter.SetPortField("In1", "c0", 1.0f);
            m_PreviewManager.SetLocalProperty("Add1", "In1", 1);

            // Request node preview image once the graph has been setup correctly
            m_PreviewManager.RequestNodePreviewTexture("Add1", out var nodeRenderOutput, out var _);
            Assert.IsNotNull(nodeRenderOutput);
            Assert.AreEqual(new Color(1, 0, 0, 1), SampleTextureColor(nodeRenderOutput));
        }

        [Test]
        public void NodePreview_AddTwoColors_Material()
        {
            // Instantiate the graph
            var graphHandler = SetupGraphWithAddNode(m_PreviewManager, m_RegistryInstance);

            var nodeWriter = graphHandler.GetNode("Add1");

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
        public void NodePreview_SubtractTwoColors_Material()
        {
            // Instantiate the graph
            var graphHandler = SetupGraphWithAddNode(m_PreviewManager, m_RegistryInstance);

            var nodeWriter = graphHandler.GetNode("Add1");

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
            // Instantiate the graph
            var graphHandler = SetupGraphWithAddNode(m_PreviewManager, m_RegistryInstance);

            var nodeWriter = graphHandler.GetNode("Add1");

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
            // Instantiate the graph
            var graphHandler = SetupGraphWithAddNode(m_PreviewManager, m_RegistryInstance);

            var nodeWriter = graphHandler.GetNode("Add1");

            // Get code for node, first version
            var nodeCodev1 = m_PreviewManager.RequestNodePreviewShaderCode("Add1", out var _);
            Assert.IsTrue(nodeCodev1 != String.Empty);

            // Make some change
            nodeWriter.SetPortField("In1", "c0", 1f);
            m_PreviewManager.SetLocalProperty("Add1", "In1", 1);

            // Get code for node, second version
            var nodeCodev2 = m_PreviewManager.RequestNodePreviewShaderCode("Add1", out var _);
            Assert.IsTrue(nodeCodev2 != String.Empty);

            // Code generated should be different after a change was made
            Assert.IsTrue(nodeCodev1 != nodeCodev2);
        }

        [Test]
        public void MainPreview_CodeChange()
        {
            // Instantiate the graph
            var graphHandler = SetupGraphWithAddNode(m_PreviewManager, m_RegistryInstance);

            var nodeWriter = graphHandler.GetNode("Add1");

            // Request master preview material once the graph has been setup correctly
            m_PreviewManager.RequestMainPreviewShaderCode(out var shaderCodev1, out var _);
            Assert.IsTrue(shaderCodev1 != String.Empty);

            // Make some change
            nodeWriter.SetPortField("In1", "c0", 1f);
            m_PreviewManager.SetLocalProperty("Add1", "In1", 1);

            m_PreviewManager.RequestMainPreviewShaderCode(out var shaderCodev2, out var _);
            Assert.IsTrue(shaderCodev2 != String.Empty);

            // Code generated should be different after a change was made
            Assert.IsTrue(shaderCodev1 != shaderCodev2);
        }


        [Test]
        public void MainPreview_ValidShaderCode()
        {
            // Instantiate the graph
            var graphHandler = SetupGraphWithAddNode(m_PreviewManager, m_RegistryInstance);

            var nodeWriter = graphHandler.GetNode("Add1");

            // Set the X component of the A input to 1, which makes the value Red
            nodeWriter.SetPortField("In1", "c0", 1f);

            // Set the Y component of the B input to 1, which makes the value Green
            nodeWriter.SetPortField("In2", "c1", 1f);

            // Request master preview material once the graph has been setup correctly
            m_PreviewManager.RequestMainPreviewShaderCode(out var shaderCode, out var shaderMessages);
            Shader testShaderOutput = ShaderUtil.CreateShaderAsset(shaderCode, true);

            Assert.IsFalse(ShaderUtil.ShaderHasError(testShaderOutput), shaderMessages.ToString());

            //var previewShader = m_PreviewManager.RequestMainPreviewShaderCode();
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

            var shaderString = m_PreviewManager.RequestNodePreviewShaderCode(m_InterpreterTestsGraph.GetNode(input.nodeToCompile).ID.LocalPath, out var shaderMessages);
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
            FunctionDescriptor passThroughFD = new(1, "PassThrough",
                "Out = In.x;",
                new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out),
                new ParameterDescriptor("In", TYPE.Float, GraphType.Usage.In));

            var registry = new Registry();
            var graphHandler = new GraphHandler(registry);
            var previewMgr = new HeadlessPreviewManager();

            registry.Register<GraphType>();
            registry.Register<GraphTypeAssignment>();
            var passKey = registry.Register(passThroughFD);

            previewMgr.SetActiveGraph(graphHandler);
            previewMgr.SetActiveRegistry(registry);
            previewMgr.Initialize(testContextDescriptor, new Vector2(mainPreviewSize, mainPreviewSize));

            var passWriter = graphHandler.AddNode(passKey, "PassNodeInst");

            passWriter.SetPortField("In", "c0", 1f);
            previewMgr.SetLocalProperty("PassNodeInst", "In", 1f);

            previewMgr.RequestNodePreviewShaderCodeStrings("PassNodeInst", out _, out _, out var blockCode, out var funcCode);

            // this block code is generated by the interpreter and is welcome to be modified if expectations of the interpreter change.
            string expectedBlock = "void PassThrough(out float1 Out, float1 In)\n{\nOut = In.x;\n}\nShaderGraphBlock_PassNodeInstBlock::ShaderGraphBlock_PassNodeInstOutput SYNTAX_PassNodeInstMain(ShaderGraphBlock_PassNodeInstBlock::ShaderGraphBlock_PassNodeInstInput In)\n{\nfloat1 SYNTAX_PassNodeInst_Out;\nPassThrough(SYNTAX_PassNodeInst_Out, float1(1));\nShaderGraphBlock_PassNodeInstBlock::ShaderGraphBlock_PassNodeInstOutput output;\noutput.Out = SYNTAX_PassNodeInst_Out;\nreturn output;\n}\nShaderGraphBlock_PassNodeInst_REMAPBlock::ShaderGraphBlock_PassNodeInst_REMAPOutput SYNTAX_ShaderGraphBlock_PassNodeInst_REMAPMain(ShaderGraphBlock_PassNodeInst_REMAPBlock::ShaderGraphBlock_PassNodeInst_REMAPInput inputs)\n{\nShaderGraphBlock_PassNodeInst_REMAPBlock::ShaderGraphBlock_PassNodeInst_REMAPOutput output;\noutput.BaseColor = float3(inputs.Out, inputs.Out, inputs.Out);\nreturn output;\n}\n";
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

            FunctionDescriptor make = new(1, "Make",
                "Out.x = X; Out.y = Y;",
                new ParameterDescriptor("Out", TYPE.Vec2, GraphType.Usage.Out),
                new ParameterDescriptor("X", TYPE.Float, GraphType.Usage.In),
                new ParameterDescriptor("Y", TYPE.Float, GraphType.Usage.In));

            FunctionDescriptor append = new(1, "Append",
                "Out.xy = In; Out.z = Z;",
                new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out),
                new ParameterDescriptor("In", TYPE.Vec2, GraphType.Usage.In),
                new ParameterDescriptor("Z", TYPE.Float, GraphType.Usage.In));

            var registry = new Registry();
            var graphHandler = new GraphHandler(registry);
            var previewMgr = new HeadlessPreviewManager();

            registry.Register<GraphType>();
            registry.Register<GraphTypeAssignment>();
            var makeKey = registry.Register(make);
            var appendKey = registry.Register(append);
            var scalarKey = registry.Register(truncate);

            previewMgr.SetActiveGraph(graphHandler);
            previewMgr.SetActiveRegistry(registry);
            previewMgr.Initialize(testContextDescriptor, new Vector2(125, 125));

            var scalarWriter = graphHandler.AddNode(scalarKey, "TruncateNodeInstance");
            var makeWriter = graphHandler.AddNode(makeKey, "MakeNodeInstance");
            var appendWriter = graphHandler.AddNode(appendKey, "AppendNodeInstance");

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
            var registry = new Registry();
            var graphHandler = new GraphHandler(registry);
            var previewMgr = new HeadlessPreviewManager();

            registry.Register<GraphType>();
            registry.Register<GraphTypeAssignment>();
            registry.Register<GradientType>();
            registry.Register<GradientNode>();
            registry.Register<SampleGradientNode>();

            previewMgr.SetActiveGraph(graphHandler);
            previewMgr.SetActiveRegistry(registry);
            previewMgr.Initialize(testContextDescriptor, new Vector2(125, 125));

            var nodeWriter = graphHandler.AddNode<SampleGradientNode>("SampleGradientNode");
            previewMgr.NotifyNodeFlowChanged("SampleGradientNode");

            // Default 0 time color on a gradient is black.
            var nodePreviewMaterial = previewMgr.RequestNodePreviewMaterial("SampleGradientNode");
            Assert.AreEqual(new Color(0, 0, 0, 1), SampleMaterialColor(nodePreviewMaterial));

            // default 1 time color is white.
            //nodeWriter.SetPortField(SampleGradientNode.kTime, "c0", 1f);
            nodeWriter.GetPort(SampleGradientNode.kTime).GetTypeField().GetSubField<float>("c0").SetData(1f);
            previewMgr.SetLocalProperty("SampleGradientNode", SampleGradientNode.kTime, 1f);
            nodePreviewMaterial = previewMgr.RequestNodePreviewMaterial("SampleGradientNode");
            Assert.AreEqual(new Color(1, 1, 1, 1), SampleMaterialColor(nodePreviewMaterial));

            // our gradient comes from a connection now, let's pick a fun color (time is still 1).
            var gradientNode = graphHandler.AddNode<GradientNode>("GradientNode");
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

        [Test]
        public void Texture2D_MaterialPropertyGeneration()
        {
            var registry = new Registry();
            var graphHandler = new GraphHandler(registry);
            var previewMgr = new HeadlessPreviewManager();

            registry.Register<GraphType>();
            registry.Register<GraphTypeAssignment>();
            registry.Register<SamplerStateType>();
            registry.Register<SamplerStateAssignment>();
            registry.Register<BaseTextureType>();
            registry.Register<BaseTextureTypeAssignment>();
            registry.Register<SimpleTextureNode>();
            registry.Register<SimpleSampleTexture2DNode>();

            previewMgr.SetActiveGraph(graphHandler);
            previewMgr.SetActiveRegistry(registry);
            previewMgr.Initialize(testContextDescriptor, new Vector2(125, 125));

            var texNode = graphHandler.AddNode<SimpleTextureNode>("Tex");
            graphHandler.AddNode<SimpleSampleTexture2DNode>("SampleTex");
            Assert.IsTrue(graphHandler.TryConnect("Tex", "Output", "SampleTex", "Input", registry));
            graphHandler.ReconcretizeAll();
            previewMgr.NotifyNodeFlowChanged("SampleTex");

            var material = previewMgr.RequestNodePreviewMaterial("SampleTex");
            Assert.AreEqual(new Color(1, 1, 1, 1), SampleMaterialColor(material));

            // check that the material property is found on the shader.
            var propName = BaseTextureType.GetUniqueUniformName(texNode.GetPort(SimpleTextureNode.kInlineStatic).GetTypeField());
            int propindex = material.shader.FindPropertyIndex(propName);
            Assert.AreNotEqual(-1, propindex);

            // check that the material properties are working correctly
            material.SetTexture(propName, Texture2D.redTexture);
            Assert.AreEqual(new Color(1, 0, 0, 1), SampleMaterialColor(material));

            material.SetTexture(propName, Texture2D.blackTexture);
            Assert.AreEqual(new Color(0, 0, 0, 1), SampleMaterialColor(material));
        }

        [Test]
        public void Texture2D_DefaultPropertyUpdatesOnReconcretization()
        {
            var registry = new Registry();
            var graphHandler = new GraphHandler(registry);
            var previewMgr = new HeadlessPreviewManager();

            registry.Register<GraphType>();
            registry.Register<GraphTypeAssignment>();
            registry.Register<SamplerStateType>();
            registry.Register<SamplerStateAssignment>();
            registry.Register<BaseTextureType>();
            registry.Register<BaseTextureTypeAssignment>();
            registry.Register<SimpleTextureNode>();
            registry.Register<SimpleSampleTexture2DNode>();

            previewMgr.SetActiveGraph(graphHandler);
            previewMgr.SetActiveRegistry(registry);
            previewMgr.Initialize(testContextDescriptor, new Vector2(125, 125));

            var texNode = graphHandler.AddNode<SimpleTextureNode>("Tex");
            var texField = texNode.GetPort(SimpleTextureNode.kInlineStatic).GetTypeField();
            graphHandler.AddNode<SimpleSampleTexture2DNode>("SampleTex");
            Assert.IsTrue(graphHandler.TryConnect("Tex", "Output", "SampleTex", "Input", registry));

            //set to a red texture and test.
            BaseTextureType.SetTextureAsset(texField, Texture2D.redTexture);
            graphHandler.ReconcretizeAll();
            previewMgr.NotifyNodeFlowChanged("SampleTex");
            var material = previewMgr.RequestNodePreviewMaterial("SampleTex");
            Assert.AreEqual(new Color(1, 0, 0, 1), SampleMaterialColor(material));

            // set it to black and reconcretize, and test.
            BaseTextureType.SetTextureAsset(texField, Texture2D.blackTexture);
            graphHandler.ReconcretizeAll();
            previewMgr.NotifyNodeFlowChanged("SampleTex");
            material = previewMgr.RequestNodePreviewMaterial("SampleTex");
            Assert.AreEqual(new Color(0, 0, 0, 1), SampleMaterialColor(material));
        }


        [Test]
        public void SamplerStateType_GeneratesCorrectly()
        {
            var registry = new Registry();
            var graphHandler = new GraphHandler(registry);
            var previewMgr = new HeadlessPreviewManager();

            registry.Register<GraphType>();
            registry.Register<GraphTypeAssignment>();
            registry.Register<SamplerStateType>();
            registry.Register<SamplerStateAssignment>();
            registry.Register<SamplerStateExampleNode>();
            registry.Register<BaseTextureType>();
            registry.Register<BaseTextureTypeAssignment>();
            registry.Register<SimpleTextureNode>();
            registry.Register<SimpleSampleTexture2DNode>();

            previewMgr.SetActiveGraph(graphHandler);
            previewMgr.SetActiveRegistry(registry);
            previewMgr.Initialize(testContextDescriptor, new Vector2(125, 125));

            var texNode = graphHandler.AddNode<SimpleTextureNode>("Tex");
            var texField = texNode.GetPort(SimpleTextureNode.kInlineStatic).GetTypeField();
            graphHandler.AddNode<SimpleSampleTexture2DNode>("SampleTex");
            graphHandler.AddNode<SamplerStateExampleNode>("Sampler");
            Assert.IsTrue(graphHandler.TryConnect("Tex", "Output", "SampleTex", "Input", registry));
            Assert.IsTrue(graphHandler.TryConnect("Sampler", "Out", "SampleTex", SimpleSampleTexture2DNode.kSampler, registry));

            //set to a red texture and test.
            BaseTextureType.SetTextureAsset(texField, Texture2D.redTexture);
            graphHandler.ReconcretizeAll();
            previewMgr.NotifyNodeFlowChanged("SampleTex");
            var material = previewMgr.RequestNodePreviewMaterial("SampleTex");
            Assert.AreEqual(new Color(1, 0, 0, 1), SampleMaterialColor(material));

            // set it to black and reconcretize, and test.
            BaseTextureType.SetTextureAsset(texField, Texture2D.blackTexture);
            graphHandler.ReconcretizeAll();
            previewMgr.NotifyNodeFlowChanged("SampleTex");
            material = previewMgr.RequestNodePreviewMaterial("SampleTex");
            Assert.AreEqual(new Color(0, 0, 0, 1), SampleMaterialColor(material));
        }
    }
}
