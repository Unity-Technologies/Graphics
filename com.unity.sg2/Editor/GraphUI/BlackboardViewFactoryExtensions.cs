using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [GraphElementsExtensionMethodsCache(typeof(BlackboardView))]
    static class BlackboardViewFactoryExtensions
    {
        public static ModelView CreateSGVariableDeclarationModelView(this ElementBuilder elementBuilder, SGVariableDeclarationModel model)
        {
            ModelView ui;

            if (elementBuilder.Context == BlackboardCreationContext.VariablePropertyCreationContext)
            {
                ui = new SGBlackboardVariablePropertyView();
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
