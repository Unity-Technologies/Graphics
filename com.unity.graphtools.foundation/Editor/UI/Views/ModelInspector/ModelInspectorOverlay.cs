#if UNITY_2022_2_OR_NEWER
using System;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [Overlay(typeof(GraphViewEditorWindow), idValue, "Inspector", defaultDisplay = true,
        defaultDockZone = DockZone.RightColumn, defaultLayout = Layout.Panel)]
    [Icon( AssetHelper.AssetPath + "UI/Stylesheets/Icons/PanelsToolbar/Inspector.png")]
    sealed class ModelInspectorOverlay : ResizableOverlay
    {
        public const string idValue = "gtf-inspector";

        protected override string Stylesheet => "ModelInspectorOverlay.uss";

        /// <inheritdoc />
        protected override VisualElement CreateResizablePanelContent()
        {
            var window = containerWindow as GraphViewEditorWindow;
            if (window != null)
            {
                var content = window.CreateModelInspectorView();
                content.AddToClassList("unity-theme-env-variables");
                content.RegisterCallback<TooltipEvent>((e) => e.StopPropagation());
                return content;
            }

            var placeholder = new VisualElement();
            placeholder.AddToClassList(ModelInspectorView.ussClassName);
            placeholder.AddStylesheet("ModelInspector.uss");
            return placeholder;
        }
    }
}
#endif
