using System;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    class PixelShaderNodeTests
    {
        /*  private UnityEngine.MaterialGraph.MaterialGraph m_Graph;
          private Vector1Node m_InputOne;
          private AbsoluteNode m_Abs;
          private MetallicMasterNode m_PixelNode;

          [TestFixtureSetUp]
          public void RunBeforeAnyTests()
          {
              Debug.unityLogger.logHandler = new ConsoleLogHandler();
          }

          [SetUp]
          public void TestSetUp()
          {
              m_Graph = new UnityEngine.MaterialGraph.MaterialGraph();
              m_PixelNode = new MetallicMasterNode();
              m_InputOne = new Vector1Node();
              m_Abs = new AbsoluteNode();

              m_Graph.AddNode(m_PixelNode);
              m_Graph.AddNode(m_InputOne);
              m_Graph.AddNode(m_PixelNode);
              m_Graph.AddNode(m_Abs);

              m_InputOne.value = 0.2f;

              m_Graph.Connect(m_InputOne.GetSlotReference(Vector1Node.OutputSlotId), m_PixelNode.GetSlotReference(AbstractSurfaceMasterNode.NormalSlotId));

              // m_Graph.Connect(m_InputOne.GetSlotReference(Vector1Node.OutputSlotId), m_Abs.GetSlotReference(Function1Input.InputSlotId));
              //m_Graph.Connect(m_Abs.GetSlotReference(Function1Input.OutputSlotId), m_PixelNode.GetSlotReference(AbstractSurfaceMasterNode.AlbedoSlotId));
          }

           [Test]
            public void TestNodeGeneratesCorrectNodeCode()
            {
                string expected = string.Format("half {0} = 0.2;" + Environment.NewLine
                        + "half {1} = abs ({0});" + Environment.NewLine
                        + "o.Albedo = {1};" + Environment.NewLine
                        + "o.Normal = {0};" + Environment.NewLine
                        , m_InputOne.GetVariableNameForSlot(Vector1Node.OutputSlotId)
                        , m_Abs.GetVariableNameForSlot(Function1Input.OutputSlotId));

                var generator = new ShaderGenerator();
                m_PixelNode.GenerateNodeCode(generator, GenerationMode.ForReals);

                Console.WriteLine(generator.GetShaderString(0));

                Assert.AreEqual(expected, generator.GetShaderString(0));
                Assert.AreEqual(string.Empty, generator.GetPragmaString());
            }*/
    }
}
