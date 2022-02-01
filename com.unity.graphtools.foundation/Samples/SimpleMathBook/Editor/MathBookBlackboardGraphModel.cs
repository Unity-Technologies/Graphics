using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    public class MathBookBlackboardGraphModel : BlackboardGraphModel
    {

        public MathBookBlackboardGraphModel(IGraphAssetModel graphAssetModel)
            : base(graphAssetModel) {}

        public override string GetBlackboardTitle()
        {
            return AssetModel?.FriendlyScriptName == null ? "MathBook" : AssetModel?.FriendlyScriptName + " MathBook";
        }
    }
}
