using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts
{
    [Serializable]
    public class SampleContextModelB : SampleContextModelBase
    {
        /// <inheritdoc />
        public override string Title
        {
            get => "Context B";
            set { }
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            this.AddExecutionInputPort("");
            this.AddExecutionOutputPort("");
        }
    }
}
