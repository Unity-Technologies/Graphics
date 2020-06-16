using System;

namespace UnityEditor.ShaderGraph
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public abstract class ContextFilterableAttribute : Attribute
    {
    }
}
