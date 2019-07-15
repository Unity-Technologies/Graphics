using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class ColorShaderProperty : AbstractShaderProperty<Color>
    {
        public ColorShaderProperty()
        {
            displayName = "Color";
        }
        
        public override PropertyType propertyType => PropertyType.Color;
        
        public override bool isBatchable => true;
        public override bool isExposable => true;
        public override bool isRenamable => true;
        
        public string hdrTagString => colorMode == ColorMode.HDR ? "[HDR]" : "";

        public override string GetPropertyBlockString()
        {
            return $"{hideTagString}{hdrTagString}{referenceName}(\"{displayName}\", Color) = ({NodeUtils.FloatToShaderValue(value.r)}, {NodeUtils.FloatToShaderValue(value.g)}, {NodeUtils.FloatToShaderValue(value.b)}, {NodeUtils.FloatToShaderValue(value.a)})";
        }

        public override string referenceNameBase => "Color";
        
        [SerializeField]
        ColorMode m_ColorMode;

        public ColorMode colorMode
        {
            get => m_ColorMode;
            set => m_ColorMode = value;
        }
        
        public override AbstractMaterialNode ToConcreteNode()
        {
            return new ColorNode { color = new ColorNode.Color(value, colorMode) };
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                colorValue = value
            };
        }        

        public override ShaderInput Copy()
        {
            return new ColorShaderProperty()
            {
                displayName = displayName,
                hidden = hidden,
                value = value,
                colorMode = colorMode
            };
        }
    }
}
