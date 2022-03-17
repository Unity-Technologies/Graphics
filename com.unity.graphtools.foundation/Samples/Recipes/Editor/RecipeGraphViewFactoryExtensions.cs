using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    [GraphElementsExtensionMethodsCache(typeof(RecipeGraphView))]
    public static class RecipeGraphViewFactoryExtensions
    {
        public static IModelView CreateNode(this ElementBuilder elementBuilder, MixNodeModel model)
        {
            IModelView ui = new VariableIngredientNode();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelView CreateNode(this ElementBuilder elementBuilder, BakeNodeModel model)
        {
            IModelView ui = new BakeNode();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }
    }
}
