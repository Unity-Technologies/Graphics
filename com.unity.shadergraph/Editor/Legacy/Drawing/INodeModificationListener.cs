using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.Drawing
{
    interface AbstractMaterialNodeModificationListener
    {
        void OnNodeModified(ModificationScope scope);
    }
}
