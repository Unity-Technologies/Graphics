using System;
using System.Runtime.InteropServices;

namespace UnityEditor.ShaderGraph
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface, Inherited = true, AllowMultiple = false)]
    internal class GenerationAPIAttribute : Attribute
    {
        public GenerationAPIAttribute() { }
    }
}
