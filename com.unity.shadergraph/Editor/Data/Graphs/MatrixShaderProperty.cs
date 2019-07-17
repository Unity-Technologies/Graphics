using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    abstract class MatrixShaderProperty : AbstractShaderProperty<Matrix4x4>
    {
        public override bool isBatchable => true;
        public override bool isExposable => false;
        public override bool isRenamable => true;
        
        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return $"{concretePrecision.ToShaderString()}4x4 {referenceName}{delimiter}";
        }
    }
}
