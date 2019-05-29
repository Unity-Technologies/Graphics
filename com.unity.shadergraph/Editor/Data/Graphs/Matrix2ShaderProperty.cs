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

#region ShaderValueType
        public override ConcreteSlotValueType concreteShaderValueType => ConcreteSlotValueType.Matrix2;
#endregion

#region Utility
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
            return new PreviewProperty(ConcreteSlotValueType.Matrix2)
            {
                name = referenceName,
                matrixValue = value
            };
        }

        public override AbstractShaderProperty Copy()
        {
            var copied = new Matrix2ShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
#endregion
    }
}
