using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.HighDefinition.Compositor;

using UnityEditor;
using UnityEditorInternal;
using UnityEditor.ShaderGraph;
using UnityEditor.SceneManagement;

namespace UnityEditor.Rendering.HighDefinition.Compositor
{
    internal class CompositorWindow : EditorWindow
    {
        static partial class Styles
        {
            static public readonly GUIContent k_EnableCompositor = EditorGUIUtility.TrTextContent("Enable Compositor", "Enabled the compositor and creates a default composition profile.");
            static public readonly GUIContent k_RemoveCompositor = EditorGUIUtility.TrTextContent("Remove compositor from scene", "Removes the compositor and any composition settings from the scene.");
        }

        static CompositorWindow s_Window;

        // Remember the last selected layer
        static int s_SelectionIndex = -1;

        CompositionManagerEditor m_Editor;
        Vector2 m_ScrollPosition = Vector2.zero;
        bool m_RequiresRedraw = false;
        float m_TimeSinceLastRepaint = 0;

        [MenuItem("Window/Render Pipeline/Graphics Compositor", false, 10400)]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            s_Window = (CompositorWindow)EditorWindow.GetWindow(typeof(CompositorWindow));
            s_Window.titleContent = new GUIContent("Graphics Compositor");
            s_Window.Show();
        }

        void OnEnable()
        {
            // Register a custom undo callback
            Undo.undoRedoPerformed += UndoCallback;
        }

        void Update()
        {
            m_TimeSinceLastRepaint += Time.deltaTime;

            // This ensures that layer thumbnails are updated at least 4 times per second (redrawing the UI on every frame is too CPU intensive)
            const float timeThreshold = 0.25f;
            if (m_TimeSinceLastRepaint > timeThreshold)
            {
                Repaint();

                // [case 1266216] Ensure the game view gets repainted a few times per second even when we are not in play mode.
                // This ensures that we will not always display the first frame, which might have some artifacts for effects that require temporal data 
                if (!Application.isPlaying)
                {
                    CompositionManager compositor = CompositionManager.GetInstance();
                    if (compositor && compositor.enableOutput)
                    {
                        compositor.timeSinceLastRepaint += Time.deltaTime;
                        // The Editor will repaint the game view if the scene view is also visible (side-by-side) and
                        // "always refresh" is enabled so we call manually repaint only if enough time has passed
                        if (compositor.timeSinceLastRepaint > timeThreshold)
                        {
                            compositor.Repaint();
#if UNITY_2021_1_OR_NEWER
                            // [case 1290622] For version 2021.1 we have to explicitely request an update of the gameview with the following call
                            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
#endif
                        }
                    }

                }
            }
        }

