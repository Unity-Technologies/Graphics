namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Data to be displayed by a badge.
    /// </summary>
    public interface IBadgeModel : IGraphElementModel
    {
        /// <summary>
        /// The model to which the badge is attached.
        /// </summary>
        IGraphElementModel ParentModel { get; }
    }
}
