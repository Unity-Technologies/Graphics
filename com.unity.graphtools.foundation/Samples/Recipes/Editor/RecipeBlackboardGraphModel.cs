using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class RecipeBlackboardGraphModel : BlackboardGraphModel
    {
        /// <inheritdoc />
        public RecipeBlackboardGraphModel(IGraphAssetModel graphAssetModel)
            : base(graphAssetModel) {}

        public override string GetBlackboardTitle()
        {
            return AssetModel?.FriendlyScriptName == null ? "Recipe" : AssetModel?.FriendlyScriptName + " Recipe";
        }

        public override string GetBlackboardSubTitle()
        {
            return "The Pantry";
        }
    }
}
