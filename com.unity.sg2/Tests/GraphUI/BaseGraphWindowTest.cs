using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Searcher;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    public class BaseGraphWindowTest
    {
        protected static readonly Rect k_WindowRect = new Rect(Vector2.zero, new Vector2( /*SelectionDragger.panAreaWidth*/ 100 * 8, /*SelectionDragger.panAreaWidth*/ 100 * 6));

        protected TestEditorWindow m_Window;
        protected TestGraphView m_GraphView;

        // Used to send events to the highest shader graph editor window
        protected TestEventHelpers m_ShaderGraphWindowTestHelper;

        protected virtual string testAssetPath => $"Assets\\{ShaderGraphStencil.DefaultGraphAssetName}.{ShaderGraphStencil.GraphExtension}";
        protected virtual bool hideOverlayWindows => true;

        // Need to match the values specified by the BlackboardOverlay and ModelInspectorOverlay in GTFO
        protected const string k_BlackboardOverlayId = SGBlackboardOverlay.k_OverlayID;
        protected const string k_InspectorOverlayId = SGInspectorOverlay.k_OverlayID;

        [SetUp]
        public virtual void SetUp()
        {
            CreateWindow();

            m_GraphView = m_Window.GraphView as TestGraphView;

            var newGraphAction = ScriptableObject.CreateInstance<GraphAssetUtils.CreateGraphAssetAction>();
            newGraphAction.Action(0, testAssetPath, "");
            var graphAsset = AssetDatabase.LoadAssetAtPath<ShaderGraphAsset>(testAssetPath);
            m_Window.GraphTool.Dispatch(new LoadGraphCommand(graphAsset.GraphModel));
            m_Window.GraphTool.Update();

            if (hideOverlayWindows)
            {
                m_Window.TryGetOverlay(k_BlackboardOverlayId, out var blackboardOverlay);
                blackboardOverlay.displayed = false;

                m_Window.TryGetOverlay(k_InspectorOverlayId, out var inspectorOverlay);
                inspectorOverlay.displayed = false;
            }

            m_Window.Focus();
        }

        [TearDown]
        public virtual void TearDown()
        {
            CloseWindow();
            AssetDatabase.DeleteAsset(testAssetPath);
        }

        public void CreateWindow()
        {
            m_Window = EditorWindow.CreateWindow<TestEditorWindow>(typeof(SceneView), typeof(TestEditorWindow));
            m_Window.shouldCloseWindowNoPrompt = true;

            m_ShaderGraphWindowTestHelper = new TestEventHelpers(m_Window);
        }

        public void CloseWindow()
        {
            // See case: https://fogbugz.unity3d.com/f/cases/998343/
            // Clearing the capture needs to happen before closing the window
            MouseCaptureController.ReleaseMouse();
            if (m_Window != null)
            {
                m_Window.Close();
            }
        }

        /// <summary>
        /// Saves the open graph, closes the tool window, then reopens the graph.
        /// m_Window is reassigned after calling this method.
        /// </summary>
        public IEnumerator SaveAndReopenGraph()
        {
            GraphAssetUtils.SaveOpenGraphAsset(m_Window.GraphTool);
            CloseWindow();
            yield return null;

            var graphAsset = ShaderGraphAssetUtils.HandleLoad(testAssetPath);
            CreateWindow();
            m_Window.Show();
            m_Window.Focus();
            m_Window.SetCurrentSelection(graphAsset, GraphViewEditorWindow.OpenMode.OpenAndFocus);

            // Wait till the graph model is loaded back up
            while (m_Window.GraphView.GraphModel == null)
                yield return null;
        }

        public SearcherWindow SummonSearcher()
        {
            m_GraphView.DisplaySmartSearch(new Vector2());

            // TODO: (Sai) This throws an exception on some occasions in DisplaySmartSearch, ask Vlad for help figuring out why?
            //TestEventHelpers.SendKeyDownEvent(m_Window, KeyCode.Space);
            //TestEventHelpers.SendKeyUpEvent(m_Window, KeyCode.Space);

            var searcherWindow = (SearcherWindow)EditorWindow.GetWindow(typeof(SearcherWindow));
            return searcherWindow;
        }

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

            Assert.IsTrue(FindNodeOnGraphByName(nodeName));
        }

        public bool FindNodeOnGraphByName(string nodeName)
        {
            var nodeModels = m_Window.GraphView.GraphModel.NodeModels;
            foreach (var nodeModel in nodeModels)
            {
                if (nodeModel is NodeModel concreteNodeModel && concreteNodeModel.Title == nodeName && !concreteNodeModel.Destroyed)
                    return true;
            }

            return false;
        }

        public INodeModel GetNodeModelFromGraphByName(string nodeName)
        {
            var nodeModels = m_Window.GraphView.GraphModel.NodeModels;
            foreach (var nodeModel in nodeModels)
            {
                if (nodeModel is NodeModel concreteNodeModel && concreteNodeModel.Title == nodeName)
                {
                    return concreteNodeModel;
                }
            }

            return null;
        }

        public IEdgeModel GetEdgeModelFromGraphByName(string sourceNodeName, string destinationNodeName)
        {
            var edgeModels = m_Window.GraphView.GraphModel.EdgeModels;
            foreach (var edgeModel in edgeModels)
            {
                var fromPortNodeModel = (NodeModel)edgeModel.FromPort.NodeModel;
                var toPortNodeModel = (NodeModel)edgeModel.ToPort.NodeModel;

                if (fromPortNodeModel.DisplayTitle == sourceNodeName
                    && toPortNodeModel.DisplayTitle == destinationNodeName)
                {
                    return edgeModel;
                }
            }

            return null;
        }
    }
}
