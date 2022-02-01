using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    [Serializable]
    [SearcherItem(typeof(RecipeStencil), SearcherContext.Graph, "Preparation/Mix")]
    public class MixNodeModel : NodeModel
    {
        [SerializeField, HideInInspector]
        int m_IngredientCount = 2;

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            AddInputPort("Cookware", PortType.Data, RecipeStencil.Cookware, options: PortModelOptions.NoEmbeddedConstant);

            for (var i = 0; i < m_IngredientCount; i++)
            {
                AddInputPort("Ingredient " + (i + 1), PortType.Data, RecipeStencil.Ingredient, options: PortModelOptions.NoEmbeddedConstant);
            }

            AddOutputPort("Result", PortType.Data, RecipeStencil.Ingredient, options: PortModelOptions.NoEmbeddedConstant);
        }

        public void AddIngredientPort()
        {
            m_IngredientCount++;
            DefineNode();
        }

        public void RemoveIngredientPort()
        {
            m_IngredientCount--;
            if (m_IngredientCount < 2)
                m_IngredientCount = 2;

            DefineNode();
        }
    }
}
