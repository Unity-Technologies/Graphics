using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods for <see cref="IGraphElementModel"/>.
    /// </summary>
    public static class GraphElementModelExtensions
    {
        /// <summary>
        /// Test if this model has a capability.
        /// </summary>
        /// <param name="self">Element model to test</param>
        /// <param name="capability">Capability to check for</param>
        /// <returns>true if the model has the capability, false otherwise</returns>
        public static bool HasCapability(this IGraphElementModel self, Capabilities capability)
        {
            for (var i = 0; i < self.Capabilities.Count; i++)
            {
                if (capability.Id == self.Capabilities[i].Id)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Set a capability for a model.
        /// </summary>
        /// <param name="self">Element model to affect</param>
        /// <param name="capability">Capability to set</param>
        /// <param name="active">true to set the capability, false to remove it</param>
        public static void SetCapability(this IGraphElementModel self, Capabilities capability, bool active)
        {
            if (!(self.Capabilities is IList<Capabilities> capabilities))
                return;

            if (active)
            {
                if (!self.HasCapability(capability))
                    capabilities.Add(capability);
            }
            else
            {
                capabilities.Remove(capability);
            }
        }

        /// <summary>
        /// Remove all capabilities from a model.
        /// </summary>
        /// <param name="self">The model to remove capabilites from</param>
        public static void ClearCapabilities(this IGraphElementModel self)
        {
            if (self.Capabilities is List<Capabilities> capabilities)
            {
                capabilities.Clear();
            }
        }

        /// <summary>
        /// Test if a model has the capability to be selected.
        /// </summary>
        /// <param name="self">Model to test.</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsSelectable(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Selectable);
        }

        /// <summary>
        /// Test if a model has the capability to be collapsed.
        /// </summary>
        /// <param name="self">Model to test.</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsCollapsible(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Collapsible);
        }

        /// <summary>
        /// Test if a model has the capability to be resized.
        /// </summary>
        /// <param name="self">Model to test.</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsResizable(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Resizable);
        }

        /// <summary>
        /// Tests if a model has the capability to be moved.
        /// </summary>
        /// <param name="self">Model to test.</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsMovable(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Movable);
        }

        /// <summary>
        /// Tests if a model has the capability to be deleted.
        /// </summary>
        /// <param name="self">Model to test.</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsDeletable(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Deletable);
        }

        /// <summary>
        /// Tests if a model has the capability to be dropped.
        /// </summary>
        /// <param name="self">Model to test.</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsDroppable(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Droppable);
        }

        /// <summary>
        /// Tests if a model has the capability to be renamed.
        /// </summary>
        /// <param name="self">Model to test.</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsRenamable(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Renamable);
        }

        /// <summary>
        /// Tests if a model has the capability to be copied.
        /// </summary>
        /// <param name="self">Model to test.</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsCopiable(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Copiable);
        }

        /// <summary>
        /// Tests if a model has the capability to change color.
        /// </summary>
        /// <param name="self">Model to test.</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsColorable(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Colorable);
        }

        /// <summary>
        /// Tests if a model has the capability to be ascended.
        /// </summary>
        /// <param name="self">Model to test.</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsAscendable(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Ascendable);
        }

        /// <summary>
        /// Test if a model needs a container to be used.
        /// </summary>
        /// <param name="self">Model to test.</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool NeedsContainer(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.NeedsContainer);
        }

        /// <summary>
        /// Returns the ZOrder of the model in the graph
        /// </summary>
        /// <param name="self">The model for which to find the Z order.</param>
        /// <typeparam name="T">The type of the model.</typeparam>
        /// <returns>The index of the model in its list, if found. -1 otherwise.</returns>
        public static int GetZOrder<T>(this T self) where T : class, IGraphElementModel
        {
            var list = (List<T>)self.GraphModel.GetListOf<T>();
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] == self)
                    return i;
            }

            return -1;
        }
    }
}
