using System.Collections.Generic;
using UnityEditor.ContextLayeredDataStorage;
using System.Linq;
using static UnityEditor.ShaderGraph.GraphDelta.GraphType;
using UnityEditor.ShaderGraph.GraphDelta;
using NUnit.Framework;
using UnityEditor.ShaderGraph.Defs;
using System.Text;
using static UnityEditor.ShaderGraph.Generation.Interpreter;
using UnityEngine;
using System;

namespace UnityEditor.ShaderGraph.Generation.Tests
{

    [TestFixture]
    class InterpreterTestFixture
    {
        private GraphHandler graphHandler;


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


        [SetUp]
        public void Setup()
        {
            var reg = new ShaderGraphRegistry();
            reg.InitializeDefaults();
            graphHandler = new GraphHandler(reg.Registry);
        }

        static object[] testRegistryKeys = new object[]
        {
            new RegistryKey{Name = "Add", Version = 1},
            new RegistryKey{Name = "Multiply", Version = 1},
            new RegistryKey{Name = "Lerp", Version = 1},
            new RegistryKey{Name = "Blend", Version = 1}
        };


        [Test]
        [TestCaseSource("testRegistryKeys")]
        public void TestGetShaderFunctionInHumanReadableForm(RegistryKey key)
        {
            StringBuilder sb = new StringBuilder();
            NodeHandler node = graphHandler.AddNode(key, "TEST");
            InterpreterTestStub.GetShaderFunctionInHumanReadableForm(ref sb, node, graphHandler.registry, key);
            Debug.Log(sb.ToString());
        }

        [Test]
        [TestCaseSource("testRegistryKeys")]
        public void TestGetShaderBlockForNode(RegistryKey key)
        {
            NodeHandler node = graphHandler.AddNode(key, "TEST");
            var output = InterpreterTestStub.GetShaderBlockInHumanReadableForm(node, graphHandler.registry);
            Debug.Log(output);
        }

        [Test]
        public void TestMultipleNodeBlock()
        {
            var key = new RegistryKey { Name = "Add", Version = 1 };
            NodeHandler nodeA = graphHandler.AddNode(key, "Add1");
            NodeHandler nodeB = graphHandler.AddNode(key, "Add2");
            graphHandler.AddEdge(nodeA.GetPort("Out").ID, nodeB.GetPort("A").ID);
            var output = InterpreterTestStub.GetShaderBlockInHumanReadableForm(nodeB, graphHandler.registry);
            Debug.Log(output);
        }

        [Test]
        public void TestReferableNode()
        {
            graphHandler.AddContextNode("FragIn");
            graphHandler.RebuildContextData("FragIn", GetTarget(), "UniversalPipeline", "SurfaceDescription", true);
            var node = graphHandler.AddReferenceNode("UV_Ref", "FragIn", "uv0");
            var output = InterpreterTestStub.GetShaderBlockInHumanReadableForm(node, graphHandler.registry);
            Debug.Log(output);
        }
    }


}
