using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    class ClassGraphAsset : GraphAsset
    {
        protected override Type GraphModelType => typeof(ClassGraphModel);
    }

    [Serializable]
    class OtherClassGraphAsset : GraphAsset
    {
        protected override Type GraphModelType => typeof(OtherClassGraphModel);
    }
}
