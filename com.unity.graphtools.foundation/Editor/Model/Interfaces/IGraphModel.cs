using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Spawn flags dictates multiple operations during the NodeModels creation.
    /// </summary>
    [Flags]
    public enum SpawnFlags
    {
        None = 0,
        Reserved0 = 1 << 0,
        Reserved1 = 1 << 1,
        /// <summary>
        /// The created NodeModel is not added to a Graph. Useful for display only purposes.
        /// </summary>
        Orphan = 1 << 2,
        /// <summary>
        /// Equivalent to None.
        /// </summary>
        Default = None,
    }

    /// <summary>
    /// Extension methods for <see cref="SpawnFlags"/>.
    /// </summary>
    public static class SpawnFlagsExtensions
    {
        /// <summary>
        /// Whether <paramref name="f"/> has the <see cref="SpawnFlags.Orphan"/> set.
        /// </summary>
        /// <param name="f">The flag set to check.</param>
        /// <returns>True if <paramref name="f"/> has the <see cref="SpawnFlags.Orphan"/> set.</returns>
        public static bool IsOrphan(this SpawnFlags f) => (f & SpawnFlags.Orphan) != 0;
    }

    /// <summary>
    /// Interface for a model that represents a graph.
    /// </summary>
    public interface IGraphModel : IGraphElementContainer
    {
        IStencil Stencil { get; }
        Type DefaultStencilType { get; }
        Type StencilType { get; set; }
        IGraphAssetModel AssetModel { get; set; }

        void OnEnable();
        void OnDisable();

        /// <summary>
        /// The name of the current graph.
        /// </summary>
        string Name { get; }

        IReadOnlyList<INodeModel> NodeModels { get; }
        IEnumerable<INodeModel> NodeAndBlockModels { get; }
        IReadOnlyList<IEdgeModel> EdgeModels { get; }
        IReadOnlyList<IBadgeModel> BadgeModels { get; }
        IReadOnlyList<IStickyNoteModel> StickyNoteModels { get; }
        IReadOnlyList<IPlacematModel> PlacematModels { get; }
        /// <summary>
        /// All the section models.
        /// </summary>
        IReadOnlyList<ISectionModel> SectionModels { get; }

        /// <summary>
        /// The variable declaration contained in this model.
        /// </summary>
        IReadOnlyList<IVariableDeclarationModel> VariableDeclarations { get; }
        IReadOnlyList<IDeclarationModel> PortalDeclarations { get; }

        /// <summary>
        /// Gets the list of edges that are connected to a port.
        /// </summary>
        /// <param name="portModel">The port for which we want the list of connected edges.</param>
        /// <returns>The list of edges connected to the port.</returns>
        IReadOnlyList<IEdgeModel> GetEdgesForPort(IPortModel portModel);

        /// <summary>
        /// Retrieves a graph element model from its GUID.
        /// </summary>
        /// <param name="guid">The guid of the model to retrieve.</param>
        /// <param name="model">The model matching the guid, or null if no model were found.</param>
        /// <returns>True if the model was found. False otherwise.</returns>
        bool TryGetModelFromGuid(SerializableGUID guid, out IGraphElementModel model);

        /// <summary>
        /// Retrieves a graph element model of type <typeparamref name="T"/> from its GUID.
        /// </summary>
        /// <param name="guid">The guid of the model to retrieve.</param>
        /// <param name="model">The model matching the guid and type, or null if no model were found.</param>
        /// <typeparam name="T">The type of the model to retrieve.</typeparam>
        /// <returns>True if the model was found and is of the requested type. False otherwise.</returns>
        bool TryGetModelFromGuid<T>(SerializableGUID guid, out T model) where T : class, IGraphElementModel;

        /// <summary>
        /// Creates a new variable declaration in the graph.
        /// </summary>
        /// <param name="variableDataType">The type of data the new variable declaration to create represents.</param>
        /// <param name="variableName">The name of the new variable declaration to create.</param>
        /// <param name="modifierFlags">The modifier flags of the new variable declaration to create.</param>
        /// <param name="isExposed">Whether the variable is exposed externally or not.</param>
        /// <param name="group">The group in which the variable is added. If null, it will go to the root group.</param>
        /// <param name="indexInGroup">The index of the variable in the group.</param>
        /// <param name="initializationModel">The initialization model of the new variable declaration to create. Can be <code>null</code>.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="spawnFlags">The flags specifying how the variable declaration is to be spawned.</param>
        /// <returns>The newly created variable declaration.</returns>
        IVariableDeclarationModel CreateGraphVariableDeclaration(TypeHandle variableDataType, string variableName,
            ModifierFlags modifierFlags, bool isExposed, IGroupModel group = null, int indexInGroup = -1, IConstant initializationModel = null, SerializableGUID guid = default,
            SpawnFlags spawnFlags = SpawnFlags.Default);

        /// <summary>
        /// Generates a unique name for a variable declaration in the graph.
        /// </summary>
        /// <param name="variableDeclarationName">The name of the variable declaration.</param>
        /// <param name="variableDeclarationGuid">The guid of the variable declaration.</param>
        /// <returns>The unique name for the variable declaration.</returns>
        string GenerateGraphVariableDeclarationUniqueName(string variableDeclarationName, SerializableGUID variableDeclarationGuid);

        /// <summary>
        /// Creates a new variable declaration in the graph.
        /// </summary>
        /// <param name="variableTypeToCreate">The type of variable declaration to create.</param>
        /// <param name="variableDataType">The type of data the new variable declaration to create represents.</param>
        /// <param name="variableName">The name of the new variable declaration to create.</param>
        /// <param name="modifierFlags">The modifier flags of the new variable declaration to create.</param>
        /// <param name="isExposed">Whether the variable is exposed externally or not.</param>
        /// <param name="group">The group in which the variable is added. If null, it will go to the root group.</param>
        /// <param name="indexInGroup">The index of the variable in the group.</param>
        /// <param name="initializationModel">The initialization model of the new variable declaration to create. Can be <code>null</code>.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the variable declaration is created.</param>
        /// <param name="spawnFlags">The flags specifying how the variable declaration is to be spawned.</param>
        /// <returns>The newly created variable declaration.</returns>
        IVariableDeclarationModel CreateGraphVariableDeclaration(Type variableTypeToCreate, TypeHandle variableDataType, string variableName,
            ModifierFlags modifierFlags, bool isExposed, IGroupModel group = null, int indexInGroup = -1, IConstant initializationModel = null, SerializableGUID guid = default,
            Action<IVariableDeclarationModel, IConstant> initializationCallback = null, SpawnFlags spawnFlags = SpawnFlags.Default);

        /// <summary>
        /// Deletes the given variable declaration models, with the option of also deleting the corresponding variable models.
        /// </summary>
        /// <remarks>If <paramref name="deleteUsages"/> is <c>false</c>, the user has to take care of deleting the corresponding variable models prior to this call.</remarks>
        /// <param name="variableModels">The variable declaration models to delete.</param>
        /// <param name="deleteUsages">Whether or not to delete the corresponding variable models.</param>
        /// <returns>The list of deleted models.</returns>
        IReadOnlyCollection<IGraphElementModel> DeleteVariableDeclarations(IReadOnlyCollection<IVariableDeclarationModel> variableModels, bool deleteUsages = true);

        /// <summary>
        /// Creates a new group.
        /// </summary>
        /// <param name="title">The title of the new group.</param>
        /// <returns>A new group.</returns>
        IGroupModel CreateVariableGroup(string title);

        /// <summary>
        /// Deletes the given variable group models.
        /// </summary>
        /// <param name="variableGroupModels">The variable group models to delete.</param>
        /// <returns>The list of deleted models.</returns>
        IReadOnlyCollection<IGraphElementModel> DeleteVariableGroups(IReadOnlyCollection<IGroupModel> variableGroupModels);

        /// <summary>
        /// Duplicates a variable declaration.
        /// </summary>
        /// <param name="sourceModel">The variable declaration to duplicate.</param>
        /// <returns>The duplicated variable declaration.</returns>
        TDeclType DuplicateGraphVariableDeclaration<TDeclType>(TDeclType sourceModel)
            where TDeclType : IVariableDeclarationModel;

        /// <summary>
        /// Creates a new declaration model representing a portal and optionally add it to the graph.
        /// </summary>
        /// <param name="portalName">The name of the portal</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="spawnFlags">The flags specifying how the portal is to be spawned.</param>
        /// <returns>The newly created declaration model</returns>
        IDeclarationModel CreateGraphPortalDeclaration(string portalName, SerializableGUID guid = default, SpawnFlags spawnFlags = SpawnFlags.Default);
        IEdgePortalModel CreateOppositePortal(IEdgePortalModel edgePortalModel, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default);
        IEdgePortalEntryModel CreateEntryPortalFromEdge(IEdgeModel edgeModel);
        IEdgePortalExitModel CreateExitPortalFromEdge(IEdgeModel edgeModel);

        /// <summary>
        /// Creates a new variable node in the graph.
        /// </summary>
        /// <param name="declarationModel">The declaration for the variable.</param>
        /// <param name="position">The position of the node to create.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <returns>The newly created variable node.</returns>
        IVariableNodeModel CreateVariableNode(IVariableDeclarationModel declarationModel, Vector2 position,
            SerializableGUID guid = default, SpawnFlags spawnFlags = SpawnFlags.Default);

        /// <summary>
        /// Creates a new constant node in the graph.
        /// </summary>
        /// <param name="constantTypeHandle">The type of the new constant node to create.</param>
        /// <param name="constantName">The name of the constant node to create.</param>
        /// <param name="position">The position of the node to create.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the constant node is created.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <returns>The newly created constant node.</returns>
        IConstantNodeModel CreateConstantNode(TypeHandle constantTypeHandle, string constantName, Vector2 position,
            SerializableGUID guid = default, Action<IConstantNodeModel> initializationCallback = null, SpawnFlags spawnFlags = SpawnFlags.Default);

        /// <summary>
        /// Creates a new subgraph node in the graph.
        /// </summary>
        /// <param name="referenceGraphAsset">The Graph Asset Model of the reference graph.</param>
        /// <param name="position">The position of the node to create.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <returns>The newly created subgraph node.</returns>
        ISubgraphNodeModel CreateSubgraphNode(GraphAssetModel referenceGraphAsset, Vector2 position, SerializableGUID guid = default, SpawnFlags spawnFlags = SpawnFlags.Default);

        /// <summary>
        /// Creates a new node in the graph.
        /// </summary>
        /// <param name="nodeTypeToCreate">The type of the new node to create.</param>
        /// <param name="nodeName">The name of the node to create.</param>
        /// <param name="position">The position of the node to create.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the node is created.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <returns>The newly created node.</returns>
        INodeModel CreateNode(Type nodeTypeToCreate, string nodeName, Vector2 position,
            SerializableGUID guid = default, Action<INodeModel> initializationCallback = null, SpawnFlags spawnFlags = SpawnFlags.Default);
        INodeModel DuplicateNode(INodeModel sourceNode, Vector2 delta);
        IReadOnlyCollection<IGraphElementModel> DeleteNodes(IReadOnlyCollection<INodeModel> nodeModels, bool deleteConnections);

        /// <summary>
        /// Duplicates variable or constant modes connected to multiple ports so there is a single node per edge.
        /// </summary>
        /// <param name="nodeOffset">The offset to apply to the position of the duplicated node.</param>
        /// <param name="outputPortModel">The output port of the node to duplicate.</param>
        /// <returns>The newly itemized node.</returns>
        IInputOutputPortsNodeModel CreateItemizedNode(int nodeOffset, ref IPortModel outputPortModel);

        /// <summary>
        /// Creates an edge and add it to the graph.
        /// </summary>
        /// <param name="toPort">The port from which the edge originates.</param>
        /// <param name="fromPort">The port to which the edge goes.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <returns>The newly created edge</returns>
        IEdgeModel CreateEdge(IPortModel toPort, IPortModel fromPort, SerializableGUID guid = default);
        IEdgeModel DuplicateEdge(IEdgeModel sourceEdge, INodeModel targetInputNode, INodeModel targetOutputNode);
        IReadOnlyCollection<IGraphElementModel> DeleteEdges(IReadOnlyCollection<IEdgeModel> edgeModels);

        /// <summary>
        /// Adds a badge to the graph.
        /// </summary>
        /// <param name="badgeModel">The badge to add.</param>
        void AddBadge(IBadgeModel badgeModel);
        IReadOnlyCollection<IGraphElementModel> DeleteBadges();
        IReadOnlyCollection<IGraphElementModel> DeleteBadgesOfType<T>() where T : IBadgeModel;

        /// <summary>
        /// Creates a new sticky note and optionally add it to the graph.
        /// </summary>
        /// <param name="position">The position of the sticky note to create.</param>
        /// <param name="spawnFlags">The flags specifying how the sticky note is to be spawned.</param>
        /// <returns>The newly created sticky note</returns>
        IStickyNoteModel CreateStickyNote(Rect position, SpawnFlags spawnFlags = SpawnFlags.Default);
        IReadOnlyCollection<IGraphElementModel> DeleteStickyNotes(IReadOnlyCollection<IStickyNoteModel> stickyNotesModels);

        /// <summary>
        /// Creates a new placemat and optionally add it to the graph.
        /// </summary>
        /// <param name="position">The position of the placemat to create.</param>
        /// <param name="spawnFlags">The flags specifying how the sticky note is to be spawned.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <returns>The newly created placemat</returns>
        IPlacematModel CreatePlacemat(Rect position, SerializableGUID guid = default, SpawnFlags spawnFlags = SpawnFlags.Default);
        IReadOnlyCollection<IGraphElementModel> DeletePlacemats(IReadOnlyCollection<IPlacematModel> placematModels);

        /// <summary>
        /// Registers an element so that the GraphModel can find it through its GUID.
        /// </summary>
        /// <param name="model">The model.</param>
        void RegisterElement(IGraphElementModel model);

        /// <summary>
        /// Unregisters an element so that the GraphModel can no longer find it through its GUID.
        /// </summary>
        /// <param name="model">The model.</param>
        void UnregisterElement(IGraphElementModel model);

        /// <summary>
        /// Gets a list of ports that can be connected to <paramref name="startPortModel"/>.
        /// </summary>
        /// <param name="portModels">The list of candidate ports.</param>
        /// <param name="startPortModel">The port to which the connection originates (can be an input or output port).</param>
        /// <returns>A list of ports that can be connected to <paramref name="startPortModel"/>.</returns>
        List<IPortModel> GetCompatiblePorts(List<IPortModel> portModels, IPortModel startPortModel);

        bool CheckIntegrity(Verbosity errors);

        void UndoRedoPerformed();
        void CloneGraph(IGraphModel sourceGraphModel);

        /// <summary>
        /// Returns the model for a given section name, creating it if it doesn't exist.
        /// </summary>
        /// <param name="sectionName">The section to use.</param>
        /// <returns>The model for a given section name.</returns>
        ISectionModel GetSectionModel(string sectionName = "");

    }
}
