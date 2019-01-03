using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.Drawing
{
    interface INodeModificationListener
    {
        void OnNodeModified(ModificationScope scope);
    }
}
