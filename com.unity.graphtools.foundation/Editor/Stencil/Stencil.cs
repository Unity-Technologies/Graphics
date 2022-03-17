using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base implementation for <see cref="IStencil"/>.
    /// </summary>
    public abstract class Stencil : IStencil
    {
        static readonly IReadOnlyDictionary<string, string> k_NoCategoryStyle = new Dictionary<string, string>();

        ITypeMetadataResolver m_TypeMetadataResolver;

        List<ITypeMetadata> m_AssembliesTypes;

        protected ISearcherDatabaseProvider m_SearcherDatabaseProvider;

        GraphProcessorContainer m_GraphProcessorContainer;

        /// <inheritdoc />
        public IGraphModel GraphModel { get; set; }

        /// <inheritdoc />
        public virtual bool AllowMultipleDataOutputInstances => false;

        public virtual IEnumerable<Type> EventTypes => Enumerable.Empty<Type>();

        /// <inheritdoc />
        public ITypeMetadataResolver TypeMetadataResolver => m_TypeMetadataResolver ??= new TypeMetadataResolver();

        protected virtual IReadOnlyDictionary<string, string> CategoryPathStyleNames => k_NoCategoryStyle;

        /// <summary>
        /// Extra stylesheet(s) to load when displaying the searcher.
        /// </summary>
        /// <remarks>(FILENAME)_dark.uss and (FILENAME)_light.uss will be loaded as well if existing.</remarks>
        protected virtual string CustomSearcherStylesheetPath => null;

        /// <inheritdoc />
        public virtual IEnumerable<string> SectionNames { get; } = new List<string>() { "Graph Variables" };

        /// <summary>
        /// Gets the graph processor container.
        /// </summary>
        /// <returns>The graph processor container.</returns>
        public GraphProcessorContainer GetGraphProcessorContainer()
        {
            if (m_GraphProcessorContainer == null)
            {
                m_GraphProcessorContainer = new GraphProcessorContainer();
                CreateGraphProcessors();
            }

            return m_GraphProcessorContainer;
        }

        protected virtual void CreateGraphProcessors()
        {
            if (!AllowMultipleDataOutputInstances)
                GetGraphProcessorContainer().AddGraphProcessor(new VariableNodeGraphProcessor());
        }

        // PF FIXME unused.
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
        /// Gets the <see cref="ISearcherAdapter"/> used to search for elements.
        /// </summary>
        /// <param name="graphModel">The graph where to search for elements.</param>
        /// <param name="title">The title to display when searching.</param>
        /// <param name="toolName">The name of the tool requesting the searcher, for display purposes.</param>
        /// <param name="searcherGraphViewType">The type of <see cref="GraphView"/> to use in the preview section of the searcher.</param>
        /// <param name="contextPortModel">The ports used for the search, if any.</param>
        /// <returns></returns>
        [CanBeNull]
        public virtual ISearcherAdapter GetSearcherAdapter(IGraphModel graphModel, string title, string toolName, Type searcherGraphViewType, IEnumerable<IPortModel> contextPortModel = null)
        {
            var adapter = new GraphNodeSearcherAdapter(graphModel, title, toolName, searcherGraphViewType);
            if (adapter is SearcherAdapter searcherAdapter)
            {
                searcherAdapter.CategoryPathStyleNames = CategoryPathStyleNames;
                searcherAdapter.CustomStyleSheetPath = CustomSearcherStylesheetPath;
            }
            return adapter;
        }

        public virtual ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return m_SearcherDatabaseProvider ??= new DefaultSearcherDatabaseProvider(this);
        }

        public virtual void OnGraphProcessingStarted(IGraphModel graphModel) {}
        public virtual void OnGraphProcessingSucceeded(IGraphModel graphModel, GraphProcessingResult results) {}
        public virtual void OnGraphProcessingFailed(IGraphModel graphModel, GraphProcessingResult results) {}

        public virtual bool RequiresInitialization(IVariableDeclarationModel decl) => decl.RequiresInitialization();

        /// <inheritdoc />
        public bool RequiresInspectorInitialization(IVariableDeclarationModel decl) => decl.RequiresInspectorInitialization();

        // PF: To preference
        public virtual bool MoveNodeDependenciesByDefault => false;

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
            instance.Initialize(constantTypeHandle);
            return instance;
        }

        public virtual void CreateNodesFromPort(IRootView view, Preferences preferences, IGraphModel graphModel, IPortModel portModel, Vector2 localPosition, Vector2 worldPosition)
        {
            Action<GraphNodeModelSearcherItem> createNode = item =>
                view.Dispatch(CreateNodeCommand.OnPort(item, portModel, localPosition));
            switch (portModel.Direction)
            {
                case PortDirection.Output:
                    SearcherService.ShowOutputToGraphNodes(this, view.GraphTool.Name, view.GetType(), preferences, graphModel, portModel, worldPosition, createNode, view.Window);
                    break;

                case PortDirection.Input:
                    SearcherService.ShowInputToGraphNodes(this, view.GraphTool.Name, view.GetType(), preferences, graphModel, Enumerable.Repeat(portModel, 1), worldPosition, createNode, view.Window);
                    break;
            }
        }

        public virtual void CreateNodesFromPort(IRootView view, Preferences preferences, IGraphModel graphModel, IReadOnlyList<IPortModel> portModels, Vector2 localPosition, Vector2 worldPosition)
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
                    SearcherService.ShowOutputToGraphNodes(this, view.GraphTool.Name, view.GetType(), preferences, graphModel, portModels, worldPosition, createNode, view.Window);
                    break;

                case PortDirection.Input:
                    SearcherService.ShowInputToGraphNodes(this, view.GraphTool.Name, view.GetType(), preferences, graphModel, portModels, worldPosition, createNode, view.Window);
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
        /// Indicates whether a variable is allowed in the graph or not.
        /// </summary>
        /// <param name="variable">The variable in the graph.</param>
        /// <param name="graphModel">The graph of the variable.</param>
        /// <returns><c>true</c> if the variable is allowed, <c>false</c> otherwise.</returns>
        public virtual bool CanAllowVariableInGraph(IVariableDeclarationModel variable, IGraphModel graphModel)
        {
            var allowMultipleDataOutputInstances = AllowMultipleDataOutputInstances;

            return allowMultipleDataOutputInstances
                   || variable.Modifiers != ModifierFlags.Write
                   || variable.IsInputOrOutputTrigger()
                   || graphModel.FindReferencesInGraph<IVariableNodeModel>(variable).Count() == 1;
        }

        /// <inheritdoc />
        public abstract bool CanPasteNode(INodeModel originalModel, IGraphModel graph);

        /// <inheritdoc />
        public abstract bool CanPasteVariable(IVariableDeclarationModel originalModel, IGraphModel graph);

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

        /// <inheritdoc />
        public abstract IInspectorModel CreateInspectorModel(IModel inspectedModel);

        public class MenuItem
        {
            public string name;
            public Action action;
        }

        /// <summary>
        /// Populates the given <paramref name="menuItems"/> given a section, to create variable declaration models for a blackboard.
        /// </summary>
        /// <param name="sectionName">The name of the section in which the menu is added.</param>
        /// <param name="menuItems">An array of <see cref="MenuItem"/> to fill.</param>
        /// <param name="view">The view.</param>
        /// <param name="selectedGroup">The currently selected group model.</param>
        public virtual void PopulateBlackboardCreateMenu(string sectionName, List<MenuItem> menuItems, IRootView view, IGroupModel selectedGroup = null)
        {
            menuItems.Add(new MenuItem{name ="Create Variable",action = () =>
            {
                view.Dispatch(new CreateGraphVariableDeclarationCommand("variable", true, TypeHandle.Float, selectedGroup ?? GraphModel.GetSectionModel(sectionName)));
            }});
        }

        /// <inheritdoc />
        public virtual string GetVariableSection(IVariableDeclarationModel variable)
        {
            return SectionNames.First();
        }

        /// <inheritdoc />
        public virtual bool CanConvertVariable(IVariableDeclarationModel variable, string sectionName)
        {
            return false;
        }

        /// <inheritdoc />
        public virtual IVariableDeclarationModel ConvertVariable(IVariableDeclarationModel variable, string sectionName)
        {
            return null;
        }

        /// <inheritdoc />
        public virtual bool CanAssignTo(TypeHandle destination, TypeHandle source)
        {
            return destination == TypeHandle.Unknown || source.IsAssignableFrom(destination, this);
        }
    }
}
