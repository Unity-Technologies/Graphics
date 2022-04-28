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
    sealed class BlackboardOverlay : Overlay
    {
        public const string idValue = "gtf-blackboard";

        BlackboardView m_BlackboardView;

        public BlackboardOverlay()
        {
            minSize = new Vector2(100, 100);
            maxSize = Vector2.positiveInfinity;
        }

        /// <inheritdoc />
        public override VisualElement CreatePanelContent()
        {
            var window = containerWindow as GraphViewEditorWindow;
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
            placeholder.AddStylesheet("BlackboardView.uss");
            return placeholder;
        }
    }
}
#endif
