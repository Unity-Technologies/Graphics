using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace UnityEditor.VFX.HDRP
{
    [VFXInfo(name = "Output ParticleStrip|HDRP Distortion|Quad", category = "#3Output Strip", synonyms = new []{ "Trail", "Ribbon" })]
    class VFXDistortionQuadStripOutput : VFXAbstractDistortionOutput
    {
        [VFXSetting, SerializeField, Tooltip("Specifies the way the UVs are interpolated along the strip. They can either be stretched or repeated per segment.")]
        protected StripTilingMode tilingMode = StripTilingMode.Stretch;

        [VFXSetting, SerializeField, Tooltip("When enabled, uvs for the strips are swapped.")]
        protected bool swapUV = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the axisZ attribute is used to orient the strip instead of facing the Camera.")]
        private bool UseCustomZAxis = false;

        VFXDistortionQuadStripOutput() : base(true) { }

        public override string name => "Output ParticleStrip".AppendLabel("HDRP Distortion", false) + "\nQuad";
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate("VFXParticleDistortionPlanarPrimitive"); } }
        public override VFXTaskType taskType => VFXTaskType.ParticleQuadOutput;
        public override bool supportsUV { get { return true; } }
        public override bool implementsMotionVector { get { return true; } }


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

        internal sealed override void GenerateErrors(VFXErrorReporter report)
        {
            if (GetAttributesInfos().Any(x => x.mode.HasFlag(VFXAttributeMode.Write) && x.attrib.Equals(VFXAttribute.Position)))
            {
                report.RegisterError("WritePositionInStrip", VFXErrorType.Warning, VFXQuadStripOutput.WriteToPositionMessage, this);
            }
        }
    }
}
