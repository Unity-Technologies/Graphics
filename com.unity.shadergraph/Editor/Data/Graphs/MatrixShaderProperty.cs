using System;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    abstract class MatrixShaderProperty : AbstractShaderProperty<Matrix4x4>
    {
        internal override bool isBatchable => true;
        internal override bool isExposable => false;
        internal override bool isRenamable => true;

        internal override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return $"{concretePrecision.ToShaderString()}4x4 {referenceName}{delimiter}";
        }
    }
}
