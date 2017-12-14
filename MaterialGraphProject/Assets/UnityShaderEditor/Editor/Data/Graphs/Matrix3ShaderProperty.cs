using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Matrix3ShaderProperty : MatrixShaderProperty
    {
        public Matrix3ShaderProperty()
        {
            displayName = "Matrix3";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Matrix3; }
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
