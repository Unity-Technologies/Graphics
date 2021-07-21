using UnityEditor.Overlays;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.Windows;
using UnityEditor.ShaderGraph.GraphUI.Utilities;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements.Views
{
    [Overlay(typeof(ShaderGraphEditorWindow), k_OverlayID)]
    class BlackboardOverlay : GraphSubWindowOverlay
    {
        public const string k_OverlayID = "Blackboard";

        protected override void OnPanelContentAttached(AttachToPanelEvent evt)
        {
            var parent = (ShaderGraphEditorWindow) containerWindow;
            if (parent == null) return;

            var blackboardView = parent.BlackboardView;
            if (blackboardView is null) return;

            this.displayed = true;
            // TODO: Resizable overlay
            blackboardView.style.width = 250;
            blackboardView.style.height = 300;

            // TODO: Add our own Blackboard.UXML and Inspector.UXML that we instantiate here as needed and also load in the styling
            m_Root.Clear();
            GraphElementHelper.LoadTemplateAndStylesheet(m_Root, "Blackboard", "ge-blackboard");

            //m_Root.Add(blackboardView);
            var contentElement = m_Root.Q("content");
            if(contentElement != null)
                contentElement.Add(blackboardView);

            m_Root.style.width = blackboardView.style.width;
            m_Root.style.height = blackboardView.style.height;
        }
    }
}
