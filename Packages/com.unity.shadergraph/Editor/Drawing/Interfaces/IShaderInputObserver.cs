using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.Drawing
{
    /// <summary>
    /// This interface is implemented by any entity that wants to be made aware of updates to a shader input
    /// </summary>
    interface IShaderInputObserver
    {
        void OnShaderInputUpdated(ModificationScope modificationScope);
    }
}
