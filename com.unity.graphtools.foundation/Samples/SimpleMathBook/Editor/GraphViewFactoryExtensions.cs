using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook.UI;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [GraphElementsExtensionMethodsCache(typeof(GraphView))]
    public static class GraphViewFactoryExtensions
    {
        public static IModelUI CreateNode(this ElementBuilder elementBuilder, MathOperator model)
        {
            IModelUI ui = new VariableInputNode();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateMathResultUI(this ElementBuilder elementBuilder, MathResult model)
        {
            var ui = new MathResultUI();

            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateMathBookVariableDeclarationModelUI(this ElementBuilder elementBuilder, MathBookVariableDeclarationModel model)
        {
            IModelUI ui;

            if (elementBuilder.Context == BlackboardCreationContext.VariablePropertyCreationContext)
            {
                ui = new MathbookBBVarPropertyView();
                ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            }
            else
            {
                ui = Overdrive.GraphViewFactoryExtensions.CreateVariableDeclarationModelUI(elementBuilder, model);
            }

            return ui;
        }
    }
}
