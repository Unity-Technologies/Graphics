using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [GraphElementsExtensionMethodsCache(typeof(BlackboardView))]
    public static class BlackboardViewFactoryExtensions
    {
        public static IModelView CreateGraphDataVariableDeclarationModelView(this ElementBuilder elementBuilder, GraphDataVariableDeclarationModel model)
        {
            IModelView ui;

            if (elementBuilder.Context == BlackboardCreationContext.VariablePropertyCreationContext)
            {
                ui = new GraphDataBlackboardVariablePropertyView();
                ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            }
            else
            {
                ui = elementBuilder.CreateVariableDeclarationModelView(model);
            }

            return ui;
        }
    }
}
