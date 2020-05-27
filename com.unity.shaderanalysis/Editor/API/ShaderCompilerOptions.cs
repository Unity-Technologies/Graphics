using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderAnalysis
{
    /// <summary>Standard shader compiler options.</summary>
    public struct ShaderCompilerOptions
    {
        /// <summary>Include folders to look for shader sources.</summary>
        public HashSet<string> includeFolders;
        /// <summary>Defines to use when compiling shaders.</summary>
        public HashSet<string> defines;
        /// <summary>Entry point name to use when compiling a shader.</summary>
        public string entry;
    }
}
