using System;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    interface IShaderNodeView : IDisposable
    {
        Node gvNode { get; }
        AbstractMaterialNode node { get; }
        void UpdatePortInputTypes();
        void OnModified(ModificationScope scope);
    }
}
