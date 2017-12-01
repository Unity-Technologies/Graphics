using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Vector4ShaderProperty : VectorShaderProperty
    {
        public Vector4ShaderProperty()
        {
            displayName = "Vector4";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Vector4; }
        }

        public override Vector4 defaultValue
        {
            get { return value; }
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty()
            {
                m_Name = referenceName,
                m_PropType = PropertyType.Vector4,
                m_Vector4 = value
            };
        }
    }
}
