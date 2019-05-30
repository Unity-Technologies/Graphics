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

#region Type
        public override PropertyType propertyType => PropertyType.Color;
#endregion

#region Capabilities
        public override bool isBatchable => true;
        public override bool isExposable => true;
        public override bool isRenamable => true;
#endregion

#region PropertyBlock
        public string hdrTagString => colorMode == ColorMode.HDR ? "[HDR]" : "";

        public override string GetPropertyBlockString()
        {
            return $"{hideTagString}{hdrTagString} {referenceName}(\"{displayName}\", Color) = ({NodeUtils.FloatToShaderValue(value.r)}, {NodeUtils.FloatToShaderValue(value.g)}, {NodeUtils.FloatToShaderValue(value.b)}, {NodeUtils.FloatToShaderValue(value.a)})";
        }
#endregion

#region Options
        [SerializeField]
        private ColorMode m_ColorMode;

        public ColorMode colorMode
        {
            get => m_ColorMode;
            set => m_ColorMode = value;
        }
#endregion

#region Utility
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

        public override AbstractShaderProperty Copy()
        {
            var copied = new ColorShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            copied.hidden = hidden;
            copied.colorMode = colorMode;
            return copied;
        }
#endregion
    }
}
