using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [GraphElementsExtensionMethodsCache(typeof(BlackboardView))]
    static class BlackboardViewFactoryExtensions
    {
        public static ModelView CreateGraphDataVariableDeclarationModelView(this ElementBuilder elementBuilder, GraphDataVariableDeclarationModel model)
        {
            ModelView ui;

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
