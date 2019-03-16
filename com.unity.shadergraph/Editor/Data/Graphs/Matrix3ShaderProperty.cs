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

        public override PropertyType propertyType
        {
            get { return PropertyType.Matrix3; }
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
            return new PreviewProperty(PropertyType.Matrix3)
            {
                name = referenceName,
                matrixValue = value
            };
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

        public override AbstractShaderProperty Copy()
        {
            var copied = new Matrix3ShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
    }
}
