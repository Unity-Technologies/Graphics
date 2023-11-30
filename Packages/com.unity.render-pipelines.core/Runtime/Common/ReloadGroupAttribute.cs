using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Attribute specifying that it contains element that should be reloaded.
    /// If the instance of the class is null, the system will try to recreate
    /// it with the default constructor.
    /// Be sure classes using it have default constructor!
    /// </summary>
    /// <seealso cref="ReloadAttribute"/>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ReloadGroupAttribute : Attribute
    { }
}
