using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [Overlay(typeof(ShaderGraphEditorWindow), k_OverlayID, "Blackboard", defaultDisplay = true,
        defaultDockZone = DockZone.LeftColumn, defaultLayout = Layout.Panel)]
    [Icon( GraphElementHelper.AssetPath + "GraphElements/Stylesheets/Icons/Blackboard.png")]
    class SGBlackboardOverlay : Overlay
    {
        public const string k_OverlayID = "sg-Blackboard";

        BlackboardView m_BlackboardView;
        public SGBlackboardOverlay()
        {
            minSize = new Vector2(100, 100);
            maxSize = Vector2.positiveInfinity;
        }

        /// <inheritdoc />
        public override VisualElement CreatePanelContent()
        {
            var window = containerWindow as ShaderGraphEditorWindow;
            if (window != null)
            {
                m_BlackboardView = window.CreateBlackboardView();
                if (m_BlackboardView != null)
                {
                    m_BlackboardView.AddToClassList("unity-theme-env-variables");
                    m_BlackboardView.RegisterCallback<TooltipEvent>((e) => e.StopPropagation());
                    m_BlackboardView.AddToClassList(BlackboardView.ussClassName);
                    m_BlackboardView.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);

                    size = new Vector2(300, 400);
                    return m_BlackboardView;
                }
            }

            var emptyPlaceholder = new VisualElement();
            return emptyPlaceholder;
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            var overlayContent = m_BlackboardView.GetFirstAncestorWithName("overlay-content");
            overlayContent.style.flexGrow = 1.0f;
        }
    }
}
