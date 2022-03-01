using NUnit.Framework;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Plugins.Debugging
{
    [GraphElementsExtensionMethodsCache(typeof(GraphView), GraphElementsExtensionMethodsCacheAttribute.lowestPriority)]
    static class DebuggingFactoryExtensions
    {
        public static IModelUI CreateGraphProcessingErrorModelUI(this ElementBuilder elementBuilder, GraphProcessingErrorModel model)
        {
            var ui = elementBuilder.CreateErrorBadgeModelUI(model) as GraphElement;
            Assert.IsNotNull(ui);
            if (model.Fix != null)
            {
                Assert.IsNotNull(ui.GraphView?.Dispatcher);
                ui.RegisterCallback<MouseDownEvent>(e => model.Fix.QuickFixAction(ui.GraphView.Dispatcher));
            }
            return ui;
        }
    }
}
