using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    abstract class VFXAbstractParticleHDRPLitOutput : VFXAbstractParticleOutput
    {
        public enum MaterialType
        {
            Standard,
            SpecularColor,
            Translucent,
        }

        [Flags]
        public enum ColorMode
        {
            None = 0,
            BaseColor = 1 << 0,
            Emissive = 1 << 1,
            BaseColorAndEmissive = BaseColor | Emissive,
        }

        private readonly string[] kMaterialTypeToName = new string[] {
            "StandardProperties",
            "SpecularColorProperties",
            "TranslucentProperties",
        };

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Header("Lighting")]
        protected MaterialType materialType = MaterialType.Standard;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Range(1, 15)]
        protected uint diffusionProfile = 1;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected bool useBaseColorMap = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected bool useMaskMap = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected bool useNormalMap = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected bool useEmissiveMap = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected ColorMode colorMode = ColorMode.BaseColor;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected bool useEmissive = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected bool doubleSided = false;

        protected virtual bool allowTextures { get { return true; }}

        public class HDRPLitInputProperties
        {
            [Range(0, 1)]
            public float smoothness = 0.5f;
        }

        public class StandardProperties
        {
            [Range(0, 1)]
            public float metallic = 0.5f;
        }

        public class SpecularColorProperties
        {
            public Color specularColor = Color.gray;
        }

        public class TranslucentProperties
        {
            [Range(0, 1)]
            public float thickness = 1.0f;
        }

        public class BaseColorMapProperties
        {
            [Tooltip("Base Color (RGB) Opacity (A)")]
            public Texture2D baseColorMap = VFXResources.defaultResources.particleTexture;
        }

        public class MaskMapProperties
        {
            [Tooltip("Metallic (R) AO (G) Smoothness (A)")]
            public Texture2D maskMap = VFXResources.defaultResources.noiseTexture;
        }

        public class NormalMapProperties
        {
            [Tooltip("Normal in tangent space")]
            public Texture2D normalMap;
            [Range(0, 2)]
            public float normalScale = 1.0f;
        }

        public class EmissiveMapProperties
        {
            [Tooltip("Normal in tangent space")]
            public Texture2D emissiveMap;
            public float emissiveScale = 1.0f;
        }

        public class BaseColorProperties
        {
            public Color baseColor = Color.black;
        }

        public class EmissiveColorProperties
        {
            public Color emissiveColor = Color.black;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = base.inputProperties;
                properties = properties.Concat(PropertiesFromType("HDRPLitInputProperties"));
                properties = properties.Concat(PropertiesFromType(kMaterialTypeToName[(int)materialType]));

                if (allowTextures)
                {
                    if (useBaseColorMap)
                        properties = properties.Concat(PropertiesFromType("BaseColorMapProperties"));
                }

                if ((colorMode & ColorMode.BaseColor) == 0) // particle color is not used as base color so add a slot
                    properties = properties.Concat(PropertiesFromType("BaseColorProperties"));

                if (allowTextures)
                {
                    if (useMaskMap)
                        properties = properties.Concat(PropertiesFromType("MaskMapProperties"));
                    if (useNormalMap)
                        properties = properties.Concat(PropertiesFromType("NormalMapProperties"));
                    if (useEmissiveMap)
                        properties = properties.Concat(PropertiesFromType("EmissiveMapProperties"));
                }

                if (((colorMode & ColorMode.Emissive) == 0) && useEmissive)
                    properties = properties.Concat(PropertiesFromType("EmissiveColorProperties"));

                return properties;
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            yield return slotExpressions.First(o => o.name == "smoothness");

            switch (materialType)
            {
                case MaterialType.Standard:
                    yield return slotExpressions.First(o => o.name == "metallic");
                    break;

                case MaterialType.SpecularColor:
                    yield return slotExpressions.First(o => o.name == "specularColor");
                    break;

                case MaterialType.Translucent:
                    yield return slotExpressions.First(o => o.name == "thickness");
                    yield return new VFXNamedExpression(VFXValue.Constant(diffusionProfile), "diffusionProfile");
                    break;

                default: break;
            }

            if (allowTextures)
            {
                if (useBaseColorMap)
                    yield return slotExpressions.First(o => o.name == "baseColorMap");
                if (useMaskMap)
                    yield return slotExpressions.First(o => o.name == "maskMap");
                if (useNormalMap)
                {
                    yield return slotExpressions.First(o => o.name == "normalMap");
                    yield return slotExpressions.First(o => o.name == "normalScale");
                }
                if (useEmissiveMap)
                {
                    yield return slotExpressions.First(o => o.name == "emissiveMap");
                    yield return slotExpressions.First(o => o.name == "emissiveScale");
                }
            }

            if ((colorMode & ColorMode.BaseColor) == 0)
                yield return slotExpressions.First(o => o.name == "baseColor");

            if (((colorMode & ColorMode.Emissive) == 0) && useEmissive)
                yield return slotExpressions.First(o => o.name == "emissiveColor");
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var d in base.additionalDefines)
                    yield return d;

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
                        break;

                    default: break;
                }

                if (allowTextures)
                {
                    if (useBaseColorMap)
                        yield return "HDRP_USE_BASE_COLOR_MAP";
                    if (useMaskMap)
                        yield return "HDRP_USE_MASK_MAP";
                    if (useNormalMap)
                        yield return "HDRP_USE_NORMAL_MAP";
                    if (useEmissiveMap)
                        yield return "HDRP_USE_EMISSIVE_MAP";
                }

                if ((colorMode & ColorMode.BaseColor) != 0)
                    yield return "HDRP_USE_BASE_COLOR";
                else
                    yield return "HDRP_USE_ADDITIONAL_BASE_COLOR";

                if ((colorMode & ColorMode.Emissive) != 0)
                    yield return "HDRP_USE_EMISSIVE_COLOR";
                else if (useEmissive)
                    yield return "HDRP_USE_ADDITIONAL_EMISSIVE_COLOR";

                if (doubleSided)
                    yield return "USE_DOUBLE_SIDED";
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

                if (materialType != MaterialType.Translucent)
                    yield return "diffusionProfile";

                if (!allowTextures)
                {
                    yield return "useBaseColorMap";
                    yield return "useMaskMap";
                    yield return "useNormalMap";
                    yield return "useEmissiveMap";
                }

                if ((colorMode & ColorMode.Emissive) != 0)
                    yield return "useEmissive";
            }
        }
    }
}
