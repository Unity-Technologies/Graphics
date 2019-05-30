using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    abstract class ShaderInput : ShaderValue
    {
#region Utility
        public abstract AbstractMaterialNode ToConcreteNode();
#endregion
    }
}
