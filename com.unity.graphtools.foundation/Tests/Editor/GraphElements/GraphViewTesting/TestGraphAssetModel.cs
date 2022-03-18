using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    class TestGraphAssetModel : GraphAssetModel
    {
        protected override Type GraphModelType => typeof(TestModels.GraphModel);
    }
}
