using System;
using System.Linq;
using GraphModel = UnityEditor.GraphToolsFoundation.Overdrive.BasicModel.GraphModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    class ClassGraphModel : GraphModel
    {
        public override Type DefaultStencilType => typeof(ClassStencil);

        public override bool IsContainerGraph() => Asset is ContainerGraphAsset;

        public override bool CanBeSubgraph() => VariableDeclarations.Any(variable => variable.IsInputOrOutput()) || Name == "I can be a Subgraph";
    }

    [Serializable]
    class OtherClassGraphModel : GraphModel
    {
        public override Type DefaultStencilType => typeof(ClassStencil);
    }
}
