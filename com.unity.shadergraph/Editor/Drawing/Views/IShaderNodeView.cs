using System;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Graphing;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph.Drawing;
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
        // Searches the ports on this node for one that matches the given slot.
        // Returns true if found, false if not.
        bool FindPort(SlotReference slot, out ShaderPort port);
    }
}
