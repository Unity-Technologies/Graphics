using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods to help dispatching commands for a placemat.
    /// </summary>
    static class PlacematCommandsExtension
    {
        public static void CollapsePlacemat(this Placemat self, bool value)
        {
            var collapsedModels = value ? self.GatherCollapsedElements() : null;
            self.GraphView.Dispatch(new CollapsePlacematCommand(self.PlacematModel, value, collapsedModels));
        }
    }
}
