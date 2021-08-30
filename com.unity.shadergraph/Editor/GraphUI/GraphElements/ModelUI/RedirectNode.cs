using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.Utilities;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements
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
