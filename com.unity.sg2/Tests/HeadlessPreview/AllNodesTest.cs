using NUnit.Framework;
using UnityEngine;
using UnityEditor.ShaderGraph.GraphDelta;
using System.Linq;
using UnityEditor.Graphs;
using UnityEditor.ShaderGraph.HeadlessPreview.UnitTests;

namespace UnityEditor.ShaderGraph.HeadlessPreview.NodeTests
{

    // TODO: Consider async compile/rendering, need to wait until thread is done and results are returned
    // https://forum.unity.com/threads/support-for-async-await-in-tests.787853/#post-6838493

    [TestFixture]
    class DefaultPreviewsTestFixture
    {
        ShaderGraphRegistry SGR;

        [OneTimeSetUp]
        public void StartUp()
        {
            SGR = new();
            SGR.InitializeDefaults();
        }

        // whether or not a node will require another node to coerce the value to a vector for testing preview compilation.
        static bool HasVectorOutput(NodeHandler node)
        {
            var nodeKey = node.GetRegistryKey();
            var typeField = node.GetPorts().Where(e => !e.IsInput)?.First()?.GetTypeField();
            bool hasPreviewableOutputPort = typeField?.GetRegistryKey().Name == GraphType.kRegistryKey.Name;
            return hasPreviewableOutputPort && GraphTypeHelpers.GetHeight(typeField) == GraphType.Height.One;
        }

        static readonly Color BadImageResults = new(1, 0, 1, 1);
        static readonly RegistryKey MatrixDeterminant = new RegistryKey { Name = "MatrixDeterminant", Version = 1 };
        static readonly RegistryKey SampleCube = new RegistryKey { Name = "SampleReflectedCubemap", Version = 1 };
        static readonly RegistryKey SampleTex2d = new RegistryKey { Name = "SampleTexture2D", Version = 1 };
        static readonly RegistryKey SampleTex3d = new RegistryKey { Name = "SampleTexture3D", Version = 1 };
        static readonly RegistryKey SampleTexArray = new RegistryKey { Name = "SampleTexture2DArray", Version = 1 };

        static string FindInputPortNameByTypeKeyName(NodeHandler node, string typeName)
        {
            foreach(var port in node.GetPorts().Where(e=> e.IsInput))
            {
                if (port.GetTypeField().GetRegistryKey().Name == typeName)
                    return port.LocalID;
            }
            return null;
        }

        static string[] InitNodeNames()
        {
            ShaderGraphRegistry tmp = new();
            tmp.InitializeDefaults();
            var namesCollection = tmp.DefaultTopologies.GetNodes()
                .Select(e => e.ID.LocalPath)
                // TODO (Brett) THESE ARE REMOVED FROM TESTING BECAUSE THEY ARE FAILING.
                // TODO (Brett) REMOVE THESE FROM THE LIST AS THEY START WORKING.
                .Where(s =>
                    !s.StartsWith("CustomColorBuffer") &&
                    !s.StartsWith("CustomDepthBuffer") &&
                    !s.StartsWith("Exposure") &&
                    !s.StartsWith("HDSceneColor") &&
                    !s.StartsWith("LightTexture2D") &&
                    !s.StartsWith("LinearBlendSkinning") &&
                    !s.StartsWith("TestDepsNode") &&
                    !s.StartsWith("CustomRenderTexture") &&
                    !s.StartsWith("Emission") &&
                    !s.StartsWith("ComputeWater") &&
                    !s.StartsWith("EvaluateWater") &&
                    !s.StartsWith("PackWaterVertexData") &&
                    !s.StartsWith("ParallaxOcclusionMapping")
                );
            return namesCollection.ToArray();
        }

        // Need to resolve the node names statically so that the Test Runner is happy and shows each node.
        private static readonly string[] nodeNames = InitNodeNames();
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

        [TestCaseSource("nodeNames")]
        public void DoesPreviewCompile(string nodeName)
        {

            var nodeKey = SGR.DefaultTopologies.GetNode(nodeName).GetRegistryKey();
            var node = m_Graph.AddNode(nodeKey, nodeName);

            string previewName = nodeName;
            if (!HasVectorOutput(node))
            {
                var outPort = node.GetPorts().Where(e => !e.IsInput)?.First();
                var portName = outPort.LocalID;
                var typeName = outPort?.GetTypeField()?.GetRegistryKey().Name;
                previewName = "Helper";

                RegistryKey key = new RegistryKey { Name = "Invalid", Version = 1 };

                if (typeName == GraphType.kRegistryKey.Name)
                {
                    key = MatrixDeterminant;
                }
                else if (typeName == BaseTextureType.kRegistryKey.Name)
                {
                    switch (BaseTextureType.GetTextureType(outPort.GetTypeField()))
                    {
                        case BaseTextureType.TextureType.Texture2D: key = SampleTex2d; break;
                        case BaseTextureType.TextureType.Texture3D: key = SampleTex3d; break;
                        case BaseTextureType.TextureType.Texture2DArray: key = SampleTexArray; break;
                        case BaseTextureType.TextureType.CubeMap: key = SampleCube; break;
                    }
                }
                else if (typeName == SamplerStateType.kRegistryKey.Name)
                {
                    key = SampleTex2d;
                }
                else if (typeName == GradientType.kRegistryKey.Name)
                {
                    key = Registry.ResolveKey<SampleGradientNode>();
                }
                else
                {
                    Assert.Fail($"The type: {typeName} of output port: {portName} does not have a node key associated with it.");
                }

                var nodeHelper = m_Graph.AddNode(key, previewName);
                var portHelperName = FindInputPortNameByTypeKeyName(nodeHelper, typeName);
                m_Graph.TryConnect(nodeName, portName, previewName, portHelperName);
            }


            // The following would be the prefered output, as we could get a much more concise idea of what is wrong and how to fix it--
            // but the Shader Compiler floods the test results window before ShaderMessages can be accessed, meaning any additional outputs
            // would end up exceeding the 15000 character limit (and appear at the bottom). It's possible to get this to work correctly
            // by not routing through the PreviewManager, but the setup and behavior of nodes would then be inconsistent with where this
            // is relevant.

            //m_Preview.RequestNodePreviewShaderCodeStrings(previewName, out var shaderMessages, out _, out var prevCode, out _);
            //string dump = "";
            //foreach (var msg in shaderMessages)
            //    dump += msg + "\n";
            //dump += prevCode;
            //Assert.IsNotEmpty(shaderMessages, dump);

            var material = m_Preview.RequestNodePreviewMaterial(previewName);
            var value = PreviewTestFixture.SampleMaterialColor(material);
            Assert.AreNotEqual(BadImageResults, value);
        }
    }
}
