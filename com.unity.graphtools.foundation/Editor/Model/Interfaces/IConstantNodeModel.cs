using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for constant node.
    /// </summary>
    public interface IConstantNodeModel : ISingleOutputPortNodeModel
    {
        // Type safe value set.
        void SetValue<T>(T value);
        object ObjectValue { get; set; }
        Type Type { get; }
        bool IsLocked { get; set; }
        IConstant Value { get; }
        void PredefineSetup();
    }
}
