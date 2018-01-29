using System;
using UnityEditor.Graphing;
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

        public override INode ToConcreteNode()
        {
            return new Matrix2Node 
            { 
                row0 = new Vector2(value.m00, value.m01), 
                row1 = new Vector2(value.m10, value.m11)
            };
        }
    }
}
