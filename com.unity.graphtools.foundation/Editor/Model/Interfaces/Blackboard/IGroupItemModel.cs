namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// An item that can be stored in a variable group.
    /// </summary>
    public interface IGroupItemModel : IGraphElementModel, IHasTitle
    {
        /// <summary>
        /// The group that contains this item.
        /// </summary>
        IGroupModel Group { get; set; }
    }
}
