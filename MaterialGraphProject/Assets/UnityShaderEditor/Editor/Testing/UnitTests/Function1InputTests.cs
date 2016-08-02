using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.UnitTests
{
    [TestFixture]
    public class Function1InputTests
    {
        private class Function1InputTestNode : Function1Input, IGeneratesFunction
        {
            public Function1InputTestNode()
            {
                name = "Function1InputTestNode";
            }

            protected override string GetFunctionName()
            {
                return "unity_test_" + precision;
            }

            public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
            {
                var outputString = new ShaderGenerator();
                outputString.AddShaderChunk(GetFunctionPrototype("arg"), false);
                outputString.AddShaderChunk("{", false);
                outputString.Indent();
                outputString.AddShaderChunk("return arg;", false);
                outputString.Deindent();
                outputString.AddShaderChunk("}", false);

                visitor.AddShaderChunk(outputString.GetShaderString(0), true);
            }
        }

        private PixelGraph m_Graph;
        private Vector1Node m_InputOne;
        private Function1InputTestNode m_TestNode;

        [TestFixtureSetUp]
        public void RunBeforeAnyTests()
        {
            Debug.logger.logHandler = new ConsoleLogHandler();
        }

        [SetUp]
        public void TestSetUp()
        {
            m_Graph = new PixelGraph();
            m_InputOne = new Vector1Node();
            m_TestNode = new Function1InputTestNode();

            m_Graph.AddNode(m_InputOne);
            m_Graph.AddNode(m_TestNode);
            m_Graph.AddNode(new PixelShaderNode());

            m_InputOne.value = 0.2f;

            m_Graph.Connect(m_InputOne.GetSlotReference(Vector1Node.OutputSlotId), m_TestNode.GetSlotReference(Function1Input.InputSlotId));
            m_Graph.Connect(m_TestNode.GetSlotReference(Function1Input.OutputSlotId), m_Graph.pixelMasterNode.GetSlotReference(BaseLightFunction.NormalSlotId));
        }

        [Test]
        public void TestGenerateNodeCodeGeneratesCorrectCode()
        {
            string expected = string.Format("half {0} = unity_test_half ({1});"
                    , m_TestNode.GetVariableNameForSlot(Function1Input.OutputSlotId)
                    , m_InputOne.GetVariableNameForSlot(Vector1Node.OutputSlotId)
                    );

            ShaderGenerator visitor = new ShaderGenerator();
            m_TestNode.GenerateNodeCode(visitor, GenerationMode.SurfaceShader);
            Assert.AreEqual(expected, visitor.GetShaderString(0).Trim());
        }

        [Test]
        public void TestGenerateNodeFunctionGeneratesCorrectCode()
        {
            string expected =
                "inline half unity_test_half (half arg)\r\n"
                + "{\r\n"
                + "\treturn arg;\r\n"
                + "}";

            ShaderGenerator visitor = new ShaderGenerator();
            m_TestNode.GenerateNodeFunction(visitor, GenerationMode.SurfaceShader);
            Assert.AreEqual(expected, visitor.GetShaderString(0).Trim());
        }
    }
}
