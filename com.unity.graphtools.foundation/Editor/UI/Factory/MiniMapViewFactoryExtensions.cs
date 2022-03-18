using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [GraphElementsExtensionMethodsCache(typeof(MiniMapView), GraphElementsExtensionMethodsCacheAttribute.lowestPriority)]
    public static class MiniMapViewFactoryExtensions
    {
        /// <summary>
        /// Creates a MiniMap from for the given model.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The <see cref="IGraphModel"/> this <see cref="IModelView"/> will display.</param>
        /// <returns>A setup <see cref="IModelView"/>.</returns>
        public static IModelView CreateMiniMap(this ElementBuilder elementBuilder, IGraphModel model)
        {
            IModelView ui = new MiniMap();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }
    }
}
