using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class RedirectNode : Node
    {
        protected override void BuildPartList()
        {
            var portContainerPart = InOutPortContainerPart.Create(
                portContainerPartName,
                Model,
                this,
                float.PositiveInfinity,
                ussClassName
            );
            PartList.AppendPart(portContainerPart);
            AddToClassList("sg-redirect-node");
            this.AddStylesheet("RedirectNode.uss");
        }
    }
}
