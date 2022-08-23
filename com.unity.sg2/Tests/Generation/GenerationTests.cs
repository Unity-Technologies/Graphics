using System;
using System.IO;
using NUnit.Framework;
using UnityEditor.ShaderGraph.Configuration;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.GraphUI;
using UnityEditor.ShaderGraph.Defs;
using UnityEngine;
using static UnityEditor.ShaderGraph.GraphDelta.IContextDescriptor;
using System.Collections.Generic;
using UnityEditor.ContextLayeredDataStorage;
using System.Linq;
using static UnityEditor.ShaderGraph.GraphDelta.GraphType;

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
            registry.Register<GraphType>();
            registry.Register<TestAddNode>();
            registry.Register<GraphTypeAssignment>();
            registry.Register<PropertyContext>();
            registry.Register<Defs.ShaderGraphContext>();
            var contextKey = Registry.ResolveKey<Defs.ShaderGraphContext>();
            var propertyKey = Registry.ResolveKey<PropertyContext>();
            graph = new GraphHandler(registry);

            graph.AddContextNode("VertIn");
            graph.AddContextNode("VertOut");
            graph.AddContextNode(propertyKey.Name);
            graph.AddContextNode(contextKey);

            graph.RebuildContextData("VertIn", GetTarget(), "UniversalPipeline", "VertexDescription", true);
            graph.RebuildContextData("VertOut", GetTarget(), "UniversalPipeline", "VertexDescription", false);
            graph.RebuildContextData(propertyKey.Name, GetTarget(), "UniversalPipeline", "SurfaceDescription", true);
            //graph.RebuildContextData(contextKey.Name, GetTarget(),  "UniversalPipeline", "SurfaceDescription", false);

            graph.graphDelta.AddDefaultConnection("ObjectSpacePosition", "VertOut.Position", registry);
            graph.graphDelta.AddDefaultConnection("ObjectSpaceNormal",   "VertOut.Normal", registry);
            graph.graphDelta.AddDefaultConnection("ObjectSpaceTangent",  "VertOut.Tangent", registry);
            //CPGraphDataProvider.GatherProviderCPIO(GetTarget(), out var descriptors);
            //foreach (var descriptor in descriptors)
            //    LogDescriptor(descriptor);

            graph.AddNode<TestAddNode>("Add1").SetPortField("In1", "c0", 1f); //(1,0,0,0)
            graph.AddNode<TestAddNode>("Add2").SetPortField("In2", "c1", 1f); //(0,1,0,0)
            graph.AddNode<TestAddNode>("Add3");
            graph.TryConnect("Add1", "Out", "Add3", "In1");
            graph.TryConnect("Add2", "Out", "Add3", "In2"); //should be (1,1,0,0)
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
            return DrawToTex(new Material(shader));
        }

        private static Texture2D DrawToTex(Material material, int width = 4, int height = 4)
        {
            var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            Graphics.Blit(null, rt, material);
            Texture2D output = new (width, height, TextureFormat.ARGB32, false);
            output.ReadPixels(new Rect(0, 0, width, height), 0, 0);
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
            graph.AddReferenceNode("FooReference", propertyKey.Name, "Foo");
            graph.AddEdge("FooReference.Output", "Add1.In2");
            graph.RebuildContextData(propertyKey.Name, GetTarget(), "UniversalPipeline", "SurfaceDescription", true);
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

        [Test]
        public static void TestMaterialPropertyGeneration()
        {
            var propertyKey = Registry.ResolveKey<PropertyContext>();
            var propContext = graph.GetNode(propertyKey.Name);
            var entry = new ContextEntry
            {
                fieldName = "Foo",
                height = GraphType.Height.One,
                length = GraphType.Length.Four,
                initialValue = Matrix4x4.zero,
                precision = GraphType.Precision.Fixed,
                primitive = GraphType.Primitive.Float
            };

            var contextKey = Registry.ResolveKey<Defs.ShaderGraphContext>();
            ContextBuilder.AddReferableEntry(propContext, TYPE.Float, "Foo", registry, ContextEntryEnumTags.PropertyBlockUsage.Included, displayName: "Foo_Var");

            var portMatch = propContext.GetPorts().Where(p => string.CompareOrdinal(p.ID.LocalPath, "Foo") == 0);
            Assert.IsTrue(portMatch.Any());

            graph.AddReferenceNode("Foo_Ref", propertyKey.Name, entry.fieldName);
            graph.AddEdge("Foo_Ref.Output", contextKey.Name + ".BaseColor");

            portMatch = propContext.GetPorts().Where(p => string.CompareOrdinal(p.ID.LocalPath, "Foo") == 0);
            Assert.IsTrue(portMatch.Any());
            graph.RebuildContextData(propertyKey.Name, GetTarget(), "UniversalPipeline", "SurfaceDescription", true);

            portMatch = propContext.GetPorts().Where(p => string.CompareOrdinal(p.ID.LocalPath, "Foo") == 0);
            Assert.IsTrue(portMatch.Any());
            Assert.IsNotNull(propContext.GetPort("Foo"));

            var shaderString = Interpreter.GetShaderForNode(graph.GetNode(contextKey.Name), graph, registry, out _);
            var shader = MakeShader(shaderString);
            var material = new Material(shader);
            material.SetVector("Foo_Var", new Vector4(1, 0, 0, 1));
            var rt = DrawToTex(material);

            try
            {
                var pixelColor = rt.GetPixel(0, 0);
                Assert.IsTrue((pixelColor - new Color(1f, 0f, 0f)).maxColorComponent < 0.01f); //getting some weird color drift (0.5 -> 0.498) hmm
            }
            catch (Exception e)
            {
                File.WriteAllBytes($"Assets/FailureImageMaterialPropertyGen1.jpg", rt.EncodeToJPG());
                File.WriteAllText("Assets/FailureMaterialPropertyGen1.shader", shaderString);
                throw e;
            }

            material.SetVector("Foo_Var", new Vector4(1, 1, 0, 1));
            rt = DrawToTex(material);

            try
            {
                var pixelColor = rt.GetPixel(0, 0);
                Assert.IsTrue((pixelColor - new Color(1f, 1f, 0f)).maxColorComponent < 0.01f); //getting some weird color drift (0.5 -> 0.498) hmm
            }
            catch (Exception e)
            {
                File.WriteAllBytes($"Assets/FailureImageMaterialPropertyGen2.jpg", rt.EncodeToJPG());
                throw e;
            }


        }

        // Cheat and do a hard-coded lookup of the UniversalTarget for testing.
        // Shader Graph should build targets however it wants to.
        static internal Target GetTarget()
        {
            var targetTypes = TypeCache.GetTypesDerivedFrom<Target>();
            foreach (var type in targetTypes)
            {
                if (type.IsAbstract || type.IsGenericType || !type.IsClass || type.Name != "UniversalTarget")
                    continue;

                var target = (Target)Activator.CreateInstance(type);
                if (!target.isHidden)
                    return target;
            }
            return null;
        }

        public static void LogDescriptor(CPGraphDataProvider.TemplateDataDescriptor desc)
        {
            Debug.Log(desc.templateName);
            foreach (var cpio in desc.CPIO)
            {
                Debug.Log("\t" + cpio.customizationPointName);
                Debug.Log("\t\tInputs");
                foreach (var input in cpio.inputs)
                {
                    Debug.Log("\t\t\t" + input.name);
                }
                Debug.Log("\t\tOutputs");
                foreach (var output in cpio.outputs)
                {
                    Debug.Log("\t\t\t" + output.name);
                }
            }
        }


        [Test]
        public void TestUV()
        {
            var propertyKey = Registry.ResolveKey<PropertyContext>();
            var propContext = graph.GetNode(propertyKey.Name);
            var contextKey = Registry.ResolveKey<ShaderGraphContext>();
            graph.AddReferenceNode("UV_Ref", propertyKey.Name, "uv0");
            graph.AddEdge("UV_Ref.Output", contextKey.Name + ".BaseColor");

            var shaderString = Interpreter.GetShaderForNode(graph.GetNode(contextKey.Name), graph, registry, out _);
            var shader = MakeShader(shaderString);
            var material = new Material(shader);
            var rt = DrawToTex(material, 128, 128);
            try
            {
                var pixelColor = rt.GetPixel(127, 0);
                Assert.IsTrue((new Color(1f, 0f, 0f) - pixelColor).maxColorComponent < 0.01f);
                pixelColor = rt.GetPixel(127, 127);
                Assert.IsTrue((new Color(1f, 1f, 0f) - pixelColor).maxColorComponent < 0.01f);
                pixelColor = rt.GetPixel(0, 0);
                Assert.IsTrue(pixelColor.maxColorComponent < 0.01f);
                pixelColor = rt.GetPixel(0, 127);
                Assert.IsTrue((new Color(0f, 1f, 0f) - pixelColor).maxColorComponent < 0.01f);
            }
            catch (Exception e)
            {
                File.WriteAllBytes($"Assets/FailureBadUV.jpg", rt.EncodeToJPG());
                File.WriteAllText("Assets/FailureBadUV.shader", shaderString);
                throw e;
            }
        }

        [Test]
        public void TestVertex()
        {
            var contextKey = Registry.ResolveKey<Defs.ShaderGraphContext>();
            var dup = graph.DuplicateNode(graph.GetNode("Add1"), true, "Add1_Copy");
            graph.AddNode<TestAddNode>("Add4").SetPortField("In1", "c0", 1f); //(1,0,0,0)
            graph.AddEdge("Add4.Out", "VertOut.Position");
            graph.AddReferenceNode("Pos_Ref", "VertIn", "ObjectSpacePosition");
            graph.AddEdge("Pos_Ref.Output", "Add4.In2");
            var shaderString = Interpreter.GetShaderForNode(graph.GetNode(contextKey.Name), graph, registry, out _);
            var shader = MakeShader(shaderString);

            File.WriteAllText("Assets/TestVertex.shader", shaderString);

        }

        [Test]
        public void DuplicateTest()
        {
            graph.DuplicateNode(graph.GetNode("Add1"), true, "Add1_Copy");
            var dup = graph.GetNode("Add1_Copy");
            var dupVal = dup.GetPort("In1").GetTypeField().GetSubField<float>("c0");
            Assert.NotNull(dupVal);
            Assert.AreEqual(1f, dupVal.GetData());
            var shaderString = Interpreter.GetShaderForNode(graph.GetNode("Add1_Copy"), graph, registry, out _);
            var shader = MakeShader(shaderString);
            var rt = DrawToTex(shader);
            try
            {
                var pixelColor = rt.GetPixel(0, 0);
                Assert.AreEqual(new Color(1,0,0,1),pixelColor);
            }
            catch (Exception e)
            {
                File.WriteAllBytes($"Assets/FailureBadDuplicate.jpg", rt.EncodeToJPG());
                throw e;
            }
        }

        [Test]
        public void DuplicateMultiTest()
        {
            graph.DuplicateNodes(new List<(NodeHandler node, ElementID duplicateID)>
            {
                (graph.GetNode("Add1"), "Add1_Copy"),
                (graph.GetNode("Add2"), "Add2_Copy"),
                (graph.GetNode("Add3"), "Add3_Copy")
            }, true);
            var dup = graph.GetNode("Add1_Copy");
            var dupVal = dup.GetPort("In1").GetTypeField().GetSubField<float>("c0");
            Assert.NotNull(dupVal);
            Assert.AreEqual(1f, dupVal.GetData());
            var connected = graph.GetConnectedNodes(dup.ID);
            Assert.AreEqual(1, connected.Count());
            Assert.IsTrue(connected.First().ID.Equals("Add3_Copy"));
            dup = graph.GetNode("Add2_Copy");
            dupVal = dup.GetPort("In2").GetTypeField().GetSubField<float>("c1");
            Assert.NotNull(dupVal);
            Assert.AreEqual(1f, dupVal.GetData());
            connected = graph.GetConnectedNodes(dup.ID);
            Assert.AreEqual(1, connected.Count());
            Assert.IsTrue(connected.First().ID.Equals("Add3_Copy"));

            var shaderString = Interpreter.GetShaderForNode(graph.GetNode("Add3_Copy"), graph, registry, out _);
            var shader = MakeShader(shaderString);
            var rt = DrawToTex(shader);
            try
            {
                var pixelColor = rt.GetPixel(0, 0);
                Assert.AreEqual(new Color(1, 1, 0, 1), pixelColor);
            }
            catch (Exception e)
            {
                File.WriteAllBytes($"Assets/FailureBadMultiDuplicate.jpg", rt.EncodeToJPG());
                throw e;
            }
        }

        [Test]
        public void CopyTest()
        {
            var copy = graph.Copy(new List<NodeHandler>()
            {
                graph.GetNode("Add1"),
                graph.GetNode("Add2"),
                graph.GetNode("Add3")
            });

            graph.Paste(copy.layerData, copy.metaData, copy.edgeData);
            var dup = graph.GetNode("Add1_1");
            var dupVal = dup.GetPort("In1").GetTypeField().GetSubField<float>("c0");
            Assert.NotNull(dupVal);
            Assert.AreEqual(1f, dupVal.GetData());
            var connected = graph.GetConnectedNodes(dup.ID);
            Assert.AreEqual(1, connected.Count());
            Assert.IsTrue(connected.First().ID.Equals("Add3_1"));
            dup = graph.GetNode("Add2_1");
            dupVal = dup.GetPort("In2").GetTypeField().GetSubField<float>("c1");
            Assert.NotNull(dupVal);
            Assert.AreEqual(1f, dupVal.GetData());
            connected = graph.GetConnectedNodes(dup.ID);
            Assert.AreEqual(1, connected.Count());
            Assert.IsTrue(connected.First().ID.Equals("Add3_1"));

            var shaderString = Interpreter.GetShaderForNode(graph.GetNode("Add3_1"), graph, registry, out _);
            var shader = MakeShader(shaderString);
            var rt = DrawToTex(shader);
            try
            {
                var pixelColor = rt.GetPixel(0, 0);
                Assert.AreEqual(new Color(1, 1, 0, 1), pixelColor);
            }
            catch (Exception e)
            {
                File.WriteAllBytes($"Assets/FailureBadCopyPaste.jpg", rt.EncodeToJPG());
                throw e;
            }

        }


        [Test]
        public void TestAllCasts()
        {
            registry.Register<TestOutputNode>();
            registry.Register<TestInputNode>();
            List<Length> allCombos = new List<Length>();
            foreach(var l in Enum.GetValues(typeof(Length)))
            {
                if ((int)l > 0)
                {
                    allCombos.Add((Length)l);
                }
            }

            foreach(var c in allCombos)
            {
                var outNodeString = $"Test_Out_{c.ToString()}";
                GraphTypeHelpers.InitGraphType(graph.AddNode<TestOutputNode>(outNodeString).AddPort<GraphType>("Out", false, registry).GetTypeField(), length: c);
                foreach(var c2 in allCombos)
                {
                    var inNodeString = $"Test_In_{c2.ToString()}_{c.ToString()}";
                    var inNode = graph.AddNode<TestInputNode>(inNodeString);
                    GraphTypeHelpers.InitGraphType(inNode.AddPort<GraphType>("In", true, registry).GetTypeField(), length: c2);
                    GraphTypeHelpers.InitGraphType(inNode.AddPort<GraphType>("Out", false, registry).GetTypeField(), length: c2);
                    if(graph.TestConnection(outNodeString, "Out", inNodeString, "In", registry))
                    {
                        graph.AddEdge($"{outNodeString}.Out", $"{inNodeString}.In");
                        try
                        {
                            var shaderString = Interpreter.GetShaderForNode(graph.GetNode(inNodeString), graph, registry, out _);
                            var shader = MakeShader(shaderString);
                            var rt = DrawToTex(shader);
                        }
                        catch(Exception e)
                        {
                            Debug.LogError($"Failed to convert from {c.ToString()} to {c2.ToString()} despite passing TestConnection");
                            Debug.LogError(e.StackTrace);
                            throw e;
                        }
                    }
                }
            }
        }

    }

}
