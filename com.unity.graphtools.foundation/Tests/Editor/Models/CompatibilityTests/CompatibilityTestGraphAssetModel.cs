using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Models
{
    public class CompatibilityTestGraphAssetModel : GraphAssetModel
    {
        /// <inheritdoc />
        protected override Type GraphModelType => typeof(CompatibilityTestGraphModel);
    }
}
