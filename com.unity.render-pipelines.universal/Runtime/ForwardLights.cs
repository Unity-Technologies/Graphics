using UnityEngine.Experimental.GlobalIllumination;
using Unity.Collections;
using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Computes and submits lighting data to the GPU.
    /// </summary>
    public class ForwardLights
    {
        static class LightConstantBuffer
        {
            public static int _MainLightPosition;   // DeferredLights.LightConstantBuffer also refers to the same ShaderPropertyID - TODO: move this definition to a common location shared by other UniversalRP classes
            public static int _MainLightColor;      // DeferredLights.LightConstantBuffer also refers to the same ShaderPropertyID - TODO: move this definition to a common location shared by other UniversalRP classes

            public static int _AdditionalLightsCount;
            public static int _AdditionalLightsPosition;
            public static int _AdditionalLightsColor;
            public static int _AdditionalLightsAttenuation;
            public static int _AdditionalLightsSpotDir;

            public static int _AdditionalLightOcclusionProbeChannel;

            public static int _ReflectionProbesCount;
        }
        const int k_MaxReflectionProbesPerObject = 2;
        const int k_ReflectionProbesCubeSize = 512;

        int m_AdditionalLightsBufferId;
        int m_AdditionalLightsIndicesId;

        int m_ReflectionProbesBufferId;
        int m_ReflectionProbeTexturesId;

        const string k_SetupLightConstants = "Setup Light Constants";
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_SetupLightConstants);
        MixedLightingSetup m_MixedLightingSetup;

        Vector4[] m_AdditionalLightPositions;
        Vector4[] m_AdditionalLightColors;
        Vector4[] m_AdditionalLightAttenuations;
        Vector4[] m_AdditionalLightSpotDirections;
        Vector4[] m_AdditionalLightOcclusionProbeChannels;

        bool m_UseStructuredBuffer;

        public ForwardLights()
        {
            m_UseStructuredBuffer = RenderingUtils.useStructuredBuffer;

            LightConstantBuffer._MainLightPosition = Shader.PropertyToID("_MainLightPosition");
            LightConstantBuffer._MainLightColor = Shader.PropertyToID("_MainLightColor");
            LightConstantBuffer._AdditionalLightsCount = Shader.PropertyToID("_AdditionalLightsCount");
            LightConstantBuffer._ReflectionProbesCount = Shader.PropertyToID("_ReflectionProbesCount");

            if (m_UseStructuredBuffer)
            {
                m_AdditionalLightsBufferId = Shader.PropertyToID("_AdditionalLightsBuffer");
                m_AdditionalLightsIndicesId = Shader.PropertyToID("_AdditionalLightsIndices");

                m_ReflectionProbesBufferId = Shader.PropertyToID("_ReflectionProbesBuffer");
                m_ReflectionProbeTexturesId = Shader.PropertyToID("_ReflectionProbeTextures");
            }
            else
            {
	            LightConstantBuffer._AdditionalLightsPosition = Shader.PropertyToID("_AdditionalLightsPosition");
	            LightConstantBuffer._AdditionalLightsColor = Shader.PropertyToID("_AdditionalLightsColor");
	            LightConstantBuffer._AdditionalLightsAttenuation = Shader.PropertyToID("_AdditionalLightsAttenuation");
	            LightConstantBuffer._AdditionalLightsSpotDir = Shader.PropertyToID("_AdditionalLightsSpotDir");
	            LightConstantBuffer._AdditionalLightOcclusionProbeChannel = Shader.PropertyToID("_AdditionalLightsOcclusionProbes");

                int maxLights = UniversalRenderPipeline.maxVisibleAdditionalLights;
	            m_AdditionalLightPositions = new Vector4[maxLights];
	            m_AdditionalLightColors = new Vector4[maxLights];
	            m_AdditionalLightAttenuations = new Vector4[maxLights];
	            m_AdditionalLightSpotDirections = new Vector4[maxLights];
	            m_AdditionalLightOcclusionProbeChannels = new Vector4[maxLights];
            }
        }

        public void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            int additionalLightsCount = renderingData.lightData.additionalLightsCount;
            bool additionalLightsPerVertex = renderingData.lightData.shadeAdditionalLightsPerVertex;
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                SetupShaderLightConstants(cmd, ref renderingData);

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightsVertex,
                    additionalLightsCount > 0 && additionalLightsPerVertex);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightsPixel,
                    additionalLightsCount > 0 && !additionalLightsPerVertex);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MixedLightingSubtractive,
                    renderingData.lightData.supportsMixedLighting &&
                    m_MixedLightingSetup == MixedLightingSetup.Subtractive);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void InitializeLightConstants(NativeArray<VisibleLight> lights, int lightIndex, out Vector4 lightPos, out Vector4 lightColor, out Vector4 lightAttenuation, out Vector4 lightSpotDir, out Vector4 lightOcclusionProbeChannel)
        {
            UniversalRenderPipeline.InitializeLightConstants_Common(lights, lightIndex, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir, out lightOcclusionProbeChannel);

            // When no lights are visible, main light will be set to -1.
            // In this case we initialize it to default values and return
            if (lightIndex < 0)
                return;

            VisibleLight lightData = lights[lightIndex];
            Light light = lightData.light;

            // TODO: Add support to shadow mask
            if (light != null && light.bakingOutput.mixedLightingMode == MixedLightingMode.Subtractive && light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed)
            {
                if (m_MixedLightingSetup == MixedLightingSetup.None && lightData.light.shadows != LightShadows.None)
                {
                    m_MixedLightingSetup = MixedLightingSetup.Subtractive;
                }
            }
        }

        void SetupShaderLightConstants(CommandBuffer cmd, ref RenderingData renderingData)
        {
            m_MixedLightingSetup = MixedLightingSetup.None;

            // Main light has an optimized shader path for main light. This will benefit games that only care about a single light.
            // Universal pipeline also supports only a single shadow light, if available it will be the main light.
            SetupMainLightConstants(cmd, ref renderingData.lightData);
            SetupAdditionalLightConstants(cmd, ref renderingData);
            SetupReflectionProbeConstants(cmd, ref renderingData);
        }

        void SetupMainLightConstants(CommandBuffer cmd, ref LightData lightData)
        {
            Vector4 lightPos, lightColor, lightAttenuation, lightSpotDir, lightOcclusionChannel;
            InitializeLightConstants(lightData.visibleLights, lightData.mainLightIndex, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir, out lightOcclusionChannel);

            cmd.SetGlobalVector(LightConstantBuffer._MainLightPosition, lightPos);
            cmd.SetGlobalVector(LightConstantBuffer._MainLightColor, lightColor);
        }

        void SetupAdditionalLightConstants(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ref LightData lightData = ref renderingData.lightData;
            var cullResults = renderingData.cullResults;
            var lights = lightData.visibleLights;
            int maxAdditionalLightsCount = UniversalRenderPipeline.maxVisibleAdditionalLights;
            int additionalLightsCount = SetupPerObjectLightIndices(cullResults, ref lightData);
            if (additionalLightsCount > 0)
            {
                if (m_UseStructuredBuffer)
                {
                    NativeArray<ShaderInput.LightData> additionalLightsData = new NativeArray<ShaderInput.LightData>(additionalLightsCount, Allocator.Temp);
                    for (int i = 0, lightIter = 0; i < lights.Length && lightIter < maxAdditionalLightsCount; ++i)
                    {
                        VisibleLight light = lights[i];
                        if (lightData.mainLightIndex != i)
                        {
                            ShaderInput.LightData data;
                            InitializeLightConstants(lights, i,
                                out data.position, out data.color, out data.attenuation,
                                out data.spotDirection, out data.occlusionProbeChannels);
                            additionalLightsData[lightIter] = data;
                            lightIter++;
                        }
                    }

                    var lightDataBuffer = ShaderData.instance.GetLightDataBuffer(additionalLightsCount);
                    lightDataBuffer.SetData(additionalLightsData);

                    int lightIndices = cullResults.lightAndReflectionProbeIndexCount;
                    var lightIndicesBuffer = ShaderData.instance.GetLightIndicesBuffer(lightIndices);

                    cmd.SetGlobalBuffer(m_AdditionalLightsBufferId, lightDataBuffer);
                    cmd.SetGlobalBuffer(m_AdditionalLightsIndicesId, lightIndicesBuffer);

                    additionalLightsData.Dispose();
                }
                else
                {
                    for (int i = 0, lightIter = 0; i < lights.Length && lightIter < maxAdditionalLightsCount; ++i)
                    {
                        VisibleLight light = lights[i];
                        if (lightData.mainLightIndex != i)
                        {
                            InitializeLightConstants(lights, i, out m_AdditionalLightPositions[lightIter],
                                out m_AdditionalLightColors[lightIter],
                                out m_AdditionalLightAttenuations[lightIter],
                                out m_AdditionalLightSpotDirections[lightIter],
                                out m_AdditionalLightOcclusionProbeChannels[lightIter]);
                            lightIter++;
                        }
                    }

                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsPosition, m_AdditionalLightPositions);
                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsColor, m_AdditionalLightColors);
                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsAttenuation, m_AdditionalLightAttenuations);
                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsSpotDir, m_AdditionalLightSpotDirections);
                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightOcclusionProbeChannel, m_AdditionalLightOcclusionProbeChannels);
                }

                cmd.SetGlobalVector(LightConstantBuffer._AdditionalLightsCount, new Vector4(lightData.maxPerObjectAdditionalLightsCount,
                    0.0f, 0.0f, 0.0f));
            }
            else
            {
                cmd.SetGlobalVector(LightConstantBuffer._AdditionalLightsCount, Vector4.zero);
            }
        }

        class ReflectionProbeSorter : IComparer<ReflectionProbe>
        {
            public int Compare(ReflectionProbe a, ReflectionProbe b)
            {
                // probes with larger importance render later (to blend over previous probes)
                if (a.importance != b.importance)
                    return b.importance.CompareTo(a.importance);
                // smaller probes render later (better handles small probes being inside larger probes cases)
                return a.bounds.extents.sqrMagnitude.CompareTo(b.bounds.extents.sqrMagnitude);
            }
        }

        void SetupReflectionProbeConstants(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ReflectionProbe[] reflectionProbes = Resources.FindObjectsOfTypeAll<ReflectionProbe>();
            if (reflectionProbes.Length > 0)
            {
                Array.Sort(reflectionProbes, new ReflectionProbeSorter());

                if (m_UseStructuredBuffer)
                {
                    // #note we need to use the same reflection texture size to use texture 2d or use the largest and upscale the others
                    var textureArray = new Texture2DArray(k_ReflectionProbesCubeSize, k_ReflectionProbesCubeSize, Math.Min(reflectionProbes.Length, k_MaxReflectionProbesPerObject), TextureFormat.RGB24, false);
                    var reflectionProbeData = new NativeArray<ShaderInput.ReflectionProbeData>(reflectionProbes.Length, Allocator.Temp);
                    // #note should we use reflectionProbe.hdr (bool)?
                    for (int i = 0; i < reflectionProbes.Length && i < k_MaxReflectionProbesPerObject; i++)
                    {
                        ShaderInput.ReflectionProbeData data;
                        data.position = reflectionProbes[i].transform.position;
                        data.position.w = reflectionProbes[i].boxProjection ? 1 : 0;
                        data.boxMin = reflectionProbes[i].bounds.min;
                        data.boxMin.w = reflectionProbes[i].blendDistance;
                        data.boxMax = reflectionProbes[i].bounds.max;
                        data.hdr = reflectionProbes[i].textureHDRDecodeValues;
                        reflectionProbeData[i] = data;

                        //Texture2D temp_tex2d = new Texture2D(k_ReflectionProbesCubeSize, k_ReflectionProbesCubeSize, TextureFormat.RGBA32, false);
                        //RenderTexture currentRT = RenderTexture.active;
                        //RenderTexture renderTexture = RenderTexture.GetTemporary(k_ReflectionProbesCubeSize, k_ReflectionProbesCubeSize, 0);
                        //Graphics.Blit(reflectionProbes[i].texture, renderTexture);
                        //RenderTexture.active = renderTexture;
                        //temp_tex2d.ReadPixels(new Rect(0, 0, k_ReflectionProbesCubeSize, k_ReflectionProbesCubeSize), 0, 0);
                        //temp_tex2d.Apply();
                        //
                        //RenderTexture.active = currentRT;
                        //RenderTexture.ReleaseTemporary(renderTexture);

                        // Create a temporary RenderTexture of the same size as the texture
                        RenderTexture tmp = RenderTexture.GetTemporary(
                                            k_ReflectionProbesCubeSize,
                                            k_ReflectionProbesCubeSize,
                                            0,
                                            RenderTextureFormat.Default,
                                            RenderTextureReadWrite.Linear);

                        // Blit the pixels on texture to the RenderTexture
                        Graphics.Blit(reflectionProbes[i].texture, tmp);
                        // Backup the currently set RenderTexture
                        RenderTexture previous = RenderTexture.active;
                        // Set the current RenderTexture to the temporary one we created
                        RenderTexture.active = tmp;
                        // Create a new readable Texture2D to copy the pixels to it
                        Texture2D myTexture2D = new Texture2D(k_ReflectionProbesCubeSize, k_ReflectionProbesCubeSize);
                        // Copy the pixels from the RenderTexture to the new Texture
                        myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                        myTexture2D.Apply();
                        // Reset the active RenderTexture
                        RenderTexture.active = previous;
                        // Release the temporary RenderTexture
                        RenderTexture.ReleaseTemporary(tmp);

                        // "myTexture2D" now has the same pixels from "texture" and it's readable.

                        //Rendering not happening for the reflection probe textures.

                        //Texture2D tex = Texture2D.CreateExternalTexture(
                        //    k_ReflectionProbesCubeSize,
                        //    k_ReflectionProbesCubeSize,
                        //    TextureFormat.BC6H,
                        //    false, false,
                        //    reflectionProbes[i].texture.GetNativeTexturePtr());
                        //Debug.Log(tex.GetPixel(0,0));
                        Debug.Log(myTexture2D.GetPixel(0, 0));
                        textureArray.SetPixels(myTexture2D.GetPixels(), i);

                        textureArray.Apply();


                    }
                    //Debug.Log(textureArray.GetPixels(1)[0]);
                    cmd.SetGlobalTexture(m_ReflectionProbeTexturesId, textureArray);

                    var probeDataBuffer = ShaderData.instance.GetReflectionProbeDataBuffer(reflectionProbes.Length);
                    probeDataBuffer.SetData(reflectionProbeData);
                    cmd.SetGlobalBuffer(m_ReflectionProbesBufferId, probeDataBuffer);

                    reflectionProbeData.Dispose();

                    // #note handle CubeMapTextureArray cache here?? See HDRP (TextureCacheCubeMap.cs & ReflectionProbeCache.cs)
                    // Notes @mortenm:
                    //  This is an important one too: TransferToSlice() in those files
                    //  to convert on the fly from cube map to panorama the array requires an uncompressed format. If on the other hand you know the platform supports cube map arrays then you could use a compressed format such as BC6
                    //  but initially to get it running you could just always set it to RGBm or 4xfp16 or 11_11_10F
                    //  also an unrelated subtlety I wanted to mention since it is easy to miss is NewFrame() must be called on each texture cache once per frame
                    //  also have you been able to find the blit shader used in TransferToPanoCache()? It's in ../com.unity.render-pipelines.high-definition/Runtime/Core/CoreResources It is CubeToPano.shader
                    var yellowImg = new Color[16 * 16];
                    for (int i = 0; i < 16 * 16; i++)
                        yellowImg[i] = Color.green;
                    var redImg = new Color[16 * 16];
                    for (int i = 0; i < 16 * 16; i++)
                        redImg[i] = Color.red;
                    //textureArray.SetPixels(yellowImg, 0, 0);
                    //textureArray.SetPixels(redImg, 1, 0);


                }
                else
                {
                    // #note To do implement UBO fall back path...
                }

                // #note might need to pack more data into this??
                cmd.SetGlobalVector(LightConstantBuffer._ReflectionProbesCount, new Vector4(Math.Min(reflectionProbes.Length, k_MaxReflectionProbesPerObject),
                    0.0f, 0.0f, 0.0f));
            }
            else
            {
                cmd.SetGlobalVector(LightConstantBuffer._ReflectionProbesCount, Vector4.zero);
            }
        }

        int SetupPerObjectLightIndices(CullingResults cullResults, ref LightData lightData)
        {
            if (lightData.additionalLightsCount == 0)
                return lightData.additionalLightsCount;

            var visibleLights = lightData.visibleLights;
            var perObjectLightIndexMap = cullResults.GetLightIndexMap(Allocator.Temp);
            int globalDirectionalLightsCount = 0;
            int additionalLightsCount = 0;

            // Disable all directional lights from the perobject light indices
            // Pipeline handles main light globally and there's no support for additional directional lights atm.
            for (int i = 0; i < visibleLights.Length; ++i)
            {
                if (additionalLightsCount >= UniversalRenderPipeline.maxVisibleAdditionalLights)
                    break;

                VisibleLight light = visibleLights[i];
                if (i == lightData.mainLightIndex)
                {
                    perObjectLightIndexMap[i] = -1;
                    ++globalDirectionalLightsCount;
                }
                else
                {
                    perObjectLightIndexMap[i] -= globalDirectionalLightsCount;
                    ++additionalLightsCount;
                }
            }

            // Disable all remaining lights we cannot fit into the global light buffer.
            for (int i = globalDirectionalLightsCount + additionalLightsCount; i < perObjectLightIndexMap.Length; ++i)
                perObjectLightIndexMap[i] = -1;

            cullResults.SetLightIndexMap(perObjectLightIndexMap);

            if (m_UseStructuredBuffer && additionalLightsCount > 0)
            {
                int lightAndReflectionProbeIndices = cullResults.lightAndReflectionProbeIndexCount;
                Assertions.Assert.IsTrue(lightAndReflectionProbeIndices > 0, "Pipelines configures additional lights but per-object light and probe indices count is zero.");
                cullResults.FillLightAndReflectionProbeIndices(ShaderData.instance.GetLightIndicesBuffer(lightAndReflectionProbeIndices));
            }

            perObjectLightIndexMap.Dispose();
            return additionalLightsCount;
        }
    }
}
