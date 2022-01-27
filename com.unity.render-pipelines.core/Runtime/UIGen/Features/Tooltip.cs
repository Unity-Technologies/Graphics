using System;

namespace UnityEngine.Rendering.UIGen
{
    public struct Tooltip : UIDefinition.IFeatureParameter
    {
        public readonly UIDefinition.PropertyTooltip tooltip;
        public Tooltip(UIDefinition.PropertyTooltip tooltip) {
            this.tooltip = tooltip;
        }
    }
}
