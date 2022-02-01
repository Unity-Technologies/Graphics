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
        /// <param name="index">The index at which insert the item.</param>
        void InsertItem(IGroupItemModel itemModel, int index = -1);

        /// <summary>
        /// Moves some items to this group.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="insertAfter">The item after which the new items will be added.</param>
        public bool MoveItemsAfter(IReadOnlyList<IGroupItemModel> items, IGroupItemModel insertAfter);

        /// <summary>
        /// Removes an item from the group.
        /// </summary>
        /// <param name="itemModel">The item.</param>
        void RemoveItem(IGroupItemModel itemModel);
    }
}
