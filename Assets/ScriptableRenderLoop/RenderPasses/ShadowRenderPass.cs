using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using System.Collections.Generic;
using System;

namespace UnityEngine.Experimental.Rendering
{
    [System.Serializable]
    public class ShadowSettings
    {
        public bool     enabled;
        public int      shadowAtlasWidth;
        public int      shadowAtlasHeight;

        public float    maxShadowDistance;
        public int      directionalLightCascadeCount;
        public Vector3  directionalLightCascades;


        public static ShadowSettings Default
        {
            get
            {
                ShadowSettings settings = new ShadowSettings();
                settings.enabled = true;
                settings.shadowAtlasHeight = settings.shadowAtlasWidth = 4096;
                settings.directionalLightCascadeCount = 1;
                settings.directionalLightCascades = new Vector3(0.05F, 0.2F, 0.3F);
                settings.directionalLightCascadeCount = 4;
                settings.maxShadowDistance = 1000.0F;
                return settings;
            }
        }
    }

    public struct InputShadowLightData
    {
        public int          lightIndex;
        public int          shadowResolution;
    }

    public struct ShadowLight
    {
        public int shadowSliceIndex;
        public int shadowSliceCount;
    }

    public struct ShadowSliceData
    {
        public Matrix4x4    shadowTransform;
        public int          atlasX;
        public int          atlasY;
        public int          shadowResolution;
    }

    public struct ShadowOutput
    {
        public ShadowSliceData[]    shadowSlices;
        public ShadowLight[]        shadowLights;
        public Vector4[]            directionalShadowSplitSphereSqr;

        public int GetShadowSliceCountLightIndex(int lightIndex)
        {
            return shadowLights[lightIndex].shadowSliceCount;
        }

        public int GetShadowSliceIndex(int lightIndex, int sliceIndex)
        {
            if (sliceIndex >= shadowLights[lightIndex].shadowSliceCount)
                throw new System.IndexOutOfRangeException();

            return shadowLights[lightIndex].shadowSliceIndex + sliceIndex;
        }
    }

    public struct ShadowRenderPass : IDisposable
    {
        ShadowSettings              m_Settings;

        [NonSerialized]
        bool                        m_FailedToPackLastTime;
        int                         m_ShadowTexName;
        const int                   k_DepthBuffer = 24;


        public ShadowRenderPass(ShadowSettings settings)
        {
            m_Settings = settings;
            m_FailedToPackLastTime = false;
            m_ShadowTexName = Shader.PropertyToID("g_tShadowBuffer");
        }

        public void Dispose()
        {
        }

        struct AtlasEntry
        {
            public AtlasEntry(int splitIndex, int lightIndex)
            {
                this.splitIndex = splitIndex;
                this.lightIndex = lightIndex;
            }

            public readonly int splitIndex;
            public readonly int lightIndex;
        }

        int CalculateNumShadowSplits(int index, VisibleLight[] lights)
        {
            var lightType = lights[index].lightType;
            switch (lightType)
            {
                case LightType.Spot:
                    return 1;

                case LightType.Directional:
                    return m_Settings.directionalLightCascadeCount;

                default:
                    return 6;
            }
        }

