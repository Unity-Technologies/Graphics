namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for edge portals.
    /// </summary>
    public interface IEdgePortalModel : IHasTitle, IHasDeclarationModel
    {
        /// <summary>
        /// Evaluation order for the portal, when multiple portals are linked together.
        /// </summary>
        int EvaluationOrder { get; }

        /// <summary>
        /// Whether we can create an opposite portal for this portal.
        /// </summary>
        /// <returns>True if we can create an opposite portal for this portal.</returns>
        bool CanCreateOppositePortal();
    }

    /// <summary>
    /// Interface for entry portals.
    /// </summary>
    public interface IEdgePortalEntryModel : IEdgePortalModel, ISingleInputPortNodeModel
    {
    }

    /// <summary>
    /// Interface for exit portals.
    /// </summary>
    public interface IEdgePortalExitModel : IEdgePortalModel, ISingleOutputPortNodeModel
    {
    }
}
