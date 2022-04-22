using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    public class MathBookBlackboardGraphModel : BlackboardGraphModel
    {
        public override string GetBlackboardTitle()
        {
            var title = base.GetBlackboardTitle();
            if (string.IsNullOrEmpty(title))
                return "MathBook";
            return title + " MathBook";
        }

        public override string GetBlackboardSubTitle()
        {
            return GraphModel.IsContainerGraph() ? "Container Graph" : "Asset Graph";
        }
    }
}
