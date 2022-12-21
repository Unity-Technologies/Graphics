using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.VFX.HDRP
{
    abstract class VFXAbstractParticleHDRPLitOutput : VFXAbstractParticleHDRPOutput
    {
        public enum MaterialType
        {
            Standard,
            SpecularColor,
            Translucent,
            SimpleLit,
            SimpleLitTranslucent,
        }
        private readonly string[] kMaterialTypeToName = new string[]
        {
            "StandardProperties",
            "SpecularColorProperties",
            "TranslucentProperties",
            "StandardProperties",
            "TranslucentProperties",
        };

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Header("Lighting"), Tooltip("Specifies the surface type of this output. Surface types determine how the particle will react to light.")]
        protected MaterialType materialType = MaterialType.Standard;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, particles in this output are not affected by any lights in the scene and only receive ambient and light probe lighting.")]
        protected bool onlyAmbientLighting = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies the diffusion profile to be used to simulate light passing through the particle. The diffusion profile needs to be also added to the HDRP asset or in the scene’s diffusion profile override.")]
        protected UnityEngine.Rendering.HighDefinition.DiffusionProfileSettings diffusionProfileAsset = null;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the thickness of the particle is multiplied with its alpha value.")]
        protected bool multiplyThicknessWithAlpha = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the normals of the particle are inverted when seen from behind, allowing quads with culling set to off to receive correct lighting information.")]
        protected bool doubleSided = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, specular lighting will be rendered regardless of opacity.")]
        protected bool preserveSpecularLighting = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Header("Simple Lit features"), Tooltip("When enabled, the particle will receive shadows.")]
        protected bool enableShadows = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, particles will receive specular highlights.")]
        protected bool enableSpecular = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, particles can be affected by light cookies.")]
        protected bool enableCookie = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, particles can be affected by environment light set in the global volume profile.")]
        protected bool enableEnvLight = true;

        protected VFXAbstractParticleHDRPLitOutput(bool strip = false) : base(strip) { }

        public class HDRPLitInputProperties
        {
            [Range(0, 1), Tooltip("Controls the scale factor for the particle’s smoothness.")]
            public float smoothness = 0.5f;
        }

        public class StandardProperties
        {
            [Range(0, 1), Tooltip("Controls the scale factor for the particle’s metallicity.")]
            public float metallic = 0.0f;
        }

        public class SpecularColorProperties
        {
            [Tooltip("Sets the specular color of the particle.")]
            public Color specularColor = Color.gray;
        }

        public class TranslucentProperties
        {
            [Range(0, 1), Tooltip("Sets the thickness of the translucent particle. This affects the influence of the diffusion profile.")]
            public float thickness = 1.0f;
        }

        protected override RPInfo currentRP
        {
            get { return hdrpLitInfo; }
        }
        public override bool isLitShader { get => true; }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = Enumerable.Empty<VFXPropertyWithValue>();

                if (GetOrRefreshShaderGraphObject() == null)
                {
                    properties = properties.Concat(PropertiesFromType("HDRPLitInputProperties"));
                    properties = properties.Concat(PropertiesFromType(kMaterialTypeToName[(int)materialType]));
                }

                properties = properties.Concat(base.inputProperties);
                return properties;
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            if (GetOrRefreshShaderGraphObject() == null)
            {
                yield return slotExpressions.First(o => o.name == "smoothness");

                switch (materialType)
                {
                    case MaterialType.Standard:
                    case MaterialType.SimpleLit:
                        yield return slotExpressions.First(o => o.name == "metallic");
                        break;

                    case MaterialType.SpecularColor:
                        yield return slotExpressions.First(o => o.name == "specularColor");
                        break;

                    case MaterialType.Translucent:
                    case MaterialType.SimpleLitTranslucent:
                    {
                        yield return slotExpressions.First(o => o.name == "thickness");
                        uint diffusionProfileHash = (diffusionProfileAsset?.profile != null) ? diffusionProfileAsset.profile.hash : 0;
                        yield return new VFXNamedExpression(VFXValue.Constant(diffusionProfileHash), "diffusionProfileHash");
                        break;
                    }

                    default:
                        break;
                }
            }
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var d in base.additionalDefines)
                    yield return d;

                if (GetOrRefreshShaderGraphObject() == null)
                    switch (materialType)
                    {
                        case MaterialType.Standard:
                            yield return "HDRP_MATERIAL_TYPE_STANDARD";
                            break;

                        case MaterialType.SpecularColor:
                            yield return "HDRP_MATERIAL_TYPE_SPECULAR";
                            break;

                        case MaterialType.Translucent:
                            yield return "HDRP_MATERIAL_TYPE_TRANSLUCENT";
                            if (multiplyThicknessWithAlpha)
                                yield return "HDRP_MULTIPLY_THICKNESS_WITH_ALPHA";
                            break;

                        case MaterialType.SimpleLit:
                            yield return "HDRP_MATERIAL_TYPE_SIMPLELIT";
                            if (enableShadows)
                                yield return "HDRP_ENABLE_SHADOWS";
                            if (enableSpecular)
                                yield return "HDRP_ENABLE_SPECULAR";
                            if (enableCookie)
                                yield return "HDRP_ENABLE_COOKIE";
                            if (enableEnvLight)
                                yield return "HDRP_ENABLE_ENV_LIGHT";
                            break;

                        case MaterialType.SimpleLitTranslucent:
                            yield return "HDRP_MATERIAL_TYPE_SIMPLELIT_TRANSLUCENT";
                            if (enableShadows)
                                yield return "HDRP_ENABLE_SHADOWS";
                            if (enableSpecular)
                                yield return "HDRP_ENABLE_SPECULAR";
                            if (enableCookie)
                                yield return "HDRP_ENABLE_COOKIE";
                            if (enableEnvLight)
                                yield return "HDRP_ENABLE_ENV_LIGHT";
                            if (multiplyThicknessWithAlpha)
                                yield return "HDRP_MULTIPLY_THICKNESS_WITH_ALPHA";
                            break;

                        default:
                            break;
                    }
                if (doubleSided)
                    yield return "USE_DOUBLE_SIDED";

                if (onlyAmbientLighting && !isBlendModeOpaque)
                    yield return "USE_ONLY_AMBIENT_LIGHTING";

                if (isBlendModeOpaque && (GetOrRefreshShaderGraphObject() != null || (materialType != MaterialType.SimpleLit && materialType != MaterialType.SimpleLitTranslucent)))
                    yield return "IS_OPAQUE_NOT_SIMPLE_LIT_PARTICLE";
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

                if (materialType != MaterialType.Translucent && materialType != MaterialType.SimpleLitTranslucent)
                {
                    yield return "diffusionProfileAsset";
                    yield return "multiplyThicknessWithAlpha";
                }

                if (materialType != MaterialType.SimpleLit && materialType != MaterialType.SimpleLitTranslucent)
                {
                    yield return "enableShadows";
                    yield return "enableSpecular";
                    yield return "enableTransmission";
                    yield return "enableCookie";
                    yield return "enableEnvLight";
                }
            }
        }

        public override IEnumerable<KeyValuePair<string, VFXShaderWriter>> additionalReplacements
        {
            get
            {
                foreach (var kvp in base.additionalReplacements)
                    yield return kvp;

                // HDRP Forward specific defines
                var forwardDefines = new VFXShaderWriter();
                forwardDefines.WriteLine("#define _ENABLE_FOG_ON_TRANSPARENT");
                forwardDefines.WriteLine("#define _DISABLE_DECALS");

                if (!isBlendModeOpaque)
                {
                    if (preserveSpecularLighting)
                        forwardDefines.WriteLine("#define SUPPORT_BLENDMODE_PRESERVE_SPECULAR_LIGHTING");
                    forwardDefines.WriteLineFormat("#define _EnableBlendModePreserveSpecularLighting {0}", preserveSpecularLighting ? 1 : 0 );
                }

                yield return new KeyValuePair<string, VFXShaderWriter>("${VFXHDRPForwardDefines}", forwardDefines);
                var forwardPassName = new VFXShaderWriter();
                forwardPassName.Write(GetOrRefreshShaderGraphObject() == null && (materialType == MaterialType.SimpleLit || materialType == MaterialType.SimpleLitTranslucent) ? "ForwardOnly" : "Forward");
                yield return new KeyValuePair<string, VFXShaderWriter>("${VFXHDRPForwardPassName}", forwardPassName);
            }
        }
    }
}
