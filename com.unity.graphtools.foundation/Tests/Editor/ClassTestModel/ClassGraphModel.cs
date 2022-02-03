using System;
using GraphModel = UnityEditor.GraphToolsFoundation.Overdrive.BasicModel.GraphModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    class ClassGraphModel : GraphModel
    {
        public override Type DefaultStencilType => typeof(ClassStencil);
    }

    [Serializable]
    class OtherClassGraphModel : GraphModel
    {
        public override Type DefaultStencilType => typeof(ClassStencil);
    }
}
