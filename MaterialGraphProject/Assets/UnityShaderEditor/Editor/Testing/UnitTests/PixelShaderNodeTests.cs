using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.UnitTests
{
    [TestFixture]
    public class PixelShaderNodeTests
    {
        private PixelGraph m_Graph;
        private Vector1Node m_InputOne;
        private AbsoluteNode m_Abs;
        private PixelShaderNode m_PixelNode;

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
            m_Abs = new AbsoluteNode();
            m_PixelNode = new PixelShaderNode();
            m_PixelNode.lightFunction = new PBRMetalicLightFunction();

            m_Graph.AddNode(m_InputOne);
            m_Graph.AddNode(m_PixelNode);
            m_Graph.AddNode(m_Abs);

            m_InputOne.value = 0.2f;

            m_Graph.Connect(m_InputOne.GetSlotReference(Vector1Node.OutputSlotId), m_PixelNode.GetSlotReference(BaseLightFunction.NormalSlotId));


            m_Graph.Connect(m_InputOne.GetSlotReference(Vector1Node.OutputSlotId), m_Abs.GetSlotReference(Function1Input.InputSlotId));
            m_Graph.Connect(m_Abs.GetSlotReference(Function1Input.OutputSlotId), m_PixelNode.GetSlotReference(PBRMetalicLightFunction.AlbedoSlotId));
        }

        [Test]
        public void TestNodeGeneratesLightFuntionProperly()
        {
            var generator = new ShaderGenerator();
            m_PixelNode.GenerateLightFunction(generator);

            Assert.AreEqual(string.Empty, generator.GetShaderString(0));
            Assert.AreEqual(PBRMetalicLightFunction.LightFunctionName, generator.GetPragmaString());
        }

        [Test]
        public void TestNodeGenerateSurfaceOutputProperly()
        {
            var generator = new ShaderGenerator();
            m_PixelNode.GenerateSurfaceOutput(generator);

            Assert.AreEqual(string.Empty, generator.GetShaderString(0));
            Assert.AreEqual(PBRMetalicLightFunction.SurfaceOutputStructureName, generator.GetPragmaString());
        }

        [Test]
        public void TestNodeGeneratesCorrectNodeCode()
        {
            string expected = string.Format("half {0} = 0.2;" + Environment.NewLine
                    + "o.Normal = {0};" + Environment.NewLine
                    + "half {1} = abs ({0});" + Environment.NewLine
                    + "o.Albedo = {1};" + Environment.NewLine
                    , m_InputOne.GetVariableNameForSlot(Vector1Node.OutputSlotId)
                    , m_Abs.GetVariableNameForSlot(Function1Input.OutputSlotId));

            var generator = new ShaderGenerator();
            m_PixelNode.GenerateNodeCode(generator, GenerationMode.SurfaceShader);

            Console.WriteLine(generator.GetShaderString(0));

            Assert.AreEqual(expected, generator.GetShaderString(0));
            Assert.AreEqual(string.Empty, generator.GetPragmaString());
        }

        [Test]
        public void TestPixelShaderNodeReturnsBuiltinPBRLights()
        {
            var lightingFuncs = PixelShaderNode.GetLightFunctions();
            Assert.AreEqual(1, lightingFuncs.OfType<PBRMetalicLightFunction>().Count());
            Assert.AreEqual(1, lightingFuncs.OfType<PBRSpecularLightFunction>().Count());
        }
    }
}
