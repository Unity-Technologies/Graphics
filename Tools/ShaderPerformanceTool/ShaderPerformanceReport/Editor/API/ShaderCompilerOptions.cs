using System.Collections.Generic;

namespace UnityEditor.Experimental.ShaderTools
{
    public struct ShaderCompilerOptions
    {
        public HashSet<string> includeFolders;
        public HashSet<string> defines;
        public string entry;
    }
}
