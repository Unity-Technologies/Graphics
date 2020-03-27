using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEditor.VFX;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    abstract class VFXAbstractParticleOutput : VFXAbstractRenderedOutput, IVFXSubRenderer
    {
        public enum ColorMappingMode
        {
            Default,
            GradientMapped
        }

        public enum UVMode
        {
            Default,
            Flipbook,
            FlipbookBlend,
            ScaleAndBias,
            FlipbookMotionBlend,
        }

        public enum ZWriteMode
        {
            Default,
            Off,
            On
        }
        public enum CullMode
        {
            Default,
            Front,
            Back,
            Off
        }

        public enum ZTestMode
        {
            Default,
            Less,
            Greater,
            LEqual,
            GEqual,
            Equal,
            NotEqual,
            Always
        }

        public enum SortMode
        {
            Auto,
            Off,
            On
        }
        protected enum StripTilingMode
        {
            Stretch,
            RepeatPerSegment,
            Custom,
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies how the particle geometry is culled. This can be used to hide the front or back facing sides or make the mesh double-sided.")]
        protected CullMode cullMode = CullMode.Default;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies whether the particle is writing to the depth buffer.")]
        protected ZWriteMode zWriteMode = ZWriteMode.Default;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies how the particle rendering is affected by the depth buffer. By default, particles render if they are closer to the camera than solid objects in the scene.")]
        protected ZTestMode zTestMode = ZTestMode.Default;

        [VFXSetting, SerializeField, Tooltip("Specifies how particles are being colored in the pixel shader. They can either use the main texture, or their color and alpha can be remapped with a gradient based on the main texture values."), Header("Particle Options"), FormerlySerializedAs("colorMappingMode")]
        protected ColorMappingMode colorMapping;

        [VFXSetting, SerializeField, Tooltip("Specifies the UV mode used when sampling the texture on the particle. The UVs can encompass the whole particle by default, be resized and offset, or they can be segmented for use with a texture flipbook to simulate an animated texture."), FormerlySerializedAs("flipbookMode")]
        protected UVMode uvMode;

        [VFXSetting, SerializeField, Tooltip("When enabled, transparent particles fade out when near the surface of objects writing into the depth buffer (e.g. when intersecting with solid objects in the level).")]
        protected bool useSoftParticle = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.None), SerializeField, Header("Rendering Options"), Tooltip("")]
        protected int sortPriority = 0;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specified whether to use GPU sorting for transparent particles.")]
        protected SortMode sort = SortMode.Auto;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the system will only output alive particles, as opposed to rendering all particles and culling dead ones in the vertex shader. Enable to improve performance when the system capacity is not reached or a high number of vertices per particle are used. Indirect draw is implicit when sorting is on.")]
        protected bool indirectDraw = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, particles will cast shadows.")]
        protected bool castShadows = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, an exposure weight slider appears in the current output. The slider can be used to control how much influence exposure control will have on the particles.")]
        protected bool useExposureWeight = false;

        protected virtual bool bypassExposure { get { return true; } } // In case exposure weight is not used, tell whether pre exposure should be applied or not

        // IVFXSubRenderer interface
        public virtual bool hasShadowCasting { get { return castShadows; } }

        protected virtual bool needsExposureWeight { get { return true; } }

        private bool hasExposure { get { return needsExposureWeight && subOutput.supportsExposure; } }

        public bool HasIndirectDraw()   { return (indirectDraw || HasSorting()) && !HasStrips(true); }
        public bool HasSorting()        { return (sort == SortMode.On || (sort == SortMode.Auto && (blendMode == BlendMode.Alpha || blendMode == BlendMode.AlphaPremultiplied))) && !HasStrips(true); }
        int IVFXSubRenderer.sortPriority
        {
            get
            {
                return sortPriority;
            }
            set
            {
                if (sortPriority != value)
                {
                    sortPriority = value;
                    Invalidate(InvalidationCause.kSettingChanged);
                }
            }
        }
        public bool NeedsDeadListCount() { return HasIndirectDraw() && (taskType == VFXTaskType.ParticleQuadOutput || taskType == VFXTaskType.ParticleHexahedronOutput); } // Should take the capacity into account to avoid false positive

        public bool HasStrips(bool data = false) { return (data ? GetData().type : ownedType) == VFXDataType.ParticleStrip; }

        protected VFXAbstractParticleOutput(bool strip = false) : base(strip ? VFXDataType.ParticleStrip : VFXDataType.Particle) {}

        public override bool codeGeneratorCompute { get { return false; } }

        public virtual bool supportsUV { get { return false; } }

        public virtual CullMode defaultCullMode { get { return CullMode.Off; } }
        public virtual ZTestMode defaultZTestMode { get { return ZTestMode.LEqual; } }

        public virtual bool supportSoftParticles { get { return !isBlendModeOpaque; } }

        private bool hasSoftParticles => supportSoftParticles && useSoftParticle;

        protected bool usesFlipbook { get { return supportsUV && (uvMode == UVMode.Flipbook || uvMode == UVMode.FlipbookBlend || uvMode == UVMode.FlipbookMotionBlend); } }

        public virtual bool exposeAlphaThreshold
        {
            get
            {
                if (useAlphaClipping)
                    return true;
                //For Motion & Shadow, allow use a alpha clipping and it shares the same value as color clipping for transparent particles
                if (!isBlendModeOpaque && (hasMotionVector || hasShadowCasting))
                    return true;
                return false;
            }
        }

        protected virtual IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            if (exposeAlphaThreshold)
                yield return slotExpressions.First(o => o.name == "alphaThreshold");

            if (colorMapping == ColorMappingMode.GradientMapped)
            {
                yield return slotExpressions.First(o => o.name == "gradient");
            }

            if (hasSoftParticles)
            {
                var softParticleFade = slotExpressions.First(o => o.name == "softParticleFadeDistance");
                var invSoftParticleFade = (VFXValue.Constant(1.0f) / softParticleFade.exp);
                yield return new VFXNamedExpression(invSoftParticleFade, "invSoftParticlesFadeDistance");
            }

            if (supportsUV && uvMode != UVMode.Default)
            {
                VFXNamedExpression flipBookSizeExp;
                switch (uvMode)
                {
                    case UVMode.Flipbook:
                    case UVMode.FlipbookBlend:
                    case UVMode.FlipbookMotionBlend:
                        flipBookSizeExp = slotExpressions.First(o => o.name == "flipBookSize");
                        yield return flipBookSizeExp;
                        yield return new VFXNamedExpression(VFXValue.Constant(Vector2.one) / flipBookSizeExp.exp, "invFlipBookSize");
                        if (uvMode == UVMode.FlipbookMotionBlend)
                        {
                            yield return slotExpressions.First(o => o.name == "motionVectorMap");
                            yield return slotExpressions.First(o => o.name == "motionVectorScale");
                        }
                        break;
                    case UVMode.ScaleAndBias:
                        yield return slotExpressions.First(o => o.name == "uvScale");
                        yield return slotExpressions.First(o => o.name == "uvBias");
                        break;
                    default: throw new NotImplementedException("Unimplemented UVMode: " + uvMode);
                }
            }

            if (hasExposure && useExposureWeight)
                yield return slotExpressions.First(o => o.name == "exposureWeight");
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            if (target == VFXDeviceTarget.GPU)
            {
                var gpuMapper = VFXExpressionMapper.FromBlocks(activeFlattenedChildrenWithImplicit);
                gpuMapper.AddExpressions(CollectGPUExpressions(GetExpressionsFromSlots(this)), -1);
                if (generateMotionVector)
                    gpuMapper.AddExpression(VFXBuiltInExpression.FrameIndex, "currentFrameIndex", -1);
                return gpuMapper;
            }
            return new VFXExpressionMapper();
        }

        public class InputPropertiesGradientMapped
        {
            [Tooltip("The gradient used to sample color")]
            public Gradient gradient = VFXResources.defaultResources.gradientMapRamp;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                foreach (var property in PropertiesFromType(GetInputPropertiesTypeName()))
                    yield return property;

                if (colorMapping == ColorMappingMode.GradientMapped)
                {
                    foreach (var property in PropertiesFromType("InputPropertiesGradientMapped"))
                        yield return property;
                }

                if (supportsUV && uvMode != UVMode.Default)
                {
                    switch (uvMode)
                    {
                        case UVMode.Flipbook:
                        case UVMode.FlipbookBlend:
                        case UVMode.FlipbookMotionBlend:
                            yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector2), "flipBookSize"), new Vector2(4, 4));
                            if (uvMode == UVMode.FlipbookMotionBlend)
                            {
                                yield return new VFXPropertyWithValue(new VFXProperty(typeof(Texture2D), "motionVectorMap"));
                                yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "motionVectorScale"), 1.0f);
                            }
                            break;
                        case UVMode.ScaleAndBias:
                            yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector2), "uvScale"), Vector2.one);
                            yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector2), "uvBias"), Vector2.zero);
                            break;
                        default: throw new NotImplementedException("Unimplemented UVMode: " + uvMode);
                    }
                }

                if (exposeAlphaThreshold)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "alphaThreshold", VFXPropertyAttribute.Create(new RangeAttribute(0.0f, 1.0f), new TooltipAttribute("Alpha threshold used for pixel clipping"))), 0.5f);

                if (hasSoftParticles)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "softParticleFadeDistance", VFXPropertyAttribute.Create(new MinAttribute(0.001f))), 1.0f);

                if (hasExposure && useExposureWeight)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "exposureWeight", VFXPropertyAttribute.Create(new RangeAttribute(0.0f, 1.0f))), 1.0f);
            }
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                switch (colorMapping)
                {
                    case ColorMappingMode.Default:
                        yield return "VFX_COLORMAPPING_DEFAULT";
                        break;
                    case ColorMappingMode.GradientMapped:
                        yield return "VFX_COLORMAPPING_GRADIENTMAPPED";
                        break;
                }

                if (isBlendModeOpaque)
                    yield return "IS_OPAQUE_PARTICLE";
                else
                    yield return "IS_TRANSPARENT_PARTICLE";

                if (hasAlphaClipping)
                    yield return "USE_ALPHA_TEST";
                if (hasSoftParticles)
                    yield return "USE_SOFT_PARTICLE";

                switch (blendMode)
                {
                    case BlendMode.Alpha:
                        yield return "VFX_BLENDMODE_ALPHA";
                        break;
                    case BlendMode.Additive:
                        yield return "VFX_BLENDMODE_ADD";
                        break;
                    case BlendMode.AlphaPremultiplied:
                        yield return "VFX_BLENDMODE_PREMULTIPLY";
                        break;
                }

                {
                    if (hasMotionVector)
                        if (isBlendModeOpaque)
                            yield return "USE_MOTION_VECTORS_PASS";
                        else
                            yield return "WRITE_MOTION_VECTOR_IN_FORWARD";
                    if (hasShadowCasting)
                        yield return "USE_CAST_SHADOWS_PASS";
                }

                if (HasIndirectDraw())
                    yield return "VFX_HAS_INDIRECT_DRAW";

                if (supportsUV && uvMode != UVMode.Default)
                {
                    switch (uvMode)
                    {
                        case UVMode.Flipbook:
                            yield return "USE_FLIPBOOK";
                            break;
                        case UVMode.FlipbookBlend:
                            yield return "USE_FLIPBOOK";
                            yield return "USE_FLIPBOOK_INTERPOLATION";
                            break;
                        case UVMode.FlipbookMotionBlend:
                            yield return "USE_FLIPBOOK";
                            yield return "USE_FLIPBOOK_INTERPOLATION";
                            yield return "USE_FLIPBOOK_MOTIONVECTORS";
                            break;
                        case UVMode.ScaleAndBias:
                            yield return "USE_UV_SCALE_BIAS";
                            break;
                        default: throw new NotImplementedException("Unimplemented UVMode: " + uvMode);
                    }
                }

                if (hasExposure)
                {
                    if (useExposureWeight)
                        yield return "USE_EXPOSURE_WEIGHT";
                    else if (bypassExposure)
                        yield return "VFX_BYPASS_EXPOSURE";
                }

                if (NeedsDeadListCount() && GetData().IsAttributeStored(VFXAttribute.Alive)) //Actually, there are still corner cases, e.g.: particles spawning immortal particles through GPU Event
                    yield return "USE_DEAD_LIST_COUNT";

                if (HasStrips(false))
                    yield return "HAS_STRIPS";
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (!supportsUV)
                    yield return "uvMode";

                if (!implementsMotionVector || !subOutput.supportsMotionVector)
                    yield return "generateMotionVector";

                if (!supportSoftParticles)
                {
                    yield return "useSoftParticle";
                }

                if (!hasExposure)
                    yield return "useExposureWeight";

                // No indirect / sorting support now for strips
                if (HasStrips(true))
                {
                    yield return "indirectDraw";
                    yield return "sort";
                }
            }
        }

        public override IEnumerable<KeyValuePair<string, VFXShaderWriter>> additionalReplacements
        {
            get
            {
                yield return new KeyValuePair<string, VFXShaderWriter>("${VFXOutputRenderState}", renderState);

                var shaderTags = new VFXShaderWriter();
                var renderQueueStr = subOutput.GetRenderQueueStr();
                var renderTypeStr = isBlendModeOpaque ? "Opaque" : "Transparent";
                shaderTags.Write(string.Format("Tags {{ \"Queue\"=\"{0}\" \"IgnoreProjector\"=\"{1}\" \"RenderType\"=\"{2}\" }}", renderQueueStr, !isBlendModeOpaque, renderTypeStr));
                yield return new KeyValuePair<string, VFXShaderWriter>("${VFXShaderTags}", shaderTags);

                foreach (var additionnalStencilReplacement in subOutput.GetStencilStateOverridesStr())
                {
                    yield return additionnalStencilReplacement;
                }
            }
        }

        protected virtual VFXShaderWriter renderState
        {
            get
            {
                var rs = new VFXShaderWriter();

                WriteBlendMode(rs);

                var zTest = zTestMode;
                if (zTest == ZTestMode.Default)
                    zTest = defaultZTestMode;

                switch (zTest)
                {
                    case ZTestMode.Default: rs.WriteLine("ZTest LEqual"); break;
                    case ZTestMode.Always: rs.WriteLine("ZTest Always"); break;
                    case ZTestMode.Equal: rs.WriteLine("ZTest Equal"); break;
                    case ZTestMode.GEqual: rs.WriteLine("ZTest GEqual"); break;
                    case ZTestMode.Greater: rs.WriteLine("ZTest Greater"); break;
                    case ZTestMode.LEqual: rs.WriteLine("ZTest LEqual"); break;
                    case ZTestMode.Less: rs.WriteLine("ZTest Less"); break;
                    case ZTestMode.NotEqual: rs.WriteLine("ZTest NotEqual"); break;
                }

                switch (zWriteMode)
                {
                    case ZWriteMode.Default:
                        if (isBlendModeOpaque)
                            rs.WriteLine("ZWrite On");
                        else
                            rs.WriteLine("ZWrite Off");
                        break;
                    case ZWriteMode.On: rs.WriteLine("ZWrite On"); break;
                    case ZWriteMode.Off: rs.WriteLine("ZWrite Off"); break;
                }

                var cull = cullMode;
                if (cull == CullMode.Default)
                    cull = defaultCullMode;

                switch (cull)
                {
                    case CullMode.Default: rs.WriteLine("Cull Off"); break;
                    case CullMode.Front: rs.WriteLine("Cull Front"); break;
                    case CullMode.Back: rs.WriteLine("Cull Back"); break;
                    case CullMode.Off: rs.WriteLine("Cull Off"); break;
                }

                return rs;
            }
        }

        public override IEnumerable<VFXMapping> additionalMappings
        {
            get
            {
                yield return new VFXMapping("sortPriority", sortPriority);
                if (HasIndirectDraw())
                    yield return new VFXMapping("indirectDraw", 1);
            }
        }
    }
}
