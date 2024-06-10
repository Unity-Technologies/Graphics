using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class HDDynamicShadowAtlas : HDShadowAtlas
    {
        readonly List<HDShadowResolutionRequest> m_ShadowResolutionRequests = new List<HDShadowResolutionRequest>();
        readonly List<HDShadowRequest> m_MixedRequestsPendingBlits = new List<HDShadowRequest>();

        float m_RcpScaleFactor = 1;
        HDShadowResolutionRequest[] m_SortedRequestsCache;

        public HDDynamicShadowAtlas(HDShadowAtlasInitParameters atlaInitParams)
            : base(atlaInitParams)
        {
            m_SortedRequestsCache = new HDShadowResolutionRequest[Mathf.CeilToInt(atlaInitParams.maxShadowRequests)];
        }

        internal void ReserveResolution(HDShadowResolutionRequest shadowRequest)
        {
            m_ShadowResolutionRequests.Add(shadowRequest);
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

        private bool AtlasLayout(bool allowResize, HDShadowResolutionRequest[] fullShadowList, int requestsCount)
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
                    if (allowResize)
                    {
                        LayoutResize();
                        return true;
                    }

                    return false;
                }
                viewport.x = curX;
                viewport.y = curY;
                shadowRequest.dynamicAtlasViewport = viewport;
                shadowRequest.resolution = viewport.size;
                curX += viewport.width;
            }

            return true;
        }

        internal bool Layout(bool allowResize = true)
        {
            int n = (m_ShadowResolutionRequests != null) ? m_ShadowResolutionRequests.Count : 0;
            int i = 0;
            for (; i < m_ShadowResolutionRequests.Count; ++i)
            {
                m_SortedRequestsCache[i] = m_ShadowResolutionRequests[i];
            }

            InsertionSort(m_SortedRequestsCache, 0, i);

            return AtlasLayout(allowResize, m_SortedRequestsCache, requestsCount: i);
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
                    m_ShadowResolutionRequests[index].dynamicAtlasViewport = r;
                    index++;
                }
                while (y < currentMaxY && index < m_ShadowResolutionRequests.Count);
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
                    m_ShadowResolutionRequests[index].dynamicAtlasViewport = r;
                    index++;
                }
                while (x < currentMaxX && index < m_ShadowResolutionRequests.Count);
                currentMaxX = Mathf.Max(currentMaxX, currentX);
                currentMaxY = currentMaxYCache;
            }

            float maxResolution = Math.Max(currentMaxX, currentMaxY);
            Vector4 scale = new Vector4(width / maxResolution, height / maxResolution, width / maxResolution, height / maxResolution);
            m_RcpScaleFactor = Mathf.Min(scale.x, scale.y);

            // Scale down every shadow rects to fit with the current atlas size
            foreach (var r in m_ShadowResolutionRequests)
            {
                Vector4 s = new Vector4(r.dynamicAtlasViewport.x, r.dynamicAtlasViewport.y, r.dynamicAtlasViewport.width, r.dynamicAtlasViewport.height);
                Vector4 reScaled = Vector4.Scale(s, scale);

                r.dynamicAtlasViewport = new Rect(reScaled.x, reScaled.y, reScaled.z, reScaled.w);
                r.resolution = r.dynamicAtlasViewport.size;
            }
        }

        public void DisplayAtlas(RTHandle atlasTexture, CommandBuffer cmd, Material debugMaterial, Rect atlasViewport, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, MaterialPropertyBlock mpb)
        {
            base.DisplayAtlas(atlasTexture, cmd, debugMaterial, atlasViewport, screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, mpb, m_RcpScaleFactor);
        }

        public void AddRequestToPendingBlitFromCache(HDShadowRequest request)
        {
            if (request.isMixedCached)
                m_MixedRequestsPendingBlits.Add(request);
        }

        class BlitCachedShadowPassData
        {
            public List<HDShadowRequest> requestsWaitingBlits;
            public Material blitMaterial;
            public Vector2Int cachedShadowAtlasSize;

            public TextureHandle sourceCachedAtlas;
            public TextureHandle atlasTexture;
        }

        public void BlitCachedIntoAtlas(RenderGraph renderGraph, TextureHandle cachedAtlasTexture, Vector2Int cachedAtlasSize, Material blitMaterial, string passName, HDProfileId profileID)
        {
            if (m_MixedRequestsPendingBlits.Count > 0)
            {
                using (var builder = renderGraph.AddRenderPass<BlitCachedShadowPassData>(passName, out var passData, ProfilingSampler.Get(profileID)))
                {
                    passData.requestsWaitingBlits = m_MixedRequestsPendingBlits;
                    passData.blitMaterial = blitMaterial;
                    passData.cachedShadowAtlasSize = cachedAtlasSize;
                    passData.sourceCachedAtlas = builder.ReadTexture(cachedAtlasTexture);
                    passData.atlasTexture = builder.WriteTexture(GetShadowMapDepthTexture(renderGraph));

                    builder.SetRenderFunc(
                        (BlitCachedShadowPassData data, RenderGraphContext ctx) =>
                        {
                            foreach (var request in data.requestsWaitingBlits)
                            {
                                var mpb = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                                ctx.cmd.SetRenderTarget(data.atlasTexture);
                                ctx.cmd.SetViewport(request.dynamicAtlasViewport);

                                Vector4 sourceScaleBias = new Vector4(request.cachedAtlasViewport.width / data.cachedShadowAtlasSize.x,
                                    request.cachedAtlasViewport.height / data.cachedShadowAtlasSize.y,
                                    request.cachedAtlasViewport.x / data.cachedShadowAtlasSize.x,
                                    request.cachedAtlasViewport.y / data.cachedShadowAtlasSize.y);

                                mpb.SetTexture(HDShaderIDs._CachedShadowmapAtlas, data.sourceCachedAtlas);
                                mpb.SetVector(HDShaderIDs._BlitScaleBias, sourceScaleBias);
                                CoreUtils.DrawFullScreen(ctx.cmd, data.blitMaterial, mpb, 0);
                            }

                            data.requestsWaitingBlits.Clear();
                        });
                }
            }
        }

        public override void Clear()
        {
            base.Clear();
            m_ShadowResolutionRequests.Clear();
            m_MixedRequestsPendingBlits.Clear();
        }
    }
}
