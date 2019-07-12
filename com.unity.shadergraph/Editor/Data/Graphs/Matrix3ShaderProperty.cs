using System;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class Matrix3ShaderProperty : MatrixShaderProperty
    {
        public Matrix3ShaderProperty()
        {
            displayName = "Matrix3x3";
            value = Matrix4x4.identity;
        }
        
        public override PropertyType propertyType => PropertyType.Matrix3;

        public override string GetPropertyAsArgumentString()
        {
            return $"{concretePrecision.ToShaderString()}3x3 {referenceName}";
        }
        
        public override AbstractMaterialNode ToConcreteNode()
        {
            return new Matrix3Node
            {
                row0 = new Vector3(value.m00, value.m01, value.m02),
                row1 = new Vector3(value.m10, value.m11, value.m12),
                row2 = new Vector3(value.m20, value.m21, value.m22)
            };
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                matrixValue = value
            };
        }

        public override ShaderInput Copy()
        {
            return new Matrix3ShaderProperty()
            {
                displayName = displayName,
                hidden = hidden,
                value = value
            };
        }
    }
}
