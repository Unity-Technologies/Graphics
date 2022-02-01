using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Overdrive.Plugins.Debugging;
using UnityEngine;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base implementation for <see cref="IStencil"/>.
    /// </summary>
    public abstract class Stencil : IStencil
    {
        ITypeMetadataResolver m_TypeMetadataResolver;

        List<ITypeMetadata> m_AssembliesTypes;

        protected IToolbarProvider m_ToolbarProvider;
        protected ISearcherDatabaseProvider m_SearcherDatabaseProvider;

        protected DebugInstrumentationHandler m_DebugInstrumentationHandler;

        /// <inheritdoc />
        public IGraphModel GraphModel { get; set; }

        /// <inheritdoc />
        public virtual bool AllowMultipleDataOutputInstances => false;

        public virtual IEnumerable<Type> EventTypes => Enumerable.Empty<Type>();

        /// <inheritdoc />
        public ITypeMetadataResolver TypeMetadataResolver => m_TypeMetadataResolver ??= new TypeMetadataResolver();

        public virtual IGraphProcessor CreateGraphProcessor()
        {
            return new NoOpGraphProcessor();
        }

        public virtual IToolbarProvider GetToolbarProvider()
        {
            return m_ToolbarProvider ??= new ToolbarProvider();
        }

        public virtual List<ITypeMetadata> GetAssembliesTypesMetadata()
        {
            if (m_AssembliesTypes != null)
                return m_AssembliesTypes;

            m_AssembliesTypes = new List<ITypeMetadata>();
            return m_AssembliesTypes;
        }

        [CanBeNull]
        public virtual ISearcherFilterProvider GetSearcherFilterProvider()
        {
            return null;
        }

        /// <summary>
        /// Gets the <see cref="IGTFSearcherAdapter"/> used to search for elements.
        /// </summary>
        /// <param name="graphModel">The graph where to search for elements.</param>
        /// <param name="title">The title to display when searching.</param>
        /// <param name="toolName">The name of the tool requesting the searcher, for display purposes.</param>
        /// <param name="contextPortModel">The ports used for the search, if any.</param>
        /// <returns></returns>
        [CanBeNull]
        public virtual IGTFSearcherAdapter GetSearcherAdapter(IGraphModel graphModel, string title, string toolName, IEnumerable<IPortModel> contextPortModel = null)
        {
            return new GraphNodeSearcherAdapter(graphModel, title, toolName);
        }

        public virtual ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return m_SearcherDatabaseProvider ??= new DefaultSearcherDatabaseProvider(this);
        }

        public virtual void OnGraphProcessingStarted(IGraphModel graphModel) {}
        public virtual void OnGraphProcessingSucceeded(IGraphModel graphModel, GraphProcessingResult results) {}
        public virtual void OnGraphProcessingFailed(IGraphModel graphModel, GraphProcessingResult results) {}

        public virtual IEnumerable<IPluginHandler> GetGraphProcessingPluginHandlers(GraphProcessingOptions getGraphProcessingOptions)
        {
            if (getGraphProcessingOptions.HasFlag(GraphProcessingOptions.Tracing))
            {
                if (m_DebugInstrumentationHandler == null)
                    m_DebugInstrumentationHandler = new DebugInstrumentationHandler();

                yield return m_DebugInstrumentationHandler;
            }
        }

        public virtual DebugInstrumentationHandler GetDebugInstrumentationHandler()
        {
            return m_DebugInstrumentationHandler;
        }

        public virtual bool RequiresInitialization(IVariableDeclarationModel decl) => decl.RequiresInitialization();

        /// <inheritdoc />
        public bool RequiresInspectorInitialization(IVariableDeclarationModel decl) => decl.RequiresInspectorInitialization();

        // PF: To preference
        public virtual bool MoveNodeDependenciesByDefault => false;

        public virtual IDebugger Debugger => null;

        /// <inheritdoc />
        public virtual Type GetConstantNodeValueType(TypeHandle typeHandle)
        {
            return null;
        }

        public virtual TypeHandle GetSubgraphNodeTypeHandle()
        {
            return TypeHandle.Subgraph;
        }

        /// <inheritdoc />
        public virtual IConstant CreateConstantValue(TypeHandle constantTypeHandle)
        {
            var nodeType = GetConstantNodeValueType(constantTypeHandle);
            var instance = (IConstant)Activator.CreateInstance(nodeType);
            instance.ObjectValue = instance.DefaultValue;
            return instance;
        }

        public virtual void CreateNodesFromPort(IModelView view, Preferences preferences, IGraphModel graphModel, IPortModel portModel, Vector2 localPosition, Vector2 worldPosition,
            IReadOnlyList<IEdgeModel> edgesToDelete)
        {
            Action<GraphNodeModelSearcherItem> createNode = item =>
                view.Dispatch(CreateNodeCommand.OnPort(item, portModel, localPosition));
            switch (portModel.Direction)
            {
                case PortDirection.Output:
                    SearcherService.ShowOutputToGraphNodes(this, view.GraphTool.Name, preferences, graphModel, portModel, worldPosition, createNode);
                    break;

                case PortDirection.Input:
                    SearcherService.ShowInputToGraphNodes(this, view.GraphTool.Name, preferences, graphModel, Enumerable.Repeat(portModel, 1), worldPosition, createNode);
                    break;
            }
        }

        public virtual void CreateNodesFromPort(IModelView view, Preferences preferences, IGraphModel graphModel, IReadOnlyList<IPortModel> portModels, Vector2 localPosition, Vector2 worldPosition,
            IReadOnlyList<IEdgeModel> edgesToDelete)
        {
            if (portModels.Count > 1)
                Debug.LogWarning("Unhandled node creation on multiple ports");

            Action<GraphNodeModelSearcherItem> createNode = item =>
            {
                view.Dispatch(CreateNodeCommand.OnPort(item, portModels.First(), localPosition));
            };
            switch (portModels.First().Direction)
            {
                case PortDirection.Output:
                    SearcherService.ShowOutputToGraphNodes(this, view.GraphTool.Name, preferences, graphModel, portModels, worldPosition, createNode);
                    break;

                case PortDirection.Input:
                    SearcherService.ShowInputToGraphNodes(this, view.GraphTool.Name, preferences, graphModel, portModels, worldPosition, createNode);
                    break;
            }
        }

        public virtual void PreProcessGraph(IGraphModel graphModel)
        {
        }

        /// <inheritdoc />
        public virtual IEnumerable<INodeModel> GetEntryPoints()
        {
            return Enumerable.Empty<INodeModel>();
        }

        /// <inheritdoc />
        public virtual bool CreateDependencyFromEdge(IEdgeModel edgeModel, out LinkedNodesDependency linkedNodesDependency, out INodeModel parentNodeModel)
        {
            linkedNodesDependency = new LinkedNodesDependency
            {
                DependentPort = edgeModel.FromPort,
                ParentPort = edgeModel.ToPort,
            };
            parentNodeModel = edgeModel.ToPort.NodeModel;

            return true;
        }

        /// <inheritdoc />
        public virtual IEnumerable<IEdgePortalModel> GetPortalDependencies(IEdgePortalModel portalModel)
        {
            if (portalModel is IEdgePortalEntryModel edgePortalModel)
            {
                return edgePortalModel.GraphModel.FindReferencesInGraph<IEdgePortalExitModel>(edgePortalModel.DeclarationModel);
            }

            return Enumerable.Empty<IEdgePortalModel>();
        }

        /// <inheritdoc />
        public virtual IEnumerable<IEdgePortalModel> GetLinkedPortals(IEdgePortalModel portalModel)
        {
            if (portalModel != null)
            {
                return portalModel.GraphModel.FindReferencesInGraph<IEdgePortalModel>(portalModel.DeclarationModel);
            }

            return Enumerable.Empty<IEdgePortalModel>();
        }

        public virtual void OnInspectorGUI()
        {}

        /// <summary>
        /// Authorizes the creation of a variable in the graph or not.
        /// </summary>
        /// <param name="variable">The variable to create in the graph.</param>
        /// <param name="graphModel">The graph in which to create the variable.</param>
        /// <returns><c>true</c> if the variable can be created, <c>false</c> otherwise.</returns>
        public virtual bool CanCreateVariableInGraph(IVariableDeclarationModel variable, IGraphModel graphModel)
        {
            var allowMultipleDataOutputInstances = AllowMultipleDataOutputInstances;

            return allowMultipleDataOutputInstances
                   || variable.Modifiers != ModifierFlags.WriteOnly
                   || variable.IsInputOrOutputTrigger()
                   || !graphModel.FindReferencesInGraph<IVariableNodeModel>(variable).Any();
        }

        /// <inheritdoc />
        public virtual bool GetPortCapacity(IPortModel portModel, out PortCapacity capacity)
        {
            capacity = default;
            return false;
        }

        /// <inheritdoc />
        public virtual bool CanPasteNode(INodeModel originalModel, IGraphModel graph) => true;

        public virtual string GetNodeDocumentation(SearcherItem node, IGraphElementModel model) =>
            null;

        /// <summary>
        /// Converts a <see cref="GraphProcessingError"/> to a <see cref="IGraphProcessingErrorModel"/>.
        /// </summary>
        /// <param name="error">The error to convert.</param>
        /// <returns>The converted error.</returns>
        public virtual IGraphProcessingErrorModel CreateProcessingErrorModel(GraphProcessingError error)
        {
            if (error.SourceNode != null && !error.SourceNode.Destroyed)
                return new GraphProcessingErrorModel(error);

            return null;
        }

        /// <inheritdoc />
        public abstract IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel);

        /// <summary>
        /// Populates the given <paramref name="menu"/> with the section to create variable declaration models for a blackboard.
        /// </summary>
        /// <param name="sectionName">The name of the section in which the menu is added.</param>
        /// <param name="menu">The menu to fill.</param>
        /// <param name="view">The view tasked with dispatching the creation command.</param>
        /// <param name="graphModel">The graph model.</param>
        /// <param name="selectedGroup">The currently selection variable group model.</param>
        public virtual void PopulateBlackboardCreateMenu(string sectionName, GenericMenu menu, IModelView view, IGraphModel graphModel, IGroupModel selectedGroup = null)
        {
            menu.AddItem(EditorGUIUtility.TrTextContent("Create Group"), false, () =>
            {
                view.Dispatch(new BlackboardGroupCreateCommand(selectedGroup ?? GraphModel.GetSectionModel(sectionName)));
            });

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Create Variable"), false, () =>
            {
                view.Dispatch(new CreateGraphVariableDeclarationCommand("variable", true, TypeHandle.Float, selectedGroup ?? GraphModel.GetSectionModel(sectionName)));
            });
        }

        public virtual string GetVariableSection(IVariableDeclarationModel variable)
        {
            return SectionNames.First();
        }

        /// <inheritdoc />
        public virtual IEnumerable<string> SectionNames =>
            new List<string>() { "Graph Variables" };
    }
}
