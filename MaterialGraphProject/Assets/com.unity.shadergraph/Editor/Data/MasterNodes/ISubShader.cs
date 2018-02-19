using System;

namespace UnityEditor.ShaderGraph {
    public interface ISubShader
    {
        string GetSubshader(IMasterNode masterNode, GenerationMode mode);
    }
}