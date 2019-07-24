using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class Matrix2ShaderProperty : MatrixShaderProperty
    {
        public Matrix2ShaderProperty()
        {
            displayName = "Matrix2x2";
            value = Matrix4x4.identity;
        }
        
        public override PropertyType propertyType => PropertyType.Matrix2;

        public override string GetPropertyAsArgumentString()
        {
            return $"{concretePrecision.ToShaderString()}2x2 {referenceName}";
        }
        
        public override AbstractMaterialNode ToConcreteNode()
        {
            return new Matrix2Node
            {
                row0 = new Vector2(value.m00, value.m01),
                row1 = new Vector2(value.m10, value.m11)
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
            return new Matrix2ShaderProperty()
            {
                displayName = displayName,
                hidden = hidden,
                value = value
            };
        }
    }
}
