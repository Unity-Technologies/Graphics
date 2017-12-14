using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Matrix2ShaderProperty : MatrixShaderProperty
    {
        public Matrix2ShaderProperty()
        {
            displayName = "Matrix2";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Matrix2; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(); }
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return default(PreviewProperty);
        }
    }
}
