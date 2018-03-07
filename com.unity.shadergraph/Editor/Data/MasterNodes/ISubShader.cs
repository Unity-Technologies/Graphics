using System;
using UnityEditor.Graphing;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    public interface ISubShader
    {
        IMasterNode owner { get; set; }
        string GetSubshader(IMasterNode masterNode, GenerationMode mode);
        VisualElement CreateSettingsElement();
        void UpdateAfterDeserialization();
    }
}
