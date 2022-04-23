using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ConversionEdgePart : EdgeBubblePart
    {
        public ConversionEdgePart(string name, IGraphElementModel model, IModelView ownerElement,
            string parentClassName) : base(name, model, ownerElement, parentClassName)
        {
        }

        // Always show our bubble. By default, bubble only appears on Execution ports (port type, not data type)
        // protected override bool ShouldShow() => true;
    }
}
