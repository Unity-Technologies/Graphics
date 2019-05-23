using System;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Graphing;
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
    }
}
