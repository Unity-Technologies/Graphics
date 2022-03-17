using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts
{
    [Serializable]
    public class ContextSample : GraphModel
    {
        public ContextSample()
        {
            StencilType = null;
        }

        public override Type DefaultStencilType => typeof(ContextSampleStencil);
    }
}
