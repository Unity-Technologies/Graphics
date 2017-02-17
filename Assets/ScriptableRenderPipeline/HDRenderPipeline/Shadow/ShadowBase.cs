using UnityEngine.Rendering;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL]
    public enum GPUShadowType
    {
        Point,
        Spot,
        Directional,
        MAX,
        Unknown = MAX,
        All = Point | Spot | Directional
    };


    [GenerateHLSL]
    public enum GPUShadowSampling
    {
        PCF_1tap,
        PCF_9Taps_Adaptive,
        VSM_1tap,
        MSM_1tap
    };

    namespace ShadowExp // temporary namespace until everything can be merged into the HDPipeline
    {


    // This is the struct passed into shaders
    [GenerateHLSL]
    public struct ShadowData
    {
        // shadow texture related params (need to be set by ShadowmapBase and derivatives)
        public Matrix4x4     worldToShadow;  // to light space matrix
        public Vector4       scaleOffset;    // scale and offset of shadowmap in atlas
        public Vector2       texelSizeRcp;   // reciprocal of the shadowmap's texel size in x and y
        public uint          id;             // packed texture id, sampler id and slice idx
        public GPUShadowType shadowType;     // determines the shadow algorithm, i.e. which map to sample and how to interpret the data
        public uint          payloadOffset;  // if this shadow type requires additional data it can be fetched from a global Buffer<uint> at payloadOffset.

        // light related params (need to be set via ShadowMgr and derivatives)
        public GPULightType lightType;      // the light type
        public float        bias;           // bias setting
        public float        quality;        // some quality parameters

        public void PackShadowmapId( uint texIdx, uint sampIdx, uint slice )
        {
            Debug.Assert( texIdx  <= 0xff   );
            Debug.Assert( sampIdx <= 0xff   );
            Debug.Assert( slice   <= 0xffff );
            id = texIdx << 24 | sampIdx << 16 | slice;
        }
    };

    public struct FrameId
    {
        public int   frameCount;
        public float deltaT;
    }

    // -------------- Begin temporary structs that need to be replaced at some point ---------------
    public struct SamplerState
    {
        // TODO: this should either contain the description for a sampler, or be replaced by a struct that does
        public static bool operator ==( SamplerState lhs, SamplerState rhs ) { return false; }
        public static bool operator !=( SamplerState lhs, SamplerState rhs ) { return true; }
        public override bool Equals( object obj ) { return (obj is SamplerState) && (SamplerState) obj == this; }
        public override int GetHashCode() { /* TODO: implement this at some point */ throw new NotImplementedException(); }
        }

    public struct ComparisonSamplerState
    {
        // TODO: this should either contain the description for a comparison sampler, or be replaced by a struct that does
        public static bool operator ==(ComparisonSamplerState lhs, ComparisonSamplerState rhs) { return false; }
        public static bool operator !=(ComparisonSamplerState lhs, ComparisonSamplerState rhs) { return true; }
        public override bool Equals( object obj ) { return (obj is ComparisonSamplerState) && (ComparisonSamplerState) obj == this; }
        public override int GetHashCode() { /* TODO: implement this at some point */ throw new NotImplementedException(); }
    }
    // -------------- End temporary structs that need to be replaced at some point ---------------

    public struct ShadowPayload
    {
        public int p0;
        public int p1;
        public int p2;
        public int p3;

        public void Set( float v0, float v1, float v2, float v3 )
        {
            p0 = ShadowUtils.Asint( v0 );
            p1 = ShadowUtils.Asint( v1 );
            p2 = ShadowUtils.Asint( v2 );
            p3 = ShadowUtils.Asint( v3 );
        }
        public void Set( Vector4 v ) { Set( v.x, v.y, v.z, v.w ); }
    }

    // Class holding resource information that needs to be synchronized with shaders.
    public class ShadowContextStorage
    {
        public struct Init
        {
            public uint maxShadowDataSlots;
            public uint maxPayloadSlots;
            public uint maxTex2DArraySlots;
            public uint maxTexCubeArraySlots;
            public uint maxComparisonSamplerSlots;
            public uint maxSamplerSlots;
        }
        protected ShadowContextStorage( ref Init initializer )
        {
            m_ShadowDatas.Reserve( initializer.maxShadowDataSlots );
            m_Payloads.Reserve( initializer.maxPayloadSlots );
            m_Tex2DArray.Reserve( initializer.maxTex2DArraySlots );
            m_TexCubeArray.Reserve( initializer.maxTexCubeArraySlots );
            m_CompSamplers.Reserve( initializer.maxComparisonSamplerSlots );
            m_Samplers.Reserve( initializer.maxSamplerSlots );
        }
        // query functions to be used by the shadowmap
        public uint RequestTex2DArraySlot() { return m_Tex2DArray.Add( new RenderTargetIdentifier() ); }
        public uint RequestTexCubeArraySlot() { return m_TexCubeArray.Add( new RenderTargetIdentifier() ); }
        public uint RequestSamplerSlot( SamplerState ss )
        {
            uint idx;
            if( m_Samplers.FindFirst( out idx, ref ss ) )
                return idx;
            idx = m_Samplers.Count();
            m_Samplers.Add( ss );
            return idx;
        }
        public uint RequestSamplerSlot( ComparisonSamplerState css )
        {
            uint idx;
            if( m_CompSamplers.FindFirst( out idx, ref css ) )
                return idx;
            idx = m_CompSamplers.Count();
            m_CompSamplers.Add( css );
            return idx;
        }
        // setters called each frame on the shadowmap
        public void SetTex2DArraySlot( uint slot, RenderTargetIdentifier val )      { m_Tex2DArray[slot] = val; }
        public void SetTexCubeArraySlot( uint slot, RenderTargetIdentifier val )  { m_TexCubeArray[slot] = val; }

        protected VectorArray<ShadowData>             m_ShadowDatas   = new VectorArray<ShadowData>( 0, false );
        protected VectorArray<ShadowPayload>          m_Payloads      = new VectorArray<ShadowPayload>( 0, false );
        protected VectorArray<RenderTargetIdentifier> m_Tex2DArray    = new VectorArray<RenderTargetIdentifier>( 0, true );
        protected VectorArray<RenderTargetIdentifier> m_TexCubeArray  = new VectorArray<RenderTargetIdentifier>( 0, true );
        protected VectorArray<ComparisonSamplerState> m_CompSamplers  = new VectorArray<ComparisonSamplerState>( 0, true );
        protected VectorArray<SamplerState>           m_Samplers      = new VectorArray<SamplerState>( 0, true );
    }

    // Class providing hooks to do the actual synchronization
    public class ShadowContext : ShadowContextStorage
    {
        public delegate void SyncDel( ShadowContext sc );
        public delegate void BindDel( ShadowContext sc, CommandBuffer cb );
        public struct CtxtInit
        {
            public Init     storage;
            public SyncDel  dataSyncer;
            public BindDel  resourceBinder;
        }
        public ShadowContext( ref CtxtInit initializer ) : base( ref initializer.storage )
        {
            Debug.Assert( initializer.dataSyncer != null && initializer.resourceBinder != null );
            m_DataSyncerDel = initializer.dataSyncer;
            m_ResourceBinderDel = initializer.resourceBinder;
        }
        public void ClearData() { m_ShadowDatas.Reset(); m_Payloads.Reset(); }
        // delegate that takes care of syncing data to the GPU
        public void SyncData() { m_DataSyncerDel( this ); }
        // delegate that takes care of binding textures, buffers and samplers to shaders just before rendering
        public void BindResources( CommandBuffer cb ) { m_ResourceBinderDel( this, cb ); }

        // the following functions are to be used by the bind and sync delegates
        public void GetShadowDatas( out ShadowData[] shadowDatas, out uint offset, out uint count )                           { shadowDatas   = m_ShadowDatas.AsArray( out offset, out count ); }
        public void GetPayloads( out ShadowPayload[] payloads, out uint offset, out uint count )                              { payloads      = m_Payloads.AsArray( out offset, out count ); }
        public void GetTex2DArrays( out RenderTargetIdentifier[] tex2DArrays, out uint offset, out uint count )               { tex2DArrays   = m_Tex2DArray.AsArray( out offset, out count ); }
        public void GetTexCubeArrays( out RenderTargetIdentifier[] texCubeArrays, out uint offset, out uint count )           { texCubeArrays = m_TexCubeArray.AsArray( out offset, out count ); }
        public void GetComparisonSamplerArrays( out ComparisonSamplerState[] compSamplers, out uint offset, out uint count )  { compSamplers  = m_CompSamplers.AsArray( out offset, out count ); }
        public void GetSamplerArrays( out SamplerState[] samplerArrays, out uint offset, out uint count )                     { samplerArrays = m_Samplers.AsArray( out offset, out count ); }

        private SyncDel m_DataSyncerDel;
        private BindDel m_ResourceBinderDel;
    }

    // Abstract base class for handling shadow maps.
    // Specific implementations managing atlases and the likes should inherit from this
    abstract public class ShadowmapBase
    {
        [Flags]
        public enum ShadowSupport
        {
            Point       = 1 << GPUShadowType.Point,
            Spot        = 1 << GPUShadowType.Spot,
            Directional = 1 << GPUShadowType.Directional
        }
        public struct ShadowRequest
        {
            private const byte k_IndexBits    = 24;
            private const byte k_FaceBits     = 32 - k_IndexBits;
            private const uint k_MaxIndex     = (1 << k_IndexBits) - 1;
            private const byte k_MaxFace      = (1 << k_FaceBits) - 1;
            public  const int  k_MaxFaceCount = k_FaceBits;

            // combined face mask and visible light index
            private uint m_MaskIndex;
            // instance Id for this light
            public int   instanceId { get; set; }
            // shadow type of this light
            public GPUShadowType shadowType { get; set; }
            // index into the visible lights array
            public uint index
            {
                get { return m_MaskIndex & k_MaxIndex; }
                set { m_MaskIndex = value & k_MaxIndex; }
            }
            // mask of which faces are requested:
            // - for spotlights the value is always 1
            // - for point lights the bit positions map to the faces as listed in the CubemapFace enum
            // - for directional lights the bit positions map to the individual cascades
            public uint facemask
            {
                get { return (m_MaskIndex >> k_IndexBits) & k_MaxFace; }
                set { m_MaskIndex = (m_MaskIndex & k_MaxIndex) | (value << k_IndexBits); }
            }
            public uint facecount
            {
                get
                {
                    uint fc = facemask;
                    uint count = 0;
                    while (fc != 0)
                    {
                        count += fc & 1;
                        fc >>= 1;
                    }
                    return count;
                }
            }
        }


        protected readonly uint                     m_Width;
        protected readonly uint                     m_Height;
        protected readonly uint                     m_Slices;
        protected readonly uint                     m_ShadowmapBits;
        protected readonly RenderTextureFormat      m_ShadowmapFormat;
        protected readonly SamplerState             m_SamplerState;
        protected readonly ComparisonSamplerState   m_CompSamplerState;
        protected readonly Vector4                  m_ClearColor;
        protected readonly float                    m_WidthRcp;
        protected readonly float                    m_HeightRcp;
        protected readonly uint                     m_MaxPayloadCount;
        protected readonly ShadowSupport            m_ShadowSupport;
        protected          uint                     m_ShadowId;
        protected          CullResults              m_CullResults; // TODO: Temporary, due to CullResults dependency in ShadowUtils' matrix extraction code. Remove this member once that dependency is gone.

        public struct BaseInit
        {
            public uint                     width;                      // width of the shadowmap
            public uint                     height;                     // height of the shadowmap
            public uint                     slices;                     // slices for the shadowmap
            public uint                     shadowmapBits;              // bit depth for native shadowmaps, or bitdepth for the temporary shadowmap if the shadowmapFormat is not native
            public RenderTextureFormat      shadowmapFormat;            // texture format of the shadowmap
            public SamplerState             samplerState;               // the desired sampler state for non-native shadowmaps
            public ComparisonSamplerState   comparisonSamplerState;     // the desired sampler state for native shadowmaps doing depth comparisons as well
            public Vector4                  clearColor;                 // the clear color used for non-native shadowmaps
            public uint                     maxPayloadCount;            // how many ints will be pushed into the payload buffer for each invocation of Reserve
            public ShadowSupport            shadowSupport;              // bitmask of all shadow types that this shadowmap supports
        };

        protected ShadowmapBase( ref BaseInit initializer )
        {
            m_Width             = initializer.width;
            m_Height            = initializer.height;
            m_Slices            = initializer.slices;
            m_ShadowmapBits     = initializer.shadowmapBits;
            m_ShadowmapFormat   = initializer.shadowmapFormat;
            m_SamplerState      = initializer.samplerState;
            m_CompSamplerState  = initializer.comparisonSamplerState;
            m_ClearColor        = initializer.clearColor;
            m_WidthRcp          = 1.0f / initializer.width;
            m_HeightRcp         = 1.0f / initializer.height;
            m_MaxPayloadCount   = initializer.maxPayloadCount;
            m_ShadowSupport     = initializer.shadowSupport;
            m_ShadowId          = 0;

            if( IsNativeDepth() && m_Slices > 1 )
            {
                // TODO: Right now when using any of the SetRendertarget functions we ultimately end up in RenderTextureD3D11.cpp
                //       SetRenderTargetD3D11Internal. This function sets the correct slice only for RTVs, whereas depth textures only
                //       support one DSV. So there's currently no way to have individual DSVs per slice to render into (ignoring going through a geometry shader and selecting the slice there).
                Debug.LogError( "Unity does not allow direct rendering into specific depth slices, yet. Defaulting back to one array slice." );
                m_Slices = 1;
            }
        }

        protected bool IsNativeDepth()
        {
            return m_ShadowmapFormat == RenderTextureFormat.Shadowmap || m_ShadowmapFormat == RenderTextureFormat.Depth;
        }


                 public ShadowSupport QueryShadowSupport() { return m_ShadowSupport; }
                 public uint GetMaxPayload() { return m_MaxPayloadCount; }
                 public void AssignId( uint shadowId ) { m_ShadowId = shadowId; }
                 public void Assign( CullResults cullResults ) { m_CullResults = cullResults; } // TODO: Remove when m_CullResults is removed again
        abstract public bool Reserve( FrameId frameId, ref ShadowData shadowData, ShadowRequest sr, uint width, uint height, ref VectorArray<ShadowData> entries, ref VectorArray<ShadowPayload> payloads, VisibleLight[] lights );
        abstract public bool Reserve( FrameId frameId, ref ShadowData shadowData, ShadowRequest sr, uint[] widths, uint[] heights, ref VectorArray<ShadowData> entries, ref VectorArray<ShadowPayload> payloads, VisibleLight[] lights );
        abstract public bool ReserveFinalize( FrameId frameId, ref VectorArray<ShadowData> entries, ref VectorArray<ShadowPayload> payloads );
        abstract public void Update( FrameId frameId, ScriptableRenderContext renderContext, CullResults cullResults, VisibleLight[] lights );
        abstract public void ReserveSlots( ShadowContextStorage sc );
        abstract public void Fill( ShadowContextStorage cs );
    }

    interface IShadowManager
    {
        // Warning: The shadowRequests array and shadowRequestsCount are modified by this function.
        //          When called the array contains the indices of lights requesting shadows,
        //          upon returning the array contains up to shadowRequestsCount valid shadow caster indices,
        //          whereas [shadowRequestsCount;originalRequestsCount) will hold all indices for lights that wanted to cast a shadow but got rejected.
        //          shadowDataIndices contains the offset into the shadowDatas array only for each shadow casting light, e.g. lights[shadowRequests[i]].shadowDataOffset = shadowDataIndices[i];
        //          shadowDatas contains shadowmap related basic parameters that can be passed to the shader.
        //          shadowPayloads contains implementation specific data that is accessed from the shader by indexing into an Buffer<int> using ShadowData.ShadowmapData.payloadOffset.
        //          This is the equivalent of a void pointer in the shader and there needs to be loader code that knows how to interpret the data.
        //          If there are no valid shadow casters all output arrays will be null, otherwise they will contain valid data that can be passed to shaders.
        void ProcessShadowRequests( FrameId frameId, CullResults cullResults, Camera camera, VisibleLight[] lights, ref uint shadowRequestsCount, int[] shadowRequests, out int[] shadowDataIndices );
        // Renders all shadows for lights the were deemed shadow casters after the last call to ProcessShadowRequests
        void RenderShadows( FrameId frameId, ScriptableRenderContext renderContext, CullResults cullResults, VisibleLight[] lights );
        // Synchronize data with GPU buffers
        void SyncData();
        // Binds resources to shader stages just before rendering the lighting pass
        void BindResources( ScriptableRenderContext renderContext );
    }

    abstract public class ShadowManagerBase : IShadowManager
    {
        public    abstract void ProcessShadowRequests( FrameId frameId, CullResults cullResults, Camera camera, VisibleLight[] lights, ref uint shadowRequestsCount, int[] shadowRequests, out int[] shadowDataIndices );
        public    abstract void RenderShadows( FrameId frameId, ScriptableRenderContext renderContext, CullResults cullResults, VisibleLight[] lights );
        public    abstract void SyncData();
        public    abstract void BindResources( ScriptableRenderContext renderContext );
        // sort the shadow requests in descending priority - may only modify shadowRequests
        protected abstract void PrioritizeShadowCasters( Camera camera, VisibleLight[] lights, uint shadowRequestsCount, int[] shadowRequests );
        // prune the shadow requests - may modify shadowRequests and shadowsCountshadowRequestsCount
        protected abstract void PruneShadowCasters( Camera camera, VisibleLight[] lights, ref VectorArray<int> shadowRequests, ref VectorArray<ShadowmapBase.ShadowRequest> requestsGranted, out uint totalRequestCount );
        // allocate the shadow requests in the shadow map, only is called if shadowsCount > 0 - may modify shadowRequests and shadowsCount
        protected abstract void AllocateShadows( FrameId frameId, VisibleLight[] lights, uint totalGranted, ref VectorArray<ShadowmapBase.ShadowRequest> grantedRequests, ref VectorArray<int> shadowIndices, ref VectorArray<ShadowData> shadowmapDatas, ref VectorArray<ShadowPayload> shadowmapPayload );
    }

    } // end of namespace ShadowExp
} // end of namespace UnityEngine.Experimental.ScriptableRenderLoop
