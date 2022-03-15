using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook.UI;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [GraphElementsExtensionMethodsCache(typeof(BlackboardView))]
    public static class BlackboardViewFactoryExtensions
    {
        public static IModelView CreateMathBookVariableDeclarationModelView(this ElementBuilder elementBuilder, MathBookVariableDeclarationModel model)
        {
            IModelView ui;

            if (elementBuilder.Context == BlackboardCreationContext.VariablePropertyCreationContext)
            {
                ui = new MathbookBBVarPropertyView();
                ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            }
            else
            {
                ui = Overdrive.BlackboardViewFactoryExtensions.CreateVariableDeclarationModelView(elementBuilder, model);
            }

            return ui;
        }
    }
}
