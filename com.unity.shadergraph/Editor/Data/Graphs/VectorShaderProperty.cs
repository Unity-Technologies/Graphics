using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    public abstract class VectorShaderProperty : AbstractShaderProperty<Vector4>
    {
        internal override bool SupportsBlockUsage(PropertyBlockUsage usage) => true;

        internal override bool SupportsCBufferUsage(CBufferUsage usage) => true;

        internal override bool isRenamable => true;

        internal override string GetPropertyBlockString()
        {
            return $"{hideTagString}{referenceName}(\"{displayName}\", Vector) = ({NodeUtils.FloatToShaderValueShaderLabSafe(value.x)}, {NodeUtils.FloatToShaderValueShaderLabSafe(value.y)}, {NodeUtils.FloatToShaderValueShaderLabSafe(value.z)}, {NodeUtils.FloatToShaderValueShaderLabSafe(value.w)})";
        }
    }
}
