using System;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    abstract class MatrixShaderProperty : AbstractShaderProperty<Matrix4x4>
    {
        // expose to UI for override
        internal PropertyHLSLGenerationType m_generationType = PropertyHLSLGenerationType.UnityPerMaterial;

        internal override bool isExposable => false;
        internal override bool isRenamable => true;

        internal override void AppendPropertyDeclarations(ShaderStringBuilder builder, Func<string, string> nameModifier, PropertyHLSLGenerationType generationTypes)
        {
            if ((generationTypes & m_generationType) != 0)
            {
                string name = nameModifier?.Invoke(referenceName) ?? referenceName;
                builder.AppendLine($"{concretePrecision.ToShaderString()}4x4 {name};");
            }
        }
    }
}
