using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class RecipeGraphWindow : GraphViewEditorWindow
    {
        [InitializeOnLoadMethod]
        static void RegisterTool()
        {
            ShortcutHelper.RegisterDefaultShortcuts<RecipeGraphWindow>(RecipeGraphTool.toolName);
        }

        [MenuItem("GTF/Samples/Recipe Editor", false)]
        public static void ShowRecipeGraphWindow()
        {
            FindOrCreateGraphWindow<RecipeGraphWindow>();
        }

        /// <inheritdoc />
        protected override BaseGraphTool CreateGraphTool()
        {
            return CsoTool.Create<RecipeGraphTool>();
        }

        protected override GraphView CreateGraphView()
        {
            // We use our own derived GraphView type to be able to scope RecipeGraphViewFactoryExtensions to our graph view.
            return new RecipeGraphView(this, GraphTool, GraphTool.Name);
        }

        protected override BlankPage CreateBlankPage()
        {
            var onboardingProviders = new List<OnboardingProvider>();
            onboardingProviders.Add(new RecipeOnboardingProvider());

            return new BlankPage(GraphTool?.Dispatcher, onboardingProviders);
        }

        /// <inheritdoc />
        protected override bool CanHandleAssetType(IGraphAssetModel asset)
        {
            return asset is RecipeGraphAssetModel;
        }
    }
}
