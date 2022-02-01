using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts
{
    [Serializable]
    public class SampleContextModelB : SampleContextModelBase
    {
        public SampleContextModelB()
        {
            Title = "Context B";
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            this.AddExecutionInputPort("");
            this.AddExecutionOutputPort("");
        }
    }
}
