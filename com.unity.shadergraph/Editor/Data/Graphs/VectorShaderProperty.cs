using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    abstract class VectorShaderProperty : AbstractShaderProperty<Vector4>
    {
        public override bool isBatchable => true;
        public override bool isExposable => true;
        public override bool isRenamable => true;
        
        public override string GetPropertyBlockString()
        {
            return $"{hideTagString}{referenceName}(\"{displayName}\", Vector) = ({NodeUtils.FloatToShaderValue(value.x)}, {NodeUtils.FloatToShaderValue(value.y)}, {NodeUtils.FloatToShaderValue(value.z)}, {NodeUtils.FloatToShaderValue(value.w)})";
        }
    }
}
