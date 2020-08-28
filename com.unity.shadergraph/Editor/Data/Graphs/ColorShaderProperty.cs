using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.ColorShaderProperty")]
    public sealed class ColorShaderProperty : AbstractShaderProperty<Color>
    {
        public override int latestVersion => 1;

        internal ColorShaderProperty()
        {
            displayName = "Color";
        }

        internal ColorShaderProperty(int version) : this()
        {
            this.sgVersion = version;
        }
        
        public override PropertyType propertyType => PropertyType.Color;
        
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
            UnityEngine.Color propColor = value;
            if (colorMode == ColorMode.Default)
            {
                if (PlayerSettings.colorSpace == ColorSpace.Linear)
                    propColor = propColor.linear;
            }
            else if (colorMode == ColorMode.HDR)
            {
                // conversion from linear to active color space is handled in the shader code (see PropertyNode.cs)
            }

            // we use Vector4 type to avoid all of the automatic color conversions of PropertyType.Color
            return new PreviewProperty(PropertyType.Vector4)
            {
                name = referenceName,
                vector4Value = propColor
            };

        }        

        internal override ShaderInput Copy()
        {
            return new ColorShaderProperty()
            {
                sgVersion = sgVersion,
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
