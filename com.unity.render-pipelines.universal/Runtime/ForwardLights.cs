using UnityEngine.Experimental.GlobalIllumination;
using Unity.Collections;
using System.Numerics;
using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Computes and submits lighting data to the GPU.
    /// </summary>
    public class ForwardLights
    {
        // #note probably need to move this to a setting like additional lights.
        const int k_MaxReflectionProbesPerObject = 2;
        const int k_ReflectionProbesCubeSize = 512;

        static class LightConstantBuffer
        {
            public static int _MainLightPosition;
            public static int _MainLightColor;

            public static int _AdditionalLightsCount;
            public static int _AdditionalLightsPosition;
            public static int _AdditionalLightsColor;
            public static int _AdditionalLightsAttenuation;
            public static int _AdditionalLightsSpotDir;

            public static int _AdditionalLightOcclusionProbeChannel;

            public static int _ReflectionProbesCount;
        }
        
        int m_AdditionalLightsBufferId;
        int m_AdditionalLightsIndicesId;

        int m_ReflectionProbesBufferId;
        int m_ReflectionProbesIndicesId;

        int m_ReflectionProbeTexturesId;

        const string k_SetupLightConstants = "Setup Light Constants";
        MixedLightingSetup m_MixedLightingSetup;

        // Holds light direction for directional lights or position for punctual lights.
        // When w is set to 1.0, it means it's a punctual light.
        Vector4 k_DefaultLightPosition = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
        Vector4 k_DefaultLightColor = Color.black;

        // Default light attenuation is setup in a particular way that it causes
        // directional lights to return 1.0 for both distance and angle attenuation
        Vector4 k_DefaultLightAttenuation = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
        Vector4 k_DefaultLightSpotDirection = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
        Vector4 k_DefaultLightsProbeChannel = new Vector4(-1.0f, 1.0f, -1.0f, -1.0f);

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
                m_ReflectionProbesIndicesId = Shader.PropertyToID("_ReflectionProbeIndices");
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
            CommandBuffer cmd = CommandBufferPool.Get(k_SetupLightConstants);
            SetupShaderLightConstants(cmd, ref renderingData);

            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightsVertex,
                additionalLightsCount > 0 && additionalLightsPerVertex);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightsPixel,
                additionalLightsCount > 0 && !additionalLightsPerVertex);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MixedLightingSubtractive,
                renderingData.lightData.supportsMixedLighting &&
                m_MixedLightingSetup == MixedLightingSetup.Subtractive);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void InitializeProbeConstants(NativeArray<VisibleReflectionProbe> probes, int probeIndex, out Vector4 probePosition, out Vector4 probeBoxMin, out Vector4 probeBoxMax, out Vector4 probeHDR)
        {
            var probeData = probes[probeIndex];
            probePosition = probeData.localToWorldMatrix.GetColumn(3);
            probePosition.w = (probeData.isBoxProjection) ? 1f : 0f;
            probeBoxMin = probeData.bounds.min;
            probeBoxMax = probeData.bounds.max;
            probeHDR = probeData.hdrData;
        }

        void InitializeLightConstants(NativeArray<VisibleLight> lights, int lightIndex, out Vector4 lightPos, out Vector4 lightColor, out Vector4 lightAttenuation, out Vector4 lightSpotDir, out Vector4 lightOcclusionProbeChannel)
        {
            lightPos = k_DefaultLightPosition;
            lightColor = k_DefaultLightColor;
            lightAttenuation = k_DefaultLightAttenuation;
            lightSpotDir = k_DefaultLightSpotDirection;
            lightOcclusionProbeChannel = k_DefaultLightsProbeChannel;

            // When no lights are visible, main light will be set to -1.
            // In this case we initialize it to default values and return
            if (lightIndex < 0)
                return;

            VisibleLight lightData = lights[lightIndex];
            if (lightData.lightType == LightType.Directional)
            {
                Vector4 dir = -lightData.localToWorldMatrix.GetColumn(2);
                lightPos = new Vector4(dir.x, dir.y, dir.z, 0.0f);
            }
            else
            {
                Vector4 pos = lightData.localToWorldMatrix.GetColumn(3);
                lightPos = new Vector4(pos.x, pos.y, pos.z, 1.0f);
            }

            // VisibleLight.finalColor already returns color in active color space
            lightColor = lightData.finalColor;

            // Directional Light attenuation is initialize so distance attenuation always be 1.0
            if (lightData.lightType != LightType.Directional)
            {
                // Light attenuation in universal matches the unity vanilla one.
                // attenuation = 1.0 / distanceToLightSqr
                // We offer two different smoothing factors.
                // The smoothing factors make sure that the light intensity is zero at the light range limit.
                // The first smoothing factor is a linear fade starting at 80 % of the light range.
                // smoothFactor = (lightRangeSqr - distanceToLightSqr) / (lightRangeSqr - fadeStartDistanceSqr)
                // We rewrite smoothFactor to be able to pre compute the constant terms below and apply the smooth factor
                // with one MAD instruction
                // smoothFactor =  distanceSqr * (1.0 / (fadeDistanceSqr - lightRangeSqr)) + (-lightRangeSqr / (fadeDistanceSqr - lightRangeSqr)
                //                 distanceSqr *           oneOverFadeRangeSqr             +              lightRangeSqrOverFadeRangeSqr

                // The other smoothing factor matches the one used in the Unity lightmapper but is slower than the linear one.
                // smoothFactor = (1.0 - saturate((distanceSqr * 1.0 / lightrangeSqr)^2))^2
                float lightRangeSqr = lightData.range * lightData.range;
                float fadeStartDistanceSqr = 0.8f * 0.8f * lightRangeSqr;
                float fadeRangeSqr = (fadeStartDistanceSqr - lightRangeSqr);
                float oneOverFadeRangeSqr = 1.0f / fadeRangeSqr;
                float lightRangeSqrOverFadeRangeSqr = -lightRangeSqr / fadeRangeSqr;
                float oneOverLightRangeSqr = 1.0f / Mathf.Max(0.0001f, lightData.range * lightData.range);

                // On mobile and Nintendo Switch: Use the faster linear smoothing factor (SHADER_HINT_NICE_QUALITY).
                // On other devices: Use the smoothing factor that matches the GI.
                lightAttenuation.x = Application.isMobilePlatform || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Switch ? oneOverFadeRangeSqr : oneOverLightRangeSqr;
                lightAttenuation.y = lightRangeSqrOverFadeRangeSqr;
            }

            if (lightData.lightType == LightType.Spot)
            {
                Vector4 dir = lightData.localToWorldMatrix.GetColumn(2);
                lightSpotDir = new Vector4(-dir.x, -dir.y, -dir.z, 0.0f);

                // Spot Attenuation with a linear falloff can be defined as
                // (SdotL - cosOuterAngle) / (cosInnerAngle - cosOuterAngle)
                // This can be rewritten as
                // invAngleRange = 1.0 / (cosInnerAngle - cosOuterAngle)
                // SdotL * invAngleRange + (-cosOuterAngle * invAngleRange)
                // If we precompute the terms in a MAD instruction
                float cosOuterAngle = Mathf.Cos(Mathf.Deg2Rad * lightData.spotAngle * 0.5f);
                // We neeed to do a null check for particle lights
                // This should be changed in the future
                // Particle lights will use an inline function
                float cosInnerAngle;
                if (lightData.light != null)
                    cosInnerAngle = Mathf.Cos(lightData.light.innerSpotAngle * Mathf.Deg2Rad * 0.5f);
                else
                    cosInnerAngle = Mathf.Cos((2.0f * Mathf.Atan(Mathf.Tan(lightData.spotAngle * 0.5f * Mathf.Deg2Rad) * (64.0f - 18.0f) / 64.0f)) * 0.5f);
                float smoothAngleRange = Mathf.Max(0.001f, cosInnerAngle - cosOuterAngle);
                float invAngleRange = 1.0f / smoothAngleRange;
                float add = -cosOuterAngle * invAngleRange;
                lightAttenuation.z = invAngleRange;
                lightAttenuation.w = add;
            }

            Light light = lightData.light;

            // Set the occlusion probe channel.
            int occlusionProbeChannel = light != null ? light.bakingOutput.occlusionMaskChannel : -1;

            // If we have baked the light, the occlusion channel is the index we need to sample in 'unity_ProbesOcclusion'
            // If we have not baked the light, the occlusion channel is -1.
            // In case there is no occlusion channel is -1, we set it to zero, and then set the second value in the
            // input to one. We then, in the shader max with the second value for non-occluded lights.
            lightOcclusionProbeChannel.x = occlusionProbeChannel == -1 ? 0f : occlusionProbeChannel;
            lightOcclusionProbeChannel.y = occlusionProbeChannel == -1 ? 1f : 0f;

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
            if(reflectionProbes.Length > 0)
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
                        Debug.Log(myTexture2D.GetPixel(0,0));
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

        int SetupPerObjectReflectionProbeIndices(CullingResults cullResults)
        {
            if (cullResults.reflectionProbeIndexCount == 0)
                return cullResults.reflectionProbeIndexCount;

            var perObjectReflectionProbeIndexMap = cullResults.GetReflectionProbeIndexMap(Allocator.Temp);
            // #note to do: discard reflection probes exceeding the max allowed number of probes
            var reflectionProbesCount = perObjectReflectionProbeIndexMap.Length;// Mathf.Min(perObjectReflectionProbeIndexMap.Length, k_MaxReflectionProbesPerObject);

            //// Disable reflection probes that do not fit into the buffer.
            //for (int i = k_MaxReflectionProbesPerObject; i < perObjectReflectionProbeIndexMap.Length; ++i)
            //    perObjectReflectionProbeIndexMap[i] = -1;

            cullResults.SetReflectionProbeIndexMap(perObjectReflectionProbeIndexMap);

            // #note duplicate code & work with SetupPerObjectLightIndices.
            if (m_UseStructuredBuffer && reflectionProbesCount > 0)
            {
                int lightAndReflectionProbeIndices = cullResults.lightAndReflectionProbeIndexCount;
                Assertions.Assert.IsTrue(lightAndReflectionProbeIndices > 0, "Pipelines configures reflection probes but per-object light and probe indices count is zero.");
                cullResults.FillLightAndReflectionProbeIndices(ShaderData.instance.GetReflectionProbeIndicesBuffer(lightAndReflectionProbeIndices));
            }

            perObjectReflectionProbeIndexMap.Dispose();
            return reflectionProbesCount;
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
