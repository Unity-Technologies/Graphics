using System;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [Overlay(typeof(ShaderGraphEditorWindow), k_OverlayID, "Inspector", defaultDisplay = true,
        defaultDockZone = DockZone.RightColumn, defaultLayout = Layout.Panel)]
    [Icon( GraphElementHelper.AssetPath + "GraphElements/Stylesheets/Icons/Inspector.png")]
    class SGInspectorOverlay : Overlay
    {
        public const string k_OverlayID = "sg-Inspector";

        ModelInspectorView m_InspectorView;

        public SGInspectorOverlay()
        {
            minSize = new Vector2(100, 100);
            maxSize = new Vector2(1000, 1000);
        }

        /// <inheritdoc />
        public override VisualElement CreatePanelContent()
        {
            var window = containerWindow as ShaderGraphEditorWindow;
            if (window != null)
            {
                m_InspectorView = window.CreateModelInspectorView();
                if (m_InspectorView != null)
                {
                    m_InspectorView.AddToClassList("unity-theme-env-variables");
                    m_InspectorView.RegisterCallback<TooltipEvent>((e) => e.StopPropagation());
                    m_InspectorView.AddToClassList(ModelInspectorView.ussClassName);
                    m_InspectorView.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
                    m_InspectorView.AddStylesheet("Inspector.uss");

                    // TODO: The overlays should be persisting the size and driving the main preview size
                    minSize = new Vector2(100, 100);
                    maxSize = new Vector2(1000, 1000);
                    if(Single.IsNaN(size.x) || Single.IsNaN(size.y))
                        size = new Vector2(300, 300);

                    return m_InspectorView;
                }
            }

            var emptyPlaceholder = new VisualElement();
            return emptyPlaceholder;
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            var overlayContent = m_InspectorView.GetFirstAncestorWithName("overlay-content");
            overlayContent.style.flexGrow = 1.0f;
        }
    }
}
