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

        public override PropertyType propertyType
        {
            get { return PropertyType.Matrix2; }
        }

        public override bool isBatchable
        {
            get { return true; }
        }

        public override bool isExposable
        {
            get { return false; }
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return "float4x4 " + referenceName + delimiter;
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(PropertyType.Matrix2)
            {
                name = referenceName,
                matrixValue = value
            };
        }

        public override AbstractMaterialNode ToConcreteNode()
        {
            return new Matrix2Node
            {
                row0 = new Vector2(value.m00, value.m01),
                row1 = new Vector2(value.m10, value.m11)
            };
        }

        public override AbstractShaderProperty Copy()
        {
            var copied = new Matrix2ShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
    }
}
