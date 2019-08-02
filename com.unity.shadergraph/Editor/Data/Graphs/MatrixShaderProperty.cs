using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    abstract class MatrixShaderProperty : AbstractShaderProperty<Matrix4x4>
    {
        public override bool isExposable => false;
        public override bool isRenamable => true;
    }
}
