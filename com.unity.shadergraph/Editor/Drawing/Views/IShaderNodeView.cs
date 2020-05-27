using System;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Graphing;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    interface IShaderNodeView : IDisposable
    {
        Node gvNode { get; }
        AbstractMaterialNode node { get; }
        VisualElement colorElement { get; }
        void SetColor(Color newColor);
        void ResetColor();
        void UpdatePortInputTypes();
        void OnModified(ModificationScope scope);
        void AttachMessage(string errString, ShaderCompilerMessageSeverity severity);
        void ClearMessage();
    }
}
