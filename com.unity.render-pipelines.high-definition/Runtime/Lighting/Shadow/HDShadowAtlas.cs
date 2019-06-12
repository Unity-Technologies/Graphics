using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    using RTHandle = RTHandleSystem.RTHandle;

    public partial class HDShadowAtlas
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

        public HDShadowAtlas(RenderPipelineResources renderPipelineResources, int width, int height, int atlasShaderID, int atlasSizeShaderID, Material clearMaterial, BlurAlgorithm blurAlgorithm = BlurAlgorithm.None, FilterMode filterMode = FilterMode.Bilinear, DepthBits depthBufferBits = DepthBits.Depth16, RenderTextureFormat format = RenderTextureFormat.Shadowmap, string name = "", int momentAtlasShaderID = 0)
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

        public struct RenderShadowsParameters
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
            cmd.SetRenderTarget(atlasRenderTexture);
            cmd.SetGlobalVector(parameters.atlasSizeShaderID, new Vector4(atlasRenderTexture.rt.width, atlasRenderTexture.rt.height, 1.0f / atlasRenderTexture.rt.width, 1.0f / atlasRenderTexture.rt.height));

            // Clear the whole atlas to avoid garbage outside of current request when viewing it.
            if (parameters.debugClearAtlas)
                CoreUtils.DrawFullScreen(cmd, parameters.clearMaterial, null, 0);

            foreach (var shadowRequest in parameters.shadowRequests)
            {
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
                cmd.SetGlobalVectorArray(HDShaderIDs._ShadowClipPlanes, shadowRequest.frustumPlanes);

                // TODO: remove this execute when DrawShadows will use a CommandBuffer
                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                renderContext.DrawShadows(ref shadowDrawSettings);
            }

            cmd.SetGlobalFloat(HDShaderIDs._ZClip, 1.0f);   // Re-enable zclip globally
        }

        public bool HasBlurredEVSM()
        {
            return (m_BlurAlgorithm == BlurAlgorithm.EVSM) && m_AtlasMoments[0] != null;
        }

        static void EVSMBlurMoments( RenderShadowsParameters parameters,
                                            RTHandle atlasRenderTexture,
                                            RTHandle[] momentAtlasRenderTextures,
                                            CommandBuffer cmd)
        {
            ComputeShader shadowBlurMomentsCS = parameters.evsmShadowBlurMomentsCS;

            using (new ProfilingSample(cmd, "Render & Blur Moment Shadows", CustomSamplerId.RenderShadowMaps.GetSampler()))
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

                cmd.SetComputeTextureParam(shadowBlurMomentsCS, generateAndBlurMomentsKernel, HDShaderIDs._DepthTexture, atlasRenderTexture);
                cmd.SetComputeVectorArrayParam(shadowBlurMomentsCS, HDShaderIDs._BlurWeightsStorage, blurWeights);

                // We need to store in which of the two moment texture a request will have its last version stored in for a final patch up at the end.
                int[] finalAtlasTexture = new int[parameters.shadowRequests.Count];

                int requestIdx = 0;
                foreach (var shadowRequest in parameters.shadowRequests)
                {
                    using (new ProfilingSample(cmd, "EVSM conversion and blur", CustomSamplerId.RenderShadowMaps.GetSampler()))
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
                        using (new ProfilingSample(cmd, "Copy into main atlas.", CustomSamplerId.RenderShadowMaps.GetSampler()))
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

            using (new ProfilingSample(cmd, "Render Moment Shadows", CustomSamplerId.RenderShadowMaps.GetSampler()))
            {
                int computeMomentKernel = momentCS.FindKernel("ComputeMomentShadows");
                int summedAreaHorizontalKernel = momentCS.FindKernel("MomentSummedAreaTableHorizontal");
                int summedAreaVerticalKernel = momentCS.FindKernel("MomentSummedAreaTableVertical");

                // First of all let's clear the moment shadow map
                HDUtils.SetRenderTarget(cmd, atlasMoment, ClearFlag.Color, Color.black);
                HDUtils.SetRenderTarget(cmd, intermediateSummedAreaTexture, ClearFlag.Color, Color.black);
                HDUtils.SetRenderTarget(cmd, summedAreaTexture, ClearFlag.Color, Color.black);


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

