using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    [Serializable]
    [SearcherItem(typeof(RecipeStencil), SearcherContext.Graph, "Preparation/Mix")]
    public class MixNodeModel : RecipeNodeBaseModel
    {
        public enum Direction
        {
            Clockwise,
            CounterClockwise,
            Both,
        }

        [SerializeField]
        [ModelSetting]
        [InspectorUseSetterMethodAttribute(nameof(SetIngredientCount))]
        [Tooltip("Number of ingredients to mix.")]
        int m_IngredientCount = 2;

        [SerializeField]
        [Tooltip("Mixing direction.")]
        // ReSharper disable once NotAccessedField.Local
        Direction m_Direction;

        public int IngredientCount => m_IngredientCount;

        public void SetIngredientCount(int count,
            out IEnumerable<IGraphElementModel> newModels,
            out IEnumerable<IGraphElementModel> changedModels,
            out IEnumerable<IGraphElementModel> deletedModels)
        {
            var edgeDiff = new NodeEdgeDiff(this, PortDirection.Input);

            m_IngredientCount = Math.Max(2, count);
            DefineNode();

            newModels = null;
            changedModels = null;
            deletedModels = edgeDiff.GetDeletedEdges();
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            AddInputPort("Cookware", PortType.Data, RecipeStencil.Cookware, options: PortModelOptions.NoEmbeddedConstant);
            AddInputPort("Attitude", PortType.Data, RecipeStencil.Attitude);

            for (var i = 0; i < IngredientCount; i++)
            {
                AddInputPort("Ingredient " + (i + 1), PortType.Data, RecipeStencil.Ingredient, options: PortModelOptions.NoEmbeddedConstant);
            }

            AddOutputPort("Result", PortType.Data, RecipeStencil.Ingredient, options: PortModelOptions.NoEmbeddedConstant);
        }
    }
}
