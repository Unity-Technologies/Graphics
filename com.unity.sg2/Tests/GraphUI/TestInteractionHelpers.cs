using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.GraphToolsFoundation.Editor;
using Unity.ItemLibrary.Editor;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    /// <summary>
    /// Meant to hold any higher level graph UI interactions that will be commonly used in tests
    /// </summary>
    class TestInteractionHelpers
    {
        TestEditorWindow m_Window;

        TestEventHelpers m_TestEventHelper;

        public TestInteractionHelpers(TestEditorWindow targetWindow, TestEventHelpers testEventHelper)
        {
            m_Window = targetWindow;
            m_TestEventHelper = testEventHelper;
        }

        public IEnumerator SelectAndCopyNodes(List<AbstractNodeModel> nodeModels)
        {
            // Select both the nodes
            m_Window.GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, nodeModels));
            yield return null;

            m_TestEventHelper.SimulateKeyPress("C", modifiers: EventModifiers.Control);
            yield return null;

            m_TestEventHelper.SimulateKeyPress("V", modifiers: EventModifiers.Control);
            yield return null;
        }

        public IEnumerator CreateNodesAndConnect(string fromNodeName = "Add", string toNodeName = "Preview", string fromPortName = "Out", string toPortName = "In")
        {
            yield return AddNodeFromSearcherAndValidate(fromNodeName);
            yield return AddNodeFromSearcherAndValidate(toNodeName);

            var fromNode = m_Window.GetNodeModelFromGraphByName(fromNodeName);
            var outPort = ShaderGraphModel.FindOutputPortByName(fromNode, fromPortName);
            Assert.IsNotNull(outPort, "Could not find output port: " + fromPortName + "on node: " + fromNodeName);

            var toNode = m_Window.GetNodeModelFromGraphByName(toNodeName);
            var inPort =  ShaderGraphModel.FindInputPortByName(toNode, toPortName);
            Assert.IsNotNull(inPort, "Could not find input port: " + toPortName + "on node: " + toNodeName);

            m_Window.GraphView.Dispatch(new CreateWireCommand(inPort, outPort));
        }

        public void ConnectNodes(string fromNodeName, string toNodeName, string fromPortName = "Out", string toPortName = "In")
        {
            var fromNode = m_Window.GetNodeModelFromGraphByName(fromNodeName);
            var fromPortModel = ShaderGraphModel.FindOutputPortByName(fromNode, fromPortName);

            var toNode = m_Window.GetNodeModelFromGraphByName(toNodeName);
            var toPortModel =  ShaderGraphModel.FindInputPortByName(toNode, toPortName);

            m_Window.GraphView.Dispatch(new CreateWireCommand(toPortModel, fromPortModel));
        }

        ItemLibraryWindow SummonSearcher()
        {
            m_Window.GraphView.ShowItemLibrary(new Vector2());

            // TODO: (Sai) This throws an exception on some occasions in DisplaySmartSearch, ask Vlad for help figuring out why?
            //TestEventHelpers.SendKeyDownEvent(m_MainWindow, KeyCode.Space);
            //TestEventHelpers.SendKeyUpEvent(m_MainWindow, KeyCode.Space);

            var searcherWindow = (ItemLibraryWindow)EditorWindow.GetWindow(typeof(ItemLibraryWindow));
            return searcherWindow;
        }

        // TODO (Sai): Move to TestInteractionHelpers
        public IEnumerator AddNodeFromSearcherAndValidate(string nodeName)
        {
            var searcherWindow = SummonSearcher();
            var searcherWindowTestHelper = new TestEventHelpers(searcherWindow);

            yield return null;

            searcherWindow.Focus();
            yield return null;
            yield return null;

            foreach (char c in nodeName)
            {
                searcherWindowTestHelper.SimulateKeyPress(c.ToString());
                yield return null;
            }

            // Sending two key-down events followed by a key-up for the Return as we normally do causes an exception
            // it seems like the searcher is waiting for that first Return event and closes immediately after,
            // any further key events sent cause a MissingReferenceException as the searcher window is now invalid
            searcherWindowTestHelper.SimulateKeyPress(KeyCode.Return, false, false);
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            Assert.IsNotNull(m_Window.GetNodeModelFromGraphByName(nodeName));
        }
    }
}
