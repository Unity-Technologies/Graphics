using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    public abstract class VectorShaderProperty : AbstractShaderProperty<Vector4>
    {
        internal override bool isBatchable => true;
        internal override bool isExposable => true;
        internal override bool isRenamable => true;

        internal override string GetPropertyBlockString()
        {
            return $"{hideTagString}{referenceName}(\"{displayName}\", Vector) = ({NodeUtils.FloatToShaderValue(value.x)}, {NodeUtils.FloatToShaderValue(value.y)}, {NodeUtils.FloatToShaderValue(value.z)}, {NodeUtils.FloatToShaderValue(value.w)})";
        }
    }
}
