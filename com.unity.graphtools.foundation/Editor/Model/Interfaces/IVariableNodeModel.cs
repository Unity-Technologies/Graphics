using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for variable nodes.
    /// </summary>
    public interface IVariableNodeModel : ISingleInputPortNodeModel, ISingleOutputPortNodeModel, IHasDeclarationModel, IHasTitle
    {
        /// <summary>
        /// The <see cref="IVariableDeclarationModel"/> associated with this node.
        /// </summary>
        IVariableDeclarationModel VariableDeclarationModel { get; }

        /// <summary>
        /// Updates the port type from the variable declaration type.
        /// </summary>
        void UpdateTypeFromDeclaration();
    }

    /// <summary>
    /// Extension methods for <see cref="IVariableNodeModel"/>.
    /// </summary>
    public static class IVariableNodeModelExtensions
    {
        /// <summary>
        /// Gets the data type of the variable node.
        /// </summary>
        /// <param name="self">The variable node for which to get the variable type.</param>
        /// <returns>The type of the variable declaration associated with this node, or <see cref="TypeHandle.Unknown"/> if there is none.</returns>
        public static TypeHandle GetDataType(this IVariableNodeModel self) =>
            self.VariableDeclarationModel?.DataType ?? TypeHandle.Unknown;
    }
}
