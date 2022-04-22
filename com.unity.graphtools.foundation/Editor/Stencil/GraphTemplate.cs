using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// The default implementation of <see cref="IGraphTemplate"/>.
    /// </summary>
    /// <typeparam name="TStencil">The stencil type.</typeparam>
    public class GraphTemplate<TStencil> : IGraphTemplate where TStencil : Stencil
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GraphTemplate{TStencil}"/> class.
        /// </summary>
        /// <param name="graphTypeName">The name of the type of graph for this template.</param>
        /// <param name="graphFileExtension">Extension for the files used to save the graph.</param>
        public GraphTemplate(string graphTypeName = "Graph", string graphFileExtension = "asset")
        {
            GraphTypeName = graphTypeName;
            GraphFileExtension = graphFileExtension;
        }

        /// <inheritdoc />
        public virtual Type StencilType => typeof(TStencil);

        /// <inheritdoc />
        public virtual void InitBasicGraph(IGraphModel graphModel)
        {
        }

        /// <inheritdoc />
        public virtual string GraphTypeName { get; }

        /// <inheritdoc />
        public virtual string DefaultAssetName => GraphTypeName;

        public string GraphFileExtension { get; }
    }
}
