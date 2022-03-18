using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// An item that can be stored in a group.
    /// </summary>
    public interface IGroupItemModel : IGraphElementModel, IHasTitle
    {
        /// <summary>
        /// The group that contains this item.
        /// </summary>
        IGroupModel ParentGroup { get; set; }

        /// <summary>
        /// This model and the models that this model contains.
        /// </summary>
        IEnumerable<IGraphElementModel> ContainedModels { get; }
    }
}
