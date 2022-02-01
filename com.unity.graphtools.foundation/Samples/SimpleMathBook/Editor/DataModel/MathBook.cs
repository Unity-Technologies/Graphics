using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class MathBook : GraphModel
    {
        public MathBook()
        {
            StencilType = null;
        }

        public override Type DefaultStencilType => typeof(MathBookStencil);
    }
}
