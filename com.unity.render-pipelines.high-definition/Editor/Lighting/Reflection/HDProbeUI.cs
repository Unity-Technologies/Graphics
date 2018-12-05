using System;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
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
    }
}
