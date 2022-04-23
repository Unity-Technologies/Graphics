using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts
{
    [Serializable]
    public class SampleContextModelA : SampleContextModelBase
    {
        /// <inheritdoc />
        public override string Title
        {
            get => "Sample A";
            set { }
        }
    }
}
