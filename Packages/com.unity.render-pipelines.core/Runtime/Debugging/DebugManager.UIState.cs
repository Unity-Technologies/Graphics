#if ENABLE_UIELEMENTS_MODULE && (UNITY_EDITOR || DEVELOPMENT_BUILD)
#define ENABLE_RENDERING_DEBUGGER_UI
#endif
#if ENABLE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM_PACKAGE
#define USE_INPUT_SYSTEM
#endif

using System;

namespace UnityEngine.Rendering
{

    public sealed partial class DebugManager
    {
        /// <summary>
        /// The modes of the UI of the Rendering Debugger
        /// </summary>
        public enum UIMode : int
        {
            /// <summary>
            /// Editor Window
            /// </summary>
            EditorMode,
            /// <summary>
            /// In Game view
            /// </summary>
            RuntimeMode
        }

        /// <summary>
        /// Event that is raised when a window state is changed
        /// </summary>
        public static event Action<UIMode, bool> windowStateChanged;

        class UIState
        {
            public UIMode mode;

            bool m_Open;

            public bool open
            {
                get => m_Open;
                set
                {
                    if (m_Open == value)
                        return;

                    m_Open = value;

                    windowStateChanged?.Invoke(mode, m_Open);
                }
            }
        }

        readonly UIState m_EditorUIState = new UIState() { mode = UIMode.EditorMode };

        /// <summary>
        /// Is the debug editor window open.
        /// </summary>
        public bool displayEditorUI
        {
            get => m_EditorUIState.open;
            set => m_EditorUIState.open = value;
        }

        bool m_EnableRuntimeUI = true;

        /// <summary>
        /// Controls whether runtime UI can be enabled. When this is set to false, there will be no overhead
        /// from debug GameObjects or runtime initialization.
        /// </summary>
        public bool enableRuntimeUI
        {
            get => m_EnableRuntimeUI;
            set
            {
                if (value != m_EnableRuntimeUI)
                {
                    m_EnableRuntimeUI = value;
                    DebugUpdater.SetEnabled(value);
                }
            }
        }

        readonly UIState m_RuntimeUIState = new UIState() { mode = UIMode.RuntimeMode };

        /// <summary>
        /// Displays the runtime version of the debug window.
        /// </summary>
        public bool displayRuntimeUI
        {
#if ENABLE_RENDERING_DEBUGGER_UI
            get => m_RuntimeDebugWindow != null && m_RuntimeDebugWindow.gameObject.activeInHierarchy;
            set
            {
                if (value)
                {
                    if (m_RuntimeDebugWindow == null && GraphicsSettings.TryGetRenderPipelineSettings<RenderingDebuggerRuntimeResources>(out _))
                    {
                        var go = new GameObject("[Debug UI]");
                        m_RuntimeDebugWindow = go.AddComponent<RuntimeDebugWindow>();
                        go.SetActive(true);
                    }
#if USE_INPUT_SYSTEM
                    m_DebugMenuActions.Enable();
#endif
                }
                else
                {
                    if (m_RuntimeDebugWindow != null)
                    {
                        CoreUtils.Destroy(m_RuntimeDebugWindow.gameObject);
                        m_RuntimeDebugWindow = null;
                    }
#if USE_INPUT_SYSTEM
                    m_DebugMenuActions.Disable();
#endif
                }

                onDisplayRuntimeUIChanged(value);

                m_RuntimeUIState.open = m_RuntimeDebugWindow != null && m_RuntimeDebugWindow.gameObject.activeInHierarchy;
            }
#else
            get => false;
            set
            {
                if (value)
                    throw new NotSupportedException("Rendering Debugger Runtime UI requires the UIElements module.");
            }
#endif
        }

        /// <summary>
        /// Displays the persistent runtime debug window.
        /// </summary>
        public bool displayPersistentRuntimeUI
        {
#if ENABLE_RENDERING_DEBUGGER_UI
            get => m_RuntimePersistentDebugUI != null && m_RuntimePersistentDebugUI.gameObject.activeInHierarchy;
            set
            {
                if (value)
                {
                    if (m_RuntimePersistentDebugUI == null && GraphicsSettings.TryGetRenderPipelineSettings<RenderingDebuggerRuntimeResources>(out _))
                    {
                        var go = new GameObject("[Persistent Debug UI]");
                        m_RuntimePersistentDebugUI = go.AddComponent<RuntimePersistentDebugUI>();
                        go.SetActive(true);
                    }
                }
                else
                {
                    CoreUtils.Destroy(m_RuntimePersistentDebugUI.gameObject);
                    m_RuntimePersistentDebugUI = null;
                }
            }
#else
            get => false;
            set
            {
                if (value)
                    throw new NotSupportedException("Rendering Debugger Runtime UI requires the UIElements module.");
            }
#endif
        }
    }
}

