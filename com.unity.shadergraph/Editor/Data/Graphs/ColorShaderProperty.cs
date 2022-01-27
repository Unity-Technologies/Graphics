using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.ColorShaderProperty")]
    [BlackboardInputInfo(10)]
    public sealed class ColorShaderProperty : AbstractShaderProperty<Color>
    {
        // 0 - original (broken color space)
        // 1 - fixed color space
        // 2 - original (broken color space) with HLSLDeclaration override
        // 3 - fixed color space with HLSLDeclaration override
        public override int latestVersion => 3;
        public const int deprecatedVersion = 2;

        internal ColorShaderProperty()
        {
            displayName = "Color";
        }

        internal ColorShaderProperty(int version) : this()
        {
            this.sgVersion = version;
        }
        
        public override PropertyType propertyType => PropertyType.Color;
        
        internal override bool isExposable => true;
        internal override bool isRenamable => true;
        
        internal string hdrTagString => colorMode == ColorMode.HDR ? "[HDR]" : "";

        internal override string GetPropertyBlockString()
        {
            return $"{hideTagString}{hdrTagString}{referenceName}(\"{displayName}\", Color) = ({NodeUtils.FloatToShaderValueShaderLabSafe(value.r)}, {NodeUtils.FloatToShaderValueShaderLabSafe(value.g)}, {NodeUtils.FloatToShaderValueShaderLabSafe(value.b)}, {NodeUtils.FloatToShaderValueShaderLabSafe(value.a)})";
        }

        internal override string GetPropertyAsArgumentString()
        {
            return $"{concreteShaderValueType.ToShaderString(concretePrecision.ToShaderString())} {referenceName}";
        }

        internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
        {
            HLSLDeclaration decl = GetDefaultHLSLDeclaration();
            action(new HLSLProperty(HLSLType._float4, referenceName, decl, concretePrecision));
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

        internal override string GetHLSLVariableName(bool isSubgraphProperty)
        {
            HLSLDeclaration decl = GetDefaultHLSLDeclaration();
            if (decl == HLSLDeclaration.HybridPerInstance)
                return $"UNITY_ACCESS_HYBRID_INSTANCED_PROP({referenceName}, {concretePrecision.ToShaderString()}4)";
            else
                return referenceName;
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
                overrideHLSLDeclaration = overrideHLSLDeclaration,
                hlslDeclarationOverride = hlslDeclarationOverride
            };
        }

        public override void OnAfterDeserialize(string json)
        {
            if (sgVersion < 2)
            {
                LegacyShaderPropertyData.UpgradeToHLSLDeclarationOverride(json, this);
                // version 0 upgrades to 2
                // version 1 upgrades to 3
                ChangeVersion((sgVersion == 0) ? 2 : 3);
            }
        }
    }
}
