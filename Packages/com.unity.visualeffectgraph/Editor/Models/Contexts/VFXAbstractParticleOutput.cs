using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.VFX
{
    [CustomEditor(typeof(VFXAbstractParticleOutput), true)]
    [CanEditMultipleObjects]
    partial class VFXAbstractParticleOutputEditor : VFXContextEditor
    {
        private static string k_HDRPAssetTypeStr = "HDRenderPipelineAsset";
        private static string k_RenderPipelineUnsupportedWarning =
            "Ray Tracing features are only available in HDRP. They will be ignored.";
        private static string k_DisabledRayTracingWarning =
            "To use Ray Tracing, enable \"Supported Ray Tracing \" and \" Visual Effects Ray Tracing \" in your active HDRP asset";
        public override void DisplayWarnings()
        {
            base.DisplayWarnings();
            foreach (VFXAbstractParticleOutput output in targets)
            {
                if(output.isRayTraced)
                    if (VFXLibrary.currentSRPBinder.SRPAssetTypeStr != k_HDRPAssetTypeStr)
                    {
                        EditorGUILayout.HelpBox(
                            k_RenderPipelineUnsupportedWarning,
                            MessageType.Warning);
                    }
                    else
                    {
                        if(!VFXLibrary.currentSRPBinder.GetSupportsRayTracing())
                            EditorGUILayout.HelpBox(k_DisabledRayTracingWarning, MessageType.Warning);
                    }
            }
        }
    }
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
            ScaleAndBias = 3
        }

        public enum SortActivationMode
        {
            Auto,
            Off,
            On
        }

        public enum StripTilingMode
        {
            Stretch,
            RepeatPerSegment,
            Custom,
        }

        protected enum FlipbookLayout
        {
            Texture2D,
            Texture2DArray
        }

        protected enum RayTracedScaleMode
        {
            Default,
            None,
            Custom,
        }

        [VFXSetting, SerializeField, Tooltip("Specifies how particles are being colored in the pixel shader. They can either use the main texture, or their color and alpha can be remapped with a gradient based on the main texture values."), Header("Particle Options"), FormerlySerializedAs("colorMappingMode")]
        protected ColorMappingMode colorMapping;

        [VFXSetting, SerializeField, Tooltip("Specifies the UV mode used when sampling the texture on the particle. The UVs can encompass the whole particle by default, be resized and offset, or they can be segmented for use with a texture flipbook to simulate an animated texture."), FormerlySerializedAs("flipbookMode")]
        protected UVMode uvMode;

        [VFXSetting, SerializeField, Tooltip("Specifies the layout of the flipbook. It can either use a single texture with multiple frames, or a Texture2DArray with multiple slices.")]
        protected FlipbookLayout flipbookLayout = FlipbookLayout.Texture2D;

        [VFXSetting, SerializeField, Tooltip("Blend between frames of the flipbook.")]
        protected bool flipbookBlendFrames = false;

        [VFXSetting, SerializeField, Tooltip("Use motion vectors to improve blending between frames of the flipbook.")]
        protected bool flipbookMotionVectors = false;

        [VFXSetting, SerializeField, Tooltip("When enabled, transparent particles fade out when near the surface of objects writing into the depth buffer (e.g. when intersecting with solid objects in the level).")]
        protected bool useSoftParticle = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.None), FormerlySerializedAs("sortPriority"), SerializeField, Header("Rendering Options"), Tooltip("")]
        protected int vfxSystemSortPriority = 0;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies whether to use GPU sorting for transparent particles.")]
        protected SortActivationMode sort = SortActivationMode.Auto;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies the draw order of particles. They can be sorted by their distance, age, depth, or by a custom value.")]
        protected VFXSortingUtility.SortCriteria sortMode = VFXSortingUtility.SortCriteria.DistanceToCamera;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Reverses the drawing order of the particles.")]
        internal bool revertSorting = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the system will only output alive particles, as opposed to rendering all particles and culling dead ones in the vertex shader. Enable to improve performance when the system capacity is not reached or a high number of vertices per particle are used.")]
        protected bool indirectDraw = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, particle that are not alive in the output are culled in a compute pass rather than in the vertex shader. Enable this to improve performance if you're setting alive attribute in the output and have a high number of vertices per particle.")]
        protected bool computeCulling = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, particles that are outside of the camera frustum are culled in a compute pass. Use this to improve performance of large systems. Note that frustum culling can cause issues with shadow casting as particles outside of the camera frustum will not be taken into account in the shadow passes.")]
        protected bool frustumCulling = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, particles will cast shadows.")]
        protected bool castShadows = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, an exposure weight slider appears in the current output. The slider can be used to control how much influence exposure control will have on the particles.")]
        protected bool useExposureWeight = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Header("Ray Tracing"), Tooltip("When enabled, particles will participate in the ray-traced effects.")]
        protected bool enableRayTracing = false;

        [VFXSetting, Delayed, Logarithmic(2, true), Range(1, 1 << 17), SerializeField, Tooltip("Specifies the inverse of the proportion of particles to include in ray-traced effects.")]
        protected uint decimationFactor = 1;

        [VFXSetting, SerializeField, Tooltip("Specifies how to scale the particles included in ray-traced effects.")]
        protected RayTracedScaleMode raytracedScaleMode = RayTracedScaleMode.Default;

        protected virtual bool bypassExposure { get { return true; } } // In case exposure weight is not used, tell whether pre exposure should be applied or not

        // IVFXSubRenderer interface
        public virtual bool hasShadowCasting { get { return castShadows; } }

        public virtual bool isRayTraced { get { return !HasStrips(false) && enableRayTracing; } }
        protected virtual bool needsExposureWeight { get { return true; } }

        protected virtual bool hasExposure { get { return needsExposureWeight && subOutput.supportsExposure; } }

        public virtual void SetupMaterial(Material material) { }

        protected bool HasUpdateInputContext()
        {
            foreach (var inputContext in inputContexts)
            {
                if (inputContext.contextType.HasFlag(VFXContextType.Update))
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasIndirectDraw() { return ((indirectDraw && HasUpdateInputContext()) || HasSorting() || VFXOutputUpdate.HasFeature(outputUpdateFeatures, VFXOutputUpdate.Features.IndirectDraw)); }
        public virtual bool HasSorting() { return sort == SortActivationMode.On || (sort == SortActivationMode.Auto && (blendMode == BlendMode.Alpha || blendMode == BlendMode.AlphaPremultiplied)); }

        public bool HasCustomSortingCriterion() { return HasSorting() && sortMode == VFXSortingUtility.SortCriteria.Custom; }
        public bool HasComputeCulling() { return computeCulling; }
        public bool HasFrustumCulling() { return frustumCulling; }
        public virtual bool NeedsOutputUpdate() { return outputUpdateFeatures != VFXOutputUpdate.Features.None; }

        public uint GetRaytracingDecimationFactor() { return decimationFactor; }

        public bool needsOwnSort = false;

        public VFXSortingUtility.SortCriteria GetSortCriterion() { return sortMode; }

        public override void OnEnable()
        {
            switch ((int)uvMode)
            {
                case 2: // FlipbookBlend
                    uvMode = UVMode.Flipbook;
                    flipbookBlendFrames = true;
                    flipbookMotionVectors = false;
                    break;
                case 4: // FlipbookMotionBlend
                    uvMode = UVMode.Flipbook;
                    flipbookBlendFrames = true;
                    flipbookMotionVectors = true;
                    break;
            }
            base.OnEnable();
        }

        public bool NeedsOwnAabbBuffer()
        {
            return needsOwnAabbBuffer;
        }
        public bool ModifiesAabbAttributes()
        {
            if (!isRayTraced)
                return false;
            var writtenAttributes = GetAttributesInfos().Where(o => (VFXAttributeMode.Write & o.mode) != 0).Select(o => o.attrib);
            bool modifiesAttributes = writtenAttributes.Intersect(VFXAttributesManager.AffectingAABBAttributes).Count() > 0;
            return modifiesAttributes || raytracedScaleMode == RayTracedScaleMode.Custom;
        }

        public bool HasSameRayTracingScalingMode(VFXAbstractParticleOutput otherOutput)
        {
            return otherOutput.raytracedScaleMode == raytracedScaleMode;
        }

        public bool needsOwnAabbBuffer = false;

        public virtual VFXOutputUpdate.Features outputUpdateFeatures
        {
            get
            {
                VFXOutputUpdate.Features features = VFXOutputUpdate.Features.None;
                if (hasMotionVector)
                    features |= VFXOutputUpdate.Features.MotionVector;
                if (HasComputeCulling())
                    features |= VFXOutputUpdate.Features.Culling;
                if (HasSorting() && (VFXOutputUpdate.HasFeature(features, VFXOutputUpdate.Features.IndirectDraw) || needsOwnSort))
                {
                    if (VFXSortingUtility.IsPerCamera(sortMode))
                        features |= VFXOutputUpdate.Features.CameraSort;
                    else
                        features |= VFXOutputUpdate.Features.Sort;
                }
                if (HasFrustumCulling())
                    features |= VFXOutputUpdate.Features.FrustumCulling;
                if (NeedsOwnAabbBuffer())
                    features |= VFXOutputUpdate.Features.FillRaytracingAABB;
                return features;
            }
        }

        int IVFXSubRenderer.vfxSystemSortPriority
        {
            get
            {
                return vfxSystemSortPriority;
            }
            set
            {
                if (vfxSystemSortPriority != value)
                {
                    vfxSystemSortPriority = value;
                    Invalidate(InvalidationCause.kSettingChanged);
                }
            }
        }
        public bool NeedsDeadListCount() { return HasIndirectDraw() && !HasStrips(true) && (taskType == VFXTaskType.ParticleQuadOutput || taskType == VFXTaskType.ParticleHexahedronOutput); } // Should take the capacity into account to avoid false positive

        public bool HasStrips(bool data = false)
        {
            if (GetData() == null)
                return false;
            return (data ? GetData().type : ownedType) == VFXDataType.ParticleStrip;
        }
        public bool HasStripsData() { return GetData().type == VFXDataType.ParticleStrip; }

        protected VFXAbstractParticleOutput(bool strip = false) : base(strip ? VFXDataType.ParticleStrip : VFXDataType.Particle) { }

        public override bool codeGeneratorCompute { get { return false; } }

        public virtual bool supportsUV { get { return false; } }

        public virtual CullMode defaultCullMode { get { return CullMode.Off; } }
        public virtual ZTestMode defaultZTestMode { get { return ZTestMode.LEqual; } }

        public virtual bool supportSoftParticles { get { return !isBlendModeOpaque; } }

        private bool hasSoftParticles => supportSoftParticles && useSoftParticle;

        public bool usesFlipbook { get { return supportsUV && uvMode == UVMode.Flipbook; } }
        public bool flipbookHasInterpolation { get { return usesFlipbook && flipbookBlendFrames; } }
        public bool flipbookHasMotionVectors { get { return flipbookHasInterpolation && flipbookMotionVectors; } }

        public Type GetFlipbookType()
        {
            switch (flipbookLayout)
            {
                case FlipbookLayout.Texture2D:
                    return typeof(Texture2D);
                case FlipbookLayout.Texture2DArray:
                    return typeof(Texture2DArray);
                default:
                    throw new NotImplementedException("Unimplemented Flipbook Layout: " + flipbookLayout);
            }
        }

        public Type GetTextureType()
        {
            if (usesFlipbook)
            {
                return GetFlipbookType();
            }
            else
            {
                return typeof(Texture2D);
            }
        }

        public virtual bool exposeAlphaThreshold
        {
            get
            {
                if (useAlphaClipping)
                    return true;
                //For Motion & Shadow, allow use a alpha clipping and it shares the same value as color clipping for transparent particles
                // Ray traced transparent particle should also expose an alpha threshold for effects using visibility pass like RTAO.
                if (!isBlendModeOpaque && (hasMotionVector || hasShadowCasting || isRayTraced))
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
                switch (uvMode)
                {
                    case UVMode.Flipbook:
                        if (flipbookLayout == FlipbookLayout.Texture2D)
                        {
                            var flipBookSizeExp = slotExpressions.FirstOrDefault(o => o.name == "flipBookSize").exp;
                            yield return new VFXNamedExpression(flipBookSizeExp, "flipBookSize");
                            yield return new VFXNamedExpression(VFXValue.Constant(Vector2.one) / flipBookSizeExp, "invFlipBookSize");
                        }
                        else if (flipbookLayout == FlipbookLayout.Texture2DArray)
                        {
                            VFXNamedExpression mainTextureExp;
                            try
                            {
                                mainTextureExp = slotExpressions.First(o =>
                                    (o.name == "mainTexture") | (o.name == "baseColorMap") |
                                    (o.name == "distortionBlurMap") | (o.name == "normalMap") |
                                    (o.name == "emissiveMap") | (o.name == "positiveAxesLightmap"));
                            }
                            catch (InvalidOperationException)
                            {
                                throw new NotImplementedException("Trying to fetch an inexistent slot Main Texture or Base Color Map or Distortion Blur Map or Normal Map or Emissive Map or Six Way Map. ");
                            }
                            yield return new VFXNamedExpression(new VFXExpressionCastUintToFloat(new VFXExpressionTextureDepth(mainTextureExp.exp)), "flipBookSize");
                        }
                        if (flipbookHasMotionVectors)
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
            if (isRayTraced && raytracedScaleMode == RayTracedScaleMode.Custom)
                yield return slotExpressions.First(o => o.name == "rayTracedScaling");

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

        public class InputPropertiesSortKey
        {
            [Tooltip("Sets the value for particle sorting in this output. Particles with lower values are rendered first and appear behind those with higher values.")]
            public float sortKey = 0.0f;
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
                            if (flipbookLayout == FlipbookLayout.Texture2D)
                            {
                                yield return new VFXPropertyWithValue(new VFXProperty(typeof(FlipBook), "flipBookSize"), FlipBook.defaultValue);
                            }

                            if (flipbookHasMotionVectors)
                            {
                                yield return new VFXPropertyWithValue(new VFXProperty(GetFlipbookType(), "motionVectorMap"));
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
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "alphaThreshold", new RangeAttribute(0.0f, 1.0f), new TooltipAttribute("Alpha threshold used for pixel clipping")), 0.5f);

                if (hasSoftParticles)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "softParticleFadeDistance", new MinAttribute(0.001f)), 1.0f);

                if (hasExposure && useExposureWeight)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "exposureWeight", new RangeAttribute(0.0f, 1.0f)), 1.0f);
                if (HasCustomSortingCriterion())
                {
                    foreach (var property in PropertiesFromType("InputPropertiesSortKey"))
                        yield return property;
                }

                if (isRayTraced && raytracedScaleMode == RayTracedScaleMode.Custom)
                {
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector2), "rayTracedScaling"), Vector2.one);
                }
            }
        }

        protected IEnumerable<VFXAttributeInfo> flipbookAttributes
        {
            get
            {
                if (usesFlipbook)
                {
                    yield return new VFXAttributeInfo(VFXAttribute.TexIndex, VFXAttributeMode.Read);
                    if (flipbookBlendFrames)
                    {
                        yield return new VFXAttributeInfo(VFXAttribute.TexIndexBlend, VFXAttributeMode.Read);
                    }
                }
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
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

                foreach (var attribute in flipbookAttributes)
                    yield return attribute;
            }
        }

        public IEnumerable<string> rayTracingDefines
        {
            get
            {
                yield return "VFX_IS_RAYTRACED";
                yield return "VFX_RT_DECIMATION_FACTOR " + decimationFactor;

                var particleData = GetData() as VFXDataParticle;
                uint capacity = 0u;
                if (particleData != null)
                {
                    capacity = (uint)particleData.GetSettingValue("capacity");
                    uint aabbCount = (capacity + decimationFactor - 1) / decimationFactor;
                    yield return "VFX_AABB_COUNT " + aabbCount;
                }

                if (raytracedScaleMode == RayTracedScaleMode.Custom)
                    yield return "VFX_USE_RT_CUSTOM_SCALE";
                else if (raytracedScaleMode == RayTracedScaleMode.Default)
                {
                    yield return "VFX_RT_DEFAULT_SCALE " + Mathf.Sqrt(Mathf.Min(decimationFactor, capacity));
                }
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

                if (hasMotionVector)
                {
                    yield return "VFX_FEATURE_MOTION_VECTORS";
                    if (isBlendModeOpaque)
                        yield return "USE_MOTION_VECTORS_PASS";
                    else
                        yield return "VFX_FEATURE_MOTION_VECTORS_FORWARD";
                    if (SupportsMotionVectorPerVertex(out uint vertsCount))
                        yield return "VFX_FEATURE_MOTION_VECTORS_VERTS " + vertsCount;
                }

                if (hasShadowCasting)
                    yield return "USE_CAST_SHADOWS_PASS";

                if (HasIndirectDraw())
                    yield return "VFX_HAS_INDIRECT_DRAW";

                if (supportsUV && uvMode != UVMode.Default)
                {
                    switch (uvMode)
                    {
                        case UVMode.Flipbook:
                            yield return "USE_FLIPBOOK";
                            if (flipbookHasInterpolation)
                                yield return "USE_FLIPBOOK_INTERPOLATION";
                            if (flipbookHasMotionVectors)
                                yield return "USE_FLIPBOOK_MOTIONVECTORS";
                            if (flipbookLayout == FlipbookLayout.Texture2DArray)
                                yield return "USE_FLIPBOOK_ARRAY_LAYOUT";
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
                else if (HasStrips(true)) // Output is not strip type, but data is
                    yield return "HAS_STRIPS_DATA";

                if (isRayTraced)
                {
                    foreach (var define in rayTracingDefines)
                        yield return define;
                }
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

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

                // indirect draw is implicit or forbidden
                if (!HasUpdateInputContext() || HasSorting() || VFXOutputUpdate.HasFeature(outputUpdateFeatures, VFXOutputUpdate.Features.IndirectDraw))
                    yield return "indirectDraw";

                // compute culling is implicit or forbidden
                if (VFXOutputUpdate.HasFeature(outputUpdateFeatures, VFXOutputUpdate.Features.MultiMesh)
                    || VFXOutputUpdate.HasFeature(outputUpdateFeatures, VFXOutputUpdate.Features.LOD)
                    || VFXOutputUpdate.HasFeature(outputUpdateFeatures, VFXOutputUpdate.Features.FrustumCulling))
                    yield return "computeCulling";

                if (!usesFlipbook)
                {
                    yield return "flipbookLayout";
                    yield return "flipbookBlendFrames";
                }
                if (!flipbookHasInterpolation)
                {
                    yield return "flipbookMotionVectors";
                }
                if (!subOutput.supportsExcludeFromTUAndAA)
                    yield return "excludeFromTUAndAA";
                if (!HasSorting())
                {
                    yield return "sortMode";
                    yield return "revertSorting";
                }
                if (!VFXViewPreference.displayExperimentalOperator || VFXLibrary.currentSRPBinder == null || !VFXLibrary.currentSRPBinder.GetSupportsRayTracing() || HasStrips(false))
                    yield return "enableRayTracing";
                if (!isRayTraced)
                {
                    yield return "decimationFactor";
                    yield return "raytracedScaleMode";
                }
            }
        }

        public override void OnSettingModified(VFXSetting setting)
        {
            base.OnSettingModified(setting);
            if (setting.name == nameof(decimationFactor))
            {
                decimationFactor = (uint)Mathf.ClosestPowerOfTwo((int)decimationFactor);
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
                yield return new VFXMapping("sortPriority", vfxSystemSortPriority);
                if (HasIndirectDraw())
                {
                    yield return new VFXMapping("indirectDraw", 1);
                    if (VFXOutputUpdate.HasFeature(outputUpdateFeatures, VFXOutputUpdate.Features.IndirectDraw) && VFXOutputUpdate.IsPerCamera(outputUpdateFeatures))
                        yield return new VFXMapping("indirectPerCamera", 1);
                }
                if (HasStrips(false))
                    yield return new VFXMapping("strips", 1);
            }
        }

        public bool IsInstancingFixedSize(out uint fixedSize)
        {
            fixedSize = 0;

            VFXDataParticle data = (VFXDataParticle)GetData();
            if (HasStrips())
            {
                UInt32 stripCapacity = (uint)data.GetSetting("stripCapacity").value;
                UInt32 particlePerStripCount = (uint)data.GetSetting("particlePerStripCount").value;
                if (HasIndirectDraw())
                {
                    // With indirect, we use all particle indices
                    fixedSize = stripCapacity * particlePerStripCount;
                }
                else
                {
                    // Without indirect, we are not counting the last particle of each strip
                    fixedSize = stripCapacity * (particlePerStripCount - 1);
                }
            }
            else
            {
                bool hasKill = data.IsAttributeStored(VFXAttribute.Alive);
                if (hasKill)
                {
                    fixedSize = (uint)data.GetSetting("capacity").value;
                }
            }

            return fixedSize != 0;
        }

        public static bool SupportsMotionVectorPerVertex(VFXTaskType taskType, bool hasStrip, bool isRayTraced, out uint vertsCount)
        {
            switch (taskType)
            {
                case VFXTaskType.ParticleQuadOutput:
                    vertsCount = 4;
                    break;
                case VFXTaskType.ParticleTriangleOutput:
                    vertsCount = 3;
                    break;
                case VFXTaskType.ParticleLineOutput:
                    vertsCount = 2;
                    break;
                case VFXTaskType.ParticlePointOutput:
                    vertsCount = 1;
                    break;
                default:
                    vertsCount = 0;
                    break;
            }
            if (hasStrip)
            {
                vertsCount /= 2;
            }
            return vertsCount != 0 && !isRayTraced;
        }

        public virtual bool SupportsMotionVectorPerVertex(out uint vertsCount)
        {
            return SupportsMotionVectorPerVertex(taskType, HasStrips(false), isRayTraced, out vertsCount);
        }

        internal override void GenerateErrors(VFXErrorReporter report)
        {
            base.GenerateErrors(report);
            var dataParticle = GetData() as VFXDataParticle;

            if (dataParticle != null && dataParticle.boundsMode != BoundsSettingMode.Manual)
            {
                var modifiedBounds = children
                    .SelectMany(b =>
                    b.attributes)
                    .Any(attr => attr.mode.HasFlag(VFXAttributeMode.Write) &&
                        (attr.attrib.name.Contains("size")
                            || attr.attrib.name.Contains("position")
                            || attr.attrib.name.Contains("scale")
                            || attr.attrib.name.Contains("pivot")));
                if (modifiedBounds && CanBeCompiled())
                    report.RegisterError("WarningBoundsComputation", VFXErrorType.Warning,
                        $"Bounds computation during recording is based on Position and Size in the Update Context." +
                        $" Changing these properties now could lead to incorrect bounds." +
                        $" Use padding to mitigate this discrepancy.", this);
            }

            if (HasSorting())
            {
                if (!needsOwnSort)
                {
                    var modifiedAttributes = children
                        .Where(c => c.enabled)
                        .SelectMany(b => b.attributes)
                        .Where(a => a.mode.HasFlag(VFXAttributeMode.Write))
                        .Select(a => a.attrib);
                    bool isCriterionModified = false;

                    if (HasCustomSortingCriterion())
                    {
                        HashSet<VFXExpression> sortKeyExpressions = new HashSet<VFXExpression>();
                        var sortKeyExp = inputSlots.First(s => s.name == "sortKey").GetExpression();
                        VFXExpression.CollectParentExpressionRecursively(sortKeyExp, sortKeyExpressions);

                        foreach (var modifiedAttribute in modifiedAttributes)
                            isCriterionModified |=
                                sortKeyExpressions.Contains(new VFXAttributeExpression(modifiedAttribute));
                    }
                    else
                    {
                        var usedAttributesInSorting = VFXSortingUtility.GetSortingDependantAttributes(sortMode);
                        isCriterionModified = usedAttributesInSorting.Intersect(modifiedAttributes).Any();
                    }

                    if (isCriterionModified)
                    {
                        report.RegisterError("SortingKeyOverriden", VFXErrorType.Warning,
                            $"Sorting happens in Update, before the attributes were modified in the Output context." +
                            $" All the modifications made here will not be taken into account during sorting.", this);
                    }
                }

                if (sortMode == VFXSortingUtility.SortCriteria.YoungestInFront)
                {
                    if (!GetData().IsAttributeUsed(VFXAttribute.Age))
                        report.RegisterError("NoAgeToSort", VFXErrorType.Warning,
                            $"The sorting mode depends on the Age attribute, which is neither set nor updated in this system.", this);
                }
            }

            if (isRayTraced)
            {
                if (!SystemInfo.supportsRayTracing)
                {
                    report.RegisterError("RaytracingNotSupported", VFXErrorType.Warning,
                        $"Ray tracing is not supported on this machine. You can still enable it in the graph for a use on another device.", this);
                }

            }

        }

        public override VFXContextCompiledData PrepareCompiledData()
        {
            var compiledData = base.PrepareCompiledData();

            int index = compiledData.tasks.Count - 1;
            var task = compiledData.tasks[index];
            if (HasIndirectDraw())
                task.bufferMappings.Add(VFXDataParticle.k_IndirectBufferName);

            return compiledData;
        }
    }
}
