using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ConversionEdgePart : WireBubblePart
    {
        public ConversionEdgePart(string name, GraphElementModel model, ModelView ownerElement,
            string parentClassName) : base(name, model, ownerElement, parentClassName)
        {
        }

        // Always show our bubble. By default, bubble only appears on Execution ports (port type, not data type)
        // protected override bool ShouldShow() => true;
    }
}
