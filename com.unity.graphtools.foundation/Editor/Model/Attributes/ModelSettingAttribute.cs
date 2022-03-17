using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Attribute to mark a field as being a basic setting, one that appear in the Basic Settings section
    /// of the model inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ModelSettingAttribute : Attribute
    {
    }
}
