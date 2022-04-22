using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Vertical
{
    class VerticalBlackboardGraphModel : BlackboardGraphModel
    {
        /// <inheritdoc />
        public override string GetBlackboardTitle()
        {
            var title = base.GetBlackboardTitle();
            if (string.IsNullOrEmpty(title))
                return "Vertical Flow";
            return title + " Vertical Flow";
        }
    }
}
