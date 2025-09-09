using System;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.PathTracing.Integration;
using UnityEngine.PathTracing.Core;
using UnityEngine.Rendering;
using UnityEngine.Rendering.UnifiedRayTracing;
using UnityEngine.Rendering.Sampling;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.PathTracing.Lightmapping
{
    using InstanceHandle = Handle<World.InstanceKey>;
    using MaterialHandle = Handle<World.MaterialDescriptor>;

    internal struct LodInstanceBuildData
    {
        public int LodMask;
        public Mesh Mesh;
        public MaterialHandle[] Materials;
        public uint[] Masks;
        public Matrix4x4 LocalToWorldMatrix;
        public Bounds Bounds;
        public bool IsStatic;
        public RenderedGameObjectsFilter Filter;
    }

    internal struct LodIdentifier
    {
        public LodIdentifier(Int32 lodGroup, byte lodMask, Int32 lodContributorLevel)
        {
            this.LodGroup = lodGroup;
            this.LodMask = lodMask;
            this.LodContributorLevel = lodContributorLevel;
        }

        public Int32 LodGroup;
        public byte LodMask;
        Int32 LodContributorLevel;

        public override int GetHashCode() => HashCode.Combine(LodGroup, LodMask);
        public override bool Equals(object obj) => obj is LodIdentifier other && other.LodGroup == LodGroup && other.LodMask == LodMask;
        public static readonly LodIdentifier Invalid = new LodIdentifier(-1, 0, -1);
        public bool IsValid() => LodGroup != -1;
        public bool IsContributor()
        {
            if (LodContributorLevel == -1) return false;
            return (LodMask & (1 << LodContributorLevel)) != 0;
        }
        public byte MinLodLevelMask() { return (byte)(LodMask & -LodMask); }
    }

    internal struct ContributorLodInfo
    {
        public InstanceHandle InstanceHandle;
        public uint[] Masks;
        public int LodMask;
    }

    internal struct FatInstance
    {
        public BoundingSphere BoundingSphere;
        public Mesh Mesh;
        public Vector2 UVBoundsSize;
        public Vector2 UVBoundsOffset;
        public MaterialHandle[] Materials;
        public uint[] SubMeshMasks;
        public Matrix4x4 LocalToWorldMatrix;
        public Bounds Bounds;
        public bool IsStatic;
        public LodIdentifier LodIdentifier;
        public bool ReceiveShadows;
        public RenderedGameObjectsFilter Filter;
        public uint RenderingObjectLayer;
        public bool EnableEmissiveSampling;
    }

    internal struct BakeInstance
    {
        public Mesh Mesh;
        public Vector4 NormalizedOccupiedST; // Transforms coordinates in [0; 1] range into the occupied rectangle in the lightmap atlas, also in [0; 1] range.
        public Vector4 SourceLightmapST;
        public Vector2Int TexelSize; // Instance size in the lightmap atlas, in pixels.
        public Vector2Int TexelOffset; // Instance offset in the lightmap atlas, in pixels.
        public Matrix4x4 LocalToWorldMatrix;
        public Matrix4x4 LocalToWorldMatrixNormals;
        public bool ReceiveShadows;
        public LodIdentifier LodIdentifier;
        public uint InstanceIndex; // Index of the instance in BakeInput

        private static float4x4 NormalMatrix(float4x4 m)
        {
            float3x3 t = new float3x3(m);
            return new float4x4(math.inverse(math.transpose(t)), new float3(0.0));
        }

        public BoundingSphere GetBoundingSphere()
        {
            var boundingSphere = new BoundingSphere();
            boundingSphere.position = LocalToWorldMatrix.MultiplyPoint(Mesh.bounds.center);
            boundingSphere.radius = (LocalToWorldMatrix.MultiplyPoint(Mesh.bounds.extents) - boundingSphere.position).magnitude;
            return boundingSphere;
        }

        public void Build(Mesh mesh, Vector4 normalizedOccupiedST, Vector4 sourceLightmapST, Vector2Int texelSize, Vector2Int texelOffset, Matrix4x4 localToWorldMatrix, bool receiveShadows, LodIdentifier lodIdentifier, uint instanceIndex)
        {
            Mesh = mesh;
            NormalizedOccupiedST = normalizedOccupiedST;
            SourceLightmapST = sourceLightmapST;
            TexelSize = texelSize;
            TexelOffset = texelOffset;
            LocalToWorldMatrix = localToWorldMatrix;
            ReceiveShadows = receiveShadows;
            LocalToWorldMatrixNormals = NormalMatrix(this.LocalToWorldMatrix);
            LodIdentifier = lodIdentifier;
            InstanceIndex = instanceIndex;
        }
    }

    internal class LightmapDesc
    {
        public uint Resolution;
        public float PushOff;
        public BakeInstance[] BakeInstances;
    }

    internal enum IntegratedOutputType
    {
        Direct,
        Indirect,
        AO,
        Validity,
        DirectionalityDirect,
        DirectionalityIndirect,
        ShadowMask
    }

    internal class LightmapIntegratorContext : IDisposable
    {
        internal UVFallbackBufferBuilder UVFallbackBufferBuilder;
        internal LightmapDirectIntegrator LightmapDirectIntegrator;
        internal LightmapIndirectIntegrator LightmapIndirectIntegrator;
        internal LightmapAOIntegrator LightmapAOIntegrator;
        internal LightmapValidityIntegrator LightmapValidityIntegrator;
        internal LightmapOccupancyIntegrator LightmapOccupancyIntegrator;
        internal LightmapShadowMaskIntegrator LightmapShadowMaskIntegrator;
        internal GBufferDebug GBufferDebugShader;
        internal IRayTracingShader GBufferShader;
        internal ComputeShader ExpansionShaders;
        internal SamplingResources SamplingResources;
        private RTHandle _emptyExposureTexture;
        internal GraphicsBuffer ClearDispatchBuffer;
        internal GraphicsBuffer CopyDispatchBuffer;
        internal GraphicsBuffer ReduceDispatchBuffer;
        internal GraphicsBuffer CompactedGBufferLength;
        internal int CompactGBufferKernel;
        internal int PopulateAccumulationDispatchKernel;
        internal int PopulateClearDispatchKernel;
        internal int PopulateCopyDispatchKernel;
        internal int PopulateReduceDispatchKernel;
        internal int ClearBufferKernel;
        internal int ReductionKernel;
        internal int CopyToLightmapKernel;

        public void Dispose()
        {
            UVFallbackBufferBuilder?.Dispose();
            UVFallbackBufferBuilder = null;
            LightmapDirectIntegrator?.Dispose();
            LightmapDirectIntegrator = null;
            LightmapIndirectIntegrator?.Dispose();
            LightmapIndirectIntegrator = null;
            LightmapAOIntegrator?.Dispose();
            LightmapAOIntegrator = null;
            LightmapValidityIntegrator?.Dispose();
            LightmapValidityIntegrator = null;
            LightmapOccupancyIntegrator = null;
            LightmapShadowMaskIntegrator?.Dispose();
            LightmapShadowMaskIntegrator = null;
            GBufferDebugShader?.Dispose();
            GBufferDebugShader = null;
            _emptyExposureTexture?.Release();
            _emptyExposureTexture = null;

            ClearDispatchBuffer?.Dispose();
            CopyDispatchBuffer?.Dispose();
            ReduceDispatchBuffer?.Dispose();
            CompactedGBufferLength?.Dispose();
        }

        internal void Initialize(SamplingResources samplingResources, LightmapResourceLibrary lightmapResourceLib, bool countNEERayAsPathSegment)
        {
            SamplingResources = samplingResources;
            _emptyExposureTexture = RTHandles.Alloc(1, 1, enableRandomWrite: true, name: "Empty EV100 Exposure", colorFormat: GraphicsFormat.R8G8B8A8_UNorm);

            UVFallbackBufferBuilder = new UVFallbackBufferBuilder();
            UVFallbackBufferBuilder.Prepare(lightmapResourceLib.UVFallbackBufferGenerationMaterial);
            LightmapDirectIntegrator = new LightmapDirectIntegrator();
            LightmapDirectIntegrator.Prepare(lightmapResourceLib.DirectAccumulationShader, lightmapResourceLib.NormalizationShader, lightmapResourceLib.ExpansionHelpers, SamplingResources, _emptyExposureTexture);
            LightmapIndirectIntegrator = new LightmapIndirectIntegrator(countNEERayAsPathSegment);
            LightmapIndirectIntegrator.Prepare(lightmapResourceLib.IndirectAccumulationShader, lightmapResourceLib.NormalizationShader, lightmapResourceLib.ExpansionHelpers, SamplingResources, _emptyExposureTexture);
            LightmapAOIntegrator = new LightmapAOIntegrator();
            LightmapAOIntegrator.Prepare(lightmapResourceLib.AOAccumulationShader, lightmapResourceLib.NormalizationShader, lightmapResourceLib.ExpansionHelpers, SamplingResources, _emptyExposureTexture);
            LightmapValidityIntegrator = new LightmapValidityIntegrator();
            LightmapValidityIntegrator.Prepare(lightmapResourceLib.ValidityAccumulationShader, lightmapResourceLib.NormalizationShader, lightmapResourceLib.ExpansionHelpers, SamplingResources, _emptyExposureTexture);
            LightmapOccupancyIntegrator = new LightmapOccupancyIntegrator();
            LightmapOccupancyIntegrator.Prepare(lightmapResourceLib.OccupancyShader);
			LightmapShadowMaskIntegrator = new LightmapShadowMaskIntegrator();
            LightmapShadowMaskIntegrator.Prepare(lightmapResourceLib.ShadowMaskAccumulationShader, lightmapResourceLib.NormalizationShader, lightmapResourceLib.ExpansionHelpers, SamplingResources, _emptyExposureTexture);
            GBufferDebugShader = new GBufferDebug();
            GBufferDebugShader.Prepare(lightmapResourceLib.GBufferDebugShader, lightmapResourceLib.ExpansionHelpers);
            GBufferShader = lightmapResourceLib.GBufferShader;
            ExpansionShaders = lightmapResourceLib.ExpansionHelpers;

            CompactGBufferKernel = ExpansionShaders.FindKernel("CompactGBuffer");
            PopulateAccumulationDispatchKernel = ExpansionShaders.FindKernel("PopulateAccumulationDispatch");
            PopulateClearDispatchKernel = ExpansionShaders.FindKernel("PopulateClearDispatch");
            PopulateCopyDispatchKernel = ExpansionShaders.FindKernel("PopulateCopyDispatch");
            PopulateReduceDispatchKernel = ExpansionShaders.FindKernel("PopulateReduceDispatch");
            ClearBufferKernel = ExpansionShaders.FindKernel("ClearFloat4Buffer");
            ReductionKernel = ExpansionShaders.FindKernel("BinaryGroupSumLeft");
            CopyToLightmapKernel = ExpansionShaders.FindKernel("AdditivelyCopyCompactedTo2D");

            ClearDispatchBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments | GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.CopySource | GraphicsBuffer.Target.CopyDestination, 3, sizeof(uint));
            CopyDispatchBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments | GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.CopySource | GraphicsBuffer.Target.CopyDestination, 3, sizeof(uint));
            ReduceDispatchBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments | GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.CopySource | GraphicsBuffer.Target.CopyDestination, 3, sizeof(uint));
            CompactedGBufferLength = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.CopySource, 1, sizeof(uint));
        }
    }

    internal class LightmapResourceLibrary
    {
        internal IRayTracingShader GBufferShader;
        internal ComputeShader NormalizationShader;
        internal IRayTracingShader DirectAccumulationShader;
        internal IRayTracingShader AOAccumulationShader;
        internal IRayTracingShader ValidityAccumulationShader;
        internal IRayTracingShader IndirectAccumulationShader;
        internal IRayTracingShader ShadowMaskAccumulationShader;
        internal IRayTracingShader NormalAccumulationShader;
        internal IRayTracingShader GBufferDebugShader;
        internal Material UVFallbackBufferGenerationMaterial;
        internal ComputeShader OccupancyShader;
        internal LightmapIntegrationHelpers.ComputeHelpers ComputeHelpers;
        internal ComputeShader BoxFilterShader;
        internal ComputeShader SelectGraphicsBufferShader;
        internal ComputeShader CopyTextureAdditiveShader;
        internal ComputeShader ExpansionHelpers;
        internal Shader SoftwareChartRasterizationShader;
        internal Shader HardwareChartRasterizationShader;

#if UNITY_EDITOR
        public void Load(RayTracingContext context)
        {
            const string packageFolder = "Packages/com.unity.render-pipelines.core/Runtime/PathTracing/";

            GBufferShader = context.LoadRayTracingShader(packageFolder + "Shaders/LightmapGBufferIntegration.urtshader");

            NormalizationShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(packageFolder + "Shaders/ResolveAccumulation.compute");
            DirectAccumulationShader = context.LoadRayTracingShader(packageFolder + "Shaders/LightmapDirectIntegration.urtshader");
            AOAccumulationShader = context.LoadRayTracingShader(packageFolder + "Shaders/LightmapAOIntegration.urtshader");
            ValidityAccumulationShader = context.LoadRayTracingShader(packageFolder + "Shaders/LightmapValidityIntegration.urtshader");
            IndirectAccumulationShader = context.LoadRayTracingShader(packageFolder + "Shaders/LightmapIndirectIntegration.urtshader");
            ShadowMaskAccumulationShader = context.LoadRayTracingShader(packageFolder + "Shaders/LightmapShadowMaskIntegration.urtshader");
            GBufferDebugShader = context.LoadRayTracingShader(packageFolder + "Shaders/LightmapGBufferDebug.urtshader");

            NormalAccumulationShader = context.LoadRayTracingShader(packageFolder + "Shaders/LightmapNormalIntegration.urtshader");

            UVFallbackBufferGenerationMaterial = new Material(AssetDatabase.LoadAssetAtPath<Shader>(packageFolder + "Shaders/Lightmapping/UVFallbackBufferGeneration.shader"));

            OccupancyShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(packageFolder + "Shaders/LightmapOccupancy.compute");

            ExpansionHelpers = AssetDatabase.LoadAssetAtPath<ComputeShader>(packageFolder + "Shaders/ExpansionHelpers.compute");

            ComputeHelpers = new LightmapIntegrationHelpers.ComputeHelpers();
            ComputeHelpers.Load();

            ChartRasterizer.LoadShaders(out SoftwareChartRasterizationShader, out HardwareChartRasterizationShader);

            BoxFilterShader = ComputeHelpers.ComputeHelperShader;
            SelectGraphicsBufferShader = ComputeHelpers.ComputeHelperShader;
            CopyTextureAdditiveShader = ComputeHelpers.ComputeHelperShader;
        }
#endif
    }
}
