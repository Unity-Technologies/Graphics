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
            value = Color.black;
        }

        internal ColorShaderProperty(int version) : this()
        {
            this.sgVersion = version;
        }

        public override PropertyType propertyType => PropertyType.Color;

        internal override bool isExposable => true;
        internal override bool isRenamable => true;

        [SerializeField]
        internal bool isMainColor = false;

        internal string hdrTagString => colorMode == ColorMode.HDR ? "[HDR]" : "";

        internal string mainColorString => isMainColor ? "[MainColor]" : "";

        internal override string GetPropertyBlockString()
        {
            return $"{hideTagString}{hdrTagString}{mainColorString}{referenceName}(\"{displayName}\", Color) = ({NodeUtils.FloatToShaderValueShaderLabSafe(value.r)}, {NodeUtils.FloatToShaderValueShaderLabSafe(value.g)}, {NodeUtils.FloatToShaderValueShaderLabSafe(value.b)}, {NodeUtils.FloatToShaderValueShaderLabSafe(value.a)})";
        }

        internal override string GetPropertyAsArgumentString(string precisionString)
        {
            return $"{concreteShaderValueType.ToShaderString(precisionString)} {referenceName}";
        }

        internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
        {
            HLSLDeclaration decl = GetDefaultHLSLDeclaration();
            action(new HLSLProperty(HLSLType._float4, referenceName, decl, concretePrecision));
        }

        public override string GetOldDefaultReferenceName()
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

        internal override string GetHLSLVariableName(bool isSubgraphProperty, GenerationMode mode)
        {
            HLSLDeclaration decl = GetDefaultHLSLDeclaration();
            if (decl == HLSLDeclaration.HybridPerInstance)
                return $"UNITY_ACCESS_HYBRID_INSTANCED_PROP({referenceName}, {concretePrecision.ToShaderString()}4)";
            else
                return base.GetHLSLVariableName(isSubgraphProperty, mode);
        }

        internal override ShaderInput Copy()
        {
            return new ColorShaderProperty()
            {
                sgVersion = sgVersion,
                displayName = displayName,
                value = value,
                colorMode = colorMode,
                isMainColor = isMainColor
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

        internal override void OnBeforePasteIntoGraph(GraphData graph)
        {
            if (isMainColor)
            {
                ColorShaderProperty existingMain = graph.GetMainColor();
                if (existingMain != null && existingMain != this)
                {
                    isMainColor = false;
                }
            }
            base.OnBeforePasteIntoGraph(graph);
        }
    }
}
