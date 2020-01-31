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

        public override bool isBatchable
        {
            get { return true; }
        }

        public override bool isExposable
        {
            get { return false; }
        }

        public override bool isRenamable
        {
            get { return true; }
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return string.Format("{0}4x4 {1}{2}", concretePrecision.ToShaderString(), referenceName, delimiter);
        }
    }
}
