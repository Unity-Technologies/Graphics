using System;
using System.IO;
using NUnit.Framework;
using UnityEditor.ShaderFoundry;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.Registry;
using UnityEngine;
using Types = UnityEditor.ShaderGraph.Registry.Types;

namespace UnityEditor.ShaderGraph.Generation.UnitTests
{
    [TestFixture]
    class GraphGenerationFixture
    {

        private static IGraphHandler graph;
        private static Registry.Registry registry;

        [SetUp]
        public static void Setup()
        {
            graph = GraphDelta.GraphUtil.CreateGraph();
            registry = new Registry.Registry();

            registry.Register<Types.GraphType>();
            registry.Register<Types.AddNode>();
            registry.Register<Types.GraphTypeAssignment>();

            graph.AddNode<Types.AddNode>("Add1", registry).SetPortField("In1", "c0", 1f); //(1,0,0,0)
            graph.AddNode<Types.AddNode>("Add2", registry).SetPortField("In2", "c1", 1f); //(0,1,0,0)
            graph.AddNode<Types.AddNode>("Add3", registry);
            graph.TryConnect("Add1", "Out", "Add3", "In1", registry);
            graph.TryConnect("Add2", "Out", "Add3", "In2", registry); //should be (1,1,0,0)
        }

        private static Shader MakeShader(string input)
        {
            bool tmp = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            Shader output = ShaderUtil.CreateShaderAsset(input, true);
            ShaderUtil.allowAsyncCompilation = tmp;
            return output;
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

        static object[] testAsIsSource = new object[]
        {
            ("Add1", new Color(1,0,0,0)), //Colors with Alpha 1 since target is opaque
            ("Add2", new Color(0,1,0,0)),
            ("Add3", new Color(1,1,0,0)),
        };

        [Test]
        [TestCaseSource("testAsIsSource")]
        public static void TestGraphAsIs((string nodeToCompile, Color expectedColor) input)
        {
            var shader = MakeShader(Interpreter.GetShaderForNode(graph.GetNodeReader(input.nodeToCompile), graph, registry));
            var rt = DrawToTex(shader);
            try
            {
                Assert.AreEqual(rt.GetPixel(0, 0), input.expectedColor);
            }
            catch(Exception e)
            {
                File.WriteAllBytes($"Assets/FailureImage{input.nodeToCompile}.jpg", rt.EncodeToJPG());
                throw e;
            }
        }

    }

}
