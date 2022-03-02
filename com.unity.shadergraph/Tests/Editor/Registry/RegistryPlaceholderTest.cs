using NUnit.Framework;
using com.unity.shadergraph.defs;
using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;
using UnityEngine.TestTools.Utils;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Registry.UnitTests
{
    [TestFixture]
    class RegistryPlaceholderFixture
    {
        [Test]
        public void RegistryPlaceholderTest()
        {
            var graph = new GraphHandler();
            var registry = new Registry();

            registry.Register<Types.GraphType>();
            registry.Register<Types.AddNode>();
            registry.Register<Types.GraphTypeAssignment>();

            // should default concretize length to 4.
            graph.AddNode<Types.AddNode>("Add1", registry);
            var reader = graph.GetNodeReader("Add1");
            reader.GetField("In1.Length", out Length len);
            Assert.AreEqual(4, (int)len);

            // Set the length of input port 1 to 1.
            var nodeWriter = graph.GetNodeWriter("Add1");
            nodeWriter.SetPortField("In1", "Length", Length.One);

            // After reconcretization, the node definition should propagate the length.
            graph.ReconcretizeNode("Add1", registry);
            reader = graph.GetNodeReader("Add1");
            reader.GetField("In1.Length", out len);
            Assert.AreEqual(1, (int)len);
            reader.GetField("In2.Length", out len);
            Assert.AreEqual(1, (int)len);
            reader.GetField("Out.Length", out len);
            Assert.AreEqual(1, (int)len);

            // Add a second Add Node, with length 2 this time.
            var node2 = graph.AddNode<Types.AddNode>("Add2", registry);
            node2.SetPortField("In2", "Length", Length.Two);
            graph.ReconcretizeNode("Add2", registry);
            reader = graph.GetNodeReader("Add2");
            reader.GetField("In1.Length", out len);
            Assert.AreEqual(2, (int)len);
            reader.GetField("In2.Length", out len);
            Assert.AreEqual(2, (int)len);
            reader.GetField("Out.Length", out len);
            Assert.AreEqual(2, (int)len);

            // Connecting Out to In should clobber the inlined length with the new length.
            graph.TryConnect("Add2", "Out", "Add1", "In1", registry);
            graph.ReconcretizeNode("Add1", registry);
            reader = graph.GetNodeReader("Add1");
            reader.TryGetPort("In1", out var portReader);
            portReader.GetField("Length", out len);
            Assert.AreEqual(2, (int)len);
            reader.GetField("In2.Length", out len);
            Assert.AreEqual(2, (int)len);
            reader.GetField("Out.Length", out len);
            Assert.AreEqual(2, (int)len);
        }

        [Test]
        public void RegisterFunctionDescriptorTest()
        {
            // create the registry
            var registry = new Registry();
            registry.Register<Types.GraphType>();

            // create the graph
            var graph = new GraphHandler();
            FunctionDescriptor fd = new(
                1,
                "Test",
                "Out = In;",
                new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
            );
            RegistryKey registryKey = registry.Register(fd);

            // add a single node to the graph
            string nodeName = $"{fd.Name}-01";
            graph.AddNode(registryKey, nodeName, registry);

            // check that the node was added
            var nodeReader = graph.GetNodeReader(nodeName);
            bool didRead = nodeReader.GetField("In.Length", out Length len);
            Assert.IsTrue(didRead);

            // EXPECT that both In and Out are concretized into length = 4 (default)
            Assert.AreEqual(Length.Four, len);
            didRead = nodeReader.GetField("Out.Length", out len);
            Assert.IsTrue(didRead);
            Assert.AreEqual(Length.Four, len);
        }

        [Test]
        public void CanDefineNodeWithDefaultParameters()
        {
            // create registry
            var registry = new Registry();
            // register the GraphType (other types are based on it)
            registry.Register<Types.GraphType>();
            // create a graph
            var graph = new GraphHandler();

            // define a function with an in field that has defaults
            FunctionDescriptor fd = new(
                1,
                "Test",
                "Out = In;",
                new ParameterDescriptor(
                    "In",
                    TYPE.Vector,
                    Usage.In,
                    new float[] { 1F, 1F, 3F, 1F }
                ),
                new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
            );
            RegistryKey registryKey = registry.Register(fd);

            // add an instance of the node to the graph
            string nodeName = "{fd.Name}-test-1";
            graph.AddNode(registryKey, nodeName, registry);

            // check that the node was added
            var nodeReader = graph.GetNodeReader(nodeName);
            bool didRead = nodeReader.GetField("In.Length", out Length len);
            Assert.IsTrue(didRead);

            // check that the value for the port made from the in param is correct
            var comparer = new FloatEqualityComparer(10e-6f);
            nodeReader.GetField("In.c0", out float v);
            Assert.That(v, Is.EqualTo(1F).Using(comparer));
            nodeReader.GetField("In.c1", out v);
            Assert.That(v, Is.EqualTo(1F).Using(comparer));
            nodeReader.GetField("In.c2", out v);
            Assert.That(v, Is.EqualTo(3F).Using(comparer));
            nodeReader.GetField("In.c3", out v);
            Assert.That(v, Is.EqualTo(1F).Using(comparer));
        }

        [Test]
        public void GradientTypeTest()
        {
            var registry = new Registry();
            registry.Register<Types.GraphType>();
            registry.Register<Types.GraphTypeAssignment>();
            registry.Register<Types.GradientType>();
            registry.Register<Types.GradientNode>();


            // check that the type initializes defaults correctly.
            var graph = new GraphHandler();
            graph.AddNode<Types.GradientNode>("TestGradientNode", registry);
            var node = graph.GetNodeReader("TestGradientNode");
            node.TryGetPort(Types.GradientNode.kInlineStatic, out var port);
            var actual = Types.GradientTypeHelpers.GetGradient((IFieldReader)port);

            var expected = new Gradient();
            expected.mode = GradientMode.Blend;
            expected.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0,0,0), 0),
                    new GradientColorKey(new Color(1,1,1), 1)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1, 0),
                    new GradientAlphaKey(1, 1)
                });

            Assert.AreEqual(expected, actual);

            // check to see that a basic round trip works.
            var nodeWriter = graph.GetNodeWriter("TestGradientNode");
            var field = (IFieldWriter)nodeWriter.GetPort(Types.GradientNode.kInlineStatic);

            expected.mode = GradientMode.Fixed;
            expected.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0,0,0), 0),
                    new GradientColorKey(new Color(0,1,0), 1)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1, 0),
                    new GradientAlphaKey(0, 1)
                });
            Types.GradientTypeHelpers.SetGradient(field, expected);


            node = graph.GetNodeReader("TestGradientNode");
            node.TryGetPort(Types.GradientNode.kInlineStatic, out port);
            actual = Types.GradientTypeHelpers.GetGradient((IFieldReader)port);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Texture2DTest()
        {
            var registry = new Registry();
            registry.Register<Types.GraphType>();
            registry.Register<Types.GraphTypeAssignment>();

            //registry.Register<Types.SamplerStateType>();
            //registry.Register<Types.SamplerStateAssignment>();
            //registry.Register<Types.SamplerStateNode>();

            registry.Register<Types.Texture2DType>();
            registry.Register<Types.Texture2DAssignment>();
            registry.Register<Types.Texture2DNode>();
            registry.Register<Types.SampleTexture2DNode>();


            var graph = new GraphHandler();

            // check that the type initializes defaults correctly.
            //graph.AddNode<Types.SamplerStateNode>("Sampler", registry);
            //var nodeReader = graph.GetNodeReader("Sampler");

            //// Test that defaults are sane
            //nodeReader.TryGetPort(Types.SamplerStateNode.kInlineStatic, out var portReader);
            //var fieldReader = (IFieldReader)portReader;
            //var filter = Types.SamplerStateHelper.GetFilter(fieldReader);
            //var wrap = Types.SamplerStateHelper.GetWrap(fieldReader);
            //Assert.AreEqual(Types.SamplerStateType.Filter.Linear, filter);
            //Assert.AreEqual(Types.SamplerStateType.Wrap.Repeat, wrap);

            //var nodeWriter = graph.GetNodeWriter("Sampler");
            //var fieldWriter = (IFieldWriter)nodeWriter.GetPort(Types.SamplerStateNode.kInlineStatic);
            //Types.SamplerStateHelper.SetFilter(fieldWriter, Types.SamplerStateType.Filter.Point);
            //Types.SamplerStateHelper.SetWrap(fieldWriter, Types.SamplerStateType.Wrap.Mirror);

            //// Test that our values round tripped
            //nodeReader.TryGetPort(Types.SamplerStateNode.kInlineStatic, out portReader);
            //fieldReader = (IFieldReader)portReader;
            //filter = Types.SamplerStateHelper.GetFilter(fieldReader);
            //wrap = Types.SamplerStateHelper.GetWrap(fieldReader);
            //Assert.AreEqual(Types.SamplerStateType.Filter.Point, filter);
            //Assert.AreEqual(Types.SamplerStateType.Wrap.Mirror, wrap);




            graph.AddNode<Types.Texture2DNode>("Texture2D", registry);
            var nodeReader = graph.GetNodeReader("Texture2D");
            nodeReader.TryGetPort(Types.Texture2DNode.kInlineStatic, out var portReader);
            var fieldReader = (IFieldReader)portReader;

            var actualName = Types.Texture2DHelpers.GetUniquePropertyName(fieldReader);
            var actualTex = Types.Texture2DHelpers.GetTextureAsset(fieldReader);
            Assert.AreEqual("Texture2D_InlineStatic_Tex", actualName);
            Assert.IsNull(actualTex);

            var expectedTex = new Internal.SerializableTexture();
            expectedTex.texture = new Texture2D(800, 600);

            var nodeWriter = graph.GetNodeWriter("Texture2D");
            var fieldWriter = (IFieldWriter)nodeWriter.GetPort(Types.Texture2DNode.kInlineStatic);
            Types.Texture2DHelpers.SetTextureAsset(fieldWriter, expectedTex);

            nodeReader = graph.GetNodeReader("Texture2D");
            nodeReader.TryGetPort(Types.Texture2DNode.kInlineStatic, out portReader);
            fieldReader = (IFieldReader)portReader;
            actualTex = Types.Texture2DHelpers.GetTextureAsset(fieldReader);

            Assert.AreEqual(800, actualTex.texture.width);
            Assert.AreEqual(600, actualTex.texture.height);
        }
    }
}
