#if UNITY_2022_2_OR_NEWER
using System;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [Overlay(typeof(GraphViewEditorWindow), idValue, "Blackboard", defaultDisplay = true,
        defaultDockZone = DockZone.LeftColumn, defaultLayout = Layout.Panel)]
    [Icon( AssetHelper.AssetPath + "UI/Stylesheets/Icons/PanelsToolbar/Blackboard.png")]
    sealed class BlackboardOverlay : ResizableOverlay
    {
        public const string idValue = "gtf-blackboard";

        BlackboardView m_BlackboardView;

        protected override string Stylesheet => "BlackboardOverlay.uss";

        /// <inheritdoc />
        protected override VisualElement CreateResizablePanelContent()
        {
            m_BlackboardView?.Dispose();

            var window = containerWindow as GraphViewEditorWindow;
            if (window != null && window.GraphView != null)
            {
                m_BlackboardView = window.CreateBlackboardView();
                m_BlackboardView.AddToClassList("unity-theme-env-variables");
                m_BlackboardView.RegisterCallback<TooltipEvent>((e) => e.StopPropagation());
                return m_BlackboardView;
            }

            var placeholder = new VisualElement();
            placeholder.AddToClassList(BlackboardView.ussClassName);
            placeholder.AddStylesheet("BlackboardView.uss");
            return placeholder;
        }
    }
}
#endif
