using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    class RecipeGraphTool : BaseGraphTool
    {
        public static readonly string toolName = "Recipe Editor";

        public RecipeGraphTool()
        {
            Name = toolName;
        }

        /// <inheritdoc />
        protected override void InitState()
        {
            base.InitState();
            Preferences.SetInitialSearcherSize(SearcherService.Usage.CreateNode, new Vector2(375, 300), 2.0f);
        }
    }
}
