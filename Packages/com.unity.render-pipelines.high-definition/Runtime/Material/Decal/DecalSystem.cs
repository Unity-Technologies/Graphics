using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
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
            AtlasProjector = 4
        };

        public static readonly string[] s_MaterialDecalPassNames = Enum.GetNames(typeof(MaterialDecalPass));
        public static readonly string s_AtlasSizeWarningMessage = "Decal texture atlas out of space, decals on transparent geometry might not render correctly, atlas size can be changed in HDRenderPipelineAsset";
        public static readonly string s_GlobalDrawDistanceWarning = "The Draw Distance on the decal projector is larger than the global Draw Distance of {0} set in the render pipeline settings. The global setting will be used.";

        static class DecalShaderIds
        {
            // Outputs that are affected
            public static readonly int _AffectAlbedo = Shader.PropertyToID(HDMaterialProperties.kAffectAlbedo);
            public static readonly int _AffectNormal = Shader.PropertyToID(HDMaterialProperties.kAffectNormal);
            public static readonly int _AffectMetal = Shader.PropertyToID(HDMaterialProperties.kAffectMetal);
            public static readonly int _AffectAO = Shader.PropertyToID(HDMaterialProperties.kAffectAO);
            public static readonly int _AffectSmoothness = Shader.PropertyToID(HDMaterialProperties.kAffectSmoothness);

            // Shader graph atlas texture pass properties
            public static readonly int _DiffuseScaleBias = Shader.PropertyToID("_DiffuseScaleBias");
            public static readonly int _NormalScaleBias = Shader.PropertyToID("_NormalScaleBias");
            public static readonly int _MaskScaleBias = Shader.PropertyToID("_MaskScaleBias");
            public static readonly int _TextureTypes = Shader.PropertyToID("_TextureTypes");
        }

        public class CullResult : IDisposable
        {
           int m_NumResults;

            public int numResults
            {
                get => m_NumResults;
                set => m_NumResults = value;
            }

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

        public IntScalableSetting transparentTextureResolution
        {
            get
            {
                HDRenderPipelineAsset hdrp = HDRenderPipeline.currentAsset;
                if (hdrp != null)
                {
                    return hdrp.currentPlatformRenderPipelineSettings.decalSettings.transparentTextureResolution;
                }
                else
                {
                    return new IntScalableSetting(new int[] { 0, 0, 0 }, ScalableSettingSchemaId.With3Levels);
                }
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
        private const int kDecalBlockGrowthPercentage = 20;
        private const int kDecalMaxBlockSize = 2048;

        // to work on Vulkan Mobile?
        // Core\CoreRP\ShaderLibrary\UnityInstancing.hlsl
        // #if (defined(SHADER_API_VULKAN) && defined(SHADER_API_MOBILE)) || defined(SHADER_API_SWITCH)
        //      #define UNITY_INSTANCED_ARRAY_SIZE  250
        private const int kDrawIndexedBatchSize = 250;

        // cube mesh bounds for decal
        static Vector4 kMin = new Vector4(-0.5f, -0.5f, -0.5f, 1.0f);
        static Vector4 kMax = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);

        static public Mesh m_DecalMesh = null;

        // These bit flags allow one to have cluster(s) of decals with different culling algorithm.
        // Both types of clusters are created if raytracing is enabled for SSR/SSGI.
        // The ViewspaceBasedCulling mode uploads only those clustered decals that are in the view frustrum
        // The WorldspaceBasedCulling uploads clustered decals more generously. This is useful for algorithms such as path tracing that require decals to be available outside of the view frustrum.
        // Again, it only impacts the behaviour for the clustered decals; the DBuffer rendering stays the same regardless of the mode.
        [Flags]
        public enum DecalCullingMode
        {
            ViewspaceBasedCulling = 1 << 0,
            WorldspaceBasedCulling = 1 << 1
        }

        static public DecalCullingMode m_CullingMode = DecalCullingMode.ViewspaceBasedCulling;

        // clustered draw data
        static public DecalData[] m_DecalDatas = new DecalData[kDecalBlockSize];
        static public SFiniteLightBound[] m_Bounds = new SFiniteLightBound[kDecalBlockSize];
        static public LightVolumeData[] m_LightVolumes = new LightVolumeData[kDecalBlockSize];
        static public TextureScaleBias[] m_DiffuseTextureScaleBias = new TextureScaleBias[kDecalBlockSize];
        static public TextureScaleBias[] m_NormalTextureScaleBias = new TextureScaleBias[kDecalBlockSize];
        static public TextureScaleBias[] m_MaskTextureScaleBias = new TextureScaleBias[kDecalBlockSize];
        static public Vector4[] m_BaseColor = new Vector4[kDecalBlockSize];

        // Clustered decal world space info -- useful when m_CullingMode is set to WorldspaceBasedCulling
        // This data is cached and can be queried for algorithms doing their own clustering (e.g. path tracing). 
        static public Vector3[] m_DecalDatasWSPositions = new Vector3[kDecalBlockSize];
        static public Vector3[] m_DecalDatasWSRanges = new Vector3[kDecalBlockSize];
 
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

        private int m_GlobalDrawDistance = kDefaultDrawDistance;

#if UNITY_EDITOR
        private bool m_ShaderGraphSaved = false;
        private bool m_ShaderGraphSaveRequested = false;
#endif

        static public int GetDecalCount(HDCamera hdCamera)
        {
            if ((hdCamera.IsPathTracingEnabled() || hdCamera.IsRayTracingEnabled()) && hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals))
                return m_DecalDatasCount;
            return 0;
        }

        public Texture2DAtlas Atlas
        {
            get
            {
                if (m_Atlas == null)
                {
                    m_Atlas = new Texture2DAtlas(HDUtils.hdrpSettings.decalSettings.atlasWidth, HDUtils.hdrpSettings.decalSettings.atlasHeight, GraphicsFormat.R8G8B8A8_UNorm, name: "DecalSystemAtlas");
                }
                return m_Atlas;
            }
        }

        public class TextureScaleBias : IComparable
        {
            public Texture texture => m_Texture;
            public int width => m_Width;
            public int height => m_Height;
            public bool blitTexture => m_BlitTexture;
            public bool updateTexture
            {
                get => m_UpdateTexture;
                set => m_UpdateTexture = value;
            }

            private Texture m_Texture = null;
            public Vector4 m_ScaleBias = Vector4.zero;
            private int m_Width = 0;
            private int m_Height = 0;
            private bool m_BlitTexture = true;
            private bool m_UpdateTexture = false;
            public int CompareTo(object obj)
            {
                TextureScaleBias other = obj as TextureScaleBias;
                int size = width * height;
                int otherSize = other.width * other.height;
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
                if (m_Texture != null)
                {
                    m_Width = texture.width;
                    m_Height = texture.height;
                }
                else
                    m_Width = m_Height = 0;
            }

            public void Initialize(int textureSize, Vector4 scaleBias)
            {
                // If transparency is not used no textures should be created
                if (textureSize == 0)
                {
                    m_Texture = null;
                }
                // For shader graphs only create a 1x1 texture to get a correct index for the atlas
                // Recreate this texture in case the resolution changes to reallocate it
                else if (m_Texture == null || m_Width != textureSize || m_Height != textureSize)
                {
                    m_Texture = new Texture2D(1, 1);
                }
                m_Width = m_Height = textureSize;
                m_ScaleBias = scaleBias;
                m_BlitTexture = false;
                m_UpdateTexture = false;
            }
        }

        private List<TextureScaleBias> m_TextureList = new List<TextureScaleBias>();

        struct ShaderGraphData
        {
            public TextureScaleBias diffuse;
            public TextureScaleBias normal;
            public TextureScaleBias mask;
            public Material material;
            public int passIndex;
            public bool updateTexture;
            public MaterialPropertyBlock propertyBlock;

            public bool HasTexture(Decal.DecalAtlasTextureType type)
            {
                switch (type)
                {
                    case Decal.DecalAtlasTextureType.Diffuse:
                        return diffuse.texture != null;
                    case Decal.DecalAtlasTextureType.Normal:
                        return normal.texture != null;
                    case Decal.DecalAtlasTextureType.Mask:
                        return mask.texture != null;
                }
                return false;
            }

            public bool UpdateTexture(Decal.DecalAtlasTextureType type)
            {
                switch (type)
                {
                    case Decal.DecalAtlasTextureType.Diffuse:
                        return diffuse.updateTexture;
                    case Decal.DecalAtlasTextureType.Normal:
                        return normal.updateTexture;
                    case Decal.DecalAtlasTextureType.Mask:
                        return mask.updateTexture;
                }
                return false;
            }
        }
        private List<ShaderGraphData> m_ShaderGraphList = new List<ShaderGraphData>();
        private List<int> m_ShaderGraphVertexCount = new List<int>();

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

        internal void Initialize()
        {
            int globalDrawDistance = DrawDistance;

            // Reset draw cached draw distances that depend on global draw distance setting
            if (m_GlobalDrawDistance != globalDrawDistance)
            {
                m_GlobalDrawDistance = globalDrawDistance;

                DecalProjector[] decalProjectors = Resources.FindObjectsOfTypeAll<DecalProjector>();
                int projectorCount = decalProjectors.Length;
                for (int i = 0; i < projectorCount; i++)
                    ResetCachedDrawDistance(decalProjectors[i]);
            }
        }

        private partial class DecalSet : IDisposable
        {
            enum AffectAttribute
            {
                _AffectAlbedo       = 1 << 0,
                _AffectNormal       = 1 << 1,
                _AffectMetal        = 1 << 2,
                _AffectAO           = 1 << 3,
                _AffectSmoothness   = 1 << 4
            }

            int GetAttributeFlag(int nameId, AffectAttribute attribute)
            {
                if (m_Material.HasProperty(nameId))
                {
                    return m_Material.GetFloat(nameId) != 0.0f ? (int)attribute : 0;
                }
                return 0;
            }

            private int GetAffectFlags()
            {
                return GetAttributeFlag(DecalShaderIds._AffectAlbedo, AffectAttribute._AffectAlbedo)
                    | GetAttributeFlag(DecalShaderIds._AffectNormal, AffectAttribute._AffectNormal)
                    | GetAttributeFlag(DecalShaderIds._AffectMetal, AffectAttribute._AffectMetal)
                    | GetAttributeFlag(DecalShaderIds._AffectAO, AffectAttribute._AffectAO)
                    | GetAttributeFlag(DecalShaderIds._AffectSmoothness, AffectAttribute._AffectSmoothness);
            }

            private bool HasAffectFlagSet(AffectAttribute attribute, int affectFlags)
            {
                return (affectFlags & (int)attribute) != 0;
            }

            public void InitializeMaterialValues()
            {
                if (m_Material == null)
                    return;

                // TODO: this test is ambiguous, it should say, I am decal or not.
                // We should have 2 function: I am decal or not and I am a SG or not...
                m_IsHDRenderPipelineDecal = IsHDRenderPipelineDecal(m_Material);

                if (m_IsHDRenderPipelineDecal)
                {
                    bool affectNormal = m_Material.GetFloat(HDShaderIDs._AffectNormal) != 0.0f;
                    m_Normal.Initialize(affectNormal ? m_Material.GetTexture("_NormalMap") : null, Vector4.zero);

                    bool affectMetal = m_Material.GetFloat(HDShaderIDs._AffectMetal) != 0.0f;
                    bool affectAO = m_Material.GetFloat(HDShaderIDs._AffectAO) != 0.0f;
                    bool affectSmoothness = m_Material.GetFloat(HDShaderIDs._AffectSmoothness) != 0.0f;
                    bool useMask = affectMetal | affectAO | affectSmoothness;
                    m_Mask.Initialize(useMask ? m_Material.GetTexture("_MaskMap") : null, Vector4.zero);

                    float normalBlendSrc = m_Material.GetFloat("_NormalBlendSrc");
                    float maskBlendSrc = m_Material.GetFloat("_MaskBlendSrc");
                    bool affectAlbedo = m_Material.GetFloat(HDShaderIDs._AffectAlbedo) != 0.0f;
                    // base color is always added since it will be used for the general alpha value
                    m_Diffuse.Initialize(m_Material.GetTexture("_BaseColorMap"), Vector4.zero);

                    m_Blend = m_Material.GetFloat("_DecalBlend");
                    m_BaseColor = m_Material.GetVector("_BaseColor");
                    m_BlendParams = new Vector3(normalBlendSrc, maskBlendSrc, 0.0f);
                    int affectFlags =
                        (affectAlbedo ? (1 << 0) : 0) |
                        (affectNormal ? (1 << 1) : 0) |
                        (affectMetal ? (1 << 2) : 0) |
                        (affectAO ? (1 << 3) : 0) |
                        (affectSmoothness ? (1 << 4) : 0);

                    // convert to float
                    m_BlendParams.z = (float)affectFlags;

                    m_SampleNormalAlpha = 0.0f;
                    m_ScalingBlueMaskMap = m_Material.GetFloat("_DecalMaskMapBlueScale");
                    // If we have a texture, we use the remapping parameter, otherwise we use the regular one and the default texture is white
                    if (m_Material.GetTexture("_MaskMap"))
                    {
                        m_RemappingAOS = new Vector4(m_Material.GetFloat("_AORemapMin"), m_Material.GetFloat("_AORemapMax"), m_Material.GetFloat("_SmoothnessRemapMin"), m_Material.GetFloat("_SmoothnessRemapMax"));
                        m_RemappingMetallic.x = m_Material.GetFloat("_MetallicRemapMin");
                        m_RemappingMetallic.y = m_Material.GetFloat("_MetallicRemapMax");
                    }
                    else
                    {
                        m_RemappingAOS = new Vector4(m_Material.GetFloat("_AO"), m_Material.GetFloat("_AO"), m_Material.GetFloat("_Smoothness"), m_Material.GetFloat("_Smoothness"));
                        m_RemappingMetallic.x = m_Material.GetFloat("_Metallic");
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
                    int affectFlags = GetAffectFlags();
                    // Diffuse is always added to ensure we have an alpha value
                    m_Diffuse.Initialize(m_MaxShaderGraphTextureSize, Vector4.zero);

                    bool outputNormal = HasAffectFlagSet(AffectAttribute._AffectNormal, affectFlags);
                    m_Normal.Initialize(outputNormal ? m_MaxShaderGraphTextureSize : 0, Vector4.zero);

                    bool outputMask = HasAffectFlagSet(AffectAttribute._AffectMetal, affectFlags) |
                        HasAffectFlagSet(AffectAttribute._AffectAO, affectFlags) |
                        HasAffectFlagSet(AffectAttribute._AffectSmoothness, affectFlags);
                    m_Mask.Initialize(outputMask ? m_MaxShaderGraphTextureSize : 0, Vector4.zero);

                    // Constant values that are modified within the graph
                    m_Blend = 1.0f;
                    m_BaseColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
                    // Blend mode selection can be done directly in the graph so use constant values
                    float normalBlendSrc = 0.0f;
                    float maskBlendSrc = 1.0f;
                    m_BlendParams = new Vector3(normalBlendSrc, maskBlendSrc, (float)affectFlags);
                    
                    m_SampleNormalAlpha = 1.0f;
                    // Metallic, AO and Smoothness remapping can be done directly in the shader graph
                    // By hard coding those values we do an additional lerp within EvalDecalMask which could be avoided
                    float remapMin = 0.0f;
                    float remapMax = 1.0f;
                    // This scale can be applied in the shader graph by multiplying the MAOS alpha
                    m_ScalingBlueMaskMap = 1.0f;
                    m_RemappingMetallic = new Vector2(remapMin, remapMax);
                    m_RemappingAOS = new Vector4(remapMin, remapMax, remapMin, remapMax);
                    
                    // With ShaderGraph it is possible that the pass isn't generated. But if it is, it can be disabled.
                    m_cachedProjectorPassValue = m_Material.FindPass(s_MaterialDecalPassNames[(int)MaterialDecalPass.DBufferProjector]);
                    if (m_cachedProjectorPassValue != -1 && m_Material.GetShaderPassEnabled(s_MaterialDecalPassNames[(int)MaterialDecalPass.DBufferProjector]) == false)
                        m_cachedProjectorPassValue = -1;
                    m_cachedProjectorEmissivePassValue = m_Material.FindPass(s_MaterialDecalPassNames[(int)MaterialDecalPass.DecalProjectorForwardEmissive]);
                    if (m_cachedProjectorEmissivePassValue != -1 && m_Material.GetShaderPassEnabled(s_MaterialDecalPassNames[(int)MaterialDecalPass.DecalProjectorForwardEmissive]) == false)
                        m_cachedProjectorEmissivePassValue = -1;
                    m_cachedAtlasProjectorPassValue = m_Material.FindPass(s_MaterialDecalPassNames[(int)MaterialDecalPass.AtlasProjector]);
                    if (m_cachedAtlasProjectorPassValue != -1 && m_Material.GetShaderPassEnabled(s_MaterialDecalPassNames[(int)MaterialDecalPass.AtlasProjector]) == false)
                        m_cachedAtlasProjectorPassValue = -1;

                    if (m_Material.HasProperty(HDShaderIDs._TransparentDynamicUpdateDecals))
                    {
                        m_UpdateShaderGraphTexture |= m_Material.GetFloat(HDShaderIDs._TransparentDynamicUpdateDecals) == 1.0f;
                    }

                    int materialCRC = m_Material.ComputeCRC();
                    if (materialCRC != m_MaterialCRC)
                    {
                        m_UpdateShaderGraphTexture = true;
                        m_MaterialCRC = materialCRC;
                    }
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

            private float GetDrawDistance(float projectorDrawDistance)
            {
                // draw distance can't be more than global draw distance
                float globalDrawDistance = instance.DrawDistance;
                return projectorDrawDistance < globalDrawDistance
                    ? projectorDrawDistance
                    : globalDrawDistance;
            }

            public void UpdateCachedData(DecalHandle handle, DecalProjector decalProjector)
            {
                DecalProjector.CachedDecalData data = decalProjector.GetCachedDecalData();

                int index = handle.m_Index;

                m_CachedDrawDistances[index].x = GetDrawDistance(data.drawDistance);
                m_CachedDrawDistances[index].y = data.fadeScale;
                // In the shader to remap from cosine -1 to 1 to new range 0..1  (with 0 = 0 degree and 1 = 180 degree)
                // Approximate acos with polynom: (-0.69 * x^2 - 0.87) * x + HALF_PI;
                // Remap result to angle fade range => 1 - saturate((acos/PI - start)/(end - start))
                // Merging both equations above give:
                // saturate((a * x^2 + b) * x + c)
                // a = 0.69 / (PI * (end - start))
                // b = 0.87 / (PI * (end - start))
                // c = 1 + (start - 0.5) / (end - start) = (end - 0.5) / (end - start)
                // Note that b = 1.25 * a
                // Final result: saturate((x * x + 1.25) * x * a + c) -> madd mul madd
                // WARNING: this code is duplicate in VFXDecalHDRPOutput.cs and in URP
                if (data.startAngleFade == 180.0f) // angle fade is disabled
                {
                    m_CachedAngleFade[index].x = 0.0f;
                    m_CachedAngleFade[index].y = 0.0f;
                }
                else
                {
                    float angleStart = data.startAngleFade / 180.0f;
                    float angleEnd = data.endAngleFade / 180.0f;
                    float range = Mathf.Max(0.0001f, angleEnd - angleStart);
                    m_CachedAngleFade[index].x = 0.222222222f / range;
                    m_CachedAngleFade[index].y = (angleEnd - 0.5f) / range;
                }
                m_CachedUVScaleBias[index] = data.uvScaleBias;
                m_CachedAffectsTransparency[index] = data.affectsTransparency;
                m_CachedLayerMask[index] = data.layerMask;
                m_CachedSceneLayerMask[index] = data.sceneLayerMask;
                m_CachedFadeFactor[index] = data.fadeFactor;
                m_CachedDecalLayerMask[index] = data.decalLayerMask;
                m_CachedShaderGraphTextureSize[index] = decalProjector.TransparentTextureResolution;

                UpdateCachedDrawOrder();

                UpdateJobArrays(index, decalProjector);
            }

            internal void ResetCachedDrawDistance(DecalHandle handle, DecalProjector decalProjector)
            {
                var data = decalProjector.GetCachedDecalData();
                int index = handle.m_Index;
                m_CachedDrawDistances[index].x = GetDrawDistance(data.drawDistance);
            }

            public void UpdateCachedDrawOrder()
            {
                // Material can be null here if it was destroyed.
                if (m_Material != null && this.m_Material.HasProperty(HDShaderIDs._DrawOrder))
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
                    int growByAmount = Math.Min(Math.Max(m_DecalsCount * kDecalBlockGrowthPercentage / 100, kDecalBlockSize), kDecalMaxBlockSize);
                    int newCapacity = m_DecalsCount + growByAmount;

                    m_ResultIndices = new int[newCapacity];

                    GrowJobArrays(growByAmount);

                    ArrayExtensions.ResizeArray(ref m_Handles, newCapacity);
                    ArrayExtensions.ResizeArray(ref m_CachedDrawDistances, newCapacity);
                    ArrayExtensions.ResizeArray(ref m_CachedAngleFade, newCapacity);
                    ArrayExtensions.ResizeArray(ref m_CachedUVScaleBias, newCapacity);
                    ArrayExtensions.ResizeArray(ref m_CachedAffectsTransparency, newCapacity);
                    ArrayExtensions.ResizeArray(ref m_CachedLayerMask, newCapacity);
                    ArrayExtensions.ResizeArray(ref m_CachedSceneLayerMask, newCapacity);
                    ArrayExtensions.ResizeArray(ref m_CachedDecalLayerMask, newCapacity);
                    ArrayExtensions.ResizeArray(ref m_CachedFadeFactor, newCapacity);
                    ArrayExtensions.ResizeArray(ref m_CachedShaderGraphTextureSize, newCapacity);
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
                int lastIndex = m_DecalsCount - 1;
                // replace with last decal in the list and update index
                m_Handles[removeAtIndex] = m_Handles[lastIndex]; // move the last decal in list
                m_Handles[removeAtIndex].m_Index = removeAtIndex;
                m_Handles[lastIndex] = null;

                // update cached data
                RemoveFromJobArrays(removeAtIndex);
                m_CachedDrawDistances[removeAtIndex] = m_CachedDrawDistances[lastIndex];
                m_CachedAngleFade[removeAtIndex] = m_CachedAngleFade[lastIndex];
                m_CachedUVScaleBias[removeAtIndex] = m_CachedUVScaleBias[lastIndex];
                m_CachedAffectsTransparency[removeAtIndex] = m_CachedAffectsTransparency[lastIndex];
                m_CachedLayerMask[removeAtIndex] = m_CachedLayerMask[lastIndex];
                m_CachedSceneLayerMask[removeAtIndex] = m_CachedSceneLayerMask[lastIndex];
                m_CachedFadeFactor[removeAtIndex] = m_CachedFadeFactor[lastIndex];
                m_CachedShaderGraphTextureSize[removeAtIndex] = m_CachedShaderGraphTextureSize[lastIndex];
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

            private Matrix4x4 GetEncodedNormalToWorldMatrix(Matrix4x4 original, int DecalIndex, float cullDistance, float distanceToDecal)
            {
                Matrix4x4 result = original;
                float fadeFactor = m_CachedFadeFactor[DecalIndex] * Mathf.Clamp((cullDistance - distanceToDecal) / (cullDistance * (1.0f - m_CachedDrawDistances[DecalIndex].y)), 0.0f, 1.0f);
                // NormalToWorldBatch is a Matrix4x4x but is a Rotation matrix so bottom row and last column can be used for other data to save space
                result.m03 = fadeFactor * m_Blend;
                result.m13 = m_CachedAngleFade[DecalIndex].x;
                result.m23 = m_CachedAngleFade[DecalIndex].y;
                result.SetRow(3, m_CachedUVScaleBias[DecalIndex]);
                return result;
            }

            public void CreateDrawData(IntScalableSetting transparentTextureResolution)
            {
                int maxTextureSize = 0;

                NativeArray<Matrix4x4> cachedDecalToWorld = m_DecalToWorlds.Reinterpret<Matrix4x4>();
                NativeArray<Matrix4x4> cachedNormalToWorld = m_NormalToWorlds.Reinterpret<Matrix4x4>();

                Vector3 cameraPos = instance.CurrentCamera.transform.position;
                var camera = instance.CurrentCamera;
                Matrix4x4 worldToView = HDRenderPipeline.WorldToCamera(camera);

                /* Prepare data for the DBuffer drawing */ 
                if ((DecalSystem.m_CullingMode & DecalCullingMode.ViewspaceBasedCulling) != 0)
                {
                    int cullingMask = camera.cullingMask;
                    ulong sceneCullingMask = HDUtils.GetSceneCullingMaskFromCamera(camera);

                    int instanceCount = 0;
                    int batchCount = 0;
                    m_InstanceCount = 0;

                    Matrix4x4[] decalToWorldBatch = null;
                    Matrix4x4[] normalToWorldBatch = null;
                    float[] decalLayerMaskBatch = null;

                    AssignCurrentBatches(ref decalToWorldBatch, ref normalToWorldBatch, ref decalLayerMaskBatch, batchCount);

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
                                normalToWorldBatch[instanceCount] = GetEncodedNormalToWorldMatrix(cachedNormalToWorld[decalIndex], decalIndex, cullDistance, distanceToDecal);
                                decalLayerMaskBatch[instanceCount] = (int)m_CachedDecalLayerMask[decalIndex];

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
                }

                /* Prepare data for clustered decals */
                // Depending on the culling mode, we consider the decals that survived culling or all of them.
                bool useWorldspaceCluster = (DecalSystem.m_CullingMode & DecalCullingMode.WorldspaceBasedCulling) != 0;
                int decalsToConsider = useWorldspaceCluster ? m_DecalsCount : m_NumResults;

                bool anyClusteredDecalsPresent = false;
                for (int resultIndex = 0; resultIndex < decalsToConsider; resultIndex++)
                {
                    int decalIndex = useWorldspaceCluster ? resultIndex : m_ResultIndices[resultIndex];
                    // Determine data to upload for clustered decal
                    if (m_CachedAffectsTransparency[decalIndex] || useWorldspaceCluster) // in viewspace based mode, only cluster decals that affect transparent. In worldspace mode, upload all
                    {
                        float distanceToDecal = (cameraPos - m_CachedBoundingSpheres[decalIndex].position).magnitude;
                        float cullDistance = m_CachedDrawDistances[decalIndex].x + m_CachedBoundingSpheres[decalIndex].radius;

                        m_DecalDatas[m_DecalDatasCount].worldToDecal = cachedDecalToWorld[decalIndex].inverse;
                        m_DecalDatas[m_DecalDatasCount].normalToWorld = GetEncodedNormalToWorldMatrix(cachedNormalToWorld[decalIndex], decalIndex, cullDistance, distanceToDecal);
                        m_DecalDatas[m_DecalDatasCount].baseColor = new Vector4(Mathf.GammaToLinearSpace(m_BaseColor.x), Mathf.GammaToLinearSpace(m_BaseColor.y), Mathf.GammaToLinearSpace(m_BaseColor.z), m_BaseColor.w);
                        m_DecalDatas[m_DecalDatasCount].blendParams = m_BlendParams;
                        m_DecalDatas[m_DecalDatasCount].remappingAOS = m_RemappingAOS;
                        m_DecalDatas[m_DecalDatasCount].remappingMetallic = m_RemappingMetallic;
                        m_DecalDatas[m_DecalDatasCount].scalingBlueMaskMap = m_ScalingBlueMaskMap;
                        m_DecalDatas[m_DecalDatasCount].sampleNormalAlpha = m_SampleNormalAlpha;
                        m_DecalDatas[m_DecalDatasCount].decalLayerMask = (uint)m_CachedDecalLayerMask[decalIndex];

                        // we have not allocated the textures in atlas yet, so only store references to them
                        m_DiffuseTextureScaleBias[m_DecalDatasCount] = m_Diffuse;
                        m_NormalTextureScaleBias[m_DecalDatasCount] = m_Normal;
                        m_MaskTextureScaleBias[m_DecalDatasCount] = m_Mask;

                        m_DecalDatasWSPositions[m_DecalDatasCount] = m_Positions[decalIndex];
                        // compute AABB for the decal, for use in world space clustering
                        var decalXExtents = cachedDecalToWorld[decalIndex].GetRow(0);
                        var decalYExtents = cachedDecalToWorld[decalIndex].GetRow(1);
                        var decalZExtents = cachedDecalToWorld[decalIndex].GetRow(2);
                        m_DecalDatasWSRanges[m_DecalDatasCount] = new Vector3(Mathf.Abs(decalXExtents.x) + Mathf.Abs(decalXExtents.y) + Mathf.Abs(decalXExtents.z),
                                                                              Mathf.Abs(decalYExtents.x) + Mathf.Abs(decalYExtents.y) + Mathf.Abs(decalYExtents.z),
                                                                              Mathf.Abs(decalZExtents.x) + Mathf.Abs(decalZExtents.y) + Mathf.Abs(decalZExtents.z));

                        GetDecalVolumeDataAndBound(cachedDecalToWorld[decalIndex], worldToView);
                        m_DecalDatasCount++;
                        anyClusteredDecalsPresent = true;
                    }

                    // Max texture size is independent of the culling
                    if (!m_IsHDRenderPipelineDecal)
                        maxTextureSize = Math.Max(m_CachedShaderGraphTextureSize[decalIndex].Value(transparentTextureResolution), maxTextureSize);
                }

                // only add if any projectors in this decal set will be clustered, doesn't actually allocate textures in the atlas yet, this is because we want all the textures in the list so we can optimize the packing
                if (anyClusteredDecalsPresent)
                {
                    AddToTextureList(ref instance.m_TextureList);
                    if (!m_IsHDRenderPipelineDecal)
                    {
                        ShaderGraphData data;
                        data.diffuse = m_Diffuse;
                        data.normal = m_Normal;
                        data.mask = m_Mask;
                        data.material = m_Material;
                        data.passIndex = m_cachedAtlasProjectorPassValue;
                        data.updateTexture = m_UpdateShaderGraphTexture;
                        data.propertyBlock = m_PropertyBlock;
                        instance.m_ShaderGraphList.Add(data);

                        if (m_MaxShaderGraphTextureSize != maxTextureSize)
                        {
                            // The update is delayed by one frame. This ensures that InitializeMaterialValues is called with the new texture size
                            // By doing this we avoid looping over all projectors in InitializeMaterialValues
                            m_UpdateShaderGraphTexture = true;
                            m_MaxShaderGraphTextureSize = maxTextureSize;
                        }
                        else
                            m_UpdateShaderGraphTexture = false;
                    }
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
                if (m_Diffuse.texture != null)
                {
                    textureList.Add(m_Diffuse);
                }
                if (m_Normal.texture != null)
                {
                    textureList.Add(m_Normal);
                }
                if (m_Mask.texture != null)
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

            public bool HasEmissivePass
            {
                get
                {
                    return m_cachedProjectorEmissivePassValue != -1;
                }
            }

            public bool updateShaderGraphTexture
            {
                set
                {
                    m_UpdateShaderGraphTexture = value;
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
            private RenderingLayerMask[] m_CachedDecalLayerMask = new RenderingLayerMask[kDecalBlockSize];
            private ulong[] m_CachedSceneLayerMask = new ulong[kDecalBlockSize];
            private float[] m_CachedFadeFactor = new float[kDecalBlockSize];
            private IntScalableSettingValue[] m_CachedShaderGraphTextureSize = new IntScalableSettingValue[kDecalBlockSize];
            private Material m_Material;
            private MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();
            private int m_MaterialCRC = 0;
            private float m_Blend = 0.0f;
            private Vector4 m_BaseColor;
            private Vector4 m_RemappingAOS;
            private Vector2 m_RemappingMetallic;
            private float m_ScalingBlueMaskMap;
            private float m_SampleNormalAlpha;
            private Vector3 m_BlendParams;

            private bool m_IsHDRenderPipelineDecal;
            // Cached value for pass index. If -1 no pass exist
            // The projector decal rendering code relies on the order shader passes that are declared in Decal.shader and DecalSubshader.cs
            // At the init of material we look for pass index by name and cached the result.
            private int m_cachedProjectorPassValue;
            private int m_cachedProjectorEmissivePassValue;
            private int m_cachedAtlasProjectorPassValue;

            // Maximum size of the shader graph texture size.
            // This is used so only a single texture is used for all projectors within this set even if they have different resolutions
            private int m_MaxShaderGraphTextureSize = 0;
            private bool m_UpdateShaderGraphTexture = true;

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

        public Vector3 GetClusteredDecalPosition(int clusteredDecalIdx)
        {
            return m_DecalDatasWSPositions[clusteredDecalIdx];
        }

        public Vector3 GetClusteredDecalRange(int clusteredDecalIdx)
        {
            return m_DecalDatasWSRanges[clusteredDecalIdx];
        }

        // Add a decal material to the decal set
        public DecalHandle AddDecal(DecalProjector decalProjector)
        {
            var material = decalProjector.material;

            DecalSet decalSet = null;
            int key = material != null ? material.GetInstanceID() : kNullMaterialIndex;
            if (!m_DecalSets.TryGetValue(key, out decalSet))
            {
				SetupMipStreamingSettings(material, true);
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

        private DecalSet GetDecalSet(DecalHandle handle)
        {
            if (!DecalHandle.IsValid(handle))
                return null;

            DecalSet decalSet = null;
            int key = handle.m_MaterialID;
            if (m_DecalSets.TryGetValue(key, out decalSet))
                return decalSet;
            else
                return null;
        }

        public void UpdateCachedData(DecalHandle handle, DecalProjector decalProjector)
        {
            DecalSet decalSet = GetDecalSet(handle);
            if (decalSet != null)
                decalSet.UpdateCachedData(handle, decalProjector);
        }

        private void ResetCachedDrawDistance(DecalProjector decalProjector)
        {
            DecalHandle handle = decalProjector.Handle;
            DecalSet decalSet = GetDecalSet(handle);
            if (decalSet != null)
                decalSet.ResetCachedDrawDistance(handle, decalProjector);
        }

        public void UpdateTransparentShaderGraphTextures(DecalHandle handle, DecalProjector decalProjector)
        {
            DecalSet decalSet = GetDecalSet(handle);
            if (decalSet != null)
                decalSet.updateShaderGraphTexture = true;
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
            cullResults.numResults = QueryCullResults(cullRequest, cullResults);
            foreach (var pair in m_DecalSets)
                pair.Value.EndCull(cullRequest[pair.Key]);
        }

        public bool HasAnyForwardEmissive()
        {
            foreach (var decalSet in m_DecalSetsRenderList)
            {
                if (decalSet.HasEmissivePass)
                    return true;
            }
            return false;
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
            // In case any shader graphs are rendered we need to recreate the mipmaps for the full texture atlas
            // In that case we can skip blitting the decal material texture into the atlas
            bool blitMipmaps = m_ShaderGraphList.Count == 0;
            if (textureScaleBias.texture != null)
            {
                if (textureScaleBias.blitTexture)
                {
                    if (Atlas.IsCached(out textureScaleBias.m_ScaleBias, textureScaleBias.texture))
                    {
                        Atlas.UpdateTexture(cmd, textureScaleBias.texture, ref textureScaleBias.m_ScaleBias, true, blitMipmaps);
                    }
                    else if (!Atlas.AddTexture(cmd, ref textureScaleBias.m_ScaleBias, textureScaleBias.texture))
                    {
                        m_AllocationSuccess = false;
                    }
                }
                else
                {
                    if (!Atlas.IsCached(out textureScaleBias.m_ScaleBias, textureScaleBias.texture))
                    {
                        if (!Atlas.AllocateTextureWithoutBlit(textureScaleBias.texture.GetInstanceID(), textureScaleBias.width, textureScaleBias.height, ref textureScaleBias.m_ScaleBias))
                        {
                            m_AllocationSuccess = false;
                        }
                        else
                        {
                            textureScaleBias.updateTexture = true;
                        }
                    }
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
            m_ShaderGraphList.Clear();
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

        class UpdateShaderGraphTexturePassData
        {
            public TextureHandle atlasTexture;
            public List<int> shaderGraphVertexCount;
            public List<ShaderGraphData> shaderGraphData;
            public bool updateMipmaps;
        }

        class UpdateAtlasMipmapsPassData
        {
            public TextureHandle atlasTexture;
        }

        public void UpdateShaderGraphAtlasTextures(RenderGraph renderGraph)
        {
#if UNITY_EDITOR
            // Ensure that the update happens the next frame after the save has been requested to use the new shader graph values
            if (m_ShaderGraphSaveRequested)
            {
                if (!m_ShaderGraphSaved)
                    m_ShaderGraphSaved = true;
                else
                    m_ShaderGraphSaveRequested = false;
            }
#endif

            m_ShaderGraphVertexCount.Clear();

            UpdateShaderGraphTexturePassData updatePassData;
            using (var builder = renderGraph.AddRenderPass<UpdateShaderGraphTexturePassData>("UpdateShaderGraphDecalTexture", out updatePassData, ProfilingSampler.Get(HDProfileId.UpdateShaderGraphDecalTexture)))
            {
                updatePassData.atlasTexture = builder.WriteTexture(renderGraph.ImportTexture(Atlas.AtlasTexture));
                updatePassData.shaderGraphData = m_ShaderGraphList;
                updatePassData.updateMipmaps = false;

                for (int i = 0; i < m_ShaderGraphList.Count; i++)
                {
                    ShaderGraphData shaderGraphData = m_ShaderGraphList[i];
                    if (shaderGraphData.passIndex == -1)
                    {
                        Debug.LogError("Trying to update a shader graph texture with an invalid pass index");
                        continue;
                    }

                    int offset = 0;
                    Vector4 textureTypes = Vector4.zero;
                    bool updateTextures = false;
                    for (int textureIndex = 0; textureIndex < (int)Decal.DecalAtlasTextureType.Count; textureIndex++)
                    {
                        var type = (Decal.DecalAtlasTextureType)textureIndex;
                        if (shaderGraphData.HasTexture(type))
                        {
                            textureTypes[offset++] = (float)type;
                            updateTextures |= shaderGraphData.UpdateTexture(type);
                        }
                    }

#if UNITY_EDITOR
                    // If any shader graphs have been saved an update of the textures is forced to ensure the changes are   propagated
                    if (m_ShaderGraphSaved)
                        updateTextures = true;
#endif
                    m_ShaderGraphVertexCount.Add(offset * 4);

                    // Skip textures if none of them have to be updated
                    // Updates can either happen through individual textures changes within the atlas or if the entire shader   graph is dynamic
                    if (!updateTextures && !shaderGraphData.updateTexture)
                        continue;

                    shaderGraphData.propertyBlock.SetVector(DecalShaderIds._DiffuseScaleBias, shaderGraphData.diffuse.m_ScaleBias);
                    shaderGraphData.propertyBlock.SetVector(DecalShaderIds._NormalScaleBias, shaderGraphData.normal.m_ScaleBias);
                    shaderGraphData.propertyBlock.SetVector(DecalShaderIds._MaskScaleBias, shaderGraphData.mask.m_ScaleBias);
                    shaderGraphData.propertyBlock.SetVector(DecalShaderIds._TextureTypes, textureTypes);

                    updatePassData.updateMipmaps = true;
                }

                updatePassData.shaderGraphVertexCount = m_ShaderGraphVertexCount;

                builder.SetRenderFunc((UpdateShaderGraphTexturePassData data, RenderGraphContext context) =>
                {
                    context.cmd.SetRenderTarget(data.atlasTexture);

                    for (int i = 0; i < data.shaderGraphData.Count; i++)
                    {
                        int vertexCount = data.shaderGraphVertexCount[i];
                        if (vertexCount == 0)
                            continue;

                        ShaderGraphData shaderGraphData = data.shaderGraphData[i];
                        context.cmd.DrawProcedural(Matrix4x4.identity, shaderGraphData.material, shaderGraphData.passIndex, MeshTopology.Quads, vertexCount, 1, shaderGraphData.propertyBlock);
                    }
                });
            }

            // Create the mipmaps for the texture atlas
            if (updatePassData.updateMipmaps)
            {
                using (var builder = renderGraph.AddRenderPass<UpdateAtlasMipmapsPassData>("UpdateDecalAtlasMipmaps", out var passData, ProfilingSampler.Get(HDProfileId.UpdateDecalAtlasMipmaps)))
                {
                    passData.atlasTexture = builder.WriteTexture(renderGraph.ImportTexture(Atlas.AtlasTexture));

                    builder.SetRenderFunc((UpdateAtlasMipmapsPassData data, RenderGraphContext context) =>
                    {
                        context.cmd.GenerateMips(data.atlasTexture);
                    });
                }
            }

#if UNITY_EDITOR
            m_ShaderGraphSaved = m_ShaderGraphSaveRequested;
#endif
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (!m_AllocationSuccess && m_PrevAllocationSuccess) // still failed to allocate, decal atlas size needs to increase, debounce so that we don't spam the console with warnings
                    Debug.LogWarning(s_AtlasSizeWarningMessage);
#endif
            }
            m_PrevAllocationSuccess = m_AllocationSuccess;
            // now that textures have been stored in the atlas we can update their location info in decal data
            UpdateDecalDatasWithAtlasInfo();
        }

        public void CreateDrawData()
        {
            // Reset number of clustered decals 
            m_DecalDatasCount = 0;
            // Count the current maximum number of decals to cluster, to allow reallocation if needed
            int maxDecalsToCluster = m_DecalsVisibleThisFrame;
            if ((m_CullingMode & DecalCullingMode.WorldspaceBasedCulling) != 0)
            {
                maxDecalsToCluster = 0;
                foreach (var pair in m_DecalSets)
                {
                    maxDecalsToCluster += pair.Value.Count;
                }
            }

            // reallocate if needed
            if (maxDecalsToCluster > m_DecalDatas.Length)
            {
                int newDecalDatasSize = ((maxDecalsToCluster + kDecalBlockSize - 1) / kDecalBlockSize) * kDecalBlockSize;
                m_DecalDatas = new DecalData[newDecalDatasSize];
                m_Bounds = new SFiniteLightBound[newDecalDatasSize];
                m_LightVolumes = new LightVolumeData[newDecalDatasSize];
                m_DiffuseTextureScaleBias = new TextureScaleBias[newDecalDatasSize];
                m_NormalTextureScaleBias = new TextureScaleBias[newDecalDatasSize];
                m_MaskTextureScaleBias = new TextureScaleBias[newDecalDatasSize];
                m_BaseColor = new Vector4[newDecalDatasSize];
                m_DecalDatasWSPositions = new Vector3[newDecalDatasSize];
                m_DecalDatasWSRanges = new Vector3[newDecalDatasSize];
            }

            // add any visible decals according to material draw order, avoid using List.Sort() because it uses quicksort, which is an unstable sort.
            m_DecalSetsRenderList.Clear();
            foreach (var pair in m_DecalSets)
            {
                pair.Value.UpdateCachedDrawOrder();

                if (pair.Value.IsDrawn() || (m_CullingMode & DecalCullingMode.WorldspaceBasedCulling) != 0)
                {
                    int insertIndex = 0;
                    while ((insertIndex < m_DecalSetsRenderList.Count) && (pair.Value.DrawOrder > m_DecalSetsRenderList[insertIndex].DrawOrder))
                    {
                        insertIndex++;
                    }
                    m_DecalSetsRenderList.Insert(insertIndex, pair.Value);
                }
            }

            IntScalableSetting textureResolutionSetting = transparentTextureResolution;

            foreach (var decalSet in m_DecalSetsRenderList)
                decalSet.CreateDrawData(textureResolutionSetting);
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

        public void RenderDebugOverlay(HDCamera hdCamera, CommandBuffer cmd, int mipLevel, Rendering.DebugOverlay debugOverlay)
        {
            cmd.SetViewport(debugOverlay.Next());
            HDUtils.BlitQuad(cmd, Atlas.AtlasTexture, new Vector4(1, 1, 0, 0), new Vector4(1, 1, 0, 0), mipLevel, true);
        }

        public void LoadCullResults(CullResult cullResult)
        {
            m_DecalsVisibleThisFrame = cullResult.numResults;
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

#if UNITY_EDITOR
        public void UpdateTransparentShaderGraphs()
        {
            m_ShaderGraphSaveRequested = true;
        }
#endif
    }
}
