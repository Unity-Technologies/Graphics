using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class RecipeBlackboardGraphModel : BlackboardGraphModel
    {
        /// <inheritdoc />
        public RecipeBlackboardGraphModel(IGraphAssetModel graphAssetModel)
            : base(graphAssetModel) {}

        public override string GetBlackboardTitle()
        {
            var title = base.GetBlackboardTitle();
            if (string.IsNullOrEmpty(title))
                return "Recipe";
            return title + " Recipe";
        }

        public override string GetBlackboardSubTitle()
        {
            return "The Pantry";
        }
    }
}
