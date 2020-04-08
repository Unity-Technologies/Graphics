using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class HDShadowAtlas
    {
        public enum BlurAlgorithm
    {
            None,
            EVSM, // exponential variance shadow maps
            IM // Improved Moment shadow maps
        }

        public RTHandle                             renderTarget { get { return m_Atlas; } }
        readonly List<HDShadowResolutionRequest>    m_ShadowResolutionRequests = new List<HDShadowResolutionRequest>();
        readonly List<HDShadowRequest>              m_ShadowRequests = new List<HDShadowRequest>();

        readonly List<HDShadowResolutionRequest>    m_ListOfCachedShadowRequests = new List<HDShadowResolutionRequest>();

        public int                  width { get; private set; }
        public int                  height  { get; private set; }

        RTHandle                    m_Atlas;
        Material                    m_ClearMaterial;
        LightingDebugSettings       m_LightingDebugSettings;
        float                       m_RcpScaleFactor = 1;
        FilterMode                  m_FilterMode;
        DepthBits                   m_DepthBufferBits;
        RenderTextureFormat         m_Format;
        string                      m_Name;
        int                         m_AtlasSizeShaderID;
        int                         m_AtlasShaderID;
        int                         m_MomentAtlasShaderID;
        RenderPipelineResources     m_RenderPipelineResources;

        // Moment shadow data
        BlurAlgorithm m_BlurAlgorithm;
        RTHandle[] m_AtlasMoments = null;
        RTHandle m_IntermediateSummedAreaTexture;
        RTHandle m_SummedAreaTexture;
        HDShadowResolutionRequest[] m_SortedRequestsCache;


        public int frameOfCacheValidity { get; private set; }
        public int atlasShapeID { get; private set; }

        // TODO: This whole caching system needs to be refactored. At the moment there is lots of unecessary data being copied often.
        HDShadowResolutionRequest[] m_CachedResolutionRequests;
        int m_CachedResolutionRequestsCounter = 0;

        bool m_HasResizedAtlas = false;
        int frameCounter = 0;

        public HDShadowAtlas(RenderPipelineResources renderPipelineResources, int width, int height, int atlasShaderID, int atlasSizeShaderID, Material clearMaterial, int maxShadowRequests, BlurAlgorithm blurAlgorithm = BlurAlgorithm.None, FilterMode filterMode = FilterMode.Bilinear, DepthBits depthBufferBits = DepthBits.Depth16, RenderTextureFormat format = RenderTextureFormat.Shadowmap, string name = "", int momentAtlasShaderID = 0)
        {
            this.width = width;
            this.height = height;
            m_FilterMode = filterMode;
            m_DepthBufferBits = depthBufferBits;
            m_Format = format;
            m_Name = name;
            m_AtlasShaderID = atlasShaderID;
            m_MomentAtlasShaderID = momentAtlasShaderID;
            m_AtlasSizeShaderID = atlasSizeShaderID;
            m_ClearMaterial = clearMaterial;
            m_BlurAlgorithm = blurAlgorithm;
            m_RenderPipelineResources = renderPipelineResources;

            m_SortedRequestsCache = new HDShadowResolutionRequest[Mathf.CeilToInt(maxShadowRequests*1.5f)];
            m_CachedResolutionRequests = new HDShadowResolutionRequest[maxShadowRequests];
            for(int i=0; i<maxShadowRequests; ++i)
            {
                m_CachedResolutionRequests[i] = new HDShadowResolutionRequest();
            }

            AllocateRenderTexture();
        }

        void AllocateRenderTexture()
        {
            if (m_Atlas != null)
                m_Atlas.Release();

            m_Atlas = RTHandles.Alloc(width, height, filterMode: m_FilterMode, depthBufferBits: m_DepthBufferBits, isShadowMap: true, name: m_Name);

            if (m_BlurAlgorithm == BlurAlgorithm.IM)
            {
                string momentShadowMapName = m_Name + "Moment";
                m_AtlasMoments = new RTHandle[1];
                m_AtlasMoments[0] = RTHandles.Alloc(width, height, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, enableRandomWrite: true, name: momentShadowMapName);
                string intermediateSummedAreaName = m_Name + "IntermediateSummedArea";
                m_IntermediateSummedAreaTexture = RTHandles.Alloc(width, height, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32G32B32A32_SInt, enableRandomWrite: true, name: intermediateSummedAreaName);
                string summedAreaName = m_Name + "SummedAreaFinal";
                m_SummedAreaTexture = RTHandles.Alloc(width, height, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32G32B32A32_SInt, enableRandomWrite: true, name: summedAreaName);
            }
            else if (m_BlurAlgorithm == BlurAlgorithm.EVSM)
            {
                string[] momentShadowMapNames = { m_Name + "Moment", m_Name + "MomentCopy" };
                m_AtlasMoments = new RTHandle[2];
                for (int i = 0; i < 2; ++i)
                {
                    m_AtlasMoments[i] = RTHandles.Alloc(width / 2, height / 2, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32G32_SFloat, useMipMap: true, autoGenerateMips: false, enableRandomWrite: true, name: momentShadowMapNames[i]);
                }
            }
        }

        public void BindResources(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(m_AtlasShaderID, m_Atlas);
            if (m_BlurAlgorithm == BlurAlgorithm.EVSM)
            {
                cmd.SetGlobalTexture(m_MomentAtlasShaderID, m_AtlasMoments[0]);
            }
        }

        public void UpdateSize(Vector2Int size)
        {
            if (m_Atlas == null || m_Atlas.referenceSize != size)
            {
                width = size.x;
                height = size.y;
                AllocateRenderTexture();
            }
        }

        internal void ReserveResolution(HDShadowResolutionRequest shadowRequest)
        {
            m_ShadowResolutionRequests.Add(shadowRequest);
        }

        internal void AddShadowRequest(HDShadowRequest shadowRequest)
        {
            m_ShadowRequests.Add(shadowRequest);
        }

        public void UpdateDebugSettings(LightingDebugSettings lightingDebugSettings)
        {
            m_LightingDebugSettings = lightingDebugSettings;
        }

        // Stable (unlike List.Sort) sorting algorithm which, unlike Linq's, doesn't use JIT (lol).
        // Sorts in place. Very efficient (O(n)) for already sorted data.
        void InsertionSort(HDShadowResolutionRequest[] array, int startIndex, int lastIndex)
        {
            int i = startIndex + 1;

            while (i < lastIndex)
            {
                var curr = array[i];

                int j = i - 1;

                // Sort in descending order.
                while ((j >= 0) && ((curr.resolution.x > array[j].resolution.x) ||
                                    (curr.resolution.y > array[j].resolution.y)))
                {
                    array[j + 1] = array[j];
                    j--;
                }

                array[j + 1] = curr;
                i++;
            }
        }

        internal HDShadowResolutionRequest GetCachedRequest(int cachedIndex)
        {
            if (cachedIndex < 0 || cachedIndex >= m_ListOfCachedShadowRequests.Count)
                return null;

            return m_ListOfCachedShadowRequests[cachedIndex];
        }

        internal bool HasResizedThisFrame()
        {
            return m_HasResizedAtlas;
        }

        internal void MarkCulledShadowMapAsEmptySlots()
        {
            for(int i=0; i<m_ListOfCachedShadowRequests.Count; ++i)
            {
                if ((frameCounter - m_ListOfCachedShadowRequests[i].lastFrameActive) > 0)
                {
                    m_ListOfCachedShadowRequests[i].emptyRequest = true;
                }
            }

            frameCounter++;
        }

        internal void PruneDeadCachedLightSlots()
        {
            m_ListOfCachedShadowRequests.RemoveAll(x => (x.emptyRequest));
            frameOfCacheValidity = 0; // Invalidate cached data.
        }

        internal void MarkCachedShadowSlotAsEmpty(int lightID)
        {
            var subList = m_ListOfCachedShadowRequests.FindAll(x => x.lightID == lightID);
            for (int i = 0; i < subList.Count; ++i)
            {
                subList[i].emptyRequest = true;
            }
        }

        internal int RegisterCachedLight(HDShadowResolutionRequest request)
        {

            // Since we are starting caching light resolution requests, it means that data cached from now on will be valid.
            frameOfCacheValidity++;

            // If it is already registered, we do nothing.
            int shadowIndex = -1;
            for(int i=0; i< m_ListOfCachedShadowRequests.Count; ++i)
            {
                if(!m_ListOfCachedShadowRequests[i].emptyRequest && m_ListOfCachedShadowRequests[i].lightID == request.lightID && m_ListOfCachedShadowRequests[i].indexInLight == request.indexInLight)
                {
                    shadowIndex = i;
                    break;
                }
            }

            if (shadowIndex == -1)
            {
                // First we search if we have a hole we can fill with it.
                float resolutionOfNewLight = request.atlasViewport.width;
                request.lastFrameActive = frameCounter;

                int holeWithRightSize = -1;
                for (int i = 0; i < m_ListOfCachedShadowRequests.Count; ++i)
                {
                    var currReq = m_ListOfCachedShadowRequests[i];
                    if (currReq.emptyRequest &&   // Is empty
                        request.atlasViewport.width <= currReq.atlasViewport.width && // fits the request
                        (currReq.atlasViewport.width - request.atlasViewport.width) <= currReq.atlasViewport.width * 0.1f) // but is not much smaller.
                    {
                        holeWithRightSize = i;
                        break;
                    }
                }

                if (holeWithRightSize >= 0)
                {
                    m_ListOfCachedShadowRequests[holeWithRightSize] = request.ShallowCopy();
                    return holeWithRightSize;
                }
                else
                {

                    // We need to resort the list, so we use the occasion to reset the pool. This feels suboptimal, but it is the easiest way to comply with the pooling system.
                    // TODO: Make this cleaner and more efficient.
                    m_CachedResolutionRequestsCounter = 0;
                    for (int i=0; i<m_ListOfCachedShadowRequests.Count; ++i)
                    {
                        int currEntryInPool = m_CachedResolutionRequestsCounter;
                        m_CachedResolutionRequests[currEntryInPool] = m_ListOfCachedShadowRequests[i].ShallowCopy();
                        m_ListOfCachedShadowRequests[i] = m_CachedResolutionRequests[currEntryInPool];
                        m_CachedResolutionRequestsCounter++;
                    }
                    // Now we add the new element
                    m_CachedResolutionRequests[m_CachedResolutionRequestsCounter] = request.ShallowCopy();
                    m_ListOfCachedShadowRequests.Add(m_CachedResolutionRequests[m_CachedResolutionRequestsCounter]);
                    m_CachedResolutionRequestsCounter++;

                    InsertionSort(m_ListOfCachedShadowRequests.ToArray(), 0, m_ListOfCachedShadowRequests.Count);
                    frameOfCacheValidity = 0;     // Invalidate cached data
                    for (int i = 0; i < m_ListOfCachedShadowRequests.Count; ++i)
                    {
                        if (m_ListOfCachedShadowRequests[i].lightID == request.lightID && m_ListOfCachedShadowRequests[i].indexInLight == request.indexInLight)
                        {
                            return i;
                        }
                    }
                }
            }
            else if (m_ListOfCachedShadowRequests[shadowIndex].emptyRequest)
            {
                // We still hold the spot, so fill it up again.
                m_ListOfCachedShadowRequests[shadowIndex].emptyRequest = false;
            }
            m_ListOfCachedShadowRequests[shadowIndex].lastFrameActive = frameCounter;

            return shadowIndex;
        }

        private bool AtlasLayout(bool allowResize, HDShadowResolutionRequest[] fullShadowList, int requestsCount, bool enteredWithPrunedCachedList = false)
        {
            float curX = 0, curY = 0, curH = 0, xMax = width, yMax = height;
            m_RcpScaleFactor = 1;
            for (int i = 0; i < requestsCount; ++i)
            {
                var shadowRequest = fullShadowList[i];
                // shadow atlas layouting
                Rect viewport = new Rect(Vector2.zero, shadowRequest.resolution);
                curH = Mathf.Max(curH, viewport.height);

                if (curX + viewport.width > xMax)
                {
                    curX = 0;
                    curY += curH;
                    curH = viewport.height;
                }
                if (curY + curH > yMax)
                {
                    if(enteredWithPrunedCachedList)
                    {
                        // We need to resize. We invalidate the data and clear stored list of cached.
                        frameOfCacheValidity = 0;
                        m_ListOfCachedShadowRequests.Clear();
                        // Since we emptied the cached list, we can start from scratch in the pool
                        m_CachedResolutionRequestsCounter = 0;

                        if (allowResize)
                        {
                            LayoutResize();
                            m_HasResizedAtlas = true;
                            return true;
                        }

                        return false;
                    }
                    else
                    {
                        // We can still prune
                        PruneDeadCachedLightSlots();
                        // Remove cached slots from the currently sorted list (instead of rebuilding it).
                        // Since it is ordered, the order post deletion is guaranteed.
                        int newIndex = 0;
                        for(int j=0; j<requestsCount; ++j)
                        {
                            if(!fullShadowList[j].emptyRequest)
                            {
                                m_SortedRequestsCache[newIndex++] = fullShadowList[j];
                            }
                        }

                        return AtlasLayout(allowResize, m_SortedRequestsCache, requestsCount: newIndex, enteredWithPrunedCachedList: true);
                    }
                }
                viewport.x = curX;
                viewport.y = curY;
                shadowRequest.atlasViewport = viewport;
                shadowRequest.resolution = viewport.size;
                curX += viewport.width;
            }

            m_HasResizedAtlas = false;
            return true;
        }

        internal bool Layout(bool allowResize = true)
        {
            // Sort non-cached requests
            // Perform a deep copy.
            int n = (m_ShadowResolutionRequests != null) ? m_ShadowResolutionRequests.Count : 0;

            // First add in front the cached shadows
            int i = 0;
            for (; i < m_ListOfCachedShadowRequests.Count; ++i)
            {
                m_SortedRequestsCache[i] = m_ListOfCachedShadowRequests[i];
            }

            int firstNonCachedIdx = i;
            for (int j = 0; j < m_ShadowResolutionRequests.Count; ++j)
            {
                if (!m_ShadowResolutionRequests[j].hasBeenStoredInCachedList)
                {
                    m_SortedRequestsCache[i++] = m_ShadowResolutionRequests[j];
                }
            }

            // Sorting the non cached shadows range
            InsertionSort(m_SortedRequestsCache, firstNonCachedIdx, i);

            return AtlasLayout(allowResize, m_SortedRequestsCache, requestsCount: i, enteredWithPrunedCachedList: false);
        }

        void LayoutResize()
        {
            int index = 0;
            float currentX = 0;
            float currentY = 0;
            float currentMaxY = 0;
            float currentMaxX = 0;

            // Place shadows in a square shape
            while (index < m_ShadowResolutionRequests.Count)
            {
                float y = 0;
                float currentMaxXCache = currentMaxX;
                do
                {
                    Rect r = new Rect(Vector2.zero, m_ShadowResolutionRequests[index].resolution);
                    r.x = currentMaxX;
                    r.y = y;
                    y += r.height;
                    currentY = Mathf.Max(currentY, y);
                    currentMaxXCache = Mathf.Max(currentMaxXCache, currentMaxX + r.width);
                    m_ShadowResolutionRequests[index].atlasViewport = r;
                    index++;
                } while (y < currentMaxY && index < m_ShadowResolutionRequests.Count);
                currentMaxY = Mathf.Max(currentMaxY, currentY);
                currentMaxX = currentMaxXCache;
                if (index >= m_ShadowResolutionRequests.Count)
                    continue;
                float x = 0;
                float currentMaxYCache = currentMaxY;
                do
                {
                    Rect r = new Rect(Vector2.zero, m_ShadowResolutionRequests[index].resolution);
                    r.x = x;
                    r.y = currentMaxY;
                    x += r.width;
                    currentX = Mathf.Max(currentX, x);
                    currentMaxYCache = Mathf.Max(currentMaxYCache, currentMaxY + r.height);
                    m_ShadowResolutionRequests[index].atlasViewport = r;
                    index++;
                } while (x < currentMaxX && index < m_ShadowResolutionRequests.Count);
                currentMaxX = Mathf.Max(currentMaxX, currentX);
                currentMaxY = currentMaxYCache;
            }

            float maxResolution = Math.Max(currentMaxX, currentMaxY);
            Vector4 scale = new Vector4(width / maxResolution, height / maxResolution, width / maxResolution, height / maxResolution);
            m_RcpScaleFactor = Mathf.Min(scale.x, scale.y);

            // Scale down every shadow rects to fit with the current atlas size
            foreach (var r in m_ShadowResolutionRequests)
            {
                Vector4 s = new Vector4(r.atlasViewport.x, r.atlasViewport.y, r.atlasViewport.width, r.atlasViewport.height);
                Vector4 reScaled = Vector4.Scale(s, scale);

                r.atlasViewport = new Rect(reScaled.x, reScaled.y, reScaled.z, reScaled.w);
                r.resolution = r.atlasViewport.size;
            }

            atlasShapeID++;
        }

        public void RenderShadows(CullingResults cullResults, FrameSettings frameSettings, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (m_ShadowRequests.Count == 0)
                return;

            ShadowDrawingSettings shadowDrawSettings = new ShadowDrawingSettings(cullResults, 0);
            shadowDrawSettings.useRenderingLayerMaskTest = frameSettings.IsEnabled(FrameSettingsField.LightLayers);

            var parameters = PrepareRenderShadowsParameters();
            RenderShadows(parameters, m_Atlas, shadowDrawSettings, renderContext, cmd);

            if (parameters.blurAlgorithm == BlurAlgorithm.IM)
            {
                IMBlurMoment(parameters, m_Atlas, m_AtlasMoments[0], m_IntermediateSummedAreaTexture, m_SummedAreaTexture, cmd);
            }
            else if (parameters.blurAlgorithm == BlurAlgorithm.EVSM)
            {
                EVSMBlurMoments(parameters, m_Atlas, m_AtlasMoments, cmd);
            }
        }

        struct RenderShadowsParameters
        {
            public List<HDShadowRequest>    shadowRequests;
            public Material                 clearMaterial;
            public bool                     debugClearAtlas;
            public int                      atlasShaderID;
            public int                      atlasSizeShaderID;
            public BlurAlgorithm            blurAlgorithm;

            // EVSM
            public ComputeShader            evsmShadowBlurMomentsCS;
            public int                      momentAtlasShaderID;

            // IM
            public ComputeShader            imShadowBlurMomentsCS;
        }

        RenderShadowsParameters PrepareRenderShadowsParameters()
        {
            var parameters = new RenderShadowsParameters();
            parameters.shadowRequests = m_ShadowRequests;
            parameters.clearMaterial = m_ClearMaterial;
            parameters.debugClearAtlas = m_LightingDebugSettings.clearShadowAtlas;
            parameters.atlasShaderID = m_AtlasShaderID;
            parameters.atlasSizeShaderID = m_AtlasSizeShaderID;
            parameters.blurAlgorithm = m_BlurAlgorithm;

            // EVSM
            parameters.evsmShadowBlurMomentsCS = m_RenderPipelineResources.shaders.evsmBlurCS;
            parameters.momentAtlasShaderID = m_MomentAtlasShaderID;

            // IM
            parameters.imShadowBlurMomentsCS = m_RenderPipelineResources.shaders.momentShadowsCS;

            return parameters;
        }

        static void RenderShadows(  RenderShadowsParameters parameters,
                                    RTHandle                atlasRenderTexture,
                                    ShadowDrawingSettings   shadowDrawSettings,
                                    ScriptableRenderContext renderContext,
                                    CommandBuffer           cmd)
        {
            cmd.SetRenderTarget(atlasRenderTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.SetGlobalVector(parameters.atlasSizeShaderID, new Vector4(atlasRenderTexture.rt.width, atlasRenderTexture.rt.height, 1.0f / atlasRenderTexture.rt.width, 1.0f / atlasRenderTexture.rt.height));

            // Clear the whole atlas to avoid garbage outside of current request when viewing it.
            if (parameters.debugClearAtlas)
                CoreUtils.DrawFullScreen(cmd, parameters.clearMaterial, null, 0);

            foreach (var shadowRequest in parameters.shadowRequests)
            {
                if (shadowRequest.shouldUseCachedShadow)
                    continue;

                cmd.SetGlobalDepthBias(1.0f, shadowRequest.slopeBias);
                cmd.SetViewport(shadowRequest.atlasViewport);

                cmd.SetGlobalFloat(HDShaderIDs._ZClip, shadowRequest.zClip ? 1.0f : 0.0f);
                CoreUtils.DrawFullScreen(cmd, parameters.clearMaterial, null, 0);

                shadowDrawSettings.lightIndex = shadowRequest.lightIndex;
                shadowDrawSettings.splitData = shadowRequest.splitData;

                // Setup matrices for shadow rendering:
                Matrix4x4 viewProjection = shadowRequest.deviceProjectionYFlip * shadowRequest.view;
                cmd.SetGlobalMatrix(HDShaderIDs._ViewMatrix, shadowRequest.view);
                cmd.SetGlobalMatrix(HDShaderIDs._InvViewMatrix, shadowRequest.view.inverse);
                cmd.SetGlobalMatrix(HDShaderIDs._ProjMatrix, shadowRequest.deviceProjectionYFlip);
                cmd.SetGlobalMatrix(HDShaderIDs._InvProjMatrix, shadowRequest.deviceProjectionYFlip.inverse);
                cmd.SetGlobalMatrix(HDShaderIDs._ViewProjMatrix, viewProjection);
                cmd.SetGlobalMatrix(HDShaderIDs._InvViewProjMatrix, viewProjection.inverse);
                cmd.SetGlobalVectorArray(HDShaderIDs._ShadowFrustumPlanes, shadowRequest.frustumPlanes);

                // TODO: remove this execute when DrawShadows will use a CommandBuffer
                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                renderContext.DrawShadows(ref shadowDrawSettings);
            }
            cmd.SetGlobalFloat(HDShaderIDs._ZClip, 1.0f);   // Re-enable zclip globally
            cmd.SetGlobalDepthBias(0.0f, 0.0f);             // Reset depth bias.

        }

        public bool HasBlurredEVSM()
        {
            return (m_BlurAlgorithm == BlurAlgorithm.EVSM) && m_AtlasMoments[0] != null;
        }

        // This is a 9 tap filter, a gaussian with std. dev of 3. This standard deviation with this amount of taps probably cuts
        // the tail of the gaussian a bit too much, and it is a very fat curve, but it seems to work fine for our use case.
        static readonly Vector4[] evsmBlurWeights = {
            new Vector4(0.1531703f, 0.1448929f, 0.1226492f, 0.0929025f),
            new Vector4(0.06297021f, 0.0f, 0.0f, 0.0f),
        };

        unsafe static void EVSMBlurMoments( RenderShadowsParameters parameters,
                                            RTHandle atlasRenderTexture,
                                            RTHandle[] momentAtlasRenderTextures,
                                            CommandBuffer cmd)
        {
            ComputeShader shadowBlurMomentsCS = parameters.evsmShadowBlurMomentsCS;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderEVSMShadowMaps)))
            {
                int generateAndBlurMomentsKernel = shadowBlurMomentsCS.FindKernel("ConvertAndBlur");
                int blurMomentsKernel = shadowBlurMomentsCS.FindKernel("Blur");
                int copyMomentsKernel = shadowBlurMomentsCS.FindKernel("CopyMoments");

                cmd.SetComputeTextureParam(shadowBlurMomentsCS, generateAndBlurMomentsKernel, HDShaderIDs._DepthTexture, atlasRenderTexture);
                cmd.SetComputeVectorArrayParam(shadowBlurMomentsCS, HDShaderIDs._BlurWeightsStorage, evsmBlurWeights);

                // We need to store in which of the two moment texture a request will have its last version stored in for a final patch up at the end.
                var finalAtlasTexture = stackalloc int[parameters.shadowRequests.Count];

                int requestIdx = 0;
                foreach (var shadowRequest in parameters.shadowRequests)
                {
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderEVSMShadowMapsBlur)))
                    {
                        int downsampledWidth = Mathf.CeilToInt(shadowRequest.atlasViewport.width * 0.5f);
                        int downsampledHeight = Mathf.CeilToInt(shadowRequest.atlasViewport.height * 0.5f);

                        Vector2 DstRectOffset = new Vector2(shadowRequest.atlasViewport.min.x * 0.5f, shadowRequest.atlasViewport.min.y * 0.5f);

                        cmd.SetComputeTextureParam(shadowBlurMomentsCS, generateAndBlurMomentsKernel, HDShaderIDs._OutputTexture, momentAtlasRenderTextures[0]);
                        cmd.SetComputeVectorParam(shadowBlurMomentsCS, HDShaderIDs._SrcRect, new Vector4(shadowRequest.atlasViewport.min.x, shadowRequest.atlasViewport.min.y, shadowRequest.atlasViewport.width, shadowRequest.atlasViewport.height));
                        cmd.SetComputeVectorParam(shadowBlurMomentsCS, HDShaderIDs._DstRect, new Vector4(DstRectOffset.x, DstRectOffset.y, 1.0f / atlasRenderTexture.rt.width, 1.0f / atlasRenderTexture.rt.height));
                        cmd.SetComputeFloatParam(shadowBlurMomentsCS, HDShaderIDs._EVSMExponent, shadowRequest.evsmParams.x);

                        int dispatchSizeX = ((int)downsampledWidth + 7) / 8;
                        int dispatchSizeY = ((int)downsampledHeight + 7) / 8;

                        cmd.DispatchCompute(shadowBlurMomentsCS, generateAndBlurMomentsKernel, dispatchSizeX, dispatchSizeY, 1);

                        int currentAtlasMomentSurface = 0;

                        RTHandle GetMomentRT() { return momentAtlasRenderTextures[currentAtlasMomentSurface]; }
                        RTHandle GetMomentRTCopy() { return momentAtlasRenderTextures[(currentAtlasMomentSurface + 1) & 1]; }

                        cmd.SetComputeVectorParam(shadowBlurMomentsCS, HDShaderIDs._SrcRect, new Vector4(DstRectOffset.x, DstRectOffset.y, downsampledWidth, downsampledHeight));
                        for (int i = 0; i < shadowRequest.evsmParams.w; ++i)
                        {
                            currentAtlasMomentSurface = (currentAtlasMomentSurface + 1) & 1;
                            cmd.SetComputeTextureParam(shadowBlurMomentsCS, blurMomentsKernel, HDShaderIDs._InputTexture, GetMomentRTCopy());
                            cmd.SetComputeTextureParam(shadowBlurMomentsCS, blurMomentsKernel, HDShaderIDs._OutputTexture, GetMomentRT());

                            cmd.DispatchCompute(shadowBlurMomentsCS, blurMomentsKernel, dispatchSizeX, dispatchSizeY, 1);
                        }

                        finalAtlasTexture[requestIdx++] = currentAtlasMomentSurface;
                    }
                }

                // We patch up the atlas with the requests that, due to different count of blur passes, remained in the copy
                for (int i = 0; i < parameters.shadowRequests.Count; ++i)
                {
                    if (finalAtlasTexture[i] != 0)
                    {
                        using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderEVSMShadowMapsCopyToAtlas)))
                        {
                            var shadowRequest = parameters.shadowRequests[i];
                            int downsampledWidth = Mathf.CeilToInt(shadowRequest.atlasViewport.width * 0.5f);
                            int downsampledHeight = Mathf.CeilToInt(shadowRequest.atlasViewport.height * 0.5f);

                            cmd.SetComputeVectorParam(shadowBlurMomentsCS, HDShaderIDs._SrcRect, new Vector4(shadowRequest.atlasViewport.min.x * 0.5f, shadowRequest.atlasViewport.min.y * 0.5f, downsampledWidth, downsampledHeight));
                            cmd.SetComputeTextureParam(shadowBlurMomentsCS, copyMomentsKernel, HDShaderIDs._InputTexture, momentAtlasRenderTextures[1]);
                            cmd.SetComputeTextureParam(shadowBlurMomentsCS, copyMomentsKernel, HDShaderIDs._OutputTexture, momentAtlasRenderTextures[0]);

                            int dispatchSizeX = ((int)downsampledWidth + 7) / 8;
                            int dispatchSizeY = ((int)downsampledHeight + 7) / 8;

                            cmd.DispatchCompute(shadowBlurMomentsCS, copyMomentsKernel, dispatchSizeX, dispatchSizeY, 1);
                        }
                    }
                }
            }
        }

        static void IMBlurMoment(   RenderShadowsParameters parameters,
                                    RTHandle atlas,
                                    RTHandle atlasMoment,
                                    RTHandle intermediateSummedAreaTexture,
                                    RTHandle summedAreaTexture,
                                    CommandBuffer cmd)
        {
            // If the target kernel is not available
            ComputeShader momentCS = parameters.imShadowBlurMomentsCS;
            if (momentCS == null) return;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderMomentShadowMaps)))
            {
                int computeMomentKernel = momentCS.FindKernel("ComputeMomentShadows");
                int summedAreaHorizontalKernel = momentCS.FindKernel("MomentSummedAreaTableHorizontal");
                int summedAreaVerticalKernel = momentCS.FindKernel("MomentSummedAreaTableVertical");

                // First of all let's clear the moment shadow map
                CoreUtils.SetRenderTarget(cmd, atlasMoment, ClearFlag.Color, Color.black);
                CoreUtils.SetRenderTarget(cmd, intermediateSummedAreaTexture, ClearFlag.Color, Color.black);
                CoreUtils.SetRenderTarget(cmd, summedAreaTexture, ClearFlag.Color, Color.black);


                // Alright, so the thing here is that for every sub-shadow map of the atlas, we need to generate the moment shadow map
                foreach (var shadowRequest in parameters.shadowRequests)
                {
                    // Let's bind the resources of this
                    cmd.SetComputeTextureParam(momentCS, computeMomentKernel, HDShaderIDs._ShadowmapAtlas, atlas);
                    cmd.SetComputeTextureParam(momentCS, computeMomentKernel, HDShaderIDs._MomentShadowAtlas, atlasMoment);
                    cmd.SetComputeVectorParam(momentCS, HDShaderIDs._MomentShadowmapSlotST, new Vector4(shadowRequest.atlasViewport.width, shadowRequest.atlasViewport.height, shadowRequest.atlasViewport.min.x, shadowRequest.atlasViewport.min.y));

                    // First of all we need to compute the moments
                    int numTilesX = Math.Max((int)shadowRequest.atlasViewport.width / 8, 1);
                    int numTilesY = Math.Max((int)shadowRequest.atlasViewport.height / 8, 1);
                    cmd.DispatchCompute(momentCS, computeMomentKernel, numTilesX, numTilesY, 1);

                    // Do the horizontal pass of the summed area table
                    cmd.SetComputeTextureParam(momentCS, summedAreaHorizontalKernel, HDShaderIDs._SummedAreaTableInputFloat, atlasMoment);
                    cmd.SetComputeTextureParam(momentCS, summedAreaHorizontalKernel, HDShaderIDs._SummedAreaTableOutputInt, intermediateSummedAreaTexture);
                    cmd.SetComputeFloatParam(momentCS, HDShaderIDs._IMSKernelSize, shadowRequest.kernelSize);
                    cmd.SetComputeVectorParam(momentCS, HDShaderIDs._MomentShadowmapSize, new Vector2((float)atlasMoment.referenceSize.x, (float)atlasMoment.referenceSize.y));

                    int numLines = Math.Max((int)shadowRequest.atlasViewport.width / 64, 1);
                    cmd.DispatchCompute(momentCS, summedAreaHorizontalKernel, numLines, 1, 1);

                    // Do the horizontal pass of the summed area table
                    cmd.SetComputeTextureParam(momentCS, summedAreaVerticalKernel, HDShaderIDs._SummedAreaTableInputInt, intermediateSummedAreaTexture);
                    cmd.SetComputeTextureParam(momentCS, summedAreaVerticalKernel, HDShaderIDs._SummedAreaTableOutputInt, summedAreaTexture);
                    cmd.SetComputeVectorParam(momentCS, HDShaderIDs._MomentShadowmapSize, new Vector2((float)atlasMoment.referenceSize.x, (float)atlasMoment.referenceSize.y));
                    cmd.SetComputeFloatParam(momentCS, HDShaderIDs._IMSKernelSize, shadowRequest.kernelSize);

                    int numColumns = Math.Max((int)shadowRequest.atlasViewport.height / 64, 1);
                    cmd.DispatchCompute(momentCS, summedAreaVerticalKernel, numColumns, 1, 1);

                    // Push the global texture
                    cmd.SetGlobalTexture(HDShaderIDs._SummedAreaTableInputInt, summedAreaTexture);
                }
            }
        }

        public void DisplayAtlas(RTHandle atlasTexture, CommandBuffer cmd, Material debugMaterial, Rect atlasViewport, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, MaterialPropertyBlock mpb)
        {
            if (atlasTexture == null)
                return;

            Vector4 validRange = new Vector4(minValue, 1.0f / (maxValue - minValue));
            float rWidth = 1.0f / width;
            float rHeight = 1.0f / height;
            Vector4 scaleBias = Vector4.Scale(new Vector4(rWidth, rHeight, rWidth, rHeight), new Vector4(atlasViewport.width, atlasViewport.height, atlasViewport.x, atlasViewport.y));

            mpb.SetTexture("_AtlasTexture", atlasTexture);
            mpb.SetVector("_TextureScaleBias", scaleBias);
            mpb.SetVector("_ValidRange", validRange);
            mpb.SetFloat("_RcpGlobalScaleFactor", m_RcpScaleFactor);
            cmd.SetViewport(new Rect(screenX, screenY, screenSizeX, screenSizeY));
            cmd.DrawProcedural(Matrix4x4.identity, debugMaterial, debugMaterial.FindPass("RegularShadow"), MeshTopology.Triangles, 3, 1, mpb);
        }

        public void Clear()
        {
            m_ShadowResolutionRequests.Clear();
            m_ShadowRequests.Clear();
        }

        public void Release()
        {
            if (m_Atlas != null)
                RTHandles.Release(m_Atlas);

            if(m_AtlasMoments != null && m_AtlasMoments.Length > 0)
            {
                for (int i = 0; i < m_AtlasMoments.Length; ++i)
                {
                    if (m_AtlasMoments[i] != null)
                    {
                        RTHandles.Release(m_AtlasMoments[i]);
                        m_AtlasMoments[i] = null;
                    }
                }
            }

            if (m_IntermediateSummedAreaTexture != null)
            {
                RTHandles.Release(m_IntermediateSummedAreaTexture);
                m_IntermediateSummedAreaTexture = null;

            }

            if (m_SummedAreaTexture != null)
            {
                RTHandles.Release(m_SummedAreaTexture);
                m_SummedAreaTexture = null;
            }
        }
    }
}

