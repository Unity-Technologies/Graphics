namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The model for a blackboard section.
    /// </summary>
    public interface ISectionModel : IGroupModel
    {
        /// <summary>
        /// Returns whether the given item can be dragged in this section.
        /// </summary>
        /// <param name="itemModel">The item.</param>
        /// <returns>Whether the given item can be dragged in this section.</returns>
        bool AcceptsDraggedModel(IGroupItemModel itemModel);
    }
}
