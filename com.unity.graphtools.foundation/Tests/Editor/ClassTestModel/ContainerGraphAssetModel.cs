using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    class ContainerGraphAssetModel : GraphAssetModel
    {
        protected override Type GraphModelType => typeof(ClassGraphModel);
        public override bool IsContainerGraph() => true;
    }
}
