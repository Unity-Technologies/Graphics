using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    [Serializable]
    [SearcherItem(typeof(RecipeStencil), SearcherContext.Graph, "Cooking/Fry")]
    public class FryNodeModel : RecipeNodeBaseModel
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
