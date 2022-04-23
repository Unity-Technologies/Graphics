using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    class TestGraphAsset : GraphAsset
    {
        protected override Type GraphModelType => typeof(TestModels.GraphModel);
    }
}
