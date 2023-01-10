using System;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [Overlay(typeof(ShaderGraphEditorWindow), k_OverlayID, "Blackboard", defaultDisplay = true,
        defaultDockZone = DockZone.LeftColumn, defaultDockPosition = DockPosition.Bottom,
        defaultLayout = Layout.Panel, defaultWidth = 300, defaultHeight = 400)]
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
                    return m_BlackboardView;
                }
            }

            var placeholder = new VisualElement();
            placeholder.AddToClassList(BlackboardView.ussClassName);
            placeholder.AddStylesheet_Internal("BlackboardView.uss");
            return placeholder;
        }
    }
}
