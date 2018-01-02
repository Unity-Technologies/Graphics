using System;

namespace UnityEditor.ShaderGraph {
    public interface IUnlitSubShader
    {
        string GetSubshader(UnlitMasterNode masterNode, GenerationMode mode);
    }
}