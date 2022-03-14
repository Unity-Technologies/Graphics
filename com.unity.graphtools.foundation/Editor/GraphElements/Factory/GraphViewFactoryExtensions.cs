using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods to create UI for graph element models for the <see cref="GraphView"/>.
    /// </summary>
    /// <remarks>
    /// Extension methods in this class are selected by matching the type of their third parameter to the type
    /// of the graph element model for which we need to instantiate a IModelUI. You can change the UI for a
    /// model by defining new extension methods for <see cref="ElementBuilder"/> in a class having
    /// the <see cref="GraphElementsExtensionMethodsCacheAttribute"/>.
    /// </remarks>
    [GraphElementsExtensionMethodsCache(typeof(GraphView), GraphElementsExtensionMethodsCacheAttribute.lowestPriority)]
    public static class GraphViewFactoryExtensions
    {
        /// <summary>
        /// Creates a context node from its model.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="nodeModel">The IContextNodeModel this IModelUI will display.</param>
        /// <returns>A setup IModelUI</returns>
        public static IModelUI CreateContext(this ElementBuilder elementBuilder, IContextNodeModel nodeModel)
        {
            IModelUI ui = new ContextNode();

            ui.SetupBuildAndUpdate(nodeModel, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        /// <summary>
        /// Creates a block node from its model.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The IBlockNodeModel this IModelUI will display.</param>
        /// <returns>A setup IModelUI</returns>
        public static IModelUI CreateBlock(this ElementBuilder elementBuilder, IBlockNodeModel model)
        {
            IModelUI ui = new BlockNode();

            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateNode(this ElementBuilder elementBuilder, INodeModel model)
        {
            IModelUI ui;

            if (model is ISingleInputPortNodeModel || model is ISingleOutputPortNodeModel)
                ui = new TokenNode();
            else if (model is IPortNodeModel)
                ui = new CollapsibleInOutNode();
            else
                ui = new Node();

            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreatePort(this ElementBuilder elementBuilder, IPortModel model)
        {
            var ui = new Port();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateEdge(this ElementBuilder elementBuilder, IEdgeModel model)
        {
            var ui = new Edge();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateStickyNote(this ElementBuilder elementBuilder, IStickyNoteModel model)
        {
            var ui = new StickyNote();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreatePlacemat(this ElementBuilder elementBuilder, IPlacematModel model)
        {
            var ui = new Placemat();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateEdgePortal(this ElementBuilder elementBuilder, IEdgePortalModel model)
        {
            var ui = new TokenNode();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        /// <summary>
        /// Creates the appropriate IModelUI for the given <see cref="IVariableDeclarationModel"/>.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The <see cref="IVariableDeclarationModel"/> for which an IModelUI is required.</param>
        /// <returns>A IModelUI for the given <see cref="IVariableDeclarationModel"/>.</returns>
        public static IModelUI CreateVariableDeclarationModelUI(this ElementBuilder elementBuilder, IVariableDeclarationModel model)
        {
            IModelUI ui;

            if (elementBuilder.Context == BlackboardCreationContext.VariablePropertyCreationContext)
            {
                ui = new BlackboardVariablePropertyView();
            }
            else if (elementBuilder.Context == BlackboardCreationContext.VariableCreationContext)
            {
                ui = new BlackboardField();
            }
            else
            {
                ui = new BlackboardRow();
            }

            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        /// <summary>
        /// Creates a blackboard from for the given model.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The <see cref="IBlackboardGraphModel"/> this IModelUI will display.</param>
        /// <returns>A setup IModelUI.</returns>
        public static IModelUI CreateBlackboard(this ElementBuilder elementBuilder, IBlackboardGraphModel model)
        {
            var ui = new Blackboard { Windowed = true };
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        /// <summary>
        /// Creates a <see cref="BlackboardVariableGroup"/> from its model.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The <see cref="IGroupModel"/> this IModelUI will display.</param>
        /// <returns>A setup IModelUI.</returns>
        public static IModelUI CreateVariableGroup(this ElementBuilder elementBuilder, IGroupModel model)
        {
            ModelUI ui = new BlackboardVariableGroup();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        /// <summary>
        /// Creates a <see cref="BlackboardSection"/> for the given model.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The <see cref="ISectionModel"/> this IModelUI will display.</param>
        /// <returns>A setup IModelUI.</returns>
        public static IModelUI CreateSection(this ElementBuilder elementBuilder, ISectionModel model)
        {
            ModelUI ui = new BlackboardSection();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateErrorBadgeModelUI(this ElementBuilder elementBuilder, IErrorBadgeModel model)
        {
            var badge = new ErrorBadge();
            badge.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return badge;
        }

        public static IModelUI CreateValueBadgeModelUI(this ElementBuilder elementBuilder, IValueBadgeModel model)
        {
            var badge = new ValueBadge();
            badge.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return badge;
        }

        /// <summary>
        /// Creates a subgraph node from its model.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The SubgraphNodeModel this IModelUI will display.</param>
        /// <returns>A setup IModelUI</returns>
        public static IModelUI CreateSubgraphNodeUI(this ElementBuilder elementBuilder, ISubgraphNodeModel model)
        {
            var ui = new SubgraphNode();

            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }
    }
}
