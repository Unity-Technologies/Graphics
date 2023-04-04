using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.UIElements;
using UnityEngine;
using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    class BaseGraphWindowTest
    {
        internal enum GraphInstantiation
        {
            None,
            MemoryBlank,
            Memory,
            MemorySubGraph,
            Disk,
            DiskSubGraph
        }

        private List<TestEditorWindow> _extraWindows = new();
        private List<string> _extraGraphAssets = new();
        protected const string k_BlackboardOverlayId = BlackboardOverlay_Internal.idValue;
        protected const string k_InspectorOverlayId = ModelInspectorOverlay_Internal.idValue;
        protected TestEditorWindow m_MainWindow;
        protected TestGraphView m_GraphView;
        protected TestEventHelpers m_TestEventHelper; // Used to send events to the highest shader graph editor window
        protected TestInteractionHelpers m_TestInteractionHelper; // Used to simulate interactions within the shader graph editor window

        protected virtual string testAssetPath => $"Assets\\{ShaderGraphStencil.DefaultGraphAssetName}.{ShaderGraphStencil.GraphExtension}";

        protected virtual bool hideOverlayWindows => true;

        protected SGGraphModel GraphModel => m_GraphView.GraphModel as SGGraphModel;

        protected virtual GraphInstantiation GraphToInstantiate => GraphInstantiation.MemoryBlank;

        [SetUp]
        public virtual void SetUp()
        {
            CreateWindow();

            m_GraphView = m_MainWindow.GraphView as TestGraphView;

            GraphAsset graphAsset = null;
            switch (GraphToInstantiate)
            {
                case GraphInstantiation.MemoryBlank:
                    graphAsset = ShaderGraphAssetUtils.CreateNewAssetGraph(false, true);
                    break;

                case GraphInstantiation.Memory:
                    graphAsset = ShaderGraphAssetUtils.CreateNewAssetGraph(false, false);
                    break;

                case GraphInstantiation.MemorySubGraph:
                    graphAsset = ShaderGraphAssetUtils.CreateNewAssetGraph(true, false);
                    break;

                case GraphInstantiation.Disk:
                {
                    var newGraphAction = ScriptableObject.CreateInstance<GraphAssetUtils.CreateGraphAssetAction>();
                    newGraphAction.Action(0, testAssetPath, "");
                    graphAsset = AssetDatabase.LoadAssetAtPath<ShaderGraphAsset>(testAssetPath);
                    break;
                }

                case GraphInstantiation.DiskSubGraph:
                {
                    var newGraphAction = ScriptableObject.CreateInstance<GraphAssetUtils.CreateGraphAssetAction>();
                    newGraphAction.isSubGraph = true;
                    newGraphAction.Action(0, testAssetPath, "");
                    graphAsset = ShaderGraphAssetUtils.HandleLoad(testAssetPath);
                    break;
                }

                case GraphInstantiation.None:
                default:
                    break;
            }

            if (graphAsset != null)
            {
                m_MainWindow.GraphTool.Dispatch(new LoadGraphCommand(graphAsset.GraphModel));
                m_MainWindow.GraphTool.Update();
            }

            if (hideOverlayWindows)
            {
                m_MainWindow.TryGetOverlay(k_BlackboardOverlayId, out var blackboardOverlay);
                blackboardOverlay.displayed = false;

                m_MainWindow.TryGetOverlay(k_InspectorOverlayId, out var inspectorOverlay);
                inspectorOverlay.displayed = false;
            }

            m_MainWindow.Focus();
        }

        [TearDown]
        public virtual void TearDown()
        {
            // Close main window and delete asset
            m_MainWindow.Close();
            AssetDatabase.DeleteAsset(testAssetPath);

            // Close any extra windows and delete extra assets
            foreach (var extraWindow in _extraWindows)
                extraWindow.Close();
            foreach (var extraGraphAsset in _extraGraphAssets)
                AssetDatabase.DeleteAsset(extraGraphAsset);

        }

        public TestEditorWindow CreateWindow()
        {
            m_MainWindow = EditorWindow.CreateWindow<TestEditorWindow>(typeof(SceneView), typeof(TestEditorWindow));
            m_MainWindow.shouldCloseWindowNoPrompt = true;

            m_TestEventHelper = new TestEventHelpers(m_MainWindow);

            m_TestInteractionHelper = new TestInteractionHelpers(m_MainWindow, m_TestEventHelper);

            return m_MainWindow;
        }

        public void CloseWindow()
        {
            // See case: https://fogbugz.unity3d.com/f/cases/998343/
            // Clearing the capture needs to happen before closing the window
            MouseCaptureController.ReleaseMouse();
            if (m_MainWindow != null)
            {
                m_MainWindow.Close();
            }
        }

        /// <summary>
        /// Saves the open graph, closes the tool window, then reopens the graph.
        /// m_MainWindow is reassigned after calling this method.
        /// </summary>
        public IEnumerator SaveAndReopenGraph()
        {
            GraphAssetUtils.SaveOpenGraphAsset(m_MainWindow.GraphTool);
            CloseWindow();
            yield return null;

            var graphAsset = ShaderGraphAssetUtils.HandleLoad(testAssetPath);

            CreateWindow();

            // this sets the selected window
            GraphViewEditorWindow.ShowGraphInExistingOrNewWindow<TestEditorWindow>(graphAsset);

            // Wait till the graph model is loaded back up
            while (m_MainWindow.GraphView.GraphModel == null)
                yield return null;
        }

        static Texture2D DrawMaterialToTexture(Material material)
        {
            var rt = RenderTexture.GetTemporary(4, 4, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            Graphics.Blit(null, rt, material);
            Texture2D output = new(4, 4, TextureFormat.ARGB32, false);
            output.ReadPixels(new Rect(0, 0, 4, 4), 0, 0);
            RenderTexture.active = prevActive;
            rt.Release();
            return output;
        }

        protected static Color SampleMaterialColor(Material material)
        {
            var outputTexture = DrawMaterialToTexture(material);
            try
            {
                return outputTexture.GetPixel(0, 0);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
