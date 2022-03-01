namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A default implementation for <see cref="ISectionModel"/>.
    /// </summary>
    public class SectionModel : GroupModel, ISectionModel
    {
        /// <summary>
        /// Returns whether a Section model can receive the given variable group item.
        /// </summary>
        /// <param name="itemModel">The item.</param>
        /// <returns>Whether a Section model can receive the given variable group item.</returns>
        public virtual bool AcceptsDraggedModel(IGroupItemModel itemModel)
        {
            return itemModel.GetSection() == this;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SectionModel" /> class.
        /// </summary>
        public SectionModel()
        {
            this.SetCapability(Overdrive.Capabilities.Deletable, false);
            this.SetCapability(Overdrive.Capabilities.Droppable, false);
            this.SetCapability(Overdrive.Capabilities.Selectable, false);
            this.SetCapability(Overdrive.Capabilities.Renamable, false);
        }

        public override IGraphElementContainer Container => GraphModel;
    }
}
