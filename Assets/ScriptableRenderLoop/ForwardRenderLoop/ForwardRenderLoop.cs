using UnityEngine.Rendering;
using System;

namespace UnityEngine.ScriptableRenderLoop
{
    public class ForwardRenderLoop : ScriptableRenderLoop
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Renderloop/CreateForwardRenderLoop")]
        static void CreateForwardRenderLoop()
        {
            var instance = ScriptableObject.CreateInstance<ForwardRenderLoop>();
            UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/forwardrenderloop.asset");
        }

#endif

        [SerializeField]
        ShadowSettings m_ShadowSettings = ShadowSettings.Default;
        ShadowRenderPass m_ShadowPass;

        const int k_MaxLights = 10;
        const int k_MaxShadowmapPerLights = 6;
        const int k_MaxDirectionalSplit = 4;
        // Directional lights become spotlights at a far distance. This is the distance we pull back to set the spotlight origin.
        const float k_DirectionalLightPullbackDistance = 10000.0f;

        [NonSerialized] private int m_WarnedTooManyLights = 0;


        void OnEnable()
        {
            Rebuild();
        }

        void OnValidate()
        {
            Rebuild();
        }

        public override void Rebuild()
        {
            m_ShadowPass = new ShadowRenderPass(m_ShadowSettings);
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------
        void UpdateLightConstants(VisibleLight[] visibleLights, ref ShadowOutput shadow)
        {
            var numLightsIncludingTooMany = 0;

            var numLights = 0;

            var lightColor = new Vector4[k_MaxLights];
            var lightPosition_invRadius = new Vector4[k_MaxLights];
            var lightDirection = new Vector4[k_MaxLights];
            var lightShadowIndex_lightParams = new Vector4[k_MaxLights];
            var lightFalloffParams = new Vector4[k_MaxLights];
            var spotLightInnerOuterConeCosines = new Vector4[k_MaxLights];
            var matWorldToShadow = new Matrix4x4[k_MaxLights * k_MaxShadowmapPerLights];
            var dirShadowSplitSpheres = new Vector4[k_MaxDirectionalSplit];

            for (int nLight = 0; nLight < visibleLights.Length; nLight++)
            {
                numLightsIncludingTooMany++;
                if (numLightsIncludingTooMany > k_MaxLights)
                    continue;

                var light = visibleLights[nLight];
                var lightType = light.lightType;
                var position = light.light.transform.position;
                var lightDir = light.light.transform.forward.normalized;
                var additionalLightData = light.light.GetComponent<AdditionalLightData>();

                // Setup shadow data arrays
                var hasShadows = shadow.GetShadowSliceCountLightIndex(nLight) != 0;

                if (lightType == LightType.Directional)
                {
                    lightColor[numLights] = light.finalColor;
                    lightPosition_invRadius[numLights] = new Vector4(
                            position.x - (lightDir.x * k_DirectionalLightPullbackDistance),
                            position.y - (lightDir.y * k_DirectionalLightPullbackDistance),
                            position.z - (lightDir.z * k_DirectionalLightPullbackDistance),
                            -1.0f);
                    lightDirection[numLights] = new Vector4(lightDir.x, lightDir.y, lightDir.z);
                    lightShadowIndex_lightParams[numLights] = new Vector4(0, 0, 1, 1);
                    lightFalloffParams[numLights] = new Vector4(0.0f, 0.0f, float.MaxValue, (float)lightType);
                    spotLightInnerOuterConeCosines[numLights] = new Vector4(0.0f, -1.0f, 1.0f);

                    if (hasShadows)
                    {
                        for (int s = 0; s < k_MaxDirectionalSplit; ++s)
                        {
                            dirShadowSplitSpheres[s] = shadow.directionalShadowSplitSphereSqr[s];
                        }
                    }
                }
                else if (lightType == LightType.Point)
                {
                    lightColor[numLights] = light.finalColor;

                    lightPosition_invRadius[numLights] = new Vector4(position.x, position.y, position.z, 1.0f / light.range);
                    lightDirection[numLights] = new Vector4(0.0f, 0.0f, 0.0f);
                    lightShadowIndex_lightParams[numLights] = new Vector4(0, 0, 1, 1);
                    lightFalloffParams[numLights] = new Vector4(1.0f, 0.0f, light.range * light.range, (float)lightType);
                    spotLightInnerOuterConeCosines[numLights] = new Vector4(0.0f, -1.0f, 1.0f);
                }
                else if (lightType == LightType.Spot)
                {
                    lightColor[numLights] = light.finalColor;
                    lightPosition_invRadius[numLights] = new Vector4(position.x, position.y, position.z, 1.0f / light.range);
                    lightDirection[numLights] = new Vector4(lightDir.x, lightDir.y, lightDir.z);
                    lightShadowIndex_lightParams[numLights] = new Vector4(0, 0, 1, 1);
                    lightFalloffParams[numLights] = new Vector4(1.0f, 0.0f, light.range * light.range, (float)lightType);

                    var flInnerConePercent = AdditionalLightData.GetInnerSpotPercent01(additionalLightData);
                    var spotAngle = light.light.spotAngle;
                    var flPhiDot = Mathf.Clamp(Mathf.Cos(spotAngle * 0.5f * Mathf.Deg2Rad), 0.0f, 1.0f);     // outer cone
                    var flThetaDot = Mathf.Clamp(Mathf.Cos(spotAngle * 0.5f * flInnerConePercent * Mathf.Deg2Rad), 0.0f, 1.0f);     // inner cone
                    spotLightInnerOuterConeCosines[numLights] = new Vector4(flThetaDot, flPhiDot, 1.0f / Mathf.Max(0.01f, flThetaDot - flPhiDot));
                }

                if (hasShadows)
                {
                    // Enable shadows
                    lightShadowIndex_lightParams[numLights].x = 1;
                    for (int s = 0; s < shadow.GetShadowSliceCountLightIndex(nLight); ++s)
                    {
                        var shadowSliceIndex = shadow.GetShadowSliceIndex(nLight, s);
                        matWorldToShadow[numLights * k_MaxShadowmapPerLights + s] = shadow.shadowSlices[shadowSliceIndex].shadowTransform.transpose;
                    }
                }

                numLights++;
            }

            // Warn if too many lights found
            if (numLightsIncludingTooMany > k_MaxLights)
            {
                if (numLightsIncludingTooMany > m_WarnedTooManyLights)
                {
                    Debug.LogError("ERROR! Found " + numLightsIncludingTooMany + " runtime lights! Valve renderer supports up to " + k_MaxLights +
                        " active runtime lights at a time!\nDisabling " + (numLightsIncludingTooMany - k_MaxLights) + " runtime light" +
                        ((numLightsIncludingTooMany - k_MaxLights) > 1 ? "s" : "") + "!\n");
                }
                m_WarnedTooManyLights = numLightsIncludingTooMany;
            }
            else
            {
                if (m_WarnedTooManyLights > 0)
                {
                    m_WarnedTooManyLights = 0;
                    Debug.Log("SUCCESS! Found " + numLightsIncludingTooMany + " runtime lights which is within the supported number of lights, " + k_MaxLights + ".\n\n");
                }
            }

            // Send constants to shaders
            Shader.SetGlobalInt("g_nNumLights", numLights);

            // New method for Unity 5.4 to set arrays of constants
            Shader.SetGlobalVectorArray("g_vLightPosition_flInvRadius", lightPosition_invRadius);
            Shader.SetGlobalVectorArray("g_vLightColor", lightColor);
            Shader.SetGlobalVectorArray("g_vLightDirection", lightDirection);
            Shader.SetGlobalVectorArray("g_vLightShadowIndex_vLightParams", lightShadowIndex_lightParams);
            Shader.SetGlobalVectorArray("g_vLightFalloffParams", lightFalloffParams);
            Shader.SetGlobalVectorArray("g_vSpotLightInnerOuterConeCosines", spotLightInnerOuterConeCosines);
            Shader.SetGlobalMatrixArray("g_matWorldToShadow", matWorldToShadow);
            Shader.SetGlobalVectorArray("g_vDirShadowSplitSpheres", dirShadowSplitSpheres);

            // Time
            #if (UNITY_EDITOR)
            {
                Shader.SetGlobalFloat("g_flTime", Time.realtimeSinceStartup);
                //Debug.Log( "Time " + Time.realtimeSinceStartup );
            }
            #else
            {
                Shader.SetGlobalFloat("g_flTime", Time.timeSinceLevelLoad);
                //Debug.Log( "Time " + Time.timeSinceLevelLoad );
            }
            #endif

            // PCF 3x3 Shadows
            var texelEpsilonX = 1.0f / m_ShadowSettings.shadowAtlasWidth;
            var texelEpsilonY = 1.0f / m_ShadowSettings.shadowAtlasHeight;
            var shadow3x3PCFTerms0 = new Vector4(20.0f / 267.0f, 33.0f / 267.0f, 55.0f / 267.0f, 0.0f);
            var shadow3x3PCFTerms1 = new Vector4(texelEpsilonX, texelEpsilonY, -texelEpsilonX, -texelEpsilonY);
            var shadow3x3PCFTerms2 = new Vector4(texelEpsilonX, texelEpsilonY, 0.0f, 0.0f);
            var shadow3x3PCFTerms3 = new Vector4(-texelEpsilonX, -texelEpsilonY, 0.0f, 0.0f);

            Shader.SetGlobalVector("g_vShadow3x3PCFTerms0", shadow3x3PCFTerms0);
            Shader.SetGlobalVector("g_vShadow3x3PCFTerms1", shadow3x3PCFTerms1);
            Shader.SetGlobalVector("g_vShadow3x3PCFTerms2", shadow3x3PCFTerms2);
            Shader.SetGlobalVector("g_vShadow3x3PCFTerms3", shadow3x3PCFTerms3);
        }

        public override void Render(Camera[] cameras, RenderLoop renderLoop)
        {
            foreach (var camera in cameras)
            {
                CullResults cullResults;
                CullingParameters cullingParams;
                if (!CullResults.GetCullingParameters(camera, out cullingParams))
                    continue;

                m_ShadowPass.UpdateCullingParameters(ref cullingParams);

                cullResults = CullResults.Cull(ref cullingParams, renderLoop);

                ShadowOutput shadows;
                m_ShadowPass.Render(renderLoop, cullResults, out shadows);

                renderLoop.SetupCameraProperties(camera);

                UpdateLightConstants(cullResults.visibleLights, ref shadows);

                var settings = new DrawRendererSettings(cullResults, camera, new ShaderPassName("ForwardBase"));
                settings.rendererConfiguration = RendererConfiguration.PerObjectLightProbe | RendererConfiguration.PerObjectReflectionProbes;
                settings.sorting.sortOptions = SortOptions.SortByMaterialThenMesh;

                renderLoop.DrawRenderers(ref settings);
                renderLoop.Submit();
            }

            // Post effects
        }

        #if UNITY_EDITOR
        public override UnityEditor.SupportedRenderingFeatures GetSupportedRenderingFeatures()
        {
            var features = new UnityEditor.SupportedRenderingFeatures
            {
                reflectionProbe = UnityEditor.SupportedRenderingFeatures.ReflectionProbe.Rotation
            };

            return features;
        }

        #endif
    }
}
