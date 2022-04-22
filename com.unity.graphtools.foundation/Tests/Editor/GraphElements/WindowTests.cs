using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class WindowTests : GraphViewTester
    {
        [Test]
        public void WindowTitleFormattingWorks()
        {
            var currentGraph = Window.GraphTool.ToolState.CurrentGraph.GetGraphAsset();

            currentGraph.Name = "popo";
            currentGraph.Dirty = false;
            Window.UpdateWindowTitle();
            Assert.AreEqual("popo", Window.titleContent.text);

            currentGraph.Name = "12345123451234512345"; // The maximum length of a window tab's title is 20 characters.
            currentGraph.Dirty = false;
            Window.UpdateWindowTitle();
            Assert.AreEqual("12345123451234512345", Window.titleContent.text);

            currentGraph.Name = "12345123451234512345" + "popo";
            currentGraph.Dirty = false;
            Window.UpdateWindowTitle();
            Assert.AreEqual("12345123451234512...", Window.titleContent.text);

            currentGraph.Dirty = true;
            Window.UpdateWindowTitle();
            Assert.AreEqual("1234512345123451...*", Window.titleContent.text);
        }

        [Test]
        public void WindowTitleFormattingWithSubgraphWorks()
        {
            // In the case the current graph is a subgraph, the window primary tab's naming should follow this format: (InitialAssetName...*) CurrentAssetName...*
            var subgraphGraphAsset = GraphAssetCreationHelpers<TestGraphAsset>.CreateInMemoryGraphAsset(typeof(TestStencil), "popo");
            Window.GraphTool.Dispatch(new LoadGraphCommand(subgraphGraphAsset.GraphModel, loadStrategy: LoadGraphCommand.LoadStrategies.PushOnStack));
            Window.GraphTool.Update();

            var initialGraph = Window.GraphTool.ToolState.SubGraphStack[0].GetGraphAsset();
            var currentGraph = Window.GraphTool.ToolState.CurrentGraph.GetGraphAsset();

            initialGraph.Name = "papa";
            initialGraph.Dirty = false;
            currentGraph.Dirty = false;
            Window.UpdateWindowTitle();
            Assert.AreEqual("(papa) popo", Window.titleContent.text);

            initialGraph.Name = "b";
            currentGraph.Name = "papapapapapapapapapapapapa";
            initialGraph.Dirty = false;
            currentGraph.Dirty = true;
            Window.UpdateWindowTitle();
            Assert.AreEqual("(b) papapapap...*", Window.titleContent.text);

            initialGraph.Name = "popopopopopopopopopopopopo";
            currentGraph.Name = "a";
            initialGraph.Dirty = false;
            currentGraph.Dirty = true;
            Window.UpdateWindowTitle();
            Assert.AreEqual("(popopopop...) a*", Window.titleContent.text);

            initialGraph.Name = "popopopopopopopopopopopopo";
            currentGraph.Name = "a";
            initialGraph.Dirty = true;
            currentGraph.Dirty = false;
            Window.UpdateWindowTitle();
            Assert.AreEqual("(popopopop...*) a", Window.titleContent.text);

            initialGraph.Name = "popopopopopopopopopopopopo";
            currentGraph.Name = "papapapapapapapapapapapapa";
            initialGraph.Dirty = false;
            currentGraph.Dirty = false;
            Window.UpdateWindowTitle();
            Assert.AreEqual("(popopo...) papap...", Window.titleContent.text);
        }
    }
}
