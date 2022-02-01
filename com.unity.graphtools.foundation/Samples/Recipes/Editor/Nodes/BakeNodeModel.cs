using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    [Serializable]
    [SearcherItem(typeof(RecipeStencil), SearcherContext.Graph, "Cooking/Bake")]
    public class BakeNodeModel : NodeModel
    {
        [SerializeField]
        int m_TemperatureC = 180;
        [SerializeField]
        int m_Minutes = 60;

        public int Temperature
        {
            get => m_TemperatureC;
            set => m_TemperatureC = value;
        }

        public int Duration
        {
            get => m_Minutes;
            set => m_Minutes = value;
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            AddInputPort("Cookware", PortType.Data, RecipeStencil.Cookware, options: PortModelOptions.NoEmbeddedConstant);

            AddInputPort("Ingredients", PortType.Data, RecipeStencil.Ingredient, options: PortModelOptions.NoEmbeddedConstant);
            AddOutputPort("Result", PortType.Data, RecipeStencil.Ingredient, options: PortModelOptions.NoEmbeddedConstant);
        }
    }
}
