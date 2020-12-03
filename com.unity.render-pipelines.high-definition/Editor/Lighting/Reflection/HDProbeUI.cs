using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    static partial class HDProbeUI
    {
        public enum Expandable
        {
            Influence = 1 << 0,
            Capture = 1 << 1,
            Projection = 1 << 2,
            Custom = 1 << 3,
        }
        internal readonly static ExpandedState<Expandable, HDProbe> k_ExpandedState = new ExpandedState<Expandable, HDProbe>(Expandable.Projection | Expandable.Capture | Expandable.Influence, "HDRP");

        [System.Flags]
        public enum AdditionalProperties
        {
            Capture = 1 << 0,
        }
        internal readonly static ExpandedState<AdditionalProperties, HDProbe> k_AdditionalPropertiesState = new ExpandedState<AdditionalProperties, HDProbe>(0, "HDRP");
    }
}
