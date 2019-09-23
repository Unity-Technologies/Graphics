using System.Diagnostics;

namespace UnityEditor.ShaderGraph.Internal
{
    public class FieldDescriptor : IField
    {
        public string tag { get; }
        public string name { get; }
        public string define { get; }

        public FieldDescriptor(string tag, string name, string define)
        {
            this.tag = tag;
            this.name = name;
            this.define = define;
        }
    }
}
