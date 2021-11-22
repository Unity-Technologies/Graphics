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
            graph.TryConnect("Add2", "Out", "Add3", "In2", registry);
        }

        [Test]
        public static void MakeShader()
        {
            StreamWriter writer = new StreamWriter("Assets/Test.shader", false);
            writer.Write(Interpreter.GetShaderForNode(graph.GetNodeReader("Add3"), graph, registry));
            writer.Close();
            AssetDatabase.ImportAsset("Assets/Test.shader");
        }

    }

}
