using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class RedirectNode : Node
    {
        protected override void BuildPartList()
        {
            PartList.AppendPart(InOutPortContainerPart.Create(portContainerPartName, Model, this, ussClassName));
            AddToClassList("sg-redirect-node");
            this.AddStylesheet("RedirectNode.uss");
        }
    }
}
