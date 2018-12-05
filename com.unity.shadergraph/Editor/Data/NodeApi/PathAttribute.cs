using System;

namespace UnityEditor.ShaderGraph
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class PathAttribute : Attribute
    {
        internal string path { get; }

        public PathAttribute(string path)
        {
            this.path = path;
        }
    }
}
