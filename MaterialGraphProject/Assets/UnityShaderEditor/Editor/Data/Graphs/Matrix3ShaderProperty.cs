using System;
using UnityEngine;
using UnityEditor.Graphing;

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

        public override INode ToConcreteNode()
        {
            return new Matrix3Node 
            { 
                row0 = new Vector3(value.m00, value.m01, value.m02), 
                row1 = new Vector3(value.m10, value.m11, value.m12),
                row2 = new Vector3(value.m20, value.m21, value.m22)
            };
        }
    }
}
