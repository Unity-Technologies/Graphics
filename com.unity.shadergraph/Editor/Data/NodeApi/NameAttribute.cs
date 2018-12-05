using System;

namespace UnityEditor.ShaderGraph
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class NameAttribute : Attribute
    {
        internal string name { get; }

        public NameAttribute(string name)
        {
            this.name = name;
        }
    }
}
