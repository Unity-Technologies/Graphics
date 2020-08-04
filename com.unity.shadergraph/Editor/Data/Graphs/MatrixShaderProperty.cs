using System;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    abstract class MatrixShaderProperty : AbstractShaderProperty<Matrix4x4>
    {
        internal override bool SupportsCBufferUsage(CBufferUsage usage) => usage != CBufferUsage.HybridRenderer;

        internal override bool SupportsBlockUsage(PropertyBlockUsage usage) => true;

        internal override bool isRenamable => true;

        internal override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return $"{concretePrecision.ToShaderString()}4x4 {referenceName}{delimiter}";
        }
    }
}
