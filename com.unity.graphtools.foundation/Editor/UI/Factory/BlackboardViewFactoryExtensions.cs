using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [GraphElementsExtensionMethodsCache(typeof(BlackboardView), GraphElementsExtensionMethodsCacheAttribute.lowestPriority)]
    public static class BlackboardViewFactoryExtensions
    {
        /// <summary>
        /// Creates the appropriate <see cref="IModelView"/> for the given <see cref="IVariableDeclarationModel"/>.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The <see cref="IVariableDeclarationModel"/> for which an <see cref="IModelView"/> is required.</param>
        /// <returns>A <see cref="IModelView"/> for the given <see cref="IVariableDeclarationModel"/>.</returns>
        public static IModelView CreateVariableDeclarationModelView(this ElementBuilder elementBuilder, IVariableDeclarationModel model)
        {
            IModelView ui;

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
        /// <param name="model">The <see cref="IBlackboardGraphModel"/> this <see cref="IModelView"/> will display.</param>
        /// <returns>A setup <see cref="IModelView"/>.</returns>
        public static IModelView CreateBlackboard(this ElementBuilder elementBuilder, IBlackboardGraphModel model)
        {
            var ui = new Blackboard();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        /// <summary>
        /// Creates a <see cref="BlackboardGroup"/> from its model.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The <see cref="IGroupModel"/> this <see cref="IModelView"/> will display.</param>
        /// <returns>A setup <see cref="IModelView"/>.</returns>
        public static IModelView CreateGroup(this ElementBuilder elementBuilder, IGroupModel model)
        {
            ModelView ui = new BlackboardGroup();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        /// <summary>
        /// Creates a <see cref="BlackboardSection"/> for the given model.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The <see cref="ISectionModel"/> this <see cref="IModelView"/> will display.</param>
        /// <returns>A setup <see cref="IModelView"/>.</returns>
        public static IModelView CreateSection(this ElementBuilder elementBuilder, ISectionModel model)
        {
            ModelView ui = new BlackboardSection();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }
    }
}
