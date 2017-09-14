using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Graphing;

namespace UnityEditor.MaterialGraph.Drawing.Inspector
{
    public abstract class AbstractNodeEditorView : VisualElement, IDisposable
    {
        public abstract INode node { get; set; }

        public abstract void Dispose();
    }
}
