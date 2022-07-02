using System;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph.GraphDelta;
using System.Linq;
using UnityEditor.ShaderGraph.HeadlessPreview.UnitTests;
using UnityEngine.TestTools;
using static UnityEditor.VersionControl.Message;

namespace UnityEditor.ShaderGraph.HeadlessPreview.NodeTests
{

    // TODO: Consider async compile/rendering, need to wait until thread is done and results are returned
    // https://forum.unity.com/threads/support-for-async-await-in-tests.787853/#post-6838493

    [TestFixture]
    class DefaultPreviewsTestFixture
    {
        static ShaderGraphRegistry InitSGR()
        {
            ShaderGraphRegistry SGR = new();
            SGR.InitializeDefaults();
            return SGR;
        }

        static string[] InitNodeNames()
        {
            return SGR.DefaultTopologies.GetNodes().Select(e => e.ID.LocalPath).ToArray();
        }

        // whether or not a node will require another node to coerce the value to a vector for testing preview compilation.
        static bool HasVectorOutput(NodeHandler node)
        {
            var nodeKey = node.GetRegistryKey();
            var typeField = node.GetPorts().Where(e => !e.IsInput)?.First()?.GetTypeField();
            bool hasPreviewableOutputPort = typeField?.GetRegistryKey().Name == GraphType.kRegistryKey.Name;
            return hasPreviewableOutputPort && GraphTypeHelpers.GetHeight(typeField) == GraphType.Height.One;
        }

        static ShaderGraphRegistry SGR = InitSGR();
        // Need to resolve the node names statically so that the Test Runner is happy and shows each node.
        static string[] nodeNames = InitNodeNames();

        [TestCaseSource("nodeNames")]
        public void DoesPreviewCompile(string nodeName)
        {
            Registry Registry = SGR.Registry;
            GraphHandler Graph = new(Registry);

            HeadlessPreviewManager Preview = new();
            Preview.SetActiveRegistry(Registry);
            Preview.SetActiveGraph(Graph);
            Preview.Initialize("ThisDontMatter", new UnityEngine.Vector2(125, 125));

            var nodeKey = SGR.DefaultTopologies.GetNode(nodeName).GetRegistryKey();
            var node = Graph.AddNode(nodeKey, nodeName);

            string previewName = nodeName;
            if (!HasVectorOutput(node))
            {
                var outPort = node.GetPorts().Where(e => !e.IsInput)?.First();
                var portName = outPort.LocalID;
                var typeName = outPort?.GetTypeField()?.GetRegistryKey().Name;
                previewName = "Helper";

                if (typeName == GraphType.kRegistryKey.Name)
                {
                    var keyMatDet = new RegistryKey { Name = "MatrixDeterminant", Version = 1 };
                    Graph.AddNode(keyMatDet, previewName);
                    Graph.TryConnect(nodeName, portName, previewName, "In");
                }
                if (typeName == BaseTextureType.kRegistryKey.Name)
                {
                    switch (BaseTextureType.GetTextureType(outPort.GetTypeField()))
                    {
                        case BaseTextureType.TextureType.Texture2D:
                            var key2d = new RegistryKey { Name = "SampleTexture2D", Version = 1 };
                            Graph.AddNode(key2d, previewName);
                            Graph.TryConnect(nodeName, portName, previewName, "Texture");
                            break;

                        case BaseTextureType.TextureType.Texture3D:
                            var key3d = new RegistryKey { Name = "SampleTexture3D", Version = 1 };
                            Graph.AddNode(key3d, previewName);
                            Graph.TryConnect(nodeName, portName, previewName, "Texture");
                            break;

                        case BaseTextureType.TextureType.Texture2DArray:
                            var keyArray = new RegistryKey { Name = "SampleTexture2DArray", Version = 1 };
                            Graph.AddNode(keyArray, previewName);
                            Graph.TryConnect(nodeName, portName, previewName, "TextureArray");
                            break;

                        case BaseTextureType.TextureType.CubeMap:
                            var keyCube = new RegistryKey { Name = "SampleReflectedCubemap", Version = 1 };
                            Graph.AddNode(keyCube, previewName);
                            Graph.TryConnect(nodeName, portName, previewName, "Cube");
                            break;
                    }
                }
                if (typeName == SamplerStateType.kRegistryKey.Name)
                {
                    var key2d = new RegistryKey { Name = "SampleTexture2D", Version = 1 };
                    Graph.AddNode(key2d, previewName);
                    Graph.TryConnect(nodeName, portName, previewName, "Sampler");
                }
                if (typeName == GradientType.kRegistryKey.Name)
                {
                    Graph.AddNode<SampleGradientNode>(previewName);
                    Graph.TryConnect(nodeName, outPort.LocalID, previewName, SampleGradientNode.kGradient);
                }
            }


            // The following would be the prefered output, as we could get a much more concise idea of what is wrong and how to fix it--
            // but the Shader Compiler floods the test results window before ShaderMessages can be accessed, meaning any additional outputs
            // would end up exceeding the 15000 character limit (and appear at the bottom). It's possible to get this to work correctly
            // by not routing through the PreviewManager, but the setup and behavior of nodes would then be inconsistent with where this
            // is relevant.

            //Preview.RequestNodePreviewShaderCodeStrings(previewName, out var shaderMessages, out _, out var prevCode, out _);
            //string dump = "";
            //foreach (var msg in shaderMessages)
            //    dump += msg + "\n";
            //dump += prevCode;
            //Assert.IsNotEmpty(shaderMessages, dump);

            var material = Preview.RequestNodePreviewMaterial(previewName);
            var value = PreviewTestFixture.SampleMaterialColor(material);
            Assert.AreNotEqual(new Color(1,1,0,1), value);
        }
    }
}
