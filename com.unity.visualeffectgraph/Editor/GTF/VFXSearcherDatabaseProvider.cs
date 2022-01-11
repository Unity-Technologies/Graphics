using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEditor.VFX
{
    public class VFXSearcherDatabaseProvider : DefaultSearcherDatabaseProvider
    {
        /// <inheritdoc />
        public VFXSearcherDatabaseProvider(Stencil stencil)
            : base(stencil)
        {
        }

        public override GraphElementSearcherDatabase InitialGraphElementDatabase(IGraphModel graphModel)
        {
            return new GraphElementSearcherDatabase(Stencil, graphModel)
                .AddNodesWithSearcherItemAttribute()
                //.AddConstant(typeof(Attitude))
                .AddStickyNote();
        }
    }
}
