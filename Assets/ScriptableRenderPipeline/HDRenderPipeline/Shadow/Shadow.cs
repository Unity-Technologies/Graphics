using UnityEngine.Rendering;
using System;


namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // temporary namespace
    namespace ShadowExp
    {

    using ShadowRequestVector = VectorArray<ShadowmapBase.ShadowRequest>;
    using ShadowDataVector    = VectorArray<ShadowData>;
    using ShadowPayloadVector = VectorArray<ShadowPayload>;
    using ShadowIndicesVector = VectorArray<int>;

    // Standard shadow map atlas implementation using one large shadow map
    public class ShadowAtlas : ShadowmapBase, IDisposable
    {
        public const uint k_MaxCascadesInShader = 4;

        protected readonly RenderTexture              m_Shadowmap;
        protected readonly RenderTargetIdentifier     m_ShadowmapId;
        protected readonly int                        m_TempDepthId;
        protected          VectorArray<CachedEntry>   m_EntryCache = new VectorArray<CachedEntry>( 0, true );
        protected          uint                       m_ActiveEntriesCount;
        protected          FrameId                    m_FrameId;
        protected          string                     m_ShaderKeyword;
        protected          int                        m_CascadeCount;
        protected          Vector3                    m_CascadeRatios;
        protected          uint                       m_TexSlot;
        protected          uint                       m_SampSlot;
        protected          uint[]                     m_TmpWidths  = new uint[ShadowmapBase.ShadowRequest.k_MaxFaceCount];
        protected          uint[]                     m_TmpHeights = new uint[ShadowmapBase.ShadowRequest.k_MaxFaceCount];
        protected          Vector4[]                  m_TmpSplits  = new Vector4[k_MaxCascadesInShader];

        protected struct Key
        {
            public int  id;
            public uint faceIdx;
            public int  visibleIdx;
            public uint shadowDataIdx;
        }

        protected struct Data
        {
            public FrameId          frameId;
            public int              contentHash;
            public uint             slice;
            public Rect             viewport;
            public Matrix4x4        view;
            public Matrix4x4        proj;
            public Vector4          lightDir;
            public ShadowSplitData  splitData;

            public bool IsValid() { return viewport.width > 0 && viewport.height > 0; }
        }
        protected struct CachedEntry : IComparable<CachedEntry>
        {
            public Key  key;
            public Data current;
            public Data previous;

            public int CompareTo( CachedEntry other )
            {
                if (current.viewport.height != other.current.viewport.height)
                    return current.viewport.height > other.current.viewport.height ? -1 : 1;
                if (current.viewport.width != other.current.viewport.width)
                    return current.viewport.width > other.current.viewport.width ? -1 : 1;
                if( key.id != other.key.id )
                    return key.id < other.key.id ? -1 : 1;

                return key.faceIdx != other.key.faceIdx ? (key.faceIdx < other.key.faceIdx ? -1 : 1) : 0;
            }
        };

        public struct AtlasInit
        {
            public BaseInit baseInit;           // the base class's initializer
            public string   shaderKeyword;      // the global shader keyword to use when rendering the shadowmap
            public int      cascadeCount;       // the number of cascades to use (these are global in ShadowSettings for now for some reason)
            public Vector3  cascadeRatios;      // cascade split ratios
        }


        public ShadowAtlas( ref AtlasInit init ) : base( ref init.baseInit )
        {
            m_Shadowmap             = new RenderTexture( (int) m_Width, (int) m_Height, (int) m_ShadowmapBits, m_ShadowmapFormat, RenderTextureReadWrite.Linear );
            m_Shadowmap.dimension   = TextureDimension.Tex2DArray;
            m_Shadowmap.volumeDepth = (int) m_Slices;
            m_ShadowmapId           = new RenderTargetIdentifier( m_Shadowmap );

            if( !IsNativeDepth() )
            {
                m_TempDepthId = Shader.PropertyToID( "Temporary Shadowmap Depth" );
            }

            Initialize( init );
        }

        public void Initialize( AtlasInit init )
        {
            m_ShaderKeyword      = init.shaderKeyword;
            m_CascadeCount       = init.cascadeCount;
            m_CascadeRatios      = init.cascadeRatios;
        }

        override public void ReserveSlots( ShadowContextStorage sc )
        {
            m_TexSlot = sc.RequestTex2DArraySlot();
            m_SampSlot = IsNativeDepth() ? sc.RequestSamplerSlot( m_CompSamplerState ) : sc.RequestSamplerSlot( m_SamplerState );
        }

        override public void Fill( ShadowContextStorage cs )
        {
            cs.SetTex2DArraySlot( m_TexSlot, m_ShadowmapId );
        }

        public void Dispose()
        {
            // TODO: clean up resources if necessary
        }

        override public bool Reserve( FrameId frameId, ref ShadowData shadowData, ShadowRequest sr, uint width, uint height, ref VectorArray<ShadowData> entries, ref VectorArray<ShadowPayload> payload, VisibleLight[] lights )
        {
            for( uint i = 0, cnt = sr.facecount; i < cnt; ++i )
            {
                m_TmpWidths[i]  = width;
                m_TmpHeights[i] = height;
            }
            return Reserve( frameId, ref shadowData, sr, m_TmpWidths, m_TmpHeights, ref entries, ref payload, lights );
        }

        override public bool Reserve( FrameId frameId, ref ShadowData shadowData, ShadowRequest sr, uint[] widths, uint[] heights, ref VectorArray<ShadowData> entries, ref VectorArray<ShadowPayload> payload, VisibleLight[] lights )
        {
            ShadowData  sd    = shadowData;
            ShadowData  dummy = new ShadowData();

            if( sr.shadowType != GPUShadowType.Point && sr.shadowType != GPUShadowType.Spot && sr.shadowType != GPUShadowType.Directional )
                return false;

            if( sr.shadowType == GPUShadowType.Directional )
            {
                for( uint i = 0; i < k_MaxCascadesInShader; ++i )
                    m_TmpSplits[i].Set( 0.0f, 0.0f, 0.0f, float.NegativeInfinity );
            }

            Key key;
            key.id            = sr.instanceId;
            key.faceIdx       = 0;
            key.visibleIdx    = (int)sr.index;
            key.shadowDataIdx = entries.Count();

            uint originalEntryCount    = entries.Count();
            uint originalPayloadCount  = payload.Count();
            uint originalActiveEntries = m_ActiveEntriesCount;

            uint facecnt  = sr.facecount;
            uint facemask = sr.facemask;
            uint bit      = 1;
            int  resIdx   = 0;

            entries.Reserve( 6 );

            float   nearPlaneOffset = QualitySettings.shadowNearPlaneOffset;

            while ( facecnt > 0 )
            {
                if( (bit & facemask) != 0 )
                {
                    uint width  = widths[resIdx];
                    uint height = heights[resIdx];
                    uint ceIdx;
                    if( !Alloc( frameId, key, width, height, out ceIdx, payload ) )
                    {
                        entries.Purge( entries.Count() - originalEntryCount );
                        payload.Purge( payload.Count() - originalPayloadCount );
                        uint added = m_ActiveEntriesCount - originalActiveEntries;
                        for( uint i = originalActiveEntries; i < m_ActiveEntriesCount; ++i )
                            m_EntryCache.Swap( i, m_EntryCache.Count()-i-1 );
                        m_EntryCache.Purge( added, Free );
                        m_ActiveEntriesCount = originalActiveEntries;
                        return false;
                    }

                    // read
                    CachedEntry ce = m_EntryCache[ceIdx];
                    // modify
                    Matrix4x4 vp;
                    if( sr.shadowType == GPUShadowType.Point )
                        vp = ShadowUtils.ExtractPointLightMatrix( lights[sr.index], key.faceIdx, 2.0f, out ce.current.view, out ce.current.proj, out ce.current.lightDir, out ce.current.splitData, m_CullResults, (int) sr.index );
                    else if( sr.shadowType == GPUShadowType.Spot )
                        vp = ShadowUtils.ExtractSpotLightMatrix( lights[sr.index], out ce.current.view, out ce.current.proj, out ce.current.lightDir, out ce.current.splitData );
                    else if( sr.shadowType == GPUShadowType.Directional )
                    {
                        vp = ShadowUtils.ExtractDirectionalLightMatrix( lights[sr.index], key.faceIdx, m_CascadeCount, m_CascadeRatios, nearPlaneOffset, width, height, out ce.current.view, out ce.current.proj, out ce.current.lightDir, out ce.current.splitData, m_CullResults, (int) sr.index );
                        m_TmpSplits[key.faceIdx]    = ce.current.splitData.cullingSphere;
                        m_TmpSplits[key.faceIdx].w *= ce.current.splitData.cullingSphere.w;
                    }
                    else
                        vp = Matrix4x4.identity; // should never happen, though
                    // write :(
                    m_EntryCache[ceIdx] = ce;

                    sd.worldToShadow = vp.transpose; // apparently we need to transpose matrices that are sent to HLSL
                    sd.scaleOffset   = new Vector4( ce.current.viewport.width * m_WidthRcp, ce.current.viewport.height * m_HeightRcp, ce.current.viewport.x, ce.current.viewport.y );
                    sd.texelSizeRcp  = new Vector2( m_WidthRcp, m_HeightRcp );
                    sd.PackShadowmapId( m_TexSlot, m_SampSlot, ce.current.slice );
                    sd.shadowType    = sr.shadowType;
                    sd.payloadOffset = payload.Count();
                    entries.AddUnchecked(sd);

                    resIdx++;
                    facecnt--;
                    key.shadowDataIdx++;
                }
                else
                {
                    // we push a dummy face in, otherwise we'd need a mapping from face index to shadowData in the shader as well
                    entries.AddUnchecked( dummy );
                }
                key.faceIdx++;
                bit <<= 1;
            }

            if (sr.shadowType == GPUShadowType.Directional)
            {
                ShadowPayload sp = new ShadowPayload();
                payload.Reserve( k_MaxCascadesInShader );
                for( uint i = 0; i < k_MaxCascadesInShader; i++ )
                {
                    sp.Set( m_TmpSplits[i] );
                    payload.AddUnchecked( sp );
                }
            }

            return true;
        }

        override public bool ReserveFinalize( FrameId frameId, ref VectorArray<ShadowData> entries, ref VectorArray<ShadowPayload> payload )
        {
            if( Layout() )
            {
                // patch up the shadow data contents with the result of the layouting step
                for( uint i = 0; i < m_ActiveEntriesCount; ++i )
                {
                    CachedEntry ce = m_EntryCache[i];
                
                    ShadowData sd = entries[ce.key.shadowDataIdx];
                    // update the shadow data with the actual result of the layouting step
                    sd.scaleOffset   = new Vector4( ce.current.viewport.width * m_WidthRcp, ce.current.viewport.height * m_HeightRcp, ce.current.viewport.x * m_WidthRcp, ce.current.viewport.y * m_HeightRcp );
                    sd.PackShadowmapId( m_TexSlot, m_SampSlot, ce.current.slice );
                    // write back the correct results
                    entries[ce.key.shadowDataIdx] = sd;
                }
                m_EntryCache.Purge(m_EntryCache.Count() - m_ActiveEntriesCount, (CachedEntry entry) => { Free(entry); });
                return true;
            }
            m_ActiveEntriesCount  = 0;
            m_EntryCache.Reset( (CachedEntry entry) => { Free(entry); });
            return false;
        }

        virtual protected void PreUpdate( FrameId frameId, CommandBuffer cb, uint rendertargetSlice )
        {
            cb.SetRenderTarget( m_ShadowmapId, 0, (CubemapFace) 0, (int) rendertargetSlice );
            if( !IsNativeDepth() )
            {
                cb.GetTemporaryRT(m_TempDepthId, (int)m_Width, (int)m_Height, (int)m_ShadowmapBits, FilterMode.Bilinear, RenderTextureFormat.Shadowmap, RenderTextureReadWrite.Default);
                cb.SetRenderTarget( new RenderTargetIdentifier( m_TempDepthId ) );
            }
            cb.ClearRenderTarget( true, !IsNativeDepth(), m_ClearColor );
        }

        override public void Update( FrameId frameId, ScriptableRenderContext renderContext, CullResults cullResults, VisibleLight[] lights )
        {
            var profilingSample = new Utilities.ProfilingSample("Shadowmap" + m_TexSlot, renderContext);

            if (!string.IsNullOrEmpty( m_ShaderKeyword ) )
            {
                var cb = new CommandBuffer();
                cb.name = "Shadowmap.EnableShadowKeyword";
                cb.EnableShaderKeyword(m_ShaderKeyword);
                renderContext.ExecuteCommandBuffer( cb );
                cb.Dispose();
            }

            // loop for generating each individual shadowmap
            uint curSlice = uint.MaxValue;
            Bounds bounds;
            DrawShadowsSettings dss = new DrawShadowsSettings( cullResults, 0 );
            for( uint i = 0; i < m_ActiveEntriesCount; ++i )
            {
                if( !cullResults.GetShadowCasterBounds( m_EntryCache[i].key.visibleIdx, out bounds ) )
                    continue;

                var cb = new CommandBuffer();
                uint entrySlice = m_EntryCache[i].current.slice;
                if( entrySlice != curSlice )
                {
                    Debug.Assert( curSlice == uint.MaxValue || entrySlice >= curSlice, "Entries in the entry cache are not ordered in slice order." );
                    cb.name = "Shadowmap.Update.Slice" + entrySlice;

                    if( curSlice != uint.MaxValue )
                    {
                        PostUpdate( frameId, cb, curSlice );
                    }
                    curSlice = entrySlice;
                    PreUpdate( frameId, cb, curSlice );
                }

                cb.name = "Shadowmap.Update - slice: " + curSlice + ", vp.x: " + m_EntryCache[i].current.viewport.x + ", vp.y: " + m_EntryCache[i].current.viewport.y + ", vp.w: " + m_EntryCache[i].current.viewport.width + ", vp.h: " + m_EntryCache[i].current.viewport.height;
                cb.SetViewport( m_EntryCache[i].current.viewport );
                cb.SetViewProjectionMatrices( m_EntryCache[i].current.view, m_EntryCache[i].current.proj );
                cb.SetGlobalVector( "g_vLightDirWs", m_EntryCache[i].current.lightDir );
                renderContext.ExecuteCommandBuffer( cb );
                cb.Dispose();

                dss.lightIndex = m_EntryCache[i].key.visibleIdx;
                dss.splitData = m_EntryCache[i].current.splitData;
                renderContext.DrawShadows( ref dss ); // <- if this was a call on the commandbuffer we would get away with using just once commandbuffer for the entire shadowmap, instead of one per face
            }

            // post update
            if( !string.IsNullOrEmpty( m_ShaderKeyword ) )
            {
                var cb = new CommandBuffer();
                cb.name = "Shadowmap.DisableShaderKeyword";
                cb.DisableShaderKeyword( m_ShaderKeyword );
                renderContext.ExecuteCommandBuffer( cb );
                cb.Dispose();
            }

            m_ActiveEntriesCount = 0;

            profilingSample.Dispose();
        }

        virtual protected void PostUpdate( FrameId frameId, CommandBuffer cb, uint rendertargetSlice )
        {
            if( !IsNativeDepth() )
                cb.ReleaseTemporaryRT( m_TempDepthId );
        }

        protected bool Alloc( FrameId frameId, Key key, uint width, uint height, out uint cachedEntryIdx, VectorArray<ShadowPayload> payload )
        {
            CachedEntry ce = new CachedEntry();
            ce.key                 = key;
            ce.current.frameId     = frameId;
            ce.current.contentHash = -1;
            ce.current.slice       = 0;
            ce.current.viewport    = new Rect( 0, 0, width, height );

            uint idx;
            if ( m_EntryCache.FindFirst( out idx, ref key, (ref Key k, ref CachedEntry entry) => { return k.id == entry.key.id && k.faceIdx == entry.key.faceIdx; } ) )
            {
                if( m_EntryCache[idx].current.viewport.width == width && m_EntryCache[idx].current.viewport.height == height )
                {
                    ce.previous = m_EntryCache[idx].current;
                    m_EntryCache[idx] = ce;
                    cachedEntryIdx = m_ActiveEntriesCount;
                    m_EntryCache.SwapUnchecked( m_ActiveEntriesCount++, idx );
                    return true;
                }
                else
                {
                    m_EntryCache.SwapUnchecked( idx, m_EntryCache.Count()-1 );
                    m_EntryCache.Purge( 1, Free );
                }
            }

            idx = m_EntryCache.Count();
            m_EntryCache.Add( ce );
            cachedEntryIdx = m_ActiveEntriesCount;
            m_EntryCache.SwapUnchecked( m_ActiveEntriesCount++, idx );
            return true;
        }

        protected bool Layout()
        {
            VectorArray<CachedEntry> tmp = m_EntryCache.Subrange( 0, m_ActiveEntriesCount );
            tmp.Sort();

            float curx = 0, cury = 0, curh = 0, xmax = m_Width, ymax = m_Height;
            uint curslice = 0;

            for (uint i = 0; i < m_ActiveEntriesCount; ++i)
            {
                // shadow atlas layouting
                CachedEntry ce = m_EntryCache[i];
                Rect vp = ce.current.viewport;
                
                if( curx + vp.width > xmax )
                {
                    curx = 0;
                    cury += curh;
                }
                if( curx + vp.width > xmax || cury + curh > ymax )
                {
                    curslice++;
                    curx = 0;
                    cury = 0;
                }
                if( curx + vp.width > xmax || cury + curh > ymax || curslice == m_Slices )
                {
                    Debug.LogError( "ERROR! Shadow atlasing failed." );
                    return false;
                }
                vp.x = curx;
                vp.y = cury;
                ce.current.viewport = vp;
                ce.current.slice    = curslice;
                m_EntryCache[i]     = ce;
                curx += vp.width;
                curh = curh >= vp.height ? curh : vp.height;
            }
            return true;
        }

        protected void Free( CachedEntry ce )
        {
            // Nothing to do for this implementation here, as the atlas is reconstructed each frame, instead of keeping state across frames
        }
    }

// -------------------------------------------------------------------------------------------------------------------------------------------------
//
//                                                      ShadowManager
//
// -------------------------------------------------------------------------------------------------------------------------------------------------


    // Standard shadow manager
    public class ShadowManager : ShadowManagerBase
    {
        protected class ShadowContextAccess : ShadowContext
        {
            public ShadowContextAccess( ref ShadowContext.CtxtInit initializer ) : base( ref initializer ) { }
            // unfortunately ref returns are only a C# 7.0 feature
            public VectorArray<ShadowData>      shadowDatas  { get { return m_ShadowDatas; } set { m_ShadowDatas = value; } }
            public VectorArray<ShadowPayload>   payloads     { get { return m_Payloads;    } set { m_Payloads = value;    } }
        }

        private const int           k_MaxShadowmapPerType = 4;
        private ShadowSettings      m_ShadowSettings;
        private ShadowmapBase[]     m_Shadowmaps;
        private ShadowmapBase[,]    m_ShadowmapsPerType = new ShadowmapBase[(int)GPUShadowType.MAX, k_MaxShadowmapPerType];
        private ShadowContextAccess m_ShadowCtxt;
        private int[,]              m_MaxShadows    = new int[(int)GPUShadowType.MAX,2];
        // The following vectors are just temporary helpers to avoid reallocation each frame. Contents are not stable.
        private VectorArray<long>   m_TmpSortKeys   = new VectorArray<long>( 0, false );
        private ShadowRequestVector m_TmpRequests   = new ShadowRequestVector( 0, false );
        // The following vector holds data that are returned to the caller so it can be sent to GPU memory in some form. Contents are stable in between calls to ProcessShadowRequests.
        private ShadowIndicesVector m_ShadowIndices = new ShadowIndicesVector( 0, false );

        public ShadowManager( ShadowSettings shadowSettings, ref ShadowContext.CtxtInit ctxtInitializer, ShadowmapBase[] shadowmaps )
        {
            m_ShadowSettings = shadowSettings;
            m_ShadowCtxt = new ShadowContextAccess( ref ctxtInitializer );

            Debug.Assert( shadowmaps != null && shadowmaps.Length > 0 );
            m_Shadowmaps = shadowmaps;
            foreach( var sm in shadowmaps )
            {
                sm.ReserveSlots( m_ShadowCtxt );
                ShadowmapBase.ShadowSupport smsupport = sm.QueryShadowSupport();
                for( int i = 0, bit = 1; i < (int) GPUShadowType.MAX; ++i, bit <<= 1 )
                {
                    if( ((int)smsupport & bit) == 0 )
                        continue;

                    for( int idx = 0; i < k_MaxShadowmapPerType; ++idx )
                    {
                        if( m_ShadowmapsPerType[i,idx] == null )
                        {
                            m_ShadowmapsPerType[i,idx] = sm;
                            break;
                        }
                    }
                    Debug.Assert( m_ShadowmapsPerType[i,k_MaxShadowmapPerType-1] == null || m_ShadowmapsPerType[i,k_MaxShadowmapPerType-1] == sm,
                        "Only up to " + k_MaxShadowmapPerType + " are allowed per light type. If more are needed then increase ShadowManager.k_MaxShadowmapPerType" );
                }
            }

            m_MaxShadows[(int)GPUShadowType.Point      ,0] = m_MaxShadows[(int)GPUShadowType.Point        ,1] = 4;
            m_MaxShadows[(int)GPUShadowType.Spot       ,0] = m_MaxShadows[(int)GPUShadowType.Spot         ,1] = 8;
            m_MaxShadows[(int)GPUShadowType.Directional,0] = m_MaxShadows[(int)GPUShadowType.Directional  ,1] = 1;
        }

        public override void ProcessShadowRequests( FrameId frameId, CullResults cullResults, Camera camera, VisibleLight[] lights, ref uint shadowRequestsCount, int[] shadowRequests, out int[] shadowDataIndices )
        {
            shadowDataIndices = null;

            // TODO:
            // Cached the cullResults here so we don't need to pass them around.
            // Allocate needs to pass them to the shadowmaps, as the ShadowUtil functions calculating view/proj matrices need them to call into C++ land.
            // Ideally we can get rid of that at some point, then we wouldn't need to cache them here, anymore.
            foreach( var sm in m_Shadowmaps )
            {
                sm.Assign( cullResults );
            }

            if( shadowRequestsCount == 0 || lights == null || shadowRequests == null )
            {
                shadowRequestsCount = 0;
                return;
            }
            
            // first sort the shadow casters according to some priority
            PrioritizeShadowCasters( camera, lights, shadowRequestsCount, shadowRequests );

            // next prune them based on some logic
            VectorArray<int> requestedShadows = new VectorArray<int>( shadowRequests, 0, shadowRequestsCount, false );
            m_TmpRequests.Reset( shadowRequestsCount );
            uint totalGranted;
            PruneShadowCasters( camera, lights, ref requestedShadows, ref m_TmpRequests, out totalGranted );
            
            // if there are no shadow casters at this point -> bail
            if( totalGranted == 0 )
            {
                shadowRequestsCount = 0;
                return;
            }

            // TODO: Now would be a good time to kick off the culling jobs for the granted requests - but there's no way to control that at the moment.

            // finally go over the lights deemed shadow casters and try to fit them into the shadow map
            // shadowmap allocation must succeed at this point.
            m_ShadowCtxt.ClearData();
            ShadowDataVector shadowVector = m_ShadowCtxt.shadowDatas;
            ShadowPayloadVector payloadVector = m_ShadowCtxt.payloads;
            m_ShadowIndices.Reset( m_TmpRequests.Count() );
            AllocateShadows( frameId, lights, totalGranted, ref m_TmpRequests, ref m_ShadowIndices, ref shadowVector, ref payloadVector );
            Debug.Assert( m_TmpRequests.Count() == m_ShadowIndices.Count() );
            m_ShadowCtxt.shadowDatas = shadowVector;
            m_ShadowCtxt.payloads = payloadVector;

            // and set the output parameters
            uint offset;
            shadowDataIndices = m_ShadowIndices.AsArray( out offset, out shadowRequestsCount );
        }


        protected override void PrioritizeShadowCasters( Camera camera, VisibleLight[] lights, uint shadowRequestsCount, int[] shadowRequests )
        {
            // this function simply looks at the projected area on the screen, ignoring all light types and shapes
            m_TmpSortKeys.Reset( shadowRequestsCount );

            for( int i = 0; i < shadowRequestsCount; ++i )
            {
                int vlidx = shadowRequests[i];
                VisibleLight vl = lights[vlidx];
                Light l = vl.light;

                // use the screen rect as a measure of importance
                float area = vl.screenRect.width * vl.screenRect.height;
                long val = ShadowUtils.Asint( area );
                val <<= 32;
                val |= (long) vlidx;
                m_TmpSortKeys.AddUnchecked( val );
            }
            m_TmpSortKeys.Sort();
            m_TmpSortKeys.ExtractTo( shadowRequests, 0, out shadowRequestsCount, delegate(long key) { return (int) (key & 0xffffffff); } );
        }

        protected override void PruneShadowCasters( Camera camera, VisibleLight[] lights, ref VectorArray<int> shadowRequests, ref ShadowRequestVector requestsGranted, out uint totalRequestCount )
        {
            Debug.Assert( shadowRequests.Count() > 0 );
            // at this point the array is sorted in order of some importance determined by the prioritize function
            requestsGranted.Reserve( shadowRequests.Count() );
            totalRequestCount = 0;

            ShadowmapBase.ShadowRequest sreq = new ShadowmapBase.ShadowRequest();
            uint totalSlots = ResetMaxShadows();
            // there's a 1:1 mapping between the index in the shadowRequests array and the element in requestsGranted at the same index.
            // if the prune function skips requests it must make sure that the array is still compact
            m_TmpSortKeys.Reset( shadowRequests.Count() );
            for( uint i = 0, count = shadowRequests.Count(); i < count && totalSlots > 0; ++i )
            {
                int requestIdx        = shadowRequests[i];
                VisibleLight vl       = lights[requestIdx];
                bool add              = false;
                int facecount         = 0;
                GPUShadowType shadowType = GPUShadowType.Point;

                switch( vl.lightType )
                {
                    case LightType.Directional  : add = m_MaxShadows[(int)GPUShadowType.Directional  , 0]-- >= 0; shadowType = GPUShadowType.Directional; facecount = m_ShadowSettings.directionalLightCascadeCount; break;
                    case LightType.Point        : add = m_MaxShadows[(int)GPUShadowType.Point        , 0]-- >= 0; shadowType = GPUShadowType.Point      ; facecount = 6; break;
                    case LightType.Spot         : add = m_MaxShadows[(int)GPUShadowType.Spot         , 0]-- >= 0; shadowType = GPUShadowType.Spot       ; facecount = 1; break;
                }

                if( add )
                {
                    sreq.instanceId = vl.light.GetInstanceID();
                    sreq.index      = (uint) requestIdx;
                    sreq.facemask   = (uint) (1 << facecount) - 1;
                    sreq.shadowType = shadowType;
                    totalRequestCount += (uint) facecount;
                    requestsGranted.AddUnchecked( sreq );
                    totalSlots--;
                }
                else
                    m_TmpSortKeys.AddUnchecked( requestIdx );
            }
            // make sure that shadowRequests contains all light indices that are going to cast a shadow first, then the rest
            shadowRequests.Reset();
            requestsGranted.ExtractTo( ref shadowRequests, (ShadowmapBase.ShadowRequest request) => { return (int) request.index; } );
            m_TmpSortKeys.ExtractTo( ref shadowRequests, (long idx) => { return (int) idx; } );
        }

        protected override void AllocateShadows( FrameId frameId, VisibleLight[] lights, uint totalGranted, ref ShadowRequestVector grantedRequests, ref ShadowIndicesVector shadowIndices, ref ShadowDataVector shadowDatas, ref ShadowPayloadVector shadowmapPayload )
        {
            ShadowData sd = new ShadowData();
            shadowDatas.Reserve( totalGranted );
            shadowIndices.Reserve( grantedRequests.Count() );
            for( uint i = 0, cnt = grantedRequests.Count(); i < cnt; ++i )
            {
                VisibleLight        vl  = lights[grantedRequests[i].index];
                Light               l   = vl.light;
                AdditionalLightData ald = l.GetComponent<AdditionalLightData>();
                
                // set light specific values that are not related to the shadowmap
                GPUShadowType shadowtype;
                ShadowUtils.MapLightType( ald.archetype, vl.lightType, out sd.lightType, out shadowtype );
                sd.bias = l.shadowBias;
                sd.quality = 0;
                
                shadowIndices.AddUnchecked( (int) shadowDatas.Count() );

                int smidx = 0;
                while( smidx < k_MaxShadowmapPerType )
                {
                    if( m_ShadowmapsPerType[(int)shadowtype,smidx].Reserve( frameId, ref sd, grantedRequests[i], (uint) ald.shadowResolution, (uint) ald.shadowResolution, ref shadowDatas, ref shadowmapPayload, lights ) )
                        break;
                    smidx++;
                }
                if( smidx == k_MaxShadowmapPerType )
                    throw new ArgumentException("The requested shadows do not fit into any shadowmap.");
            }

            // final step for shadowmaps that only gather data during the previous loop and do the actual allocation once they have all the data.
            foreach( var sm in m_Shadowmaps )
            {
                if( !sm.ReserveFinalize( frameId, ref shadowDatas, ref shadowmapPayload ) )
                    throw new ArgumentException( "Shadow allocation failed in the ReserveFinalize step." );
            }
        }

        public override void RenderShadows( FrameId frameId, ScriptableRenderContext renderContext, CullResults cullResults, VisibleLight[] lights )
        {
            using (new Utilities.ProfilingSample("Render Shadows Exp", renderContext))
            {
                foreach( var sm in m_Shadowmaps )
                {
                    sm.Update( frameId, renderContext, cullResults, lights );
                }
            }
        }

        public override void SyncData()
        {
            m_ShadowCtxt.SyncData();
        }

        public override void BindResources( ScriptableRenderContext renderContext )
        {
            foreach( var sm in m_Shadowmaps )
            {
                sm.Fill( m_ShadowCtxt );
            }
            CommandBuffer cb = new CommandBuffer(); // <- can we just keep this around or does this have to be newed every frame?
            cb.name = "Bind resources to GPU";
            m_ShadowCtxt.BindResources( cb );
            renderContext.ExecuteCommandBuffer( cb );
            cb.Dispose();
        }

        // resets the shadow slot counters and returns the sum of all slots
        private uint ResetMaxShadows()
        {
            int total = 0;
            for( int i = 0; i < (int) GPUShadowType.MAX; ++i )
            {
                m_MaxShadows[i,0] = m_MaxShadows[i,1];
                total += m_MaxShadows[i,1];
            }
            return total > 0 ? (uint) total : 0;
        }
    }

    } // end of temporary namespace ShadowExp
} // end of namespace UnityEngine.Experimental.ScriptableRenderLoop
