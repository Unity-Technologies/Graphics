using System;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [Flags]
    public enum ModifierFlags
    {
        None = 0,
        Read = 1 << 0,
        Write = 1 << 1,
        ReadWrite = Read | Write,
    }

    /// <summary>
    /// Interface for variable declarations.
    /// </summary>
    public interface IVariableDeclarationModel : IDeclarationModel, IGroupItemModel
    {
        /// <summary>
        /// The type of the variable.
        /// </summary>
        TypeHandle DataType { get; set; }

        /// <summary>
        /// The read/write modifiers.
        /// </summary>
        ModifierFlags Modifiers { get; set; }

        /// <summary>
        /// A tooltip to show on nodes associated with this variable.
        /// </summary>
        string Tooltip { get; set; }

        /// <summary>
        /// The default value for this variable.
        /// </summary>
        IConstant InitializationModel { get; }

        /// <summary>
        /// Whether the variable is shown in the inspector.
        /// </summary>
        bool IsExposed { get; set; }

        /// <summary>
        /// Gets the name of the variable with non-alphanumeric characters replaced by an underscore.
        /// </summary>
        /// <returns>The name of the variable with non-alphanumeric characters replaced by an underscore.</returns>
        string GetVariableName();

        /// <summary>
        /// Sets the <see cref="InitializationModel"/> to a new value.
        /// </summary>
        void CreateInitializationValue();

        /// <summary>
        /// Returns if this variable is used in the graph, it won't be selected when select unused is dispatched.
        /// </summary>
        /// <returns>If this variable is used in the graph, it won't be selected when select unused is dispatched.</returns>
        bool IsUsed();
    }
}
