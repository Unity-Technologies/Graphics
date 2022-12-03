using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.UIElements;
using UnityEngine;
using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    class BaseGraphWindowTest
    {
        protected static readonly Rect k_WindowRect = new Rect(Vector2.zero, new Vector2( /*SelectionDragger.panAreaWidth*/ 100 * 8, /*SelectionDragger.panAreaWidth*/ 100 * 6));

        protected TestEditorWindow m_Window;
        protected TestGraphView m_GraphView;

        protected ShaderGraphModel GraphModel => m_GraphView.GraphModel as ShaderGraphModel;

        // Used to send events to the highest shader graph editor window
        protected TestEventHelpers m_TestEventHelper;

        // Used to simulate interactions within the shader graph editor window
        protected TestInteractionHelpers m_TestInteractionHelper;

        protected virtual string testAssetPath => $"Assets\\{ShaderGraphStencil.DefaultGraphAssetName}.{ShaderGraphStencil.GraphExtension}";
        protected virtual bool hideOverlayWindows => true;

        // Need to match the values specified by the BlackboardOverlay and ModelInspectorOverlay in GTFO
        protected const string k_BlackboardOverlayId = SGBlackboardOverlay.k_OverlayID;
        protected const string k_InspectorOverlayId = SGInspectorOverlay.k_OverlayID;

        internal enum GraphInstantiation
        {
            None,
            MemoryBlank,
            Memory,
            MemorySubGraph,
            Disk,
            DiskSubGraph
        }

        protected virtual GraphInstantiation GraphToInstantiate => GraphInstantiation.MemoryBlank;

        [SetUp]
        public virtual void SetUp()
        {
            CreateWindow();

            m_GraphView = m_Window.GraphView as TestGraphView;

            GraphAsset graphAsset = null;
            switch (GraphToInstantiate)
            {
                case GraphInstantiation.MemoryBlank:
                    graphAsset = ShaderGraphAssetUtils.CreateNewAssetGraph(testAssetPath);
                    break;

                case GraphInstantiation.Memory:
                    graphAsset = ShaderGraphAssetUtils.CreateNewAssetGraph(testAssetPath, LegacyTargetType.URPUnlit);
                    break;

                case GraphInstantiation.MemorySubGraph:
                    graphAsset = ShaderGraphAssetUtils.CreateNewSubGraph(testAssetPath);
                    break;

                case GraphInstantiation.Disk:
                {
                    var newGraphAction = ScriptableObject.CreateInstance<AssetUtils.CreateAssetGraphAction>();
                    newGraphAction.Action(0, testAssetPath, "");
                    graphAsset = AssetDatabase.LoadAssetAtPath<ShaderGraphAsset>(testAssetPath);
                    break;
                }

                // TODO: This should call a CreateSubGraphAssetAction instead
                case GraphInstantiation.DiskSubGraph:
                {
                    var newGraphAction = ScriptableObject.CreateInstance<AssetUtils.CreateAssetGraphAction>();
                    //newGraphAction.isSubGraph = true;
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
                m_Window.GraphTool.Dispatch(new LoadGraphCommand(graphAsset.GraphModel));
                m_Window.GraphTool.Update();
            }

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

        public TestEditorWindow CreateWindow()
        {
            m_Window = EditorWindow.CreateWindow<TestEditorWindow>(typeof(SceneView), typeof(TestEditorWindow));
            m_Window.shouldCloseWindowNoPrompt = true;

            m_TestEventHelper = new TestEventHelpers(m_Window);

            m_TestInteractionHelper = new TestInteractionHelpers(m_Window, m_TestEventHelper);

            return m_Window;
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
            AssetUtils.SaveOpenGraphAsset(m_Window.GraphTool);
            CloseWindow();
            yield return null;

            var graphAsset = ShaderGraphAssetUtils.HandleLoad(testAssetPath);

            CreateWindow();
            // this sets the selected window
            GraphViewEditorWindow.ShowGraphInExistingOrNewWindow<TestEditorWindow>(graphAsset);

            // Wait till the graph model is loaded back up
            while (m_Window.GraphView.GraphModel == null)
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
