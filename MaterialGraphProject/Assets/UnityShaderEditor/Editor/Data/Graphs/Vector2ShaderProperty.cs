using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Vector2ShaderProperty : VectorShaderProperty
    {
        public Vector2ShaderProperty()
        {
            displayName = "Vector2";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Vector2; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(value.x, value.y, 0, 0); }
        }

        public override string GetInlinePropertyDeclarationString()
        {
            return "float2 " + referenceName + ";";
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty()
            {
                m_Name = referenceName,
                m_PropType = PropertyType.Vector2,
                m_Vector4 = value
            };
        }
    }
}
