using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    public sealed class ColorShaderProperty_V1 : AbstractShaderProperty<Color>
    {
        internal ColorShaderProperty_V1()
        {
            displayName = "Color";
        }

        public ColorShaderProperty_V1(ColorShaderProperty_V0 colorProperty)
        {
            displayName = colorProperty.displayName;
            hidden = colorProperty.hidden;
            value = colorProperty.value;
            colorMode = colorProperty.colorMode;
            precision = colorProperty.precision;
            gpuInstanced = colorProperty.gpuInstanced;
        }

        public override PropertyType propertyType => PropertyType.Color_V1;

        internal override bool isBatchable => true;
        internal override bool isExposable => true;
        internal override bool isRenamable => true;
        internal override bool isGpuInstanceable => true;

        internal string hdrTagString => colorMode == ColorMode.HDR ? "[HDR]" : "";

        internal override string GetPropertyBlockString()
        {
            return $"{hideTagString}{hdrTagString}{referenceName}(\"{displayName}\", Color) = ({NodeUtils.FloatToShaderValue(value.r)}, {NodeUtils.FloatToShaderValue(value.g)}, {NodeUtils.FloatToShaderValue(value.b)}, {NodeUtils.FloatToShaderValue(value.a)})";
        }

        public override string GetDefaultReferenceName()
        {
            return $"Color_{objectId}";
        }

        [SerializeField]
        ColorMode m_ColorMode;

        public ColorMode colorMode
        {
            get => m_ColorMode;
            set => m_ColorMode = value;
        }

        internal override AbstractMaterialNode ToConcreteNode()
        {
            return new ColorNode { color = new ColorNode.Color(value, colorMode) };
        }

        internal override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                colorValue = value
            };
        }

        internal override ShaderInput Copy()
        {
            return new ColorShaderProperty_V1()
            {
                displayName = displayName,
                hidden = hidden,
                value = value,
                colorMode = colorMode,
                precision = precision,
                gpuInstanced = gpuInstanced,
            };
        }
    }

    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.ColorShaderProperty")]
    [FormerName("UnityEditor.ShaderGraph.Internal.ColorShaderProperty")]
    public sealed class ColorShaderProperty_V0 : AbstractShaderProperty<Color>
    {
        internal ColorShaderProperty_V0()
        {
            displayName = "Color";
        }
        
        public override PropertyType propertyType => PropertyType.Color_V0;
        
        internal override bool isBatchable => true;
        internal override bool isExposable => true;
        internal override bool isRenamable => true;
        internal override bool isGpuInstanceable => true;
        
        internal string hdrTagString => colorMode == ColorMode.HDR ? "[HDR]" : "";

        internal override string GetPropertyBlockString()
        {
            return $"{hideTagString}{hdrTagString}{referenceName}(\"{displayName}\", Color) = ({NodeUtils.FloatToShaderValue(value.r)}, {NodeUtils.FloatToShaderValue(value.g)}, {NodeUtils.FloatToShaderValue(value.b)}, {NodeUtils.FloatToShaderValue(value.a)})";
        }

        public override string GetDefaultReferenceName()
        {
            return $"Color_{objectId}";
        }
        
        [SerializeField]
        ColorMode m_ColorMode;

        public ColorMode colorMode
        {
            get => m_ColorMode;
            set => m_ColorMode = value;
        }
        
        internal override AbstractMaterialNode ToConcreteNode()
        {
            return new ColorNode { color = new ColorNode.Color(value, colorMode) };
        }

        internal override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                colorValue = value
            };
        }        

        internal override ShaderInput Copy()
        {
            return new ColorShaderProperty_V0()
            {
                displayName = displayName,
                hidden = hidden,
                value = value,
                colorMode = colorMode,
                precision = precision,
                gpuInstanced = gpuInstanced,
            };
        }
    }
}
