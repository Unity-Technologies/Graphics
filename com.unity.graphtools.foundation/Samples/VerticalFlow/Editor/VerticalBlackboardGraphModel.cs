using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Vertical
{
    class VerticalBlackboardGraphModel : BlackboardGraphModel
    {
        /// <inheritdoc />
        public VerticalBlackboardGraphModel(IGraphAssetModel graphAssetModel)
            : base(graphAssetModel) {}

        /// <inheritdoc />
        public override string GetBlackboardTitle()
        {
            return "Vertical Flow";
        }
    }
}
