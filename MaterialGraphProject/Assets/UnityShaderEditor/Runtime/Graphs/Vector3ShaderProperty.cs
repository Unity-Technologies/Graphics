using System;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class Vector3ShaderProperty : VectorShaderProperty
    {
        public override PropertyType propertyType
        {
            get { return PropertyType.Vector3; }
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty()
            {
                m_Name = name,
                m_PropType = PropertyType.Vector3,
                m_Vector4 = value
            };
        }
    }
}
