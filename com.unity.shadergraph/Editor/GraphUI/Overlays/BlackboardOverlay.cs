using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.Overlays;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.Windows;
using UnityEditor.ShaderGraph.GraphUI.Utilities;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements.Views
{
    [Overlay(typeof(ShaderGraphEditorWindow), k_OverlayID)]
    class BlackboardOverlay : GraphSubWindowOverlay<Blackboard>
    {
        public const string k_OverlayID = "Blackboard";
        protected override string elementName => "Blackboard";
        protected override string ussRootClassName => "ge-blackboard";

        protected override void OnPanelContentAttached(AttachToPanelEvent evt)
        {
            base.OnPanelContentAttached(evt);
            this.displayed = true;
        }
    }
}
