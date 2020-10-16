using System;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    abstract class MatrixShaderProperty : AbstractShaderProperty<Matrix4x4>
    {
        // expose to UI for override
        internal HLSLDeclaration m_generationType = HLSLDeclaration.UnityPerMaterial;

        internal override bool isExposable => false;
        internal override bool isRenamable => true;

        internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
        {
            // HLSL decl is always 4x4 even if matrix smaller
            action(new HLSLProperty(HLSLType._matrix4x4, referenceName, m_generationType, concretePrecision));
        }
    }
}
