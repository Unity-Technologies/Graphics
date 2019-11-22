using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.Block;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(experimental = true)]
    class VFXDistortionQuadStripOutput : VFXAbstractDistortionOutput
    {
        [VFXSetting, SerializeField, Tooltip("Specifies the way the UVs are interpolated along the strip. They can either be stretched or repeated per segment.")]
        protected StripTilingMode tilingMode = StripTilingMode.Stretch;

        [VFXSetting, SerializeField, Tooltip("When enabled, uvs for the strips are swapped.")]
        protected bool swapUV = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the axisZ attribute is used to orient the strip instead of facing the Camera.")]
        private bool UseCustomZAxis = false;

        VFXDistortionQuadStripOutput() : base(true) { }

        public override string name { get { return "Output Strip Distortion Quad"; } }
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate("VFXParticleDistortionPlanarPrimitive"); } }
        public override VFXTaskType taskType => VFXTaskType.ParticleQuadOutput;
        public override bool supportsUV { get { return true; } }

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
                if (tilingMode == StripTilingMode.Custom)
                    properties = properties.Concat(PropertiesFromType("CustomUVInputProperties"));
                return properties;
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;
            if (tilingMode == StripTilingMode.Custom)
                yield return slotExpressions.First(o => o.name == "texCoord");
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

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
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

                if (usesFlipbook)
                    yield return new VFXAttributeInfo(VFXAttribute.TexIndex, VFXAttributeMode.Read);
            }
        }
    }
}
