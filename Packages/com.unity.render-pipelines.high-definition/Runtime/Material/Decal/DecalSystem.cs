using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Jobs;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>Decal Layers.</summary>
    [Flags]
    public enum DecalLayerEnum
    {
        /// <summary>The light will no affect any object.</summary>
        Nothing = 0,   // Custom name for "Nothing" option
        /// <summary>Decal Layer 0.</summary>
        DecalLayerDefault = 1 << 0,
        /// <summary>Decal Layer 1.</summary>
        DecalLayer1 = 1 << 1,
        /// <summary>Decal Layer 2.</summary>
        DecalLayer2 = 1 << 2,
        /// <summary>Decal Layer 3.</summary>
        DecalLayer3 = 1 << 3,
        /// <summary>Decal Layer 4.</summary>
        DecalLayer4 = 1 << 4,
        /// <summary>Decal Layer 5.</summary>
        DecalLayer5 = 1 << 5,
        /// <summary>Decal Layer 6.</summary>
        DecalLayer6 = 1 << 6,
        /// <summary>Decal Layer 7.</summary>
        DecalLayer7 = 1 << 7,
        /// <summary>Everything.</summary>
        Everything = 0xFF, // Custom name for "Everything" option
    }

    partial class DecalSystem
    {
        // Relies on the order shader passes are declared in Decal.shader and DecalSubTarget.cs
        // Caution: Enum num must match pass name for s_MaterialDecalPassNames array
        public enum MaterialDecalPass
        {
            DBufferProjector = 0,
            DecalProjectorForwardEmissive = 1,
            DBufferMesh = 2,
            DecalMeshForwardEmissive = 3,
        };

        public static readonly string[] s_MaterialDecalPassNames = Enum.GetNames(typeof(MaterialDecalPass));
        public static readonly string s_AtlasSizeWarningMessage = "Decal texture atlas out of space, decals on transparent geometry might not render correctly, atlas size can be changed in HDRenderPipelineAsset";

        public class CullResult : IDisposable
        {
            public class Set : IDisposable
            {
                int m_NumResults;
                int[] m_ResultIndices;

                public int numResults => m_NumResults;
                public int[] resultIndices => m_ResultIndices;

                public void Dispose() => Dispose(true);

                void Dispose(bool disposing)
                {
                    if (disposing)
                    {
                        Clear();
                        m_ResultIndices = null;
                    }
                }

                public void Clear() => m_NumResults = 0;

                public int QueryIndices(int maxLength, CullingGroup cullingGroup)
                {
                    if (m_ResultIndices == null || m_ResultIndices.Length < maxLength)
                        Array.Resize(ref m_ResultIndices, maxLength);
                    m_NumResults = cullingGroup.QueryIndices(true, m_ResultIndices, 0);
                    return m_NumResults;
                }
            }

            Dictionary<int, Set> m_Requests = new Dictionary<int, Set>();
            public Dictionary<int, Set> requests => m_Requests;

            public Set this[int index]
            {
                get
                {
                    if (!m_Requests.TryGetValue(index, out var v))
                    {
                        v = GenericPool<Set>.Get();
                        m_Requests.Add(index, v);
                    }
                    return v;
                }
            }

            public void Clear()
            {
                Assert.IsNotNull(m_Requests);

                foreach (var pair in m_Requests)
                {
                    pair.Value.Clear();
                    GenericPool<Set>.Release(pair.Value);
                }
                m_Requests.Clear();
            }

            public void Dispose() => Dispose(true);

            void Dispose(bool disposing)
            {
                if (disposing)
                {
                    m_Requests.Clear();
                    m_Requests = null;
                }
            }
        }

        public class CullRequest : IDisposable
        {
            public class Set : IDisposable
            {
                CullingGroup m_CullingGroup;

                public CullingGroup cullingGroup => m_CullingGroup;

                public void Dispose() => Dispose(true);

                void Dispose(bool disposing)
                {
                    if (disposing)
                        Clear();
                }

                public void Clear()
                {
                    if (m_CullingGroup != null)
                        CullingGroupManager.instance.Free(m_CullingGroup);
                    m_CullingGroup = null;
                }

                public void Initialize(CullingGroup cullingGroup)
                {
                    Assert.IsNull(m_CullingGroup);
                    m_CullingGroup = cullingGroup;
                }
            }

            Dictionary<int, Set> m_Requests = new Dictionary<int, Set>();

            public Set this[int index]
            {
                get
                {
                    if (!m_Requests.TryGetValue(index, out var v))
                    {
                        v = GenericPool<Set>.Get();
                        m_Requests.Add(index, v);
                    }
                    return v;
                }
            }

            public void Clear()
            {
                Assert.IsNotNull(m_Requests);

                foreach (var pair in m_Requests)
                {
                    pair.Value.Clear();
                    GenericPool<Set>.Release(pair.Value);
                }
                m_Requests.Clear();
            }

            public void Dispose() => Dispose(true);

            void Dispose(bool disposing)
            {
                if (disposing)
                {
                    m_Requests.Clear();
                    m_Requests = null;
                }
            }
        }

        public const int kInvalidIndex = -1;
        public const int kNullMaterialIndex = int.MaxValue;
        public class DecalHandle
        {
            public DecalHandle(int index, int materialID)
            {
                m_MaterialID = materialID;
                m_Index = index;
            }

            public static bool IsValid(DecalHandle handle)
            {
                if (handle == null)
                    return false;
                if (handle.m_Index == kInvalidIndex)
                    return false;
                return true;
            }

            public int m_MaterialID;    // identifies decal set
            public int m_Index;         // identifies decal within the set
        }

        static DecalSystem m_Instance;
        static public DecalSystem instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new DecalSystem();
                return m_Instance;
            }
        }

        private const int kDefaultDrawDistance = 1000;
        public int DrawDistance
        {
            get
            {
                HDRenderPipelineAsset hdrp = HDRenderPipeline.currentAsset;
                if (hdrp != null)
                {
                    return hdrp.currentPlatformRenderPipelineSettings.decalSettings.drawDistance;
                }
                return kDefaultDrawDistance;
            }
        }

        public bool perChannelMask
        {
            get
            {
                HDRenderPipelineAsset hdrp = HDRenderPipeline.currentAsset;
                if (hdrp != null)
                {
                    return hdrp.currentPlatformRenderPipelineSettings.decalSettings.perChannelMask;
                }
                return false;
            }
        }

        public Camera CurrentCamera
        {
            get
            {
                return m_Camera;
            }
            set
            {
                m_Camera = value;
            }
        }

        private const int kDecalBlockSize = 128;

        // to work on Vulkan Mobile?
        // Core\CoreRP\ShaderLibrary\UnityInstancing.hlsl
        // #if (defined(SHADER_API_VULKAN) && defined(SHADER_API_MOBILE)) || defined(SHADER_API_SWITCH)
        //      #define UNITY_INSTANCED_ARRAY_SIZE  250
        private const int kDrawIndexedBatchSize = 250;

        // cube mesh bounds for decal
        static Vector4 kMin = new Vector4(-0.5f, -0.5f, -0.5f, 1.0f);
        static Vector4 kMax = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);

        static public Mesh m_DecalMesh = null;

        // clustered draw data
        static public DecalData[] m_DecalDatas = new DecalData[kDecalBlockSize];
        static public SFiniteLightBound[] m_Bounds = new SFiniteLightBound[kDecalBlockSize];
        static public LightVolumeData[] m_LightVolumes = new LightVolumeData[kDecalBlockSize];
        static public TextureScaleBias[] m_DiffuseTextureScaleBias = new TextureScaleBias[kDecalBlockSize];
        static public TextureScaleBias[] m_NormalTextureScaleBias = new TextureScaleBias[kDecalBlockSize];
        static public TextureScaleBias[] m_MaskTextureScaleBias = new TextureScaleBias[kDecalBlockSize];
        static public Vector4[] m_BaseColor = new Vector4[kDecalBlockSize];

        static public int m_DecalDatasCount = 0;

        static public float[] m_BoundingDistances = new float[1];

        private Dictionary<int, DecalSet> m_DecalSets = new Dictionary<int, DecalSet>();
        private List<DecalSet> m_DecalSetsRenderList = new List<DecalSet>(); // list of visible decalsets sorted by material draw order

        // current camera
        private Camera m_Camera;

        static public int m_DecalsVisibleThisFrame = 0;

        private Texture2DAtlas m_Atlas = null;
        public bool m_AllocationSuccess = true;
        public bool m_PrevAllocationSuccess = true;

        public Texture2DAtlas Atlas
        {
            get
            {
                if (m_Atlas == null)
                {
                    m_Atlas = new Texture2DAtlas(HDUtils.hdrpSettings.decalSettings.atlasWidth, HDUtils.hdrpSettings.decalSettings.atlasHeight, GraphicsFormat.R8G8B8A8_UNorm);
                }
                return m_Atlas;
            }
        }

        public class TextureScaleBias : IComparable
        {
            public Texture m_Texture = null;
            public Vector4 m_ScaleBias = Vector4.zero;
            public int CompareTo(object obj)
            {
                TextureScaleBias other = obj as TextureScaleBias;
                int size = m_Texture.width * m_Texture.height;
                int otherSize = other.m_Texture.width * other.m_Texture.height;
                if (size > otherSize)
                {
                    return -1;
                }
                else if (size < otherSize)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }

            public void Initialize(Texture texture, Vector4 scaleBias)
            {
                m_Texture = texture;
                m_ScaleBias = scaleBias;
            }
        }

        private List<TextureScaleBias> m_TextureList = new List<TextureScaleBias>();

        static public bool IsHDRenderPipelineDecal(Shader shader)
        {
            // Warning: accessing Shader.name generate 48B of garbage at each frame, we want to avoid that in the future
            return shader.name == "HDRP/Decal";
        }

        const string kIdentifyHDRPDecal = "_Unity_Identify_HDRP_Decal";

        // Non alloc version of IsHDRenderPipelineDecal (Slower but does not generate garbage)
        static public bool IsHDRenderPipelineDecal(Material material)
        {
            // Check if the material has a marker _Unity_Identify_HDRP_Decal
            return material.HasProperty(kIdentifyHDRPDecal);
        }

        static public bool IsDecalMaterial(Material material)
        {
            // Check if the material has at least one pass from the decal.shader / Shader Graph (shader stripping can remove one or more passes)
            foreach (var passName in s_MaterialDecalPassNames)
            {
                if (material.FindPass(passName) != -1)
                    return true;
            }

            return false;
        }

        private partial class DecalSet : IDisposable
        {
            public void InitializeMaterialValues()
            {
                if (m_Material == null)
                    return;

                // TODO: this test is ambiguous, it should say, I am decal or not.
                // We should have 2 function: I am decal or not and I am a SG or not...
                m_IsHDRenderPipelineDecal = IsHDRenderPipelineDecal(m_Material);

                if (m_IsHDRenderPipelineDecal)
                {
                    m_Diffuse.Initialize(m_Material.GetTexture("_BaseColorMap"), Vector4.zero);
                    m_Normal.Initialize(m_Material.GetTexture("_NormalMap"), Vector4.zero);
                    m_Mask.Initialize(m_Material.GetTexture("_MaskMap"), Vector4.zero);
                    m_Blend = m_Material.GetFloat("_DecalBlend");
                    m_BaseColor = m_Material.GetVector("_BaseColor");
                    m_BlendParams = new Vector3(m_Material.GetFloat("_NormalBlendSrc"), m_Material.GetFloat("_MaskBlendSrc"), 0.0f);
                    int affectFlags =
                        (m_Material.GetFloat("_AffectAlbedo") != 0.0f ? (1 << 0) : 0) |
                        (m_Material.GetFloat("_AffectNormal") != 0.0f ? (1 << 1) : 0) |
                        (m_Material.GetFloat("_AffectMetal") != 0.0f ? (1 << 2) : 0) |
                        (m_Material.GetFloat("_AffectAO") != 0.0f ? (1 << 3) : 0) |
                        (m_Material.GetFloat("_AffectSmoothness") != 0.0f ? (1 << 4) : 0);

                    // convert to float
                    m_BlendParams.z = (float)affectFlags;

                    m_ScalingBAndRemappingM = new Vector4(0.0f, m_Material.GetFloat("_DecalMaskMapBlueScale"), 0.0f, 0.0f);
                    // If we have a texture, we use the remapping parameter, otherwise we use the regular one and the default texture is white
                    if (m_Material.GetTexture("_MaskMap"))
                    {
                        m_RemappingAOS = new Vector4(m_Material.GetFloat("_AORemapMin"), m_Material.GetFloat("_AORemapMax"), m_Material.GetFloat("_SmoothnessRemapMin"), m_Material.GetFloat("_SmoothnessRemapMax"));
                        m_ScalingBAndRemappingM.z = m_Material.GetFloat("_MetallicRemapMin");
                        m_ScalingBAndRemappingM.w = m_Material.GetFloat("_MetallicRemapMax");
                    }
                    else
                    {
                        m_RemappingAOS = new Vector4(m_Material.GetFloat("_AO"), m_Material.GetFloat("_AO"), m_Material.GetFloat("_Smoothness"), m_Material.GetFloat("_Smoothness"));
                        m_ScalingBAndRemappingM.z = m_Material.GetFloat("_Metallic");
                    }

                    // For HDRP/Decal, pass are always present but can be enabled/disabled
                    m_cachedProjectorPassValue = -1;
                    if (m_Material.GetShaderPassEnabled(s_MaterialDecalPassNames[(int)MaterialDecalPass.DBufferProjector]))
                        m_cachedProjectorPassValue = (int)MaterialDecalPass.DBufferProjector;

                    m_cachedProjectorEmissivePassValue = -1;
                    if (m_Material.GetShaderPassEnabled(s_MaterialDecalPassNames[(int)MaterialDecalPass.DecalProjectorForwardEmissive]))
                        m_cachedProjectorEmissivePassValue = (int)MaterialDecalPass.DecalProjectorForwardEmissive;
                }
                else
                {
                    m_Blend = 1.0f;
                    // With ShaderGraph it is possible that the pass isn't generated. But if it is, it can be disabled.
                    m_cachedProjectorPassValue = m_Material.FindPass(s_MaterialDecalPassNames[(int)MaterialDecalPass.DBufferProjector]);
                    if (m_cachedProjectorPassValue != -1 && m_Material.GetShaderPassEnabled(s_MaterialDecalPassNames[(int)MaterialDecalPass.DBufferProjector]) == false)
                        m_cachedProjectorPassValue = -1;
                    m_cachedProjectorEmissivePassValue = m_Material.FindPass(s_MaterialDecalPassNames[(int)MaterialDecalPass.DecalProjectorForwardEmissive]);
                    if (m_cachedProjectorEmissivePassValue != -1 && m_Material.GetShaderPassEnabled(s_MaterialDecalPassNames[(int)MaterialDecalPass.DecalProjectorForwardEmissive]) == false)
                        m_cachedProjectorEmissivePassValue = -1;
                }
            }

            public void Dispose() => Dispose(true);

            void Dispose(bool disposing)
            {
                if (!disposing)
                    return;

                DisposeJobArrays();
            }

            public DecalSet(Material material)
            {
                m_Material = material;
                InitializeMaterialValues();
            }

            public void UpdateCachedData(DecalHandle handle, DecalProjector decalProjector)
            {
                DecalProjector.CachedDecalData data = decalProjector.GetCachedDecalData();

                int index = handle.m_Index;

                // draw distance can't be more than global draw distance
                m_CachedDrawDistances[index].x = data.drawDistance < instance.DrawDistance
                    ? data.drawDistance
                    : instance.DrawDistance;
                m_CachedDrawDistances[index].y = data.fadeScale;
                // In the shader to remap from cosine -1 to 1 to new range 0..1  (with 0 - 0 degree and 1 - 180 degree)
                // we do 1.0 - (dot() * 0.5 + 0.5) => 0.5 * (1 - dot())
                // we actually square that to get smoother result => x = (0.5 - 0.5 * dot())^2
                // Do a remap in the shader. 1.0 - saturate((x - start) / (end - start))
                // After simplification => saturate(a + b * dot() * (dot() - 2.0))
                // a = 1.0 - (0.25 - start) / (end - start), y = - 0.25 / (end - start)
                if (data.startAngleFade == 180.0f) // angle fade is disabled
                {
                    m_CachedAngleFade[index].x = 0.0f;
                    m_CachedAngleFade[index].y = 0.0f;
                }
                else
                {
                    float angleStart = data.startAngleFade / 180.0f;
                    float angleEnd = data.endAngleFade / 180.0f;
                    var range = Mathf.Max(0.0001f, angleEnd - angleStart);
                    m_CachedAngleFade[index].x = 1.0f - (0.25f - angleStart) / range;
                    m_CachedAngleFade[index].y = -0.25f / range;
                }
                m_CachedUVScaleBias[index] = data.uvScaleBias;
                m_CachedAffectsTransparency[index] = data.affectsTransparency;
                m_CachedLayerMask[index] = data.layerMask;
                m_CachedSceneLayerMask[index] = data.sceneLayerMask;
                m_CachedFadeFactor[index] = data.fadeFactor;
                m_CachedDecalLayerMask[index] = data.decalLayerMask;

                UpdateCachedDrawOrder();

                UpdateJobArrays(index, decalProjector);
            }

            public void UpdateCachedDrawOrder()
            {
                if (this.m_Material.HasProperty(HDShaderIDs._DrawOrder))
                {
                    m_CachedDrawOrder = this.m_Material.GetInt(HDShaderIDs._DrawOrder);
                }
                else
                {
                    m_CachedDrawOrder = 0;
                }
            }

            // Update memory allocation and assign decal handle, then update cached data
            public DecalHandle AddDecal(int materialID, DecalProjector decalProjector)
            {
                // increase array size if no space left
                if (m_DecalsCount == m_Handles.Length)
                {
                    int newCapacity = m_DecalsCount + kDecalBlockSize;

                    m_ResultIndices = new int[newCapacity];

                    ResizeJobArrays(newCapacity);

                    ArrayExtensions.ResizeArray(ref m_Handles, newCapacity);
                    ArrayExtensions.ResizeArray(ref m_CachedDrawDistances, newCapacity);
                    ArrayExtensions.ResizeArray(ref m_CachedAngleFade, newCapacity);
                    ArrayExtensions.ResizeArray(ref m_CachedUVScaleBias, newCapacity);
                    ArrayExtensions.ResizeArray(ref m_CachedAffectsTransparency, newCapacity);
                    ArrayExtensions.ResizeArray(ref m_CachedLayerMask, newCapacity);
                    ArrayExtensions.ResizeArray(ref m_CachedSceneLayerMask, newCapacity);
                    ArrayExtensions.ResizeArray(ref m_CachedDecalLayerMask, newCapacity);
                    ArrayExtensions.ResizeArray(ref m_CachedFadeFactor, newCapacity);
                }

                DecalHandle decalHandle = new DecalHandle(m_DecalsCount, materialID);
                m_Handles[m_DecalsCount] = decalHandle;
                UpdateCachedData(decalHandle, decalProjector);
                m_DecalsCount++;
                return decalHandle;
            }

            public void RemoveDecal(DecalHandle handle)
            {
                int removeAtIndex = handle.m_Index;
                // replace with last decal in the list and update index
                m_Handles[removeAtIndex] = m_Handles[m_DecalsCount - 1]; // move the last decal in list
                m_Handles[removeAtIndex].m_Index = removeAtIndex;
                m_Handles[m_DecalsCount - 1] = null;

                // update cached data
                RemoveFromJobArrays(removeAtIndex);
                m_CachedDrawDistances[removeAtIndex] = m_CachedDrawDistances[m_DecalsCount - 1];
                m_CachedAngleFade[removeAtIndex] = m_CachedAngleFade[m_DecalsCount - 1];
                m_CachedUVScaleBias[removeAtIndex] = m_CachedUVScaleBias[m_DecalsCount - 1];
                m_CachedAffectsTransparency[removeAtIndex] = m_CachedAffectsTransparency[m_DecalsCount - 1];
                m_CachedLayerMask[removeAtIndex] = m_CachedLayerMask[m_DecalsCount - 1];
                m_CachedSceneLayerMask[removeAtIndex] = m_CachedSceneLayerMask[m_DecalsCount - 1];
                m_CachedFadeFactor[removeAtIndex] = m_CachedFadeFactor[m_DecalsCount - 1];
                m_DecalsCount--;
                handle.m_Index = kInvalidIndex;
            }

            public void BeginCull(CullRequest.Set cullRequest)
            {
                Assert.IsNotNull(cullRequest);

                cullRequest.Clear();

                if (m_Material == null)
                    return;
                if (cullRequest.cullingGroup != null)
                    Debug.LogError("Begin/EndCull() called out of sequence for decal projectors.");

                ResolveUpdateJob();

                // let the culling group code do some of the heavy lifting for global draw distance
                m_BoundingDistances[0] = DecalSystem.instance.DrawDistance;
                m_NumResults = 0;
                var cullingGroup = CullingGroupManager.instance.Alloc();
                cullingGroup.targetCamera = instance.CurrentCamera;
                cullingGroup.SetDistanceReferencePoint(cullingGroup.targetCamera.transform.position);
                cullingGroup.SetBoundingDistances(m_BoundingDistances);
                cullingGroup.SetBoundingSpheres(m_CachedBoundingSpheres);
                cullingGroup.SetBoundingSphereCount(m_DecalsCount);

                cullRequest.Initialize(cullingGroup);
            }

            public int QueryCullResults(CullRequest.Set cullRequest, CullResult.Set cullResult)
            {
                if (m_Material == null || cullRequest.cullingGroup == null)
                    return 0;
                return cullResult.QueryIndices(m_Handles.Length, cullRequest.cullingGroup);
            }

            private void GetDecalVolumeDataAndBound(Matrix4x4 decalToWorld, Matrix4x4 worldToView)
            {
                var influenceX = decalToWorld.GetColumn(0) * 0.5f;
                var influenceY = decalToWorld.GetColumn(1) * 0.5f;
                var influenceZ = decalToWorld.GetColumn(2) * 0.5f;
                var pos = decalToWorld.GetColumn(3);

                Vector3 influenceExtents = new Vector3();
                influenceExtents.x = influenceX.magnitude;
                influenceExtents.y = influenceY.magnitude;
                influenceExtents.z = influenceZ.magnitude;

                // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
                var influenceRightVS = worldToView.MultiplyVector(influenceX / influenceExtents.x);
                var influenceUpVS = worldToView.MultiplyVector(influenceY / influenceExtents.y);
                var influenceForwardVS = worldToView.MultiplyVector(influenceZ / influenceExtents.z);
                var influencePositionVS = worldToView.MultiplyPoint(pos); // place the mesh pivot in the center

                m_Bounds[m_DecalDatasCount].center = influencePositionVS;
                m_Bounds[m_DecalDatasCount].boxAxisX = influenceRightVS * influenceExtents.x;
                m_Bounds[m_DecalDatasCount].boxAxisY = influenceUpVS * influenceExtents.y;
                m_Bounds[m_DecalDatasCount].boxAxisZ = influenceForwardVS * influenceExtents.z;
                m_Bounds[m_DecalDatasCount].scaleXY = 1.0f;
                m_Bounds[m_DecalDatasCount].radius = influenceExtents.magnitude;

                // The culling system culls pixels that are further
                //   than a threshold to the box influence extents.
                // So we use an arbitrary threshold here (k_BoxCullingExtentOffset)
                m_LightVolumes[m_DecalDatasCount].lightCategory = (uint)LightCategory.Decal;
                m_LightVolumes[m_DecalDatasCount].lightVolume = (uint)LightVolumeType.Box;
                m_LightVolumes[m_DecalDatasCount].featureFlags = (uint)LightFeatureFlags.Env;
                m_LightVolumes[m_DecalDatasCount].lightPos = influencePositionVS;
                m_LightVolumes[m_DecalDatasCount].lightAxisX = influenceRightVS;
                m_LightVolumes[m_DecalDatasCount].lightAxisY = influenceUpVS;
                m_LightVolumes[m_DecalDatasCount].lightAxisZ = influenceForwardVS;
                m_LightVolumes[m_DecalDatasCount].boxInnerDist = influenceExtents - HDRenderPipeline.k_BoxCullingExtentThreshold;
                m_LightVolumes[m_DecalDatasCount].boxInvRange.Set(1.0f / HDRenderPipeline.k_BoxCullingExtentThreshold.x, 1.0f / HDRenderPipeline.k_BoxCullingExtentThreshold.y, 1.0f / HDRenderPipeline.k_BoxCullingExtentThreshold.z);
            }

            private void AssignCurrentBatches(ref Matrix4x4[] decalToWorldBatch, ref Matrix4x4[] normalToWorldBatch, ref float[] decalLayerMaskBatch, int batchCount)
            {
                if (m_DecalToWorld.Count == batchCount)
                {
                    decalToWorldBatch = new Matrix4x4[kDrawIndexedBatchSize];
                    m_DecalToWorld.Add(decalToWorldBatch);
                    normalToWorldBatch = new Matrix4x4[kDrawIndexedBatchSize];
                    m_NormalToWorld.Add(normalToWorldBatch);
                    decalLayerMaskBatch = new float[kDrawIndexedBatchSize];
                    m_DecalLayerMasks.Add(decalLayerMaskBatch);
                }
                else
                {
                    decalToWorldBatch = m_DecalToWorld[batchCount];
                    normalToWorldBatch = m_NormalToWorld[batchCount];
                    decalLayerMaskBatch = m_DecalLayerMasks[batchCount];
                }
            }

            public bool IsDrawn()
            {
                return ((m_Material != null) && (m_NumResults > 0));
            }

            public void CreateDrawData()
            {
                int instanceCount = 0;
                int batchCount = 0;
                m_InstanceCount = 0;
                Matrix4x4[] decalToWorldBatch = null;
                Matrix4x4[] normalToWorldBatch = null;
                float[] decalLayerMaskBatch = null;
                bool anyAffectTransparency = false;

                AssignCurrentBatches(ref decalToWorldBatch, ref normalToWorldBatch, ref decalLayerMaskBatch, batchCount);

                NativeArray<Matrix4x4> cachedDecalToWorld = m_DecalToWorlds.Reinterpret<Matrix4x4>();
                NativeArray<Matrix4x4> cachedNormalToWorld = m_NormalToWorlds.Reinterpret<Matrix4x4>();

                Vector3 cameraPos = instance.CurrentCamera.transform.position;
                var camera = instance.CurrentCamera;
                Matrix4x4 worldToView = HDRenderPipeline.WorldToCamera(camera);
                int cullingMask = camera.cullingMask;
                ulong sceneCullingMask = HDUtils.GetSceneCullingMaskFromCamera(camera);

                for (int resultIndex = 0; resultIndex < m_NumResults; resultIndex++)
                {
                    int decalIndex = m_ResultIndices[resultIndex];
                    int decalMask = 1 << m_CachedLayerMask[decalIndex];
                    ulong decalSceneCullingMask = m_CachedSceneLayerMask[decalIndex];
                    bool sceneViewCullingMaskTest = true;
#if UNITY_EDITOR
                    // In the player, both masks will be zero. Besides we don't want to pay the cost in this case.
                    sceneViewCullingMaskTest = (sceneCullingMask & decalSceneCullingMask) != 0;
#endif
                    if ((cullingMask & decalMask) != 0 && sceneViewCullingMaskTest)
                    {
                        // do additional culling based on individual decal draw distances
                        float distanceToDecal = (cameraPos - m_CachedBoundingSpheres[decalIndex].position).magnitude;
                        float cullDistance = m_CachedDrawDistances[decalIndex].x + m_CachedBoundingSpheres[decalIndex].radius;
                        if (distanceToDecal < cullDistance)
                        {
                            // d-buffer data
                            decalToWorldBatch[instanceCount] = cachedDecalToWorld[decalIndex];
                            normalToWorldBatch[instanceCount] = cachedNormalToWorld[decalIndex];
                            float fadeFactor = m_CachedFadeFactor[decalIndex] * Mathf.Clamp((cullDistance - distanceToDecal) / (cullDistance * (1.0f - m_CachedDrawDistances[decalIndex].y)), 0.0f, 1.0f);
                            // NormalToWorldBatchis a Matrix4x4x but is a Rotation matrix so bottom row and last column can be used for other data to save space
                            normalToWorldBatch[instanceCount].m03 = fadeFactor * m_Blend;
                            normalToWorldBatch[instanceCount].m13 = m_CachedAngleFade[decalIndex].x;
                            normalToWorldBatch[instanceCount].m23 = m_CachedAngleFade[decalIndex].y;
                            normalToWorldBatch[instanceCount].SetRow(3, m_CachedUVScaleBias[decalIndex]);
                            decalLayerMaskBatch[instanceCount] = (int)m_CachedDecalLayerMask[decalIndex];

                            // clustered forward data
                            if (m_CachedAffectsTransparency[decalIndex])
                            {
                                m_DecalDatas[m_DecalDatasCount].worldToDecal = decalToWorldBatch[instanceCount].inverse;
                                m_DecalDatas[m_DecalDatasCount].normalToWorld = normalToWorldBatch[instanceCount];
                                m_DecalDatas[m_DecalDatasCount].baseColor = m_BaseColor;
                                m_DecalDatas[m_DecalDatasCount].blendParams = m_BlendParams;
                                m_DecalDatas[m_DecalDatasCount].remappingAOS = m_RemappingAOS;
                                m_DecalDatas[m_DecalDatasCount].scalingBAndRemappingM = m_ScalingBAndRemappingM;
                                m_DecalDatas[m_DecalDatasCount].decalLayerMask = (uint)m_CachedDecalLayerMask[decalIndex];

                                // we have not allocated the textures in atlas yet, so only store references to them
                                m_DiffuseTextureScaleBias[m_DecalDatasCount] = m_Diffuse;
                                m_NormalTextureScaleBias[m_DecalDatasCount] = m_Normal;
                                m_MaskTextureScaleBias[m_DecalDatasCount] = m_Mask;

                                GetDecalVolumeDataAndBound(decalToWorldBatch[instanceCount], worldToView);
                                m_DecalDatasCount++;
                                anyAffectTransparency = true;
                            }

                            instanceCount++;
                            m_InstanceCount++; // total not culled by distance or cull mask
                            if (instanceCount == kDrawIndexedBatchSize)
                            {
                                instanceCount = 0;
                                batchCount++;
                                AssignCurrentBatches(ref decalToWorldBatch, ref normalToWorldBatch, ref decalLayerMaskBatch, batchCount);
                            }
                        }
                    }
                }

                // only add if any projectors in this decal set affect transparency, doesn't actually allocate textures in the atlas yet, this is because we want all the textures in the list so we can optimize the packing
                if (anyAffectTransparency)
                {
                    AddToTextureList(ref instance.m_TextureList);
                }
            }

            public void EndCull(CullRequest.Set request)
            {
                if (m_Material == null)
                    return;
                if (request.cullingGroup == null)
                    Debug.LogError("Begin/EndCull() called out of sequence for decal projectors.");
                else
                    request.Clear();
            }

            public void AddToTextureList(ref List<TextureScaleBias> textureList)
            {
                if (m_Diffuse.m_Texture != null)
                {
                    textureList.Add(m_Diffuse);
                }
                if (m_Normal.m_Texture != null)
                {
                    textureList.Add(m_Normal);
                }
                if (m_Mask.m_Texture != null)
                {
                    textureList.Add(m_Mask);
                }
            }

            public void RenderIntoDBuffer(CommandBuffer cmd)
            {
                if (m_Material == null || m_cachedProjectorPassValue == -1 || (m_NumResults == 0))
                    return;

                int batchIndex = 0;
                int totalToDraw = m_InstanceCount;

                for (; batchIndex < m_InstanceCount / kDrawIndexedBatchSize; batchIndex++)
                {
                    m_PropertyBlock.SetMatrixArray(HDShaderIDs._NormalToWorldID, m_NormalToWorld[batchIndex]);
                    m_PropertyBlock.SetFloatArray(HDMaterialProperties.kDecalLayerMaskFromDecal, m_DecalLayerMasks[batchIndex]);
                    cmd.DrawMeshInstanced(m_DecalMesh, 0, m_Material, m_cachedProjectorPassValue, m_DecalToWorld[batchIndex], kDrawIndexedBatchSize, m_PropertyBlock);
                    totalToDraw -= kDrawIndexedBatchSize;
                }

                if (totalToDraw > 0)
                {
                    m_PropertyBlock.SetMatrixArray(HDShaderIDs._NormalToWorldID, m_NormalToWorld[batchIndex]);
                    m_PropertyBlock.SetFloatArray(HDMaterialProperties.kDecalLayerMaskFromDecal, m_DecalLayerMasks[batchIndex]);
                    cmd.DrawMeshInstanced(m_DecalMesh, 0, m_Material, m_cachedProjectorPassValue, m_DecalToWorld[batchIndex], totalToDraw, m_PropertyBlock);
                }
            }

            public void RenderForwardEmissive(CommandBuffer cmd)
            {
                if (m_Material == null || m_cachedProjectorEmissivePassValue == -1 || m_NumResults == 0)
                    return;

                int batchIndex = 0;
                int totalToDraw = m_InstanceCount;

                for (; batchIndex < m_InstanceCount / kDrawIndexedBatchSize; batchIndex++)
                {
                    m_PropertyBlock.SetMatrixArray(HDShaderIDs._NormalToWorldID, m_NormalToWorld[batchIndex]);
                    m_PropertyBlock.SetFloatArray(HDMaterialProperties.kDecalLayerMaskFromDecal, m_DecalLayerMasks[batchIndex]);
                    cmd.DrawMeshInstanced(m_DecalMesh, 0, m_Material, m_cachedProjectorEmissivePassValue, m_DecalToWorld[batchIndex], kDrawIndexedBatchSize, m_PropertyBlock);
                    totalToDraw -= kDrawIndexedBatchSize;
                }

                if (totalToDraw > 0)
                {
                    m_PropertyBlock.SetMatrixArray(HDShaderIDs._NormalToWorldID, m_NormalToWorld[batchIndex]);
                    m_PropertyBlock.SetFloatArray(HDMaterialProperties.kDecalLayerMaskFromDecal, m_DecalLayerMasks[batchIndex]);
                    cmd.DrawMeshInstanced(m_DecalMesh, 0, m_Material, m_cachedProjectorEmissivePassValue, m_DecalToWorld[batchIndex], totalToDraw, m_PropertyBlock);
                }
            }

            public Material KeyMaterial
            {
                get
                {
                    return this.m_Material;
                }
            }

            public int Count
            {
                get
                {
                    return this.m_DecalsCount;
                }
            }

            public int DrawOrder => m_CachedDrawOrder;

            private List<Matrix4x4[]> m_DecalToWorld = new List<Matrix4x4[]>();
            private List<Matrix4x4[]> m_NormalToWorld = new List<Matrix4x4[]>();
            private List<float[]> m_DecalLayerMasks = new List<float[]>();

            private DecalHandle[] m_Handles = new DecalHandle[kDecalBlockSize];
            private int[] m_ResultIndices = new int[kDecalBlockSize];
            private int m_NumResults = 0;
            private int m_InstanceCount = 0;
            private int m_DecalsCount = 0;
            private int m_CachedDrawOrder = 0;
            private Vector2[] m_CachedDrawDistances = new Vector2[kDecalBlockSize]; // x - draw distance, y - fade scale
            private Vector2[] m_CachedAngleFade = new Vector2[kDecalBlockSize]; // x - scale fade, y - bias fade
            private Vector4[] m_CachedUVScaleBias = new Vector4[kDecalBlockSize]; // xy - scale, zw bias
            private bool[] m_CachedAffectsTransparency = new bool[kDecalBlockSize];
            private int[] m_CachedLayerMask = new int[kDecalBlockSize];
            private DecalLayerEnum[] m_CachedDecalLayerMask = new DecalLayerEnum[kDecalBlockSize];
            private ulong[] m_CachedSceneLayerMask = new ulong[kDecalBlockSize];
            private float[] m_CachedFadeFactor = new float[kDecalBlockSize];
            private Material m_Material;
            private MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();
            private float m_Blend = 0.0f;
            private Vector4 m_BaseColor;
            private Vector4 m_RemappingAOS;
            private Vector4 m_ScalingBAndRemappingM; // unused, mask map blue, metal remap min, metal remap max
            private Vector3 m_BlendParams;

            private bool m_IsHDRenderPipelineDecal;
            // Cached value for pass index. If -1 no pass exist
            // The projector decal rendering code relies on the order shader passes that are declared in Decal.shader and DecalSubshader.cs
            // At the init of material we look for pass index by name and cached the result.
            private int m_cachedProjectorPassValue;
            private int m_cachedProjectorEmissivePassValue;

            TextureScaleBias m_Diffuse = new TextureScaleBias();
            TextureScaleBias m_Normal = new TextureScaleBias();
            TextureScaleBias m_Mask = new TextureScaleBias();

            internal void SetCullResult(CullResult.Set value)
            {
                m_NumResults = value.numResults;
                if (m_ResultIndices.Length < m_NumResults)
                    Array.Resize(ref m_ResultIndices, m_NumResults);
                Array.Copy(value.resultIndices, m_ResultIndices, m_NumResults);
            }
        }

        void SetupMipStreamingSettings(Texture texture, bool allMips)
        {
            if (texture)
            {
                if (texture.dimension == UnityEngine.Rendering.TextureDimension.Tex2D)
                {
                    Texture2D tex2D = (texture as Texture2D);
                    if (tex2D)
                    {
                        if (allMips)
                            tex2D.requestedMipmapLevel = 0;
                        else
                            tex2D.ClearRequestedMipmapLevel();
                    }
                }
            }
        }

        void SetupMipStreamingSettings(Material material, bool allMips)
        {
            if (material != null)
            {
                if (IsHDRenderPipelineDecal(material.shader))
                {
                    SetupMipStreamingSettings(material.GetTexture("_BaseColorMap"), allMips);
                    SetupMipStreamingSettings(material.GetTexture("_NormalMap"), allMips);
                    SetupMipStreamingSettings(material.GetTexture("_MaskMap"), allMips);
                }
            }
        }

        // Add a decal material to the decal set
        public DecalHandle AddDecal(DecalProjector decalProjector)
        {
            var material = decalProjector.material;
            SetupMipStreamingSettings(material, true);

            DecalSet decalSet = null;
            int key = material != null ? material.GetInstanceID() : kNullMaterialIndex;
            if (!m_DecalSets.TryGetValue(key, out decalSet))
            {
                decalSet = new DecalSet(material);
                m_DecalSets.Add(key, decalSet);
            }
            return decalSet.AddDecal(key, decalProjector);
        }

        public void RemoveDecal(DecalHandle handle)
        {
            if (!DecalHandle.IsValid(handle))
                return;

            DecalSet decalSet = null;
            int key = handle.m_MaterialID;
            if (m_DecalSets.TryGetValue(key, out decalSet))
            {
                decalSet.RemoveDecal(handle);
                if (decalSet.Count == 0)
                {
                    SetupMipStreamingSettings(decalSet.KeyMaterial, false);

                    decalSet.Dispose();
                    m_DecalSets.Remove(key);
                }
            }
        }

        public void UpdateCachedData(DecalHandle handle, DecalProjector decalProjector)
        {
            if (!DecalHandle.IsValid(handle))
                return;

            DecalSet decalSet = null;
            int key = handle.m_MaterialID;
            if (m_DecalSets.TryGetValue(key, out decalSet))
            {
                decalSet.UpdateCachedData(handle, decalProjector);
            }
        }

        public void BeginCull(CullRequest request)
        {
            Assert.IsNotNull(request);

            request.Clear();
            foreach (var pair in m_DecalSets)
                pair.Value.BeginCull(request[pair.Key]);
        }

        private int QueryCullResults(CullRequest decalCullRequest, CullResult cullResults)
        {
            var totalVisibleDecals = 0;
            foreach (var pair in m_DecalSets)
                totalVisibleDecals += pair.Value.QueryCullResults(decalCullRequest[pair.Key], cullResults[pair.Key]);
            return totalVisibleDecals;
        }

        public void EndCull(CullRequest cullRequest, CullResult cullResults)
        {
            m_DecalsVisibleThisFrame = QueryCullResults(cullRequest, cullResults);
            foreach (var pair in m_DecalSets)
                pair.Value.EndCull(cullRequest[pair.Key]);
        }

        public void RenderIntoDBuffer(CommandBuffer cmd)
        {
            if (m_DecalMesh == null)
                m_DecalMesh = CoreUtils.CreateCubeMesh(kMin, kMax);

            foreach (var decalSet in m_DecalSetsRenderList)
            {
                decalSet.RenderIntoDBuffer(cmd);
            }
        }

        public void RenderForwardEmissive(CommandBuffer cmd)
        {
            if (m_DecalMesh == null)
                m_DecalMesh = CoreUtils.CreateCubeMesh(kMin, kMax);

            foreach (var decalSet in m_DecalSetsRenderList)
            {
                decalSet.RenderForwardEmissive(cmd);
            }
        }

        public void SetAtlas(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._DecalAtlas2DID, Atlas.AtlasTexture);
        }

        public void AddTexture(CommandBuffer cmd, TextureScaleBias textureScaleBias)
        {
            if (textureScaleBias.m_Texture != null)
            {
                if (Atlas.IsCached(out textureScaleBias.m_ScaleBias, textureScaleBias.m_Texture))
                {
                    Atlas.UpdateTexture(cmd, textureScaleBias.m_Texture, ref textureScaleBias.m_ScaleBias);
                }
                else if (!Atlas.AddTexture(cmd, ref textureScaleBias.m_ScaleBias, textureScaleBias.m_Texture))
                {
                    m_AllocationSuccess = false;
                }
            }
            else
            {
                textureScaleBias.m_ScaleBias = Vector4.zero;
            }
        }

        // updates textures, texture atlas indices and blend value
        public void UpdateCachedMaterialData()
        {
            m_TextureList.Clear();
            foreach (var pair in m_DecalSets)
            {
                pair.Value.InitializeMaterialValues();
            }
        }

        private void UpdateDecalDatasWithAtlasInfo()
        {
            for (int decalDataIndex = 0; decalDataIndex < m_DecalDatasCount; decalDataIndex++)
            {
                m_DecalDatas[decalDataIndex].diffuseScaleBias = m_DiffuseTextureScaleBias[decalDataIndex].m_ScaleBias;
                m_DecalDatas[decalDataIndex].normalScaleBias = m_NormalTextureScaleBias[decalDataIndex].m_ScaleBias;
                m_DecalDatas[decalDataIndex].maskScaleBias = m_MaskTextureScaleBias[decalDataIndex].m_ScaleBias;
            }
        }

        public void UpdateTextureAtlas(CommandBuffer cmd)
        {
            m_AllocationSuccess = true;
            foreach (TextureScaleBias textureScaleBias in m_TextureList)
            {
                AddTexture(cmd, textureScaleBias);
            }

            if (!m_AllocationSuccess) // texture failed to find space in the atlas
            {
                m_TextureList.Sort();   // sort the texture list largest to smallest for better packing
                Atlas.ResetAllocator(); // clear all allocations
                // try again
                m_AllocationSuccess = true;
                foreach (TextureScaleBias textureScaleBias in m_TextureList)
                {
                    AddTexture(cmd, textureScaleBias);
                }

                if (!m_AllocationSuccess && m_PrevAllocationSuccess) // still failed to allocate, decal atlas size needs to increase, debounce so that we don't spam the console with warnings
                {
                    Debug.LogWarning(s_AtlasSizeWarningMessage);
                }
            }
            m_PrevAllocationSuccess = m_AllocationSuccess;
            // now that textures have been stored in the atlas we can update their location info in decal data
            UpdateDecalDatasWithAtlasInfo();
        }

        public void CreateDrawData()
        {
            m_DecalDatasCount = 0;
            // reallocate if needed
            if (m_DecalsVisibleThisFrame > m_DecalDatas.Length)
            {
                int newDecalDatasSize = ((m_DecalsVisibleThisFrame + kDecalBlockSize - 1) / kDecalBlockSize) * kDecalBlockSize;
                m_DecalDatas = new DecalData[newDecalDatasSize];
                m_Bounds = new SFiniteLightBound[newDecalDatasSize];
                m_LightVolumes = new LightVolumeData[newDecalDatasSize];
                m_DiffuseTextureScaleBias = new TextureScaleBias[newDecalDatasSize];
                m_NormalTextureScaleBias = new TextureScaleBias[newDecalDatasSize];
                m_MaskTextureScaleBias = new TextureScaleBias[newDecalDatasSize];
                m_BaseColor = new Vector4[newDecalDatasSize];
            }

            // add any visible decals according to material draw order, avoid using List.Sort() because it uses quicksort, which is an unstable sort.
            m_DecalSetsRenderList.Clear();
            foreach (var pair in m_DecalSets)
            {
                pair.Value.UpdateCachedDrawOrder();

                if (pair.Value.IsDrawn())
                {
                    int insertIndex = 0;
                    while ((insertIndex < m_DecalSetsRenderList.Count) && (pair.Value.DrawOrder > m_DecalSetsRenderList[insertIndex].DrawOrder))
                    {
                        insertIndex++;
                    }
                    m_DecalSetsRenderList.Insert(insertIndex, pair.Value);
                }
            }

            foreach (var decalSet in m_DecalSetsRenderList)
                decalSet.CreateDrawData();
        }

        public void Cleanup()
        {
            if (m_Atlas != null)
                m_Atlas.Release();
            CoreUtils.Destroy(m_DecalMesh);
            // set to null so that they get recreated
            m_DecalMesh = null;
            m_Atlas = null;
        }

        public void RenderDebugOverlay(HDCamera hdCamera, CommandBuffer cmd, int mipLevel, DebugOverlay debugOverlay)
        {
            debugOverlay.SetViewport(cmd);
            HDUtils.BlitQuad(cmd, Atlas.AtlasTexture, new Vector4(1, 1, 0, 0), new Vector4(1, 1, 0, 0), mipLevel, true);
            debugOverlay.Next();
        }

        public void LoadCullResults(CullResult cullResult)
        {
            using (var enumerator = cullResult.requests.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (!m_DecalSets.TryGetValue(enumerator.Current.Key, out var decalSet))
                        continue;

                    decalSet.SetCullResult(cullResult.requests[enumerator.Current.Key]);
                }
            }
        }

        public bool IsAtlasAllocatedSuccessfully()
        {
            return m_AllocationSuccess;
        }
    }
}
