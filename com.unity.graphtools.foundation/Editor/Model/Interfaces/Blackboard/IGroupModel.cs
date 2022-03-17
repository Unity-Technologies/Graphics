using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The model for a blackboard group.
    /// </summary>
    public interface IGroupModel : IGroupItemModel, IGraphElementContainer
    {
        /// <summary>
        /// The items in this group.
        /// </summary>
        IReadOnlyList<IGroupItemModel> Items { get; }

        /// <summary>
        /// Inserts an item at the given index.
        /// </summary>
        /// <param name="itemModel">The item.</param>
        /// <param name="index">The index at which insert the item. For index &lt;= 0, The item will be added at the beginning. For index &gt;= Items.Count, items will be added at the end./</param>
        /// <returns>The graph elements changed by this method.</returns>
        IEnumerable<IGraphElementModel> InsertItem(IGroupItemModel itemModel, int index = int.MaxValue);

        /// <summary>
        /// Moves some items to this group.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="insertAfter">The item after which the new items will be added. To add the items at the end, pass null.</param>
        /// <returns>The graph elements changed by this method.</returns>
        IEnumerable<IGraphElementModel> MoveItemsAfter(IReadOnlyList<IGroupItemModel> items, IGroupItemModel insertAfter);

        /// <summary>
        /// Removes an item from the group.
        /// </summary>
        /// <param name="itemModel">The item.</param>
        /// <returns>The graph elements changed by this method.</returns>
        IEnumerable<IGraphElementModel> RemoveItem(IGroupItemModel itemModel);
    }
}
