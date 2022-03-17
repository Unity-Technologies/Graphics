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
            var currentGraph = Window.GraphTool.ToolState.AssetModel;

            currentGraph.Dirty = false;
            currentGraph.Name = "popo";
            Window.UpdateWindowTitle();
            Assert.AreEqual("popo", Window.titleContent.text);

            currentGraph.Dirty = false;
            currentGraph.Name = "12345123451234512345"; // The maximum length of a window tab's title is 20 characters.
            Window.UpdateWindowTitle();
            Assert.AreEqual("12345123451234512345", Window.titleContent.text);

            currentGraph.Dirty = false;
            currentGraph.Name = "12345123451234512345" + "popo";
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
            var subgraphGraphAsset = GraphAssetCreationHelpers<TestGraphAssetModel>.CreateInMemoryGraphAsset(typeof(TestStencil), "popo");
            Window.GraphTool.Dispatch(new LoadGraphAssetCommand(subgraphGraphAsset, loadStrategy: LoadGraphAssetCommand.LoadStrategies.PushOnStack));
            Window.GraphTool.Update();

            var initialGraph = Window.GraphTool.ToolState.SubGraphStack[0].GetGraphAssetModel();
            var currentGraph = Window.GraphTool.ToolState.AssetModel;

            initialGraph.Dirty = false;
            currentGraph.Dirty = false;
            initialGraph.Name = "papa";
            Window.UpdateWindowTitle();
            Assert.AreEqual("(papa) popo", Window.titleContent.text);

            initialGraph.Dirty = false;
            currentGraph.Dirty = true;
            initialGraph.Name = "b";
            currentGraph.Name = "papapapapapapapapapapapapa";
            Window.UpdateWindowTitle();
            Assert.AreEqual("(b) papapapap...*", Window.titleContent.text);

            initialGraph.Dirty = false;
            currentGraph.Dirty = true;
            initialGraph.Name = "popopopopopopopopopopopopo";
            currentGraph.Name = "a";
            Window.UpdateWindowTitle();
            Assert.AreEqual("(popopopop...) a*", Window.titleContent.text);

            initialGraph.Dirty = true;
            currentGraph.Dirty = false;
            initialGraph.Name = "popopopopopopopopopopopopo";
            currentGraph.Name = "a";
            Window.UpdateWindowTitle();
            Assert.AreEqual("(popopopop...*) a", Window.titleContent.text);

            initialGraph.Dirty = false;
            currentGraph.Dirty = false;
            initialGraph.Name = "popopopopopopopopopopopopo";
            currentGraph.Name = "papapapapapapapapapapapapa";
            Window.UpdateWindowTitle();
            Assert.AreEqual("(popopo...) papap...", Window.titleContent.text);
        }
    }
}
