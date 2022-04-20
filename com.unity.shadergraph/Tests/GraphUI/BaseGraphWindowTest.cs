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
        protected static readonly Rect k_WindowRect = new Rect(Vector2.zero, new Vector2(SelectionDragger.panAreaWidth * 8, SelectionDragger.panAreaWidth * 6));

        protected ShaderGraphEditorWindow m_Window;
        protected ShaderGraphView m_GraphView;

        // Used to send events to the highest shader graph editor window
        protected TestEventHelpers m_ShaderGraphWindowTestHelper;

        protected string m_TestAssetPath = $"Assets\\{ShaderGraphStencil.DefaultAssetName}.{ShaderGraphStencil.GraphExtension}";

        [SetUp]
        public void SetUp()
        {
            CreateWindow();

            m_GraphView = m_Window.GraphView as ShaderGraphView;

            var newGraphAction = ScriptableObject.CreateInstance<GraphAssetUtils.CreateGraphAssetAction>();
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

        public void CreateWindow()
        {
            m_Window = EditorWindow.CreateWindow<ShaderGraphEditorWindow>(typeof(SceneView), typeof(ShaderGraphEditorWindow));
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
                if (nodeModel is NodeModel concreteNodeModel && concreteNodeModel.Title == nodeName)
                    return true;
            }

            return false;
        }
    }
}
