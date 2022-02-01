using System.Linq;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels
{
    class SingleOutputNodeModel : NodeModel, ISingleOutputPortNodeModel
    {
        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            this.AddDataOutputPort("", TypeHandle.Unknown);
        }

        public IPortModel OutputPort => Ports.First();

        public SingleOutputNodeModel()
        {
            this.SetCapability(Overdrive.Capabilities.Renamable, false);
        }
    }
}
