using System;
using System.Text;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class GradientShaderProperty : AbstractShaderProperty<Gradient>
    {
        [SerializeField]
        private bool m_Modifiable = true;

        public GradientShaderProperty()
        {
            value = new Gradient();
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Gradient; }
        }

        public bool modifiable
        {
            get { return m_Modifiable; }
            set { m_Modifiable = value; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(); }
        }

        public override string GetPropertyBlockString()
        {
            return "";
        }

        public override string GetPropertyDeclarationString()
        {
            return "";
        }

        public override string GetInlinePropertyDeclarationString()
        {
            return "Gradient ShaderGraph_DefaultGradient;";
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty()
            {
                m_Name = referenceName,
                m_PropType = PropertyType.Gradient
            };
        }
    }
}
