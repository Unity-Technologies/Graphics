using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Models
{
    public class CompatibilityTestGraphAsset : GraphAsset
    {
        /// <inheritdoc />
        protected override Type GraphModelType => typeof(CompatibilityTestGraphModel);
    }
}
