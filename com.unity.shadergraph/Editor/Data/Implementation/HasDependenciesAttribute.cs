using System;

namespace UnityEditor.ShaderGraph
{
    [AttributeUsage(AttributeTargets.Class)]
    class HasDependenciesAttribute : Attribute
    {
        public HasDependenciesAttribute(Type minimalType)
        {
            this.minimalType = minimalType;
        }

        public Type minimalType { get; }
    }
}
