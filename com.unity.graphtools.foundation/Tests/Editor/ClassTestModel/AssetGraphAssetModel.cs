using System;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    class AssetGraphAssetModel : GraphAssetModel
    {
        protected override Type GraphModelType => typeof(ClassGraphModel);
        public override bool IsContainerGraph() => false;

        public override bool CanBeSubgraph() => GraphModel.VariableDeclarations.Any(variable => variable.IsInputOrOutput()) || GraphModel.Name == "I can be a Subgraph";
    }
}
