using System;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph.GraphDelta;
using System.Linq;
using UnityEditor.ShaderGraph.HeadlessPreview.UnitTests;
using UnityEngine.TestTools;

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
            // Matrices don't convert propertly in preview for now, so we want to filter out matrices
            return SGR.DefaultTopologies.GetNodes().Where(GetIsNotMatrixOutput).Select(e => e.ID.LocalPath).ToArray();
        }

        static bool GetIsNotMatrixOutput(NodeHandler node)
        {
            return true;

            var typeField = node.GetPorts().Where(e => !e.IsInput)?.First()?.GetTypeField();
            return !(typeField.GetRegistryKey().Name == GraphType.kRegistryKey.Name && (int)GraphTypeHelpers.GetHeight(typeField) > 1);
        }

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

                // This case is skipped currently because Matrix outputs are excluded.
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
                            var keyCube = new RegistryKey { Name = "Cube", Version = 1 };
                            Graph.AddNode(keyCube, previewName);
                            Graph.TryConnect(nodeName, portName, previewName, "TextureArray");
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


            //Preview.RequestNodePreviewShaderCodeStrings(previewName, out var shaderMessages, out _, out var prevCode, out _);
            //prevCode += "\n\n" + shaderMessages.FirstOrDefault().message;

            //Debug.LogAssertion(prevCode);

            var material = Preview.RequestNodePreviewMaterial(previewName);

            var value = PreviewTestFixture.SampleMaterialColor(material);
            Assert.AreNotEqual(new Color(1,1,0,1), value);
        }
    }
}
