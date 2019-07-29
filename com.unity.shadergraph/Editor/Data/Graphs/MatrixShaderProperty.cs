using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    abstract class MatrixShaderProperty : AbstractShaderProperty<Matrix4x4>
    {
        public override bool isExposable => false;
        public override bool isRenamable => true;

        public override IEnumerable<(string cbName, string line)> GetPropertyDeclarationStrings()
        {
            yield return (s_UnityPerMaterialCbName, $"{concretePrecision.ToShaderString()}4x4 {referenceName}");
        }
    }
}
