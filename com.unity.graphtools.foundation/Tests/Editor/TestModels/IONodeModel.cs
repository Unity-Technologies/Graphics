using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels
{
    class IONodeModel : NodeModel
    {
        public IONodeModel()
        {
            this.SetCapability(Overdrive.Capabilities.Renamable, false);
        }

        public int ExeInputCount { get; set; }
        public int ExeOutputCount { get; set; }

        public int InputCount { get; set; }
        public int OutputCount { get; set; }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            for (var i = 0; i < ExeInputCount; i++)
                this.AddExecutionInputPort("Exe In " + i);

            for (var i = 0; i < ExeOutputCount; i++)
                this.AddExecutionOutputPort("Exe Out " + i);

            for (var i = 0; i < InputCount; i++)
                this.AddDataInputPort("In " + i, TypeHandle.Unknown);

            for (var i = 0; i < OutputCount; i++)
                this.AddDataOutputPort("Out " + i, TypeHandle.Unknown);
        }
    }
}
