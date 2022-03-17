using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods to create UI for graph element models for the <see cref="GraphView"/>.
    /// </summary>
    /// <remarks>
    /// Extension methods in this class are selected by matching the type of their third parameter to the type
    /// of the graph element model for which we need to instantiate a <see cref="IModelView"/>. You can change the UI for a
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
        /// <param name="nodeModel">The IContextNodeModel this <see cref="IModelView"/> will display.</param>
        /// <returns>A setup <see cref="IModelView"/></returns>
        public static IModelView CreateContext(this ElementBuilder elementBuilder, IContextNodeModel nodeModel)
        {
            IModelView ui = new ContextNode();

            ui.SetupBuildAndUpdate(nodeModel, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        /// <summary>
        /// Creates a block node from its model.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The IBlockNodeModel this <see cref="IModelView"/> will display.</param>
        /// <returns>A setup <see cref="IModelView"/></returns>
        public static IModelView CreateBlock(this ElementBuilder elementBuilder, IBlockNodeModel model)
        {
            IModelView ui = new BlockNode();

            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelView CreateNode(this ElementBuilder elementBuilder, INodeModel model)
        {
            IModelView ui;

            if (model is ISingleInputPortNodeModel || model is ISingleOutputPortNodeModel)
                ui = new TokenNode();
            else if (model is IPortNodeModel)
                ui = new CollapsibleInOutNode();
            else
                ui = new Node();

            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelView CreatePort(this ElementBuilder elementBuilder, IPortModel model)
        {
            var ui = new Port();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelView CreateEdge(this ElementBuilder elementBuilder, IEdgeModel model)
        {
            var ui = new Edge();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelView CreateStickyNote(this ElementBuilder elementBuilder, IStickyNoteModel model)
        {
            var ui = new StickyNote();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelView CreatePlacemat(this ElementBuilder elementBuilder, IPlacematModel model)
        {
            var ui = new Placemat();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelView CreateEdgePortal(this ElementBuilder elementBuilder, IEdgePortalModel model)
        {
            var ui = new TokenNode();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelView CreateErrorBadgeModelView(this ElementBuilder elementBuilder, IErrorBadgeModel model)
        {
            var badge = new ErrorBadge();
            badge.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return badge;
        }

        public static IModelView CreateGraphProcessingErrorBadgeModelView(this ElementBuilder elementBuilder, GraphProcessingErrorModel model)
        {
            var badge = new ErrorBadge();
            badge.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);

            Assert.IsNotNull(badge);
            if (model.Fix != null)
            {
                var contextualMenuManipulator = new ContextualMenuManipulator(e =>
                {
                    e.menu.AppendAction("Fix Error/" + model.Fix.Description,
                        _ => model.Fix.QuickFixAction(elementBuilder.View));
                });
                badge.AddManipulator(contextualMenuManipulator);
            }
            return badge;
        }

        /// <summary>
        /// Creates a subgraph node from its model.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The SubgraphNodeModel this <see cref="IModelView"/> will display.</param>
        /// <returns>A setup <see cref="IModelView"/></returns>
        public static IModelView CreateSubgraphNodeUI(this ElementBuilder elementBuilder, ISubgraphNodeModel model)
        {
            var ui = new SubgraphNode();

            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }
    }
}
