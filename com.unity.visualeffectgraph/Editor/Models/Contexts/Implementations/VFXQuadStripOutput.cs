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

        // Deprecated
        [VFXSetting(VFXSettingAttribute.VisibleFlags.None), SerializeField, Tooltip("When enabled, the axisZ attribute is used to orient the strip instead of facing the Camera.")]
        private bool UseCustomZAxis = false;

        protected VFXQuadStripOutput() : base(true) { }

        public override string name
        {
            get
            {
                if (shaderName != string.Empty)
                    return $"Output ParticleStrip {shaderName} Quad";
                return "Output ParticleStrip Quad";
            }
        }
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate("VFXParticlePlanarPrimitive"); } }
        public override VFXTaskType taskType { get { return VFXTaskType.ParticleQuadOutput; } }
        public override bool supportsUV { get { return true; } }
        public override bool implementsMotionVector { get { return true; } }

        protected IEnumerable<VFXPropertyWithValue> optionalInputProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(GetFlipbookType(), "mainTexture", new TooltipAttribute("Specifies the base color (RGB) and opacity (A) of the particle.")), (usesFlipbook ? null : VFXResources.defaultResources.particleTexture));
            }
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
                if (GetOrRefreshShaderGraphObject() == null)
                    properties = properties.Concat(optionalInputProperties);
                if (tilingMode == StripTilingMode.Custom)
                    properties = properties.Concat(PropertiesFromType("CustomUVInputProperties"));
                return properties;
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            if (GetOrRefreshShaderGraphObject() == null)
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

                yield return VFXPlanarPrimitiveHelper.GetShaderDefine(VFXPrimitiveType.Quad);
            }
        }

        public static void SanitizeOrient(VFXContext model, int version, bool useCustomZAxis)
        {
            if (version < 6)
            {
                Block.Orient orientBlock = model.children.OfType<Block.Orient>().FirstOrDefault();
                if (orientBlock == null) // If no orient block, add one
                {
                    Debug.Log("Sanitize Graph: Add Orient block to quad strip output");

                    orientBlock = CreateInstance<Block.Orient>();
                    if (useCustomZAxis)
                    {
                        orientBlock.SetSettingValue("mode", Block.Orient.Mode.CustomZ);

                        var axisZNode = CreateInstance<VFXAttributeParameter>();
                        axisZNode.SetSettingValue("attribute", "axisZ");
                        axisZNode.position = model.position + new Vector2(-225, 150);
                        model.GetGraph().AddChild(axisZNode);

                        axisZNode.GetOutputSlot(0).Link(orientBlock.GetInputSlot(0));
                    }
                    else
                        orientBlock.SetSettingValue("mode", Block.Orient.Mode.FaceCameraPosition);

                    model.AddChild(orientBlock, 0);
                }
                else
                {
                    if ((Block.Orient.Mode)orientBlock.GetSettingValue("mode") == Block.Orient.Mode.FaceCameraPlane)
                    {
                        Debug.Log("Sanitize Graph: Change Orient block mode in quad strip output from \"Face Camera Plane\" to \"Face Camera Position\"");
                        orientBlock.SetSettingValue("mode", Block.Orient.Mode.FaceCameraPosition);
                    }
                    // Other invalid modes (Along Velocity and FixedAxis) will fail and require manual fixing
                }
            }
        }

        public override void Sanitize(int version)
        {
            SanitizeOrient(this, version, UseCustomZAxis);
            base.Sanitize(version);
        }
    }
}
