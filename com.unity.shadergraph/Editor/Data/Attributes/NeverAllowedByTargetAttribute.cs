using System;

namespace UnityEditor.ShaderGraph
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal class NeverAllowedByTargetAttribute : ContextFilterableAttribute
    {
    }
}
