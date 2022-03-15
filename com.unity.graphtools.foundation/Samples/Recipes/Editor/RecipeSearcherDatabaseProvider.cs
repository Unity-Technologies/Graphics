namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class RecipeSearcherDatabaseProvider : DefaultSearcherDatabaseProvider
    {
        /// <inheritdoc />
        public RecipeSearcherDatabaseProvider(Stencil stencil)
            : base(stencil) { }

        public override GraphElementSearcherDatabase InitialGraphElementDatabase(IGraphModel graphModel)
        {
            return new GraphElementSearcherDatabase(Stencil, graphModel)
                .AddNodesWithSearcherItemAttribute()
                .AddConstant(typeof(Attitude))
                .AddStickyNote();
        }
    }
}
