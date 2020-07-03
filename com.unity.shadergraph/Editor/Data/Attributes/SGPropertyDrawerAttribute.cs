using System;

namespace UnityEditor.ShaderGraph.Drawing
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SGPropertyDrawerAttribute : Attribute
    {
        public Type propertyType { get; private set; }

        public SGPropertyDrawerAttribute(Type propertyType)
        {
            this.propertyType = propertyType;
        }
    }
}
