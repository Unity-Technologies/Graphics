using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.Drawing.Inspector
{
    public abstract class AbstractNodeEditorView : VisualElement, IDisposable
    {
        public abstract INode node { get; set; }

        public abstract void Dispose();
    }
}
