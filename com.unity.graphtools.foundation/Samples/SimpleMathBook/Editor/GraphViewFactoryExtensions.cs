using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook.UI;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [GraphElementsExtensionMethodsCache(typeof(MathBookGraphView))]
    public static class GraphViewFactoryExtensions
    {
        public static IModelView CreateNode(this ElementBuilder elementBuilder, MathOperator model)
        {
            IModelView ui = new VariableInputNode();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }
    }
}
