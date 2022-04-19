using System;
using System.Collections;
using NUnit.Framework;
using System.Linq;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    [TestFixture]
    public class GraphNodeTests
    {
        protected static readonly Rect k_WindowRect = new Rect(Vector2.zero, new Vector2(SelectionDragger.panAreaWidth * 8, SelectionDragger.panAreaWidth * 6));

        ShaderGraphEditorWindow m_Window;
        ShaderGraphView m_GraphView;

        // Used to send events to the highest shader graph editor window
        TestEventHelpers m_ShaderGraphWindowTestHelper;

        string m_TestAssetPath = $"Assets\\{ShaderGraphStencil.DefaultAssetName}.{ShaderGraphStencil.Extension}";

        protected virtual void CreateWindow()
        {
            m_Window = EditorWindow.CreateWindow<ShaderGraphEditorWindow>(typeof(SceneView), typeof(ShaderGraphEditorWindow));
            m_Window.shouldCloseWindowNoPrompt = true;

            m_ShaderGraphWindowTestHelper = new TestEventHelpers(m_Window);
        }

        [SetUp]
        public void SetUp()
        {
            CreateWindow();

            m_GraphView = m_Window.GraphView as ShaderGraphView;

            var newGraphAction = ScriptableObject.CreateInstance<GraphAssetUtils.CreateAssetAction>();
            newGraphAction.Action(0, m_TestAssetPath, "");
            var graphAsset = AssetDatabase.LoadAssetAtPath<ShaderGraphAssetModel>(m_TestAssetPath);
            m_Window.GraphTool.Dispatch(new LoadGraphAssetCommand(graphAsset));
            m_Window.GraphTool.Update();

            m_Window.Focus();
        }

        [TearDown]
        public void TearDown()
        {
            CloseWindow();
            AssetDatabase.DeleteAsset(m_TestAssetPath);
        }

        SearcherWindow SummonSearcher()
        {
            m_GraphView.DisplaySmartSearch(new Vector2());

            // TODO: (Sai) This throws an exception on some occasions in DisplaySmartSearch, ask Vlad for help figuring out why?
            //TestEventHelpers.SendKeyDownEvent(m_Window, KeyCode.Space);
            //TestEventHelpers.SendKeyUpEvent(m_Window, KeyCode.Space);

            var searcherWindow = (SearcherWindow)EditorWindow.GetWindow(typeof(SearcherWindow));
            return searcherWindow;
        }

        [UnityTest]
        public IEnumerator CreateAddNodeFromSearcherTest()
        {
            return AddNodeFromSearcherAndValidate("Add");
        }

        [UnityTest]
        public IEnumerator TestSaveLoadEdges()
        {
            const string FromNodeName = "Add", FromPortName = "Out";
            const string ToNodeName = "Preview", ToPortName = "In";

            // Set up the graph
            {
                yield return AddNodeFromSearcherAndValidate(FromNodeName);
                yield return AddNodeFromSearcherAndValidate(ToNodeName);

                var nodeModels = m_GraphView.GraphModel.NodeModels;
                var addNode = (GraphDataNodeModel)nodeModels.First(n => n is GraphDataNodeModel {Title: FromNodeName});
                var addOut = addNode.GetOutputPorts().First(p => p.UniqueName == FromPortName);

                var previewNode = (GraphDataNodeModel)nodeModels.First(n => n is GraphDataNodeModel {Title: ToNodeName});
                var previewIn = previewNode.GetInputPorts().First(p => p.UniqueName == ToPortName);

                m_GraphView.Dispatch(new CreateEdgeCommand(previewIn, addOut));
            }

            // Save and reload
            {
                GraphAssetUtils.SaveImplementation(m_Window.GraphTool);
                CloseWindow();
                yield return null;

                var graphAsset = ShaderGraphAsset.HandleLoad(m_TestAssetPath);
                CreateWindow();
                m_Window.Show();
                m_Window.Focus();
                m_Window.SetCurrentSelection(graphAsset, GraphViewEditorWindow.OpenMode.OpenAndFocus);
                yield return null;
            }

            // Verify that edge is preserved
            {
                var edge = m_GraphView.GraphModel.EdgeModels.FirstOrDefault();
                Assert.IsNotNull(edge, "Edge should exist in loaded graph");

                Assert.IsTrue(edge.FromPort is
                {
                    UniqueName: FromPortName,
                    NodeModel: GraphDataNodeModel {Title: FromNodeName}
                }, $"Edge should begin at port {FromPortName} on node {FromNodeName}");

                Assert.IsTrue(edge.ToPort is
                {
                    UniqueName: ToPortName,
                    NodeModel: GraphDataNodeModel {Title: ToNodeName}
                }, $"Edge should end at port {ToPortName} on node {ToNodeName}");
            }
        }

        /*
        /* This test needs the ability to distinguish between nodes and non-node graph elements like the Sticky Note
        /* When we have categories for the searcher items we can distinguish between them
        [UnityTest]
        public IEnumerator CreateAllNodesFromSearcherTest()
        {
            if (m_Window.GraphView.GraphModel is ShaderGraphModel shaderGraphModel)
            {
                var shaderGraphStencil = shaderGraphModel.Stencil as ShaderGraphStencil;
                var searcherDatabaseProvider = new ShaderGraphSearcherDatabaseProvider(shaderGraphStencil);
                var searcherDatabases = searcherDatabaseProvider.GetGraphElementsSearcherDatabases(shaderGraphModel);
                foreach (var database in searcherDatabases)
                {
                    foreach (var searcherItem in database.Search(""))
                    {
                        return AddNodeFromSearcherAndValidate(searcherItem.Name);
                    }
                }
            }

            return null;
        }
        */

        IEnumerator AddNodeFromSearcherAndValidate(string nodeName)
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

            Assert.IsTrue(FindNodeOnGraphByName(nodeName));
        }

        bool FindNodeOnGraphByName(string nodeName)
        {
            var nodeModels = m_Window.GraphView.GraphModel.NodeModels;
            foreach (var nodeModel in nodeModels)
            {
                if (nodeModel is NodeModel concreteNodeModel && concreteNodeModel.Title == nodeName)
                    return true;
            }

            return false;
        }

        void CloseWindow()
        {
            // See case: https://fogbugz.unity3d.com/f/cases/998343/
            // Clearing the capture needs to happen before closing the window
            MouseCaptureController.ReleaseMouse();
            if (m_Window != null)
            {
                m_Window.Close();
            }
        }
    }
}
