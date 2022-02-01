using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    [GraphElementsExtensionMethodsCache(typeof(RecipeGraphView))]
    public static class RecipeGraphViewFactoryExtensions
    {
        public static IModelUI CreateNode(this ElementBuilder elementBuilder, MixNodeModel model)
        {
            IModelUI ui = new VariableIngredientNode();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateNode(this ElementBuilder elementBuilder, BakeNodeModel model)
        {
            IModelUI ui = new BakeNode();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }
    }
}
