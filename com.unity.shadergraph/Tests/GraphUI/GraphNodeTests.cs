using System;
using System.Collections;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphUI;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    [TestFixture]
    public class GraphNodeTests
    {
        protected static readonly Rect k_WindowRect = new Rect(Vector2.zero, new Vector2(SelectionDragger.panAreaWidth * 8, SelectionDragger.panAreaWidth * 6));

        ShaderGraphEditorWindow m_Window;
        ShaderGraphView m_GraphView;

        string m_TestAssetPath =  $"Assets\\{ShaderGraphStencil.DefaultAssetName}.{ShaderGraphStencil.Extension}";

        protected virtual void CreateWindow()
        {
            m_Window = EditorWindow.CreateWindow<ShaderGraphEditorWindow>(typeof(SceneView), typeof(ShaderGraphEditorWindow));
            m_Window.shouldCloseWindowNoPrompt = true;
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

            var searcherWindow = (SearcherWindow) EditorWindow.GetWindow(typeof(SearcherWindow));
            return searcherWindow;
        }

        void SimulateKeyInput(EditorWindow targetWindow, KeyCode inputKey, bool sendTwice = true, bool sendKeyUp = true)
        {
            TestEventHelpers.SendKeyDownEvent(targetWindow, inputKey, EventModifiers.None, sendTwice);
            if(sendKeyUp)
                TestEventHelpers.SendKeyUpEvent(targetWindow);
        }

        [UnityTest]
        public IEnumerator CreateNodeFromSearcherTest()
        {
            var searcherWindow = SummonSearcher();
            yield return null;

            searcherWindow.Focus();
            yield return null;
            yield return null;

            SimulateKeyInput(searcherWindow, KeyCode.A);
            yield return null;

            SimulateKeyInput(searcherWindow, KeyCode.D);
            yield return null;

            SimulateKeyInput(searcherWindow, KeyCode.D);
            yield return null;

            // Sending two key-down events followed by a key-up for the Return as we normally do causes an exception
            // it seems like the searcher is just waiting for that first Return event and closes immediately after,
            // any further events sent cause a MissingReferenceException
            SimulateKeyInput(searcherWindow, KeyCode.Return, false, false);
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            Assert.IsTrue(FindNodeOnGraphByName("Add"));
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
