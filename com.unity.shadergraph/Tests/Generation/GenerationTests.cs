using System;
using System.IO;
using NUnit.Framework;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Generation.UnitTests
{
    [TestFixture]
    class GraphGenerationFixture
    {

        private static GraphHandler graph;
        private static Registry registry;

        [SetUp]
        public static void Setup()
        {
            registry = new Registry();
            // var contextKey = Registry.ResolveKey<Registry.Default.DefaultContext>();
            var propertyKey = Registry.ResolveKey<PropertyContext>();

            graph = new GraphHandler();

            registry.Register<GraphType>();
            registry.Register<TestAddNode>();
            registry.Register<GraphTypeAssignment>();

            graph.AddContextNode(propertyKey, registry);
            // graph.AddContextNode(contextKey, registry);

            graph.AddNode<TestAddNode>("Add1", registry).SetPortField("In1", "c0", 1f); //(1,0,0,0)
            graph.AddNode<TestAddNode>("Add2", registry).SetPortField("In2", "c1", 1f); //(0,1,0,0)
            graph.AddNode<TestAddNode>("Add3", registry);
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
            Texture2D output = new(4, 4, TextureFormat.ARGB32, false);
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
            var shaderString = Interpreter.GetShaderForNode(graph.GetNodeReader(input.nodeToCompile), graph, registry, out _);
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
            var propertyKey = Registry.ResolveKey<PropertyContext>();
            var propContext = graph.GetNode(propertyKey.Name);
            propContext.AddPort<GraphType>("Foo", true, registry);
            propContext.SetPortField("Foo", "c1", .5f);
            propContext.SetPortField("Foo", "c2", .5f);
            propContext.AddPort<GraphType>("out_Foo", false, registry);
            graph.AddReferenceNode("FooReference", propertyKey.Name, "Foo", registry);
            graph.AddEdge("FooReference.Output", "Add1.In2");

            var shaderString = Interpreter.GetShaderForNode(graph.GetNodeReader("Add1"), graph, registry, out _);
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
