namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for node models that own a <see cref="IDeclarationModel"/>.
    /// </summary>
    public interface IHasDeclarationModel : INodeModel
    {
        /// <summary>
        /// The declaration model.
        /// </summary>
        IDeclarationModel DeclarationModel { get; set; }
    }
}
