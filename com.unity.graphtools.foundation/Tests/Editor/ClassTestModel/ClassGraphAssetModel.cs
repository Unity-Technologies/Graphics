using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    class ClassGraphAssetModel : GraphAssetModel
    {
        protected override Type GraphModelType => typeof(ClassGraphModel);
    }

    [Serializable]
    class OtherClassGraphAssetModel : GraphAssetModel
    {
        protected override Type GraphModelType => typeof(OtherClassGraphModel);
    }
}
