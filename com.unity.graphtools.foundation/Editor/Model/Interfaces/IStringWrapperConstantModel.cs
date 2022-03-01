using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Undocumented.
    /// </summary>
    public interface IStringWrapperConstantModel : IConstant
    {
        string StringValue { get; set; }
        string Label { get; }
    }
}
