using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    [Serializable]
    [SearcherItem(typeof(RecipeStencil), SearcherContext.Graph, "Cooking/Fry")]
    public class FryNodeModel : NodeModel
    {
        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            AddInputPort("Cookware", PortType.Data, RecipeStencil.Cookware, options: PortModelOptions.NoEmbeddedConstant);

            AddInputPort("Ingredients", PortType.Data, RecipeStencil.Ingredient, options: PortModelOptions.NoEmbeddedConstant);
            AddOutputPort("Result", PortType.Data, RecipeStencil.Ingredient, options: PortModelOptions.NoEmbeddedConstant);
        }
    }
}
