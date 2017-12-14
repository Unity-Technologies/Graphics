using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Matrix4ShaderProperty : MatrixShaderProperty
    {
        public Matrix4ShaderProperty()
        {
            displayName = "Matrix4";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Matrix4; }
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
