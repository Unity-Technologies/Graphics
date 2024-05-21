#if HAS_VFX_GRAPH
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

namespace UnityEditor.VFX.URP
{
    [VFXHelpURL("Context-OutputParticleURPLitDecal")]
    [VFXInfo(name = "Output Particle|URP Lit|Decal", category = "#4Output Advanced")]
    internal class VFXDecalURPOutput : VFXAbstractParticleURPLitOutput
    {
        public override string name => "Output Particle".AppendLabel("URP Lit").AppendLabel("Decal");

        public override string codeGeneratorTemplate => RenderPipeTemplate("VFXParticleURPDecal");

        public override VFXTaskType taskType => VFXTaskType.ParticleHexahedronOutput;
        public override bool supportsUV => GetOrRefreshShaderGraphObject() == null;

        public override void OnEnable()
        {
            base.OnEnable();
            blendMode = BlendMode.Opaque;
            workflowMode = WorkflowMode.Metallic;
        }

        public enum BlendSource
        {
            BaseColorMapAlpha,
            MetallicMapBlue,
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Header("Opacity Channels"), SerializeField,
         Tooltip("Specifies the source this Material uses as opacity for its Normal Map.")]
        BlendSource normalOpacityChannel = BlendSource.BaseColorMapAlpha;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, InspectorName("MAOS Opacity Channel"),
         Tooltip("Specifies the source this Material uses as opacity for its Mask Map.")]
        BlendSource MAOSOpacityChannel = BlendSource.BaseColorMapAlpha;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Enables fading the decal based on the angle between the decal backward direction and the receiving surface normal.")]
        internal bool angleFade = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Header("Surface options"), SerializeField,
         Tooltip("When enabled, modifies the base color of the surface it projects onto.")]
        private bool affectBaseColor = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField,
         Tooltip("When enabled, modifies the metallic, ambient occlusion and smoothness of the surface it projects onto. The ambient occlusion slider is available when using an Occlusion Map.")]
        private bool affectMAOS = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specify the layer mask for the decals. Unity renders decals on all meshes where at least one Rendering Layer value matches.")]
        private uint decalLayer = ~0u;

        protected override bool useSmoothness => affectMAOS;
        protected override bool useMetallic => affectMAOS;
        protected override bool useNormalScale => false;

        public override bool HasSorting() => (sort == SortActivationMode.On) || (sort == SortActivationMode.Auto);
        public class FadeFactorProperty
        {
            [Range(0, 1), Tooltip("Controls the transparency of the decal.")]
            public float fadeFactor = 1.0f;
        }

        public class AngleFadeProperty
        {
            [Tooltip("Use the min-max slider to control the fade out range of the decal based on the angle between the Decal backward direction and the vertex normal of the receiving surface."), MinMax(0.0f, 180.0f)]
            public Vector2 angleFade = new Vector2(0.0f, 180.0f);
        }

        public class NormalAlphaProperty
        {
            [Tooltip("Controls the blending factor of the normal map."), Range(0, 1)]
            public float normalAlpha = 1.0f;
        }

        public class AmbientOcclusionProperty
        {
            [Tooltip("Controls the scale factor for the particle’s ambient occlusion."), Range(0, 1)]
            public float ambientOcclusion = 1.0f;
        }
        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = Enumerable.Empty<VFXPropertyWithValue>();

                properties = properties.Concat(PropertiesFromType(nameof(FadeFactorProperty)));
                if(angleFade)
                    properties = properties.Concat(PropertiesFromType(nameof(AngleFadeProperty)));

                foreach (var prop in base.inputProperties)
                {
                    //Inserts slots in the correct order
                    properties = properties.Append(prop);
                    if(prop.property.name ==  "normalMap")
                        properties = properties.Concat(PropertiesFromType(nameof(NormalAlphaProperty)));

                    if(affectMAOS && useOcclusionMap && prop.property.name == "occlusionMap")
                        properties = properties.Concat(PropertiesFromType(nameof(AmbientOcclusionProperty)));

                }

                return properties;
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(
            IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
            {
                yield return exp;
            }

            if (GetOrRefreshShaderGraphObject() == null)
            {
                yield return slotExpressions.First(o => o.name == nameof(FadeFactorProperty.fadeFactor));

                if (angleFade)
                {
                    var angleFadeExp = slotExpressions.First(o => o.name == nameof(AngleFadeProperty.angleFade));
                    yield return new VFXNamedExpression(AngleFadeSimplification(angleFadeExp.exp),
                        nameof(AngleFadeProperty.angleFade));
                }

                if (affectMAOS && useOcclusionMap)
                    yield return slotExpressions.First(o => o.name == nameof(AmbientOcclusionProperty.ambientOcclusion));

                if (useNormalMap)
                    yield return slotExpressions.First(o => o.name == nameof(NormalAlphaProperty.normalAlpha));
                yield return new VFXNamedExpression(VFXValue.Constant(decalLayer), "decalLayerMask");
            }
        }

        //URP uses the old angle fade simplification
        VFXExpression AngleFadeSimplification(VFXExpression angleFadeExp)
        {
            angleFadeExp = angleFadeExp / VFXValue.Constant(new Vector2(180.0f, 180.0f));
            var angleStart = new VFXExpressionExtractComponent(angleFadeExp, 0);
            var angleEnd = new VFXExpressionExtractComponent(angleFadeExp, 1);
            var range = new VFXExpressionMax(VFXValue.Constant(0.0001f), angleEnd - angleStart);
            var simplifiedAngleFade = new VFXExpressionCombine(
                VFXValue.Constant(1.0f) - (VFXValue.Constant(0.25f) - angleStart) / range,
                VFXValue.Constant(-0.25f) / range);
            return simplifiedAngleFade;
        }


        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var def in base.additionalDefines)
                    yield return def;
                if (GetOrRefreshShaderGraphObject() == null)
                {
                    if (angleFade)
                        yield return "DECAL_ANGLE_FADE";
                    if (affectBaseColor)
                        yield return "AFFECT_BASE_COLOR";
                    if (affectMAOS)
                    {
                        yield return "AFFECT_METALLIC";
                        yield return "AFFECT_SMOOTHNESS";
                        if (MAOSOpacityChannel == BlendSource.BaseColorMapAlpha)
                            yield return "VFX_MAOS_BLEND_BASE_COLOR_ALPHA";
                        else
                            yield return "VFX_MAOS_BLEND_METALLIC_BLUE";
                    }
                    if (affectMAOS && useOcclusionMap)
                        yield return "AFFECT_AMBIENT_OCCLUSION";

                    if (useEmissive /*TODO: add useEmissiveColor like in HDRP */ || useEmissiveMap)
                    {
                        yield return "AFFECT_EMISSIVE";
                    }

                    if (useNormalMap)
                    {
                        yield return "AFFECT_NORMAL";
                        if (normalOpacityChannel == BlendSource.BaseColorMapAlpha)
                            yield return "VFX_NORMAL_BLEND_BASE_COLOR_ALPHA";
                        else
                            yield return "VFX_NORMAL_BLEND_METALLIC_BLUE";
                    }
                }
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;
                yield return nameof(workflowMode);
                yield return nameof(zTestMode);
                yield return nameof(zWriteMode);
                yield return nameof(doubleSided);
                yield return nameof(castShadows);
                yield return nameof(blendMode);
                yield return nameof(shaderGraph);
                if (!affectBaseColor)
                    yield return nameof(useBaseColorMap);
                if (!affectMAOS)
                    yield return nameof(useMetallicMap);
                if (!useEmissive)
                    yield return nameof(useEmissiveMap);
                if (!affectMAOS)
                {
                    yield return nameof(useOcclusionMap);
                    yield return nameof(MAOSOpacityChannel);
                }

            }
        }

        protected override IEnumerable<string> untransferableSettings
        {
            get
            {
                foreach (var setting in base.untransferableSettings)
                {
                    yield return setting;
                }
                yield return nameof(blendMode);
            }
        }


        protected VFXShaderWriter GetDBufferMaskColor(int maskIndex)
        {
            var rs = new VFXShaderWriter();
            switch (maskIndex)
            {
                case 0:
                    rs.Write(affectBaseColor ? "RBGA" : "0");
                    break;
                case 1:
                    rs.Write(useNormalMap ? "RGBA" : "0");
                    break;
                case 2:
                    rs.Write(affectMAOS ? "RGBA" : "0");
                    break;
            }

            return rs;
        }

        protected VFXShaderWriter GetGBufferDecalMaskColor(int maskIndex)
        {
            var rs = new VFXShaderWriter();
            switch (maskIndex)
            {
                case 0:
                    rs.Write(affectBaseColor ? "RBG" : "0");
                    break;
                case 1:
                    rs.Write("0");
                    break;
                case 2:
                    rs.Write(useNormalMap ? "RGB" : "0");
                    break;
                case 3:
                {
                    rs.Write("RGB");
                    break;
                }
            }

            return rs;
        }
        public override IEnumerable<KeyValuePair<string, VFXShaderWriter>> additionalReplacements
        {
            get
            {
                foreach (var rep in base.additionalReplacements)
                    yield return rep;

                for (int i = 0; i < 3; i++)
                {
                    yield return new KeyValuePair<string, VFXShaderWriter>("${VFXDBufferColorMask" + i + "}",
                        GetDBufferMaskColor(i));
                }
                for (int i = 0; i < 4; i++)
                {
                    yield return new KeyValuePair<string, VFXShaderWriter>("${VFXGBufferDecalColorMask" + i + "}",
                        GetGBufferDecalMaskColor(i));
                }
            }
        }

        public override void OnSettingModified(VFXSetting setting)
        {
            if (setting.name == nameof(affectMAOS))
            {
                if (!affectMAOS)
                {
                    useOcclusionMap = false;
                    useMetallicMap = false;
                }
            }

            if (setting.name == nameof(affectBaseColor))
            {
                if (!affectBaseColor)
                {
                    useBaseColorMap = BaseColorMapMode.Alpha;
                }
                else
                {
                    useBaseColorMap = BaseColorMapMode.ColorAndAlpha;
                }
            }
        }
    }
}




#endif
