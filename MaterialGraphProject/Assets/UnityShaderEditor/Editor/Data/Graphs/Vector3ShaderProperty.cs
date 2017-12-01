using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Vector3ShaderProperty : VectorShaderProperty
    {
        public Vector3ShaderProperty()
        {
            displayName = "Vector3";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Vector3; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(value.x, value.y, value.z, 0); }
        }

        public override string GetInlinePropertyDeclarationString()
        {
            return "float3 " + referenceName + ";";
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty()
            {
                m_Name = referenceName,
                m_PropType = PropertyType.Vector3,
                m_Vector4 = value
            };
        }
    }
}
