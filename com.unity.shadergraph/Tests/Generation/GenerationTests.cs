using System;
using System.IO;
using NUnit.Framework;
using UnityEditor.ShaderFoundry;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.Registry.Types;
using UnityEngine;
using Types = UnityEditor.ShaderGraph.Registry.Types;

namespace UnityEditor.ShaderGraph.Generation.UnitTests
{
    [TestFixture]
    class GraphGenerationFixture
    {

        private static GraphHandler graph;
        private static Registry.Registry registry;

        [SetUp]
        public static void Setup()
        {
            registry = Registry.Default.DefaultRegistry.CreateDefaultRegistry();
            var contextKey = Registry.Registry.ResolveKey<Registry.Default.DefaultContext>();
            var propertyKey = Registry.Registry.ResolveKey<Registry.Default.PropertyContext>();

            graph = new GraphHandler();
            graph.AddContextNode(propertyKey, registry);
            graph.AddContextNode(contextKey, registry);

            registry.Register<Types.AddNode>();

            graph.AddNode<Types.AddNode>("Add1", registry).SetPortField("A", "c0", 1f); //(1,0,0,0)
            graph.AddNode<Types.AddNode>("Add2", registry).SetPortField("B", "c1", 1f); //(0,1,0,0)
            graph.AddNode<Types.AddNode>("Add3", registry);
            graph.TryConnect("Add1", "Out", "Add3", "A", registry);
            graph.TryConnect("Add2", "Out", "Add3", "B", registry); //should be (1,1,0,0)
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
            ("Add1", new Color(1,0,0,1)), //Colors with Alpha 1 since target is opaque
            ("Add2", new Color(0,1,0,1)),
            ("Add3", new Color(1,1,0,1)),
        };

        [Test]
        [TestCaseSource("testAsIsSource")]
        public static void TestGraphAsIs((string nodeToCompile, Color expectedColor) input)
        {
            var shaderString = Interpreter.GetShaderForNode(graph.GetNodeReader(input.nodeToCompile), graph, registry);
            var shader = MakeShader(shaderString);
            var rt = DrawToTex(shader);
            try
            {
                var pixelColor = rt.GetPixel(0,0);
                Assert.AreEqual(pixelColor, input.expectedColor);
            }
            catch(Exception e)
            {
                File.WriteAllBytes($"Assets/FailureImage{input.nodeToCompile}.jpg", rt.EncodeToJPG());
                throw e;
            }
        }

        [Test]
        public static void TestGraphReferenceNode()
        {
            var propertyKey = Registry.Registry.ResolveKey<Registry.Default.PropertyContext>();
            var propContext = graph.GetNode(propertyKey.Name);
            propContext.AddPort<GraphType>("Foo", true, registry);
            propContext.SetPortField("Foo", "c1", .5f);
            propContext.SetPortField("Foo", "c2", .5f);
            propContext.AddPort<GraphType>("out_Foo", false, registry);
            graph.AddReferenceNode("FooReference", propertyKey.Name, "Foo", registry);
            graph.AddEdge("FooReference.Output", "Add1.B");

            var shaderString = Interpreter.GetShaderForNode(graph.GetNodeReader("Add1"), graph, registry);
            var shader = MakeShader(shaderString);
            var rt = DrawToTex(shader);
            try
            {
                var pixelColor = rt.GetPixel(0, 0);
                Assert.IsTrue((pixelColor - new Color(1f,.5f,.5f)).maxColorComponent < 0.01f); //getting some weird color drift (0.5 -> 0.498) hmm
            }
            catch (Exception e)
            {
                File.WriteAllBytes($"Assets/FailureImageReferenceNode.jpg", rt.EncodeToJPG());
                throw e;
            }

        }

    }

}
