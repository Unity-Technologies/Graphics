using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    abstract class MatrixShaderProperty : AbstractShaderProperty<Matrix4x4>
    {
        public override string GetPropertyBlockString()
        {
            return string.Empty;
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(); }
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return "float4x4 " + referenceName + delimiter;
        }
    }
}
