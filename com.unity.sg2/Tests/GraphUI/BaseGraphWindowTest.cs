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

            m_TestEventHelper = new TestEventHelpers(m_Window);

            m_TestInteractionHelper = new TestInteractionHelpers(m_Window, m_TestEventHelper);
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

        [MenuItem("Tests/Shader Graph/Create Texture 2D Array")]
        static void CreateTexture2DArray()
        {
            var texture1 = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Shaders/Tests/Textures/bone_02.png", typeof(Texture2D));
            var texture2 = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Shaders/Tests/Textures/cobblestone_d.tga", typeof(Texture2D));

            var textures = new [] {texture1, texture2};

            var array = new Texture2DArray(textures[0].width, textures[0].height, textures.Length, textures[0].format, false);
            for (int i = 0; i < textures.Length; i++)
                array.SetPixels(textures[i].GetPixels(), i);

            array.Apply();
            AssetDatabase.CreateAsset(array, "Assets/TextureArray.asset");
        }

        [MenuItem("Tests/Shader Graph/Create Texture 3D")]
        static void CreateTexture3D()
        {
            // Configure the texture
            int size = 32;
            TextureFormat format = TextureFormat.RGBA32;
            TextureWrapMode wrapMode =  TextureWrapMode.Clamp;

            // Create the texture and apply the configuration
            Texture3D texture = new Texture3D(size, size, size, format, false);
            texture.wrapMode = wrapMode;

            // Create a 3-dimensional array to store color data
            Color[] colors = new Color[size * size * size];

            // Populate the array so that the x, y, and z values of the texture will map to red, blue, and green colors
            float inverseResolution = 1.0f / (size - 1.0f);
            for (int z = 0; z < size; z++)
            {
                int zOffset = z * size * size;
                for (int y = 0; y < size; y++)
                {
                    int yOffset = y * size;
                    for (int x = 0; x < size; x++)
                    {
                        colors[x + yOffset + zOffset] = new Color(x * inverseResolution,
                            y * inverseResolution, z * inverseResolution, 1.0f);
                    }
                }
            }

            // Copy the color values to the texture
            texture.SetPixels(colors);

            // Apply the changes to the texture and upload the updated texture to the GPU
            texture.Apply();

            // Save the texture to your Unity Project
            AssetDatabase.CreateAsset(texture, "Assets/Texture3D.asset");
        }
    }
}
