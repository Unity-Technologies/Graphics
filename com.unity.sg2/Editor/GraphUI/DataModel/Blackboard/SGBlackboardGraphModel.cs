using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class SGBlackboardGraphModel : BlackboardGraphModel
    {
        public SGBlackboardGraphModel(IGraphModel graphModel) // : base(graphModel)
        {
            this.GraphModel = graphModel;
        }
    }
}
