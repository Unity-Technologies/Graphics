using System;

namespace UnityEditor.ShaderGraph
{
    [AttributeUsage(AttributeTargets.Struct)]
    class GenerateBlocksAttribute : Attribute
    {
        internal string path { get; set; }

        public GenerateBlocksAttribute(string path = "")
        {
            this.path = path;
        }
    }
}
