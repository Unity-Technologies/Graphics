namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model that represents a badge in a graph.
    /// </summary>
    public class BadgeModel : GraphElementModel, IBadgeModel
    {
        /// <inheritdoc />
        public IGraphElementModel ParentModel { get; }

        /// <summary>
        /// Creates a badge linked to a parent model.
        /// </summary>
        /// <param name="parentModel">Parent model of the badge</param>
        public BadgeModel(IGraphElementModel parentModel)
        {
            m_AssetModel = parentModel.AssetModel as GraphAssetModel;
            ParentModel = parentModel;
        }
    }
}
