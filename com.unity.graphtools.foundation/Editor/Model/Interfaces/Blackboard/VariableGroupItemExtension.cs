namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods for <see cref="IGroupItemModel"/>.
    /// </summary>
    public static class VariableGroupItemExtension
    {
        /// <summary>
        /// Returns the section for a given item.
        /// </summary>
        /// <param name="itemModel">The item.</param>
        /// <returns>The section for a given item.</returns>
        static public ISectionModel GetSection(this IGroupItemModel itemModel)
        {
            if (itemModel is ISectionModel section)
                return section;
            IGroupItemModel current = itemModel;
            while (current.Group != null)
                current = current.Group;
            return (ISectionModel)current;
        }

        /// <summary>
        /// Returns whether a given item is contained in another given item.
        /// </summary>
        /// <param name="itemModel">The item that might be contained.</param>
        /// <param name="graphElementModel">The container item.</param>
        /// <returns>Whether a given item is contained in another given item.</returns>
        public static bool IsIn(this IGroupItemModel itemModel, IGroupItemModel graphElementModel)
        {
            if (graphElementModel == itemModel) return true;

            IGroupModel group = itemModel.Group;
            while (group != null)
            {
                if (group == graphElementModel)
                    return true;
                group = group.Group;
            }

            return false;
        }
    }
}
