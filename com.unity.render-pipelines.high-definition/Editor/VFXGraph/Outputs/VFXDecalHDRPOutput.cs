using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.Block;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.VFX
{
    [VFXInfo(experimental = true)]
    class VFXDecalHDRPOutput : VFXAbstractParticleHDRPLitOutput
    {

        public override string name { get { return "Output Particle HDRP Decal"; } }
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate("VFXParticleHDRPDecal"); } }
        public override VFXTaskType taskType { get { return VFXTaskType.ParticleHexahedronOutput; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                if (colorMode != ColorMode.None)
                    yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleZ, VFXAttributeMode.Read);
            }
        }

        public enum BlendSource
        {
            BaseColorMapAlpha,
            MaskMapBlue,
        }
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies the source this Material uses as opacity for its Normal Map.")]
        BlendSource normalOpacityChannel = BlendSource.BaseColorMapAlpha;
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies the source this Material uses as opacity for its Mask Map.")]
        BlendSource maskOpacityChannel = BlendSource.BaseColorMapAlpha;

        public class NoMaskMapProperties
        {
            [Range(0, 1), Tooltip("Controls the metallic of the decal.")]
            public float metallic = 0.0f;
            [Range(0, 1), Tooltip("Controls the ambient occlusion of the decal.")]
            public float ambientOcclusion = 0.0f;
            [Range(0, 1), Tooltip("Controls the smoothness of the decal.")]
            public float smoothness = 0.0f;
        }

        public class BlendSourcesProperties
        {
            [Tooltip("Specifies the source this Material uses as opacity for its Normal Map."), Range(0,1)]
            public int maskBlendSrc = 0;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = base.inputProperties;
                //properties = properties.Concat(PropertiesFromType("BlendSourcesProperties"));
                return properties;
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

                yield return "cullMode";
                yield return "blendMode";
                yield return "useAlphaClipping";
                yield return "doubleSided";
                yield return "shaderGraph";
                yield return "zTestMode";
                yield return "zWriteMode";
            }
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var def in base.additionalDefines)
                    yield return def;
                if (maskOpacityChannel == BlendSource.BaseColorMapAlpha)
                {
                    yield return "VFX_MASK_BLEND_BASE_COLOR_ALPHA";
                }
                else
                {
                    yield return "VFX_MASK_BLEND_MASK_BLUE";
                }
                if (normalOpacityChannel == BlendSource.BaseColorMapAlpha)
                {
                    yield return "VFX_NORMAL_BLEND_BASE_COLOR_ALPHA";
                }
                else
                {
                    yield return "VFX_NORMAL_BLEND_MASK_BLUE";
                }
            }
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            var mapper = base.GetExpressionMapper(target);

            //if (target == VFXDeviceTarget.GPU)
            //{
            //    mapper.AddExpression(VFXValue.Constant((int)normalOpacityChannel), "normalBlendSrc", -1);
            //    mapper.AddExpression(VFXValue.Constant((int)maskOpacityChannel), "maskBlendSrc", -1);
            //}

            return mapper;
        }
    }
}
