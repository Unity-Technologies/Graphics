using System;

namespace UnityEngine.ShaderGraph.Hlsl
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class AnyDimensionAttribute : Attribute
    {
        public string Group;
        public AnyDimensionAttribute(string group = "")
        {
            Group = group;
        }
    }
}