        public static void ClearPackedShadows(VisibleLight[] lights, out ShadowOutput packedShadows)
        {
            packedShadows.directionalShadowSplitSphereSqr = null;
            packedShadows.shadowSlices = null;
            packedShadows.shadowLights = new ShadowLight[lights.Length];
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------
        bool AutoPackLightsIntoShadowTexture(List<InputShadowLightData> shadowLights, VisibleLight[] lights, out ShadowOutput packedShadows)
        {
            var activeShadowLights = new Dictionary<int, InputShadowLightData>();
            var shadowIndices = new List<int>();

            //@TODO: Disallow multiple directional lights

            for (int i = 0; i < shadowLights.Count; i++)
            {
                shadowIndices.Add(shadowLights[i].lightIndex);
                activeShadowLights[shadowLights[i].lightIndex] = shadowLights[i];
            }

            // World's stupidest sheet packer:
            //    1. Sort all lights from largest to smallest
            //    2. In a left->right, top->bottom pattern, fill quads until you reach the edge of the texture
            //    3. Move position to x=0, y=bottomOfFirstTextureInThisRow
            //    4. Goto 2.
            // Yes, this will produce holes as the quads shrink, but it's good enough for now. I'll work on this more later to fill the gaps.

            // Sort all lights from largest to smallest
            shadowIndices.Sort(
                delegate(int l1, int l2)
                {
                    var nCompare = 0;
                    // Sort shadow-casting lights by shadow resolution
                    nCompare = activeShadowLights[l1].shadowResolution.CompareTo(activeShadowLights[l2].shadowResolution); // Sort by shadow size

                    if (nCompare == 0)   // Same, so sort by range to stabilize sort results
                        nCompare = lights[l1].range.CompareTo(lights[l2].range);   // Sort by shadow size

                    if (nCompare == 0)   // Still same, so sort by instance ID to stabilize sort results
                        nCompare = lights[l1].light.GetInstanceID().CompareTo(lights[l2].light.GetInstanceID());

                    return nCompare;
                }
                );

            // Start filling lights into texture
            var requestedPages = new List<AtlasEntry>();
            packedShadows.shadowLights = new ShadowLight[lights.Length];
            for (int i = 0; i != shadowIndices.Count; i++)
            {
                var numShadowSplits = CalculateNumShadowSplits(shadowIndices[i], lights);

                packedShadows.shadowLights[shadowIndices[i]].shadowSliceCount = numShadowSplits;
                packedShadows.shadowLights[shadowIndices[i]].shadowSliceIndex = requestedPages.Count;

                for (int s = 0; s < numShadowSplits; s++)
                    requestedPages.Add(new AtlasEntry(requestedPages.Count, shadowIndices[i]));
            }

            var nCurrentX = 0;
            var nCurrentY = -1;
            var nNextY = 0;

            packedShadows.shadowSlices = new ShadowSliceData[requestedPages.Count];
            packedShadows.directionalShadowSplitSphereSqr = new Vector4[4];

            foreach (var entry in requestedPages)
            {
                var shadowResolution = activeShadowLights[entry.lightIndex].shadowResolution;

                // Check if first texture is too wide
                if (nCurrentY == -1)
                {
                    if ((shadowResolution > m_Settings.shadowAtlasWidth) || (shadowResolution > m_Settings.shadowAtlasHeight))
                    {
                        Debug.LogError("ERROR! Shadow packer ran out of space in the " + m_Settings.shadowAtlasWidth + "x" + m_Settings.shadowAtlasHeight + " texture!\n\n");
                        m_FailedToPackLastTime = true;
                        ClearPackedShadows(lights, out packedShadows);
                        return false;
                    }
                }

                // Goto next scanline
                if ((nCurrentY == -1) || ((nCurrentX + shadowResolution) > m_Settings.shadowAtlasWidth))
                {
                    nCurrentX = 0;
                    nCurrentY = nNextY;
                    nNextY += shadowResolution;
                }

                // Check if we've run out of space
                if ((nCurrentY + shadowResolution) > m_Settings.shadowAtlasHeight)
                {
                    Debug.LogError("ERROR! Shadow packer ran out of space in the " + m_Settings.shadowAtlasWidth + "x" + m_Settings.shadowAtlasHeight + " texture!\n\n");
                    m_FailedToPackLastTime = true;
                    ClearPackedShadows(lights, out packedShadows);
                    return false;
                }

                // Save location to light
                packedShadows.shadowSlices[entry.splitIndex].atlasX = nCurrentX;
                packedShadows.shadowSlices[entry.splitIndex].atlasY = nCurrentY;
                packedShadows.shadowSlices[entry.splitIndex].shadowResolution = shadowResolution;

                // Move ahead
                nCurrentX += shadowResolution;

                //Debug.Log( "Sheet packer: " + vl.m_cachedLight.name + " ( " + vl.m_shadowX + ", " + vl.m_shadowY + " ) " + vl.m_shadowResolution + "\n\n" );
            }

            if (m_FailedToPackLastTime)
            {
                m_FailedToPackLastTime = false;
                Debug.Log("SUCCESS! Shadow packer can now fit all lights into the " + m_Settings.shadowAtlasWidth + "x" + m_Settings.shadowAtlasHeight + " texture!\n\n");
            }

            return requestedPages.Count != 0;
        }

        static List<InputShadowLightData> GetInputShadowLightData(CullResults cullResults)
        {
            var shadowCasters = new List<InputShadowLightData>();
            var lights = cullResults.visibleLights;
            int directionalLightCount = 0;

            for (int i = 0; i < lights.Length; i++)
            {
                //@TODO: ignore baked. move this logic to c++...
                if (lights[i].light.shadows == LightShadows.None)
                    continue;

                // Only a single directional shadow casting light is supported
                if (lights[i].lightType == LightType.Directional)
                {
                    directionalLightCount++;
                    if (directionalLightCount != 1)
                        continue;
                }

                AdditionalLightData additionalLight = lights[i].light.GetComponent<AdditionalLightData>();

                InputShadowLightData light;
                light.lightIndex = i;
                light.shadowResolution = AdditionalLightData.GetShadowResolution(additionalLight);

                shadowCasters.Add(light);
            }
            return shadowCasters;
        }

        public void UpdateCullingParameters(ref CullingParameters parameters)
        {
            parameters.shadowDistance = Mathf.Min(m_Settings.maxShadowDistance, parameters.shadowDistance);
        }

        public void Render(ScriptableRenderContext loop, CullResults cullResults, out ShadowOutput packedShadows)
        {
            if (!m_Settings.enabled)
            {
                ClearPackedShadows(cullResults.visibleLights, out packedShadows);
            }

            // Pack all shadow quads into the texture
            if (!AutoPackLightsIntoShadowTexture(GetInputShadowLightData(cullResults), cullResults.visibleLights, out packedShadows))
            {
                // No shadowing lights found, so skip all rendering
                return;
            }

            RenderPackedShadows(loop, cullResults, ref packedShadows);
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------
        // Render shadows
        //---------------------------------------------------------------------------------------------------------------------------------------------------
        void RenderPackedShadows(ScriptableRenderContext loop, CullResults cullResults, ref ShadowOutput packedShadows)
        {
            var setRenderTargetCommandBuffer = new CommandBuffer();

            setRenderTargetCommandBuffer.name = "Render packed shadows";
            setRenderTargetCommandBuffer.GetTemporaryRT(m_ShadowTexName, m_Settings.shadowAtlasWidth, m_Settings.shadowAtlasHeight, k_DepthBuffer, FilterMode.Bilinear, RenderTextureFormat.Shadowmap, RenderTextureReadWrite.Linear);
            setRenderTargetCommandBuffer.SetRenderTarget(new RenderTargetIdentifier(m_ShadowTexName));

            setRenderTargetCommandBuffer.ClearRenderTarget(true, true, Color.green);
            loop.ExecuteCommandBuffer(setRenderTargetCommandBuffer);
            setRenderTargetCommandBuffer.Dispose();

            VisibleLight[] visibleLights = cullResults.visibleLights;
            var shadowSlices = packedShadows.shadowSlices;

            // Render each light's shadow buffer into a subrect of the shared depth texture
            for (int lightIndex = 0; lightIndex < packedShadows.shadowLights.Length; lightIndex++)
            {
                int shadowSliceCount = packedShadows.shadowLights[lightIndex].shadowSliceCount;
                if (shadowSliceCount == 0)
                    continue;

                Profiler.BeginSample("Shadows.GetShadowCasterBounds");
                Bounds bounds;
                if (!cullResults.GetShadowCasterBounds(lightIndex, out bounds))
                {
                    Profiler.EndSample();
                    continue;
                }
                Profiler.EndSample();

                Profiler.BeginSample("Shadows.DrawShadows");

                Matrix4x4 proj;
                Matrix4x4 view;

                var lightType = visibleLights[lightIndex].lightType;
                var lightDirection = visibleLights[lightIndex].light.transform.forward;
                var shadowNearPlaneOffset = QualitySettings.shadowNearPlaneOffset;

                int shadowSliceIndex = packedShadows.GetShadowSliceIndex(lightIndex, 0);

                if (lightType == LightType.Spot)
                {
                    var settings = new DrawShadowsSettings(cullResults, lightIndex);
                    bool needRendering = cullResults.ComputeSpotShadowMatricesAndCullingPrimitives(lightIndex, out view, out proj, out settings.splitData);
                    SetupShadowSplitMatrices(ref packedShadows.shadowSlices[shadowSliceIndex], proj, view);
                    if (needRendering)
                        RenderShadowSplit(ref shadowSlices[shadowSliceIndex], lightDirection, proj, view, ref loop, settings);
                }
                else if (lightType == LightType.Directional)
                {
                    Vector3 splitRatio = m_Settings.directionalLightCascades;

                    for (int s = 0; s < 4; ++s)
                        packedShadows.directionalShadowSplitSphereSqr[s] = new Vector4(0, 0, 0, float.NegativeInfinity);

                    for (int s = 0; s < shadowSliceCount; ++s, shadowSliceIndex++)
                    {
                        var settings = new DrawShadowsSettings(cullResults, lightIndex);
                        var shadowResolution = shadowSlices[shadowSliceIndex].shadowResolution;
                        bool needRendering = cullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(lightIndex, s, shadowSliceCount, splitRatio, shadowResolution, shadowNearPlaneOffset, out view, out proj, out settings.splitData);

                        packedShadows.directionalShadowSplitSphereSqr[s] = settings.splitData.cullingSphere;
                        packedShadows.directionalShadowSplitSphereSqr[s].w *= packedShadows.directionalShadowSplitSphereSqr[s].w;

                        SetupShadowSplitMatrices(ref shadowSlices[shadowSliceIndex], proj, view);
                        if (needRendering)
                            RenderShadowSplit(ref shadowSlices[shadowSliceIndex], lightDirection, proj, view, ref loop, settings);
                    }
                }
                else if (lightType == LightType.Point)
                {
                    for (int s = 0; s < shadowSliceCount; ++s, shadowSliceIndex++)
                    {
                        var settings = new DrawShadowsSettings(cullResults, lightIndex);
                        bool needRendering = cullResults.ComputePointShadowMatricesAndCullingPrimitives(lightIndex, (CubemapFace)s, 2.0f, out view, out proj, out settings.splitData);

                        SetupShadowSplitMatrices(ref shadowSlices[shadowSliceIndex], proj, view);
                        if (needRendering)
                            RenderShadowSplit(ref shadowSlices[shadowSliceIndex], lightDirection, proj, view, ref loop, settings);
                    }
                }
                Profiler.EndSample();
            }
        }

        private void SetupShadowSplitMatrices(ref ShadowSliceData lightData, Matrix4x4 proj, Matrix4x4 view)
        {
            var matScaleBias = Matrix4x4.identity;
            matScaleBias.m00 = 0.5f;
            matScaleBias.m11 = 0.5f;
            matScaleBias.m22 = 0.5f;
            matScaleBias.m03 = 0.5f;
            matScaleBias.m13 = 0.5f;
            matScaleBias.m23 = 0.5f;

            var matTile = Matrix4x4.identity;
            matTile.m00 = (float)lightData.shadowResolution / (float)m_Settings.shadowAtlasWidth;
            matTile.m11 = (float)lightData.shadowResolution / (float)m_Settings.shadowAtlasHeight;
            matTile.m03 = (float)lightData.atlasX / (float)m_Settings.shadowAtlasWidth;
            matTile.m13 = (float)lightData.atlasY / (float)m_Settings.shadowAtlasHeight;
            lightData.shadowTransform = matTile * matScaleBias * proj * view;
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------
        private void RenderShadowSplit(ref ShadowSliceData slice, Vector3 lightDirection, Matrix4x4 proj, Matrix4x4 view, ref ScriptableRenderContext loop, DrawShadowsSettings settings)
        {
            var commandBuffer = new CommandBuffer { name = "ShadowSetup" };

            // Set viewport / matrices etc
            commandBuffer.SetViewport(new Rect(slice.atlasX, slice.atlasY, slice.shadowResolution, slice.shadowResolution));
            //commandBuffer.ClearRenderTarget (true, true, Color.green);
            commandBuffer.SetGlobalVector("g_vLightDirWs", new Vector4(lightDirection.x, lightDirection.y, lightDirection.z));
            commandBuffer.SetViewProjectionMatrices(view, proj);
            //	commandBuffer.SetGlobalDepthBias (1.0F, 1.0F);
            loop.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Dispose();

            // Render
            loop.DrawShadows(settings);
        }
    }
}
