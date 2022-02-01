using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for the Stencil, which represents the capabilities of a graph.
    /// </summary>
    public interface IStencil
    {
        /// <summary>
        /// The graph model to which this stencil is associated.
        /// </summary>
        IGraphModel GraphModel { get; set; }

        /// <summary>
        /// The metadata resolver for the graph.
        /// </summary>
        ITypeMetadataResolver TypeMetadataResolver { get; }

        /// <summary>
        /// Whether it is allowed to have multiple instances of data output variables.
        /// </summary>
        bool AllowMultipleDataOutputInstances { get; }

        /// <summary>
        /// Indicates whether a <see cref="IVariableDeclarationModel"/> requires special inspector initialization.
        /// </summary>
        /// <param name="decl">The variable declaration model to query.</param>
        /// <returns>True if the variable declaration model requires initialization, false otherwise.</returns>
        bool RequiresInspectorInitialization(IVariableDeclarationModel decl);

        /// <summary>
        /// Create a constant of the type represented by <paramref name="constantTypeHandle"/>
        /// </summary>
        /// <param name="constantTypeHandle">The type of the constant that will be created.</param>
        /// <returns>A new constant.</returns>
        IConstant CreateConstantValue(TypeHandle constantTypeHandle);

        /// <summary>
        /// Gets the constant type associated with the given <see cref="TypeHandle"/>.
        /// </summary>
        /// <param name="typeHandle">The handle for which to retrieve the type.</param>
        /// <returns>The type associated with <paramref name="typeHandle"/></returns>
        Type GetConstantNodeValueType(TypeHandle typeHandle);

        /// <summary>
        /// Gets the type for subgraph nodes.
        /// </summary>
        /// <returns>The type associated with subgraph nodes</returns>
        TypeHandle GetSubgraphNodeTypeHandle();

        /// <summary>
        /// Gets the port capacity of a port.
        /// </summary>
        /// <param name="portModel">The port model to check.</param>
        /// <param name="capacity">The resulting capacity for the given <paramref name="portModel"/></param>
        /// <returns>True if a capacity other than the default one was returned in <paramref name="capacity"/>. False otherwise.</returns>
        bool GetPortCapacity(IPortModel portModel, out PortCapacity capacity);

        /// <summary>
        /// Get the entry points of the associated <see cref="GraphModel"/>.
        /// </summary>
        /// <returns>The entry points of the associated <see cref="GraphModel"/>.</returns>
        IEnumerable<INodeModel> GetEntryPoints();

        /// <summary>
        /// Creates a <see cref="LinkedNodesDependency"/> between the two nodes connected by the given <paramref name="edgeModel"/>.
        /// </summary>
        /// <param name="edgeModel">The edge model to create a dependency from.</param>
        /// <param name="linkedNodesDependency">The resulting dependency.</param>
        /// <param name="parentNodeModel">The node model considered as parent in the dependency.</param>
        /// <returns>True is a dependency was created, false otherwise.</returns>
        bool CreateDependencyFromEdge(IEdgeModel edgeModel, out LinkedNodesDependency linkedNodesDependency,
            out INodeModel parentNodeModel);

        /// <summary>
        /// Retrieves the portals models dependant on the given <paramref name="portalModel"/> (if any).
        /// </summary>
        /// <remarks>
        /// <p>In a pull model, an exit portal's dependency are the entry portals linked to it and entry portals have no dependencies.</p>
        /// <p>In a push model, an entry portal's dependency are the exit portals linked to it and exit portals have no dependencies.</p>
        /// </remarks>
        /// <param name="portalModel">The portal to retrieve the dependent portals from.</param>
        /// <returns>The portals dependant on the given one.</returns>
        IEnumerable<IEdgePortalModel> GetPortalDependencies(IEdgePortalModel portalModel);

        /// <summary>
        /// Retrieves all the portals linked to the given <paramref name="portalModel"/> (if any).
        /// </summary>
        /// <remarks>
        /// <p>For an entry portal, all the linked exit portals are returned.</p>
        /// <p>For an exit portal, all the linked entry portals are returned.</p>
        /// </remarks>
        /// <param name="portalModel">The portal to retrieve the linked portals from.</param>
        /// <returns>The portals linked to the given one.</returns>
        IEnumerable<IEdgePortalModel> GetLinkedPortals(IEdgePortalModel portalModel);

        /// <summary>
        /// Indicates if a node can be pasted or duplicated.
        /// </summary>
        /// <param name="originalModel">The node model to copy</param>
        /// <param name="graph">The graph in which the action takes place.</param>
        /// <returns>If the node can be pasted or duplicated.</returns>
        bool CanPasteNode(INodeModel originalModel, IGraphModel graph);

        /// <summary>
        /// Creates a <see cref="IBlackboardGraphModel"/> for the <paramref name="graphAssetModel"/>.
        /// </summary>
        /// <param name="graphAssetModel">The graph asset to wrap in a <see cref="IBlackboardGraphModel"/>.</param>
        /// <returns>A new <see cref="IBlackboardGraphModel"/></returns>
        IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel);

        public IEnumerable<string> SectionNames { get; }

        /// <summary>
        /// Returns a valid section for a given variable. Default is to return the first section.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>A valid section for a given variable. Default is to return the first section.</returns>
        string GetVariableSection(IVariableDeclarationModel variable);
    }
}
