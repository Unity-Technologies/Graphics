using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(experimental = true)]
    class VFXQuadStripOutput : VFXShaderGraphParticleOutput
    {
        [VFXSetting, SerializeField, Tooltip("Specifies the way the UVs are interpolated along the strip. They can either be stretched or repeated per segment.")]
        protected StripTilingMode tilingMode = StripTilingMode.Stretch;

        [VFXSetting, SerializeField, Tooltip("When enabled, uvs for the strips are swapped.")]
        protected bool swapUV = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the axisZ attribute is used to orient the strip instead of facing the Camera.")]
        private bool UseCustomZAxis = false;

        protected VFXQuadStripOutput() : base(true) { }
        public override string name { get { return "Output ParticleStrip Quad"; } }
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate("VFXParticlePlanarPrimitive"); } }
        public override VFXTaskType taskType { get { return VFXTaskType.ParticleQuadOutput; } }

        public override bool supportsUV { get { return true; } }

        public class OptionalInputProperties
        {
            [Tooltip("Specifies the base color (RGB) and opacity (A) of the particle.")]
            public Texture2D mainTexture = VFXResources.defaultResources.particleTexture;
        }

        public class CustomUVInputProperties
        {
            [Tooltip("Specifies the texture coordinate value (u or v depending on swap UV being enabled) used along the strip.")]
            public float texCoord = 0.0f; 
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                IEnumerable<VFXPropertyWithValue> properties = base.inputProperties;
                if (shaderGraph == null)
                    properties = properties.Concat(PropertiesFromType("OptionalInputProperties"));
                if (tilingMode == StripTilingMode.Custom)
                    properties = properties.Concat(PropertiesFromType("CustomUVInputProperties"));
                return properties;
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            if (shaderGraph == null)
                yield return slotExpressions.First(o => o.name == "mainTexture");
            if (tilingMode == StripTilingMode.Custom)
                yield return slotExpressions.First(o => o.name == "texCoord");
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.AngleX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);

                if (usesFlipbook)
                    yield return new VFXAttributeInfo(VFXAttribute.TexIndex, VFXAttributeMode.Read);
            }
        }



        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var def in base.additionalDefines)
                    yield return def;

                if (tilingMode == StripTilingMode.Stretch)
                    yield return "VFX_STRIPS_UV_STRECHED";
                else if (tilingMode == StripTilingMode.RepeatPerSegment)
                    yield return "VFX_STRIPS_UV_PER_SEGMENT";

                if (swapUV)
                    yield return "VFX_STRIPS_SWAP_UV";

                if (UseCustomZAxis)
                    yield return "VFX_STRIPS_ORIENT_CUSTOM";

                yield return VFXPlanarPrimitiveHelper.GetShaderDefine(VFXPrimitiveType.Quad);
            }
        }
    }
}