        void OnGUI()
        {
            m_TimeSinceLastRepaint = 0;
            CompositionManager compositor = CompositionManager.GetInstance();
            bool enableCompositor = false;
            if (compositor)
            {
                enableCompositor = compositor.enabled;
            }

            bool enableCompositorCached = enableCompositor;
            enableCompositor = EditorGUILayout.Toggle(Styles.k_EnableCompositor, enableCompositor);

            // Track if the user changed the compositor enable state and mark the scene dirty if necessary
            if (enableCompositorCached != enableCompositor && compositor != null)
            {
                EditorUtility.SetDirty(compositor);
            }

            if (compositor == null && enableCompositor)
            {
                Debug.Log("The scene does not have a compositor. Creating a new one with the default configuration.");
                GameObject go = new GameObject("HDRP Compositor") { hideFlags = HideFlags.HideInHierarchy };
                compositor = go.AddComponent<CompositionManager>();

                // Mark as dirty, so if the user closes the scene right away, the change will be saved.
                EditorUtility.SetDirty(compositor);

                // Now add the default configuration
                CompositionUtils.LoadDefaultCompositionGraph(compositor);
                CompositionUtils.LoadOrCreateCompositionProfileAsset(compositor);
                compositor.SetupCompositionMaterial();
                CompositionUtils.SetDefaultCamera(compositor);
                CompositionUtils.SetDefaultLayers(compositor);

                Undo.RegisterCreatedObjectUndo(compositor.outputCamera.gameObject, "Create Compositor");
                Undo.RegisterCreatedObjectUndo(go, "Create Compositor");
            }
            else if (compositor)
            {
                string message = enableCompositor ? "Enable Compositor" : "Disable Compositor";
                Undo.RecordObject(compositor, message);
                compositor.enabled = enableCompositor;
            }
            else
            {
                return;
            }

            if (compositor && !compositor.enabled)
            {
                if (GUILayout.Button(new GUIContent(Styles.k_RemoveCompositor)))
                {
                    if (compositor.outputCamera)
                    {
                        if(compositor.outputCamera.name == CompositionUtils.k_DefaultCameraName)
                        {
                            var cameraData = compositor.outputCamera.GetComponent<HDAdditionalCameraData>();
                            if (cameraData != null)
                            {
                                CoreUtils.Destroy(cameraData);
                            }
                            CoreUtils.Destroy(compositor.outputCamera.gameObject);
                            CoreUtils.Destroy(compositor.outputCamera);
                            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                        }
                    }
                    
                    CoreUtils.Destroy(compositor);
                    return;
                }
            }

            // keep track of shader graph changes: when the user saves a graph, we should load/reflect any new shader properties
            GraphData.onSaveGraph += MarkShaderAsDirty;

            if (compositor.profile == null)
            {
                // The compositor was loaded, but there was no profile (someone deleted the asset from disk?), so create a new one
                CompositionUtils.LoadOrCreateCompositionProfileAsset(compositor);
                compositor.SetupCompositionMaterial();
                m_RequiresRedraw = true;
            }

            if (m_Editor == null || m_Editor.target == null || m_Editor.isDirty || m_RequiresRedraw)
            {
                if (m_Editor != null)
                {
                    // Remember the previously selected layer when recreating the Editor
                    s_SelectionIndex = m_Editor.selectionIndex;
                }
                m_Editor = (CompositionManagerEditor)Editor.CreateEditor(compositor);
                m_RequiresRedraw = false;
                m_Editor.defaultSelection = s_SelectionIndex;
            }

            m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);
            using (new EditorGUI.DisabledScope(!compositor.enabled))
            {
                if (m_Editor)
                {
                    m_Editor.OnInspectorGUI();
                }
            }
            GUILayout.EndScrollView();
        }

        void MarkShaderAsDirty(Shader shader, object context)
        {
            CompositionManager compositor = CompositionManager.GetInstance();
            if (compositor)
            {
                compositor.shaderPropertiesAreDirty = true;
                m_RequiresRedraw = true;

                EditorUtility.SetDirty(compositor);
                EditorUtility.SetDirty(compositor.profile);
            }
        }

        private void OnDestroy()
        {
            GraphData.onSaveGraph -= MarkShaderAsDirty;

            Undo.undoRedoPerformed -= UndoCallback;
            s_SelectionIndex = m_Editor ? m_Editor.selectionIndex : -1;
        }

        void UndoCallback()
        {
            // Undo-redo might change the layer order, so we need to redraw the compositor UI and also refresh the layer setup
            if (!m_Editor)
            {
                return;
            }

            m_Editor.CacheSerializedObjects();
            m_RequiresRedraw = true;
            s_SelectionIndex = m_Editor.selectionIndex;

            CompositionManager compositor = CompositionManager.GetInstance();
            // The compositor might be null even if the CompositionManagerEditor is not (in case the user switches from a scene with a compositor to a scene without one)
            if (compositor)
            {
                // Some properties were changed, mark the profile as dirty so it can be saved if the user saves the scene
                EditorUtility.SetDirty(compositor);
                EditorUtility.SetDirty(compositor.profile);
                compositor.UpdateLayerSetup();
            }
        }
    }
}
