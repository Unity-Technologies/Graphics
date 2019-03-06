using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class HDShadowAtlas
    {
        public RenderTargetIdentifier               identifier { get; private set; }
        readonly List<HDShadowResolutionRequest>    m_ShadowResolutionRequests = new List<HDShadowResolutionRequest>();
        readonly List<HDShadowRequest>              m_ShadowRequests = new List<HDShadowRequest>();

        public int                  width { get; private set; }
        public int                  height  { get; private set; }

        RTHandleSystem.RTHandle     m_Atlas;
        Material                    m_ClearMaterial;
        LightingDebugSettings       m_LightingDebugSettings;
        float                       m_RcpScaleFactor = 1;
        FilterMode                  m_FilterMode;
        DepthBits                   m_DepthBufferBits;
        RenderTextureFormat         m_Format;
        string                      m_Name;
        int                         m_AtlasSizeShaderID;
        RenderPipelineResources     m_RenderPipelineResources;

        // Moment shadow data
        bool m_SupportMomentShadows;
        bool m_isBlurredEVSM;
        int m_CurrentAtlasMomentSurface = 0;    // We ping pong for blurs.
        RTHandleSystem.RTHandle[] m_AtlasMoments = null;
        RTHandleSystem.RTHandle m_IntermediateSummedAreaTexture;
        RTHandleSystem.RTHandle m_SummedAreaTexture;

        public HDShadowAtlas(RenderPipelineResources renderPipelineResources, int width, int height, int atlasSizeShaderID, Material clearMaterial, bool supportMomentShadows, bool BlurredEVSM = false, FilterMode filterMode = FilterMode.Bilinear, DepthBits depthBufferBits = DepthBits.Depth16, RenderTextureFormat format = RenderTextureFormat.Shadowmap, string name = "")
        {
            this.width = width;
            this.height = height;
            m_FilterMode = filterMode;
            m_DepthBufferBits = depthBufferBits;
            m_Format = format;
            m_Name = name;
            m_AtlasSizeShaderID = atlasSizeShaderID;
            m_ClearMaterial = clearMaterial;
            m_SupportMomentShadows = supportMomentShadows;
            m_RenderPipelineResources = renderPipelineResources;
            m_isBlurredEVSM = BlurredEVSM;

            AllocateRenderTexture();
        }

        void AllocateRenderTexture()
        {
            if (m_Atlas != null)
                m_Atlas.Release();

            m_Atlas = RTHandles.Alloc(width, height, filterMode: m_FilterMode, depthBufferBits: m_DepthBufferBits, isShadowMap: true, name: m_Name);

            if (m_SupportMomentShadows)
            {
                string momentShadowMapName = m_Name + "Moment";
                m_AtlasMoments = new RTHandleSystem.RTHandle[1];
                m_AtlasMoments[0] = RTHandles.Alloc(width, height, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, enableRandomWrite: true, name: momentShadowMapName);
                string intermediateSummedAreaName = m_Name + "IntermediateSummedArea";
                m_IntermediateSummedAreaTexture = RTHandles.Alloc(width, height, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32G32B32A32_SInt, enableRandomWrite: true, name: intermediateSummedAreaName);
                string summedAreaName = m_Name + "SummedAreaFinal";
                m_SummedAreaTexture = RTHandles.Alloc(width, height, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32G32B32A32_SInt, enableRandomWrite: true, name: summedAreaName);
            }
            else if (m_isBlurredEVSM)
            {
                string[] momentShadowMapNames = { m_Name + "Moment", m_Name + "MomentCopy" };
                m_AtlasMoments = new RTHandleSystem.RTHandle[2];
                for (int i = 0; i < 2; ++i)
                {
                    m_AtlasMoments[i] = RTHandles.Alloc(width / 2, height / 2, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32G32_SFloat, useMipMap: true, autoGenerateMips: false, enableRandomWrite: true, name: momentShadowMapNames[i]);
                }
            }

            identifier = new RenderTargetIdentifier(m_Atlas);
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

        public void ReserveResolution(HDShadowResolutionRequest shadowRequest)
        {
            m_ShadowResolutionRequests.Add(shadowRequest);
        }

        public void AddShadowRequest(HDShadowRequest shadowRequest)
        {
            m_ShadowRequests.Add(shadowRequest);
        }

        public void UpdateDebugSettings(LightingDebugSettings lightingDebugSettings)
        {
            m_LightingDebugSettings = lightingDebugSettings;
        }

        // Stable (unlike List.Sort) sorting algorithm which, unlike Linq's, doesn't use JIT (lol).
        // Sorts in place. Very efficient (O(n)) for already sorted data.
        void InsertionSort(List<HDShadowResolutionRequest> array)
        {
            int i = 1;
            int n = array.Count;

            while (i < n)
            {
                var curr = array[i];

                int j = i - 1;

                // Sort in descending order.
                while ((j >= 0) && ((curr.atlasViewport.height > array[j].atlasViewport.height) ||
                                    (curr.atlasViewport.width  > array[j].atlasViewport.width)))
                {
                    array[j + 1] = array[j];
                    j--;
                }

                array[j + 1] = curr;
                i++;
            }
        }

        public bool Layout(bool allowResize = true)
        {
            // Perform a deep copy.
            int n = (m_ShadowResolutionRequests != null) ? m_ShadowResolutionRequests.Count : 0;
            var sortedRequests = new List<HDShadowResolutionRequest>(n);

            for (int i = 0; i < n; i++)
            {
                sortedRequests.Add(m_ShadowResolutionRequests[i]);
            }

            // Note: it is very important to keep the added order for shadow maps that are the same size (for punctual lights)
            // and because of that we can't use List.Sort because it messes up the list even with a good custom comparator
            // Sort in place.
            InsertionSort(sortedRequests);

            float curX = 0, curY = 0, curH = 0, xMax = width, yMax = height;
            m_RcpScaleFactor = 1;

            // Assign to every view shadow viewport request a position in the atlas
            foreach (var shadowRequest in sortedRequests)
            {
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
                    else
                        return false;
                }
                viewport.x = curX;
                viewport.y = curY;
                shadowRequest.atlasViewport = viewport;
                shadowRequest.resolution = viewport.size;
                curX += viewport.width;
            }

            return true;
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
        }

        public void RenderShadows(ScriptableRenderContext renderContext, CommandBuffer cmd, ShadowDrawingSettings dss)
        {
            if (m_ShadowRequests.Count == 0)
                return;

            cmd.SetRenderTarget(identifier);
            cmd.SetGlobalVector(m_AtlasSizeShaderID, new Vector4(width, height, 1.0f / width, 1.0f / height));

            if (m_LightingDebugSettings.clearShadowAtlas)
                CoreUtils.DrawFullScreen(cmd, m_ClearMaterial, null, 0);

            foreach (var shadowRequest in m_ShadowRequests)
            {
                cmd.SetViewport(shadowRequest.atlasViewport);

                cmd.SetViewProjectionMatrices(shadowRequest.view, shadowRequest.projection);

                cmd.SetGlobalFloat(HDShaderIDs._ZClip, shadowRequest.zClip ? 1.0f : 0.0f);
                if (!m_LightingDebugSettings.clearShadowAtlas)
                {
                    CoreUtils.DrawFullScreen(cmd, m_ClearMaterial, null, 0);
                }

                dss.lightIndex = shadowRequest.lightIndex;
                dss.splitData = shadowRequest.splitData;

                // TODO: remove this execute when DrawShadows will use a CommandBuffer
                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                renderContext.DrawShadows(ref dss);
            }

            cmd.SetGlobalFloat(HDShaderIDs._ZClip, 1.0f);   // Re-enable zclip globally
        }

        public bool HasBlurredEVSM()
        {
            return m_isBlurredEVSM && GetMomentRT() != null;
        }

        public RTHandleSystem.RTHandle GetMomentRT()
        {
            return m_AtlasMoments[m_CurrentAtlasMomentSurface];
        }

        private RTHandleSystem.RTHandle GetMomentRTCopy()
        {
            return m_AtlasMoments[(m_CurrentAtlasMomentSurface + 1) & 1];
        }

        public void AreaShadowBlurMoments(CommandBuffer cmd, HDCamera hdCamera)
        {
            ComputeShader shadowBlurMomentsCS = m_RenderPipelineResources.shaders.evsmBlurCS;

            if (!m_isBlurredEVSM || shadowBlurMomentsCS == null) return;


            using (new ProfilingSample(cmd, "Render & Blur Moment Shadows", CustomSamplerId.RenderShadows.GetSampler()))
            {
                int generateAndBlurMomentsKernel = shadowBlurMomentsCS.FindKernel("ConvertAndBlur");
                int blurMomentsKernel = shadowBlurMomentsCS.FindKernel("Blur");
                int copyMomentsKernel = shadowBlurMomentsCS.FindKernel("CopyMoments");

                Vector4[] blurWeights = new Vector4[2];

                // This is a 9 tap filter, a gaussian with std. dev of 3. This standard deviation with this amount of taps probably cuts 
                // the tail of the gaussian a bit too much, and it is a very fat curve, but it seems to work fine for our use case.
                blurWeights[0].x = 0.1531703f;
                blurWeights[0].y = 0.1448929f;
                blurWeights[0].z = 0.1226492f;
                blurWeights[0].w = 0.0929025f;
                blurWeights[1].x = 0.06297021f;

                cmd.SetComputeTextureParam(shadowBlurMomentsCS, generateAndBlurMomentsKernel, HDShaderIDs._DepthTexture, m_Atlas);
                cmd.SetComputeVectorArrayParam(shadowBlurMomentsCS, HDShaderIDs._BlurWeightsStorage, blurWeights);

                // We need to store in which of the two moment texture a request will have its last version stored in for a final patch up at the end.
                int[] finalAtlasTexture = new int[m_ShadowRequests.Count];

                int requestIdx = 0;
                foreach (var shadowRequest in m_ShadowRequests)
                {
                    using (new ProfilingSample(cmd, "EVSM conversion and blur", CustomSamplerId.RenderShadows.GetSampler()))
                    {
                        int downsampledWidth = Mathf.CeilToInt(shadowRequest.atlasViewport.width * 0.5f);
                        int downsampledHeight = Mathf.CeilToInt(shadowRequest.atlasViewport.height * 0.5f);

                        Vector2 DstRectOffset = new Vector2(shadowRequest.atlasViewport.min.x * 0.5f, shadowRequest.atlasViewport.min.y * 0.5f);

                        cmd.SetComputeTextureParam(shadowBlurMomentsCS, generateAndBlurMomentsKernel, HDShaderIDs._OutputTexture, GetMomentRT());
                        cmd.SetComputeVectorParam(shadowBlurMomentsCS, HDShaderIDs._SrcRect, new Vector4(shadowRequest.atlasViewport.min.x, shadowRequest.atlasViewport.min.y, shadowRequest.atlasViewport.width, shadowRequest.atlasViewport.height));
                        cmd.SetComputeVectorParam(shadowBlurMomentsCS, HDShaderIDs._DstRect, new Vector4(DstRectOffset.x, DstRectOffset.y, 1.0f / width, 1.0f / height));
                        cmd.SetComputeFloatParam(shadowBlurMomentsCS, HDShaderIDs._EVSMExponent, shadowRequest.evsmParams.x);

                        int dispatchSizeX = ((int)downsampledWidth + 7) / 8;
                        int dispatchSizeY = ((int)downsampledHeight + 7) / 8;

                        cmd.DispatchCompute(shadowBlurMomentsCS, generateAndBlurMomentsKernel, dispatchSizeX, dispatchSizeY, 1);


                        cmd.SetComputeVectorParam(shadowBlurMomentsCS, HDShaderIDs._SrcRect, new Vector4(DstRectOffset.x, DstRectOffset.y, downsampledWidth, downsampledHeight));
                        for (int i = 0; i < shadowRequest.evsmParams.w; ++i)
                        {
                            m_CurrentAtlasMomentSurface = (m_CurrentAtlasMomentSurface + 1) & 1;
                            cmd.SetComputeTextureParam(shadowBlurMomentsCS, blurMomentsKernel, HDShaderIDs._InputTexture, GetMomentRTCopy());
                            cmd.SetComputeTextureParam(shadowBlurMomentsCS, blurMomentsKernel, HDShaderIDs._OutputTexture, GetMomentRT());

                            cmd.DispatchCompute(shadowBlurMomentsCS, blurMomentsKernel, dispatchSizeX, dispatchSizeY, 1);
                        }

                        finalAtlasTexture[requestIdx++] = m_CurrentAtlasMomentSurface;

                    }
                }

                // We patch up the atlas with the requests that, due to different count of blur passes, remained in the copy
                for (int i = 0; i < m_ShadowRequests.Count; ++i)
                {
                    if (m_CurrentAtlasMomentSurface != finalAtlasTexture[i])
                    {
                        using (new ProfilingSample(cmd, "Copy into main atlas.", CustomSamplerId.RenderShadows.GetSampler()))
                        {
                            var shadowRequest = m_ShadowRequests[i];
                            int downsampledWidth = Mathf.CeilToInt(shadowRequest.atlasViewport.width * 0.5f);
                            int downsampledHeight = Mathf.CeilToInt(shadowRequest.atlasViewport.height * 0.5f);

                            cmd.SetComputeVectorParam(shadowBlurMomentsCS, HDShaderIDs._SrcRect, new Vector4(shadowRequest.atlasViewport.min.x * 0.5f, shadowRequest.atlasViewport.min.y * 0.5f, downsampledWidth, downsampledHeight));
                            cmd.SetComputeTextureParam(shadowBlurMomentsCS, copyMomentsKernel, HDShaderIDs._InputTexture, GetMomentRTCopy());
                            cmd.SetComputeTextureParam(shadowBlurMomentsCS, copyMomentsKernel, HDShaderIDs._OutputTexture, GetMomentRT());

                            int dispatchSizeX = ((int)downsampledWidth + 7) / 8;
                            int dispatchSizeY = ((int)downsampledHeight + 7) / 8;

                            cmd.DispatchCompute(shadowBlurMomentsCS, copyMomentsKernel, dispatchSizeX, dispatchSizeY, 1);
                        }
                    }
                }
            }
        }

        public void ComputeMomentShadows(CommandBuffer cmd, HDCamera hdCamera )
        {
            // If the target kernel is not available
            ComputeShader momentCS = m_RenderPipelineResources.shaders.momentShadowsCS;
            if (momentCS == null) return;

            using (new ProfilingSample(cmd, "Render Moment Shadows", CustomSamplerId.RenderShadows.GetSampler()))
            {
                int computeMomentKernel = momentCS.FindKernel("ComputeMomentShadows");
                int summedAreaHorizontalKernel = momentCS.FindKernel("MomentSummedAreaTableHorizontal");
                int summedAreaVerticalKernel = momentCS.FindKernel("MomentSummedAreaTableVertical");

                // First of all let's clear the moment shadow map
                HDUtils.SetRenderTarget(cmd, hdCamera, GetMomentRT(), ClearFlag.Color, Color.black);
                HDUtils.SetRenderTarget(cmd, hdCamera, m_IntermediateSummedAreaTexture, ClearFlag.Color, Color.black);
                HDUtils.SetRenderTarget(cmd, hdCamera, m_SummedAreaTexture, ClearFlag.Color, Color.black);

                var atlasMoment = GetMomentRT();

                // Alright, so the thing here is that for every sub-shadow map of the atlas, we need to generate the moment shadow map
                foreach (var shadowRequest in m_ShadowRequests)
                {
                    // Let's bind the resources of this
                    cmd.SetComputeTextureParam(momentCS, computeMomentKernel, HDShaderIDs._ShadowmapAtlas, m_Atlas);
                    cmd.SetComputeTextureParam(momentCS, computeMomentKernel, HDShaderIDs._MomentShadowAtlas, atlasMoment);
                    cmd.SetComputeVectorParam(momentCS, HDShaderIDs._MomentShadowmapSlotST, new Vector4(shadowRequest.atlasViewport.width, shadowRequest.atlasViewport.height, shadowRequest.atlasViewport.min.x, shadowRequest.atlasViewport.min.y));

                    // First of all we need to compute the moments
                    int numTilesX = Math.Max((int)shadowRequest.atlasViewport.width / 8, 1);
                    int numTilesY = Math.Max((int)shadowRequest.atlasViewport.height / 8, 1);
                    cmd.DispatchCompute(momentCS, computeMomentKernel, numTilesX, numTilesY, 1);

                    // Do the horizontal pass of the summed area table
                    cmd.SetComputeTextureParam(momentCS, summedAreaHorizontalKernel, HDShaderIDs._SummedAreaTableInputFloat, atlasMoment);
                    cmd.SetComputeTextureParam(momentCS, summedAreaHorizontalKernel, HDShaderIDs._SummedAreaTableOutputInt, m_IntermediateSummedAreaTexture);
                    cmd.SetComputeFloatParam(momentCS, HDShaderIDs._IMSKernelSize, shadowRequest.kernelSize);
                    cmd.SetComputeVectorParam(momentCS, HDShaderIDs._MomentShadowmapSize, new Vector2((float)atlasMoment.referenceSize.x, (float)atlasMoment.referenceSize.y));

                    int numLines = Math.Max((int)shadowRequest.atlasViewport.width / 64, 1);
                    cmd.DispatchCompute(momentCS, summedAreaHorizontalKernel, numLines, 1, 1);

                    // Do the horizontal pass of the summed area table
                    cmd.SetComputeTextureParam(momentCS, summedAreaVerticalKernel, HDShaderIDs._SummedAreaTableInputInt, m_IntermediateSummedAreaTexture);
                    cmd.SetComputeTextureParam(momentCS, summedAreaVerticalKernel, HDShaderIDs._SummedAreaTableOutputInt, m_SummedAreaTexture);
                    cmd.SetComputeVectorParam(momentCS, HDShaderIDs._MomentShadowmapSize, new Vector2((float)atlasMoment.referenceSize.x, (float)atlasMoment.referenceSize.y));
                    cmd.SetComputeFloatParam(momentCS, HDShaderIDs._IMSKernelSize, shadowRequest.kernelSize);

                    int numColumns = Math.Max((int)shadowRequest.atlasViewport.height / 64, 1);
                    cmd.DispatchCompute(momentCS, summedAreaVerticalKernel, numColumns, 1, 1);

                    // Push the global texture
                    cmd.SetGlobalTexture(HDShaderIDs._SummedAreaTableInputInt, m_SummedAreaTexture);
                }
            }
        }

        public void DisplayAtlas(CommandBuffer cmd, Material debugMaterial, Rect atlasViewport, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue)
        {
            if (m_Atlas == null)
                return;

            Vector4 validRange = new Vector4(minValue, 1.0f / (maxValue - minValue));
            float rWidth = 1.0f / width;
            float rHeight = 1.0f / height;
            Vector4 scaleBias = Vector4.Scale(new Vector4(rWidth, rHeight, rWidth, rHeight), new Vector4(atlasViewport.width, atlasViewport.height, atlasViewport.x, atlasViewport.y));

            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetTexture("_AtlasTexture", m_Atlas.rt);
            propertyBlock.SetVector("_TextureScaleBias", scaleBias);
            propertyBlock.SetVector("_ValidRange", validRange);
            propertyBlock.SetFloat("_RcpGlobalScaleFactor", m_RcpScaleFactor);
            cmd.SetViewport(new Rect(screenX, screenY, screenSizeX, screenSizeY));
            cmd.DrawProcedural(Matrix4x4.identity, debugMaterial, debugMaterial.FindPass("RegularShadow"), MeshTopology.Triangles, 3, 1, propertyBlock);
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
