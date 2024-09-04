using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    class MaterialGraphTests
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            Debug.unityLogger.logHandler = new ConsoleLogHandler();
        }

        [Test]
        public void TestCreateMaterialGraph()
        {
            var graph = new GraphData();

            Assert.IsNotNull(graph);

            Assert.AreEqual(0, graph.GetNodes<AbstractMaterialNode>().Count());
        }

        [Test]
        public void TestUndoRedoPerformedMethod()
        {
            var view = new MaterialGraphView();
            var viewType = typeof(MaterialGraphView);
            var fieldInfo = viewType.GetField("m_UndoRedoPerformedMethodInfo", BindingFlags.NonPublic | BindingFlags.Instance);
            var fieldInfoValue = fieldInfo.GetValue(view);

            Assert.IsNotNull(fieldInfoValue, "m_UndoRedoPerformedMethodInfo must not be null.");
        }
    }
}
