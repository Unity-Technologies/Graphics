using System;
using System.Diagnostics;
using UnityEngine.Rendering.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_ANDROID || UNITY_IPHONE || UNITY_TVOS || UNITY_SWITCH || UNITY_SWITCH2
using UnityEngine.UI;
#endif

namespace UnityEngine.Rendering
{
    using UnityObject = UnityEngine.Object;

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

            [SerializeField]
            private bool m_Open;
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

        private UIState editorUIState = new UIState() { mode = UIMode.EditorMode };

        /// <summary>
        /// Is the debug editor window open.
        /// </summary>
        public bool displayEditorUI
        {
            get => editorUIState.open;
            set => editorUIState.open = value;
        }

        private bool m_EnableRuntimeUI = true;

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

        private UIState runtimeUIState = new UIState() { mode = UIMode.RuntimeMode };

        /// <summary>
        /// Displays the runtime version of the debug window.
        /// </summary>
        public bool displayRuntimeUI
        {
            get => m_Root != null && m_Root.activeInHierarchy;
            set
            {
                if (value)
                {
                    m_Root = UnityObject.Instantiate(Resources.Load<Transform>("DebugUICanvas")).gameObject;
                    m_Root.name = "[Debug Canvas]";
                    m_Root.transform.localPosition = Vector3.zero;
                    m_RootUICanvas = m_Root.GetComponent<DebugUIHandlerCanvas>();

#if UNITY_ANDROID || UNITY_IPHONE || UNITY_TVOS || UNITY_SWITCH || UNITY_SWITCH2
                    var canvasScaler = m_Root.GetComponent<CanvasScaler>();
                    canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
#endif

                    m_Root.SetActive(true);
                }
                else
                {
                    CoreUtils.Destroy(m_Root);
                    m_Root = null;
                    m_RootUICanvas = null;
                }

                onDisplayRuntimeUIChanged(value);
                DebugUpdater.HandleInternalEventSystemComponents(value);

                runtimeUIState.open = m_Root != null && m_Root.activeInHierarchy;
            }
        }

        /// <summary>
        /// Displays the persistent runtime debug window.
        /// </summary>
        public bool displayPersistentRuntimeUI
        {
            get => m_RootUIPersistentCanvas != null && m_PersistentRoot.activeInHierarchy;
            set
            {
                if (value)
                {
                    EnsurePersistentCanvas();
                }
                else
                {
                    CoreUtils.Destroy(m_PersistentRoot);
                    m_PersistentRoot = null;
                    m_RootUIPersistentCanvas = null;
                }
            }
        }
    }
}

