using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.Block;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(experimental = true)]
    class VFXLitQuadStripOutput : VFXAbstractParticleHDRPLitOutput
    {
        protected VFXLitQuadStripOutput() : base(true) {}  // strips

        public override string name { get { return "Output ParticleStrip Lit Quad"; } }
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate("VFXParticleLitPlanarPrimitive"); } }
        public override VFXTaskType taskType { get { return VFXTaskType.ParticleQuadOutput; } }
        public override bool supportsUV { get { return true; } }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, a Normal Bending Factor slider becomes available in the output which can be used to adjust the curvature of the normals.")]
        protected bool normalBending = false;

        [VFXSetting, SerializeField, Tooltip("Specifies the way the UVs are interpolated along the strip. They can either be stretched or repeated per segment.")]
        private StripTilingMode tilingMode = StripTilingMode.Stretch;

        [VFXSetting, SerializeField, Tooltip("When enabled, uvs for the strips are swapped.")]
        protected bool swapUV = false;

        // Deprecated
        [VFXSetting(VFXSettingAttribute.VisibleFlags.None), SerializeField, Tooltip("When enabled, the axisZ attribute is used to orient the strip instead of facing the Camera.")]
        private bool UseCustomZAxis = false;

        public class NormalBendingProperties
        {
            [Range(0, 1), Tooltip("Controls the amount by which the normals will be bent, creating a rounder look.")]
            public float normalBendingFactor = 0.1f;
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
                var properties = base.inputProperties;
                if (normalBending)
                    properties = properties.Concat(PropertiesFromType("NormalBendingProperties"));
                if (tilingMode == StripTilingMode.Custom)
                    properties = properties.Concat(PropertiesFromType("CustomUVInputProperties"));
                return properties;
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                if (colorMode != ColorMode.None)
                    yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.Read);
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

                if (usesFlipbook)
                    yield return new VFXAttributeInfo(VFXAttribute.TexIndex, VFXAttributeMode.Read);
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            if (normalBending)
                yield return slotExpressions.First(o => o.name == "normalBendingFactor");
            if (tilingMode == StripTilingMode.Custom)
                yield return slotExpressions.First(o => o.name == "texCoord");
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var d in base.additionalDefines)
                    yield return d;

                if (normalBending)
                    yield return "USE_NORMAL_BENDING";

                if (tilingMode == StripTilingMode.Stretch)
                    yield return "VFX_STRIPS_UV_STRECHED";
                else if (tilingMode == StripTilingMode.RepeatPerSegment)
                    yield return "VFX_STRIPS_UV_PER_SEGMENT";

                if (swapUV)
                    yield return "VFX_STRIPS_SWAP_UV";

                yield return "FORCE_NORMAL_VARYING"; // To avoid discrepancy between depth and color pass which could cause glitch with ztest

                yield return VFXPlanarPrimitiveHelper.GetShaderDefine(VFXPrimitiveType.Quad);
            }
        }

        public override void Sanitize(int version)
        {
            VFXQuadStripOutput.SanitizeOrient(this, version, UseCustomZAxis);
            base.Sanitize(version);
        }
    }
}
