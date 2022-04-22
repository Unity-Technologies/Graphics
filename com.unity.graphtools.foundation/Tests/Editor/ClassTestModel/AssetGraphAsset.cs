using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    class AssetGraphAsset : GraphAsset
    {
        protected override Type GraphModelType => typeof(ClassGraphModel);
    }
}
