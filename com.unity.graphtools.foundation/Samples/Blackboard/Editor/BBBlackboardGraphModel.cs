using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Blackboard
{
    public class BBBlackboardGraphModel : BlackboardGraphModel
    {
        public override string GetBlackboardTitle()
        {
            var title = base.GetBlackboardTitle();
            if (string.IsNullOrEmpty(title))
                return "Example";
            return title + " Example";
        }

        public override string GetBlackboardSubTitle()
        {
            return "Try out blackboard behavior";
        }
    }
}
