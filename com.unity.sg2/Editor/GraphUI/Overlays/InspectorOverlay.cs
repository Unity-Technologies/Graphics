using System;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [Overlay(typeof(ShaderGraphEditorWindow), k_OverlayID, "Inspector", defaultDisplay = true,
        defaultDockZone = DockZone.RightColumn, defaultDockPosition = DockPosition.Bottom,
        defaultLayout = Layout.Panel, defaultWidth = 300, defaultHeight = 400)]
    [Icon( GraphElementHelper.AssetPath + "GraphElements/Stylesheets/Icons/Inspector.png")]
    class SGInspectorOverlay : Overlay
    {
        public const string k_OverlayID = "sg-Inspector";

        public SGInspectorOverlay()
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
                var content = window.CreateModelInspectorView();
                if (content != null)
                {
                    content.AddToClassList("unity-theme-env-variables");
                    content.RegisterCallback<TooltipEvent>((e) => e.StopPropagation());
                    return content;
                }
            }

            var placeholder = new VisualElement();
            placeholder.AddToClassList(ModelInspectorView.ussClassName);
            placeholder.AddStylesheet_Internal("ModelInspector.uss");
            return placeholder;
        }
    }
}
