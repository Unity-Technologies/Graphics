using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    internal abstract class ShaderInput : ShaderValue
    {
#region Utility
        public abstract AbstractMaterialNode ToConcreteNode();
#endregion
    }
}
