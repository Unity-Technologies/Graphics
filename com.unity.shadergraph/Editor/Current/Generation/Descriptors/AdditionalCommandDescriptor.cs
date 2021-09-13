using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    internal class AdditionalCommandDescriptor
    {
        public string token { get; }
        public string content { get; }

        public AdditionalCommandDescriptor(string token, string content)
        {
            this.token = token;
            this.content = content;
        }
    }
}
