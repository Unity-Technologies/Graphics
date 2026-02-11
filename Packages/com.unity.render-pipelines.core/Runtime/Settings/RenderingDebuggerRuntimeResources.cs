#if ENABLE_UIELEMENTS_MODULE && (UNITY_EDITOR || DEVELOPMENT_BUILD)
#define ENABLE_RENDERING_DEBUGGER_UI
#endif

using System;
#if ENABLE_RENDERING_DEBUGGER_UI
using UnityEngine.UIElements;
#endif

namespace UnityEngine.Rendering
{
    [Serializable, HideInInspector]
    [SupportedOnRenderPipeline]
    [Categorization.CategoryInfo(Name = "R : Rendering Debugger Resources", Order = 100)]
    [Categorization.ElementInfo(Order = 0)]
    class RenderingDebuggerRuntimeResources : IRenderPipelineResources
    {
        enum Version
        {
            Initial,

            Count,
            Last = Count - 1
        }
        [SerializeField, HideInInspector]
        private Version m_version = Version.Last;
        int IRenderPipelineGraphicsSettings.version => (int)m_version;

        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

#if ENABLE_RENDERING_DEBUGGER_UI
        [SerializeField, ResourcePath("Runtime/Debugging/Runtime UI Resources/RuntimeDebugWindow_PanelSettings.asset")]
        private PanelSettings m_PanelSettings;

        /// <summary>StyleSheet for the Rendering Debugger Runtime UI</summary>
        public PanelSettings panelSettings
        {
            get => m_PanelSettings;
            set => this.SetValueAndNotify(ref m_PanelSettings, value, nameof(m_PanelSettings));
        }

        [SerializeField, ResourcePaths(new []
        {
            "Runtime/Debugging/Runtime UI Resources/DebugWindowCommon.uss",
            "Runtime/Debugging/Runtime UI Resources/RuntimeDebugWindow.uss"
        })]
        private StyleSheet[] m_StyleSheets;

        /// <summary>StyleSheets for the Rendering Debugger Runtime UI</summary>
        public StyleSheet[] styleSheets
        {
            get => m_StyleSheets;
            set => this.SetValueAndNotify(ref m_StyleSheets, value, nameof(m_StyleSheets));
        }

        [SerializeField, ResourcePath("Runtime/Debugging/Runtime UI Resources/RuntimeDebugWindow.uxml")]
        private VisualTreeAsset m_VisualTreeAsset;

        /// <summary>Visual Tree Asset for the Rendering Debugger Runtime UI</summary>
        public VisualTreeAsset visualTreeAsset
        {
            get => m_VisualTreeAsset;
            set => this.SetValueAndNotify(ref m_VisualTreeAsset, value, nameof(m_VisualTreeAsset));
        }
#endif
    }
}
