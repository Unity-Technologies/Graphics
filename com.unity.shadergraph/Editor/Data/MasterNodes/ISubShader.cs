using System;
using UnityEditor.Graphing;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    public interface ISubShader
    {
        string GetSubshader(IMasterNode masterNode, GenerationMode mode);
    }
}
