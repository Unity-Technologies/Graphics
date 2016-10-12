using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;

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


        const int MAX_LIGHTS = 10;
        const int MAX_SHADOWMAP_PER_LIGHTS = 6;
        const int MAX_DIRECTIONAL_SPLIT = 4;
        // Directional lights become spotlights at a far distance. This is the distance we pull back to set the spotlight origin.
        const float DIRECTIONAL_LIGHT_PULLBACK_DISTANCE = 10000.0f;

        [NonSerialized] private int m_nWarnedTooManyLights = 0;


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
            int nNumLightsIncludingTooMany = 0;

            int g_nNumLights = 0;

            Vector4[] g_vLightColor = new Vector4[MAX_LIGHTS];
            Vector4[] g_vLightPosition_flInvRadius = new Vector4[MAX_LIGHTS];
            Vector4[] g_vLightDirection = new Vector4[MAX_LIGHTS];
            Vector4[] g_vLightShadowIndex_vLightParams = new Vector4[MAX_LIGHTS];
            Vector4[] g_vLightFalloffParams = new Vector4[MAX_LIGHTS];
            Vector4[] g_vSpotLightInnerOuterConeCosines = new Vector4[MAX_LIGHTS];
            Matrix4x4[] g_matWorldToShadow = new Matrix4x4[MAX_LIGHTS * MAX_SHADOWMAP_PER_LIGHTS];
            Vector4[] g_vDirShadowSplitSpheres = new Vector4[MAX_DIRECTIONAL_SPLIT];

            for (int nLight = 0; nLight < visibleLights.Length; nLight++)
            {
                nNumLightsIncludingTooMany++;
                if (nNumLightsIncludingTooMany > MAX_LIGHTS)
                    continue;

                VisibleLight light = visibleLights[nLight];
                LightType lightType = light.lightType;
                Vector3 position = light.light.transform.position;
                Vector3 lightDir = light.light.transform.forward.normalized;
                AdditionalLightData additionalLightData = light.light.GetComponent<AdditionalLightData>();

                // Setup shadow data arrays
                bool hasShadows = shadow.GetShadowSliceCountLightIndex(nLight) != 0;

                if (lightType == LightType.Directional)
                {
                    g_vLightColor[g_nNumLights] = light.finalColor;
                    g_vLightPosition_flInvRadius[g_nNumLights] = new Vector4(
                            position.x - (lightDir.x * DIRECTIONAL_LIGHT_PULLBACK_DISTANCE),
                            position.y - (lightDir.y * DIRECTIONAL_LIGHT_PULLBACK_DISTANCE),
                            position.z - (lightDir.z * DIRECTIONAL_LIGHT_PULLBACK_DISTANCE),
                            -1.0f);
                    g_vLightDirection[g_nNumLights] = new Vector4(lightDir.x, lightDir.y, lightDir.z);
                    g_vLightShadowIndex_vLightParams[g_nNumLights] = new Vector4(0, 0, 1, 1);
                    g_vLightFalloffParams[g_nNumLights] = new Vector4(0.0f, 0.0f, float.MaxValue, (float)lightType);
                    g_vSpotLightInnerOuterConeCosines[g_nNumLights] = new Vector4(0.0f, -1.0f, 1.0f);

                    if (hasShadows)
                    {
                        for (int s = 0; s < MAX_DIRECTIONAL_SPLIT; ++s)
                        {
                            g_vDirShadowSplitSpheres[s] = shadow.directionalShadowSplitSphereSqr[s];
                        }
                    }
                }
                else if (lightType == LightType.Point)
                {
                    g_vLightColor[g_nNumLights] = light.finalColor;

                    g_vLightPosition_flInvRadius[g_nNumLights] = new Vector4(position.x, position.y, position.z, 1.0f / light.range);
                    g_vLightDirection[g_nNumLights] = new Vector4(0.0f, 0.0f, 0.0f);
                    g_vLightShadowIndex_vLightParams[g_nNumLights] = new Vector4(0, 0, 1, 1);
                    g_vLightFalloffParams[g_nNumLights] = new Vector4(1.0f, 0.0f, light.range * light.range, (float)lightType);
                    g_vSpotLightInnerOuterConeCosines[g_nNumLights] = new Vector4(0.0f, -1.0f, 1.0f);
                }
                else if (lightType == LightType.Spot)
                {
                    g_vLightColor[g_nNumLights] = light.finalColor;
                    g_vLightPosition_flInvRadius[g_nNumLights] = new Vector4(position.x, position.y, position.z, 1.0f / light.range);
                    g_vLightDirection[g_nNumLights] = new Vector4(lightDir.x, lightDir.y, lightDir.z);
                    g_vLightShadowIndex_vLightParams[g_nNumLights] = new Vector4(0, 0, 1, 1);
                    g_vLightFalloffParams[g_nNumLights] = new Vector4(1.0f, 0.0f, light.range * light.range, (float)lightType);

                    float flInnerConePercent = AdditionalLightData.GetInnerSpotPercent01(additionalLightData);
                    float spotAngle = light.light.spotAngle;
                    float flPhiDot = Mathf.Clamp(Mathf.Cos(spotAngle * 0.5f * Mathf.Deg2Rad), 0.0f, 1.0f);     // outer cone
                    float flThetaDot = Mathf.Clamp(Mathf.Cos(spotAngle * 0.5f * flInnerConePercent * Mathf.Deg2Rad), 0.0f, 1.0f);     // inner cone
                    g_vSpotLightInnerOuterConeCosines[g_nNumLights] = new Vector4(flThetaDot, flPhiDot, 1.0f / Mathf.Max(0.01f, flThetaDot - flPhiDot));
                }

                if (hasShadows)
                {
                    // Enable shadows
                    g_vLightShadowIndex_vLightParams[g_nNumLights].x = 1;
                    for (int s = 0; s < shadow.GetShadowSliceCountLightIndex(nLight); ++s)
                    {
                        int shadowSliceIndex = shadow.GetShadowSliceIndex(nLight, s);
                        g_matWorldToShadow[g_nNumLights * MAX_SHADOWMAP_PER_LIGHTS + s] = shadow.shadowSlices[shadowSliceIndex].shadowTransform.transpose;
                    }
                }

                g_nNumLights++;
            }

            // Warn if too many lights found
            if (nNumLightsIncludingTooMany > MAX_LIGHTS)
            {
                if (nNumLightsIncludingTooMany > m_nWarnedTooManyLights)
                {
                    Debug.LogError("ERROR! Found " + nNumLightsIncludingTooMany + " runtime lights! Valve renderer supports up to " + MAX_LIGHTS +
                        " active runtime lights at a time!\nDisabling " + (nNumLightsIncludingTooMany - MAX_LIGHTS) + " runtime light" +
                        ((nNumLightsIncludingTooMany - MAX_LIGHTS) > 1 ? "s" : "") + "!\n");
                }
                m_nWarnedTooManyLights = nNumLightsIncludingTooMany;
            }
            else
            {
                if (m_nWarnedTooManyLights > 0)
                {
                    m_nWarnedTooManyLights = 0;
                    Debug.Log("SUCCESS! Found " + nNumLightsIncludingTooMany + " runtime lights which is within the supported number of lights, " + MAX_LIGHTS + ".\n\n");
                }
            }

            // Send constants to shaders
            Shader.SetGlobalInt("g_nNumLights", g_nNumLights);

            // New method for Unity 5.4 to set arrays of constants
            Shader.SetGlobalVectorArray("g_vLightPosition_flInvRadius", g_vLightPosition_flInvRadius);
            Shader.SetGlobalVectorArray("g_vLightColor", g_vLightColor);
            Shader.SetGlobalVectorArray("g_vLightDirection", g_vLightDirection);
            Shader.SetGlobalVectorArray("g_vLightShadowIndex_vLightParams", g_vLightShadowIndex_vLightParams);
            Shader.SetGlobalVectorArray("g_vLightFalloffParams", g_vLightFalloffParams);
            Shader.SetGlobalVectorArray("g_vSpotLightInnerOuterConeCosines", g_vSpotLightInnerOuterConeCosines);
            Shader.SetGlobalMatrixArray("g_matWorldToShadow", g_matWorldToShadow);
            Shader.SetGlobalVectorArray("g_vDirShadowSplitSpheres", g_vDirShadowSplitSpheres);

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
            float flTexelEpsilonX = 1.0f / m_ShadowSettings.shadowAtlasWidth;
            float flTexelEpsilonY = 1.0f / m_ShadowSettings.shadowAtlasHeight;
            Vector4 g_vShadow3x3PCFTerms0 = new Vector4(20.0f / 267.0f, 33.0f / 267.0f, 55.0f / 267.0f, 0.0f);
            Vector4 g_vShadow3x3PCFTerms1 = new Vector4(flTexelEpsilonX, flTexelEpsilonY, -flTexelEpsilonX, -flTexelEpsilonY);
            Vector4 g_vShadow3x3PCFTerms2 = new Vector4(flTexelEpsilonX, flTexelEpsilonY, 0.0f, 0.0f);
            Vector4 g_vShadow3x3PCFTerms3 = new Vector4(-flTexelEpsilonX, -flTexelEpsilonY, 0.0f, 0.0f);

            Shader.SetGlobalVector("g_vShadow3x3PCFTerms0", g_vShadow3x3PCFTerms0);
            Shader.SetGlobalVector("g_vShadow3x3PCFTerms1", g_vShadow3x3PCFTerms1);
            Shader.SetGlobalVector("g_vShadow3x3PCFTerms2", g_vShadow3x3PCFTerms2);
            Shader.SetGlobalVector("g_vShadow3x3PCFTerms3", g_vShadow3x3PCFTerms3);
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

                DrawRendererSettings settings = new DrawRendererSettings(cullResults, camera, new ShaderPassName("ForwardBase"));
                settings.rendererConfiguration = RendererConfiguration.ConfigureOneLightProbePerRenderer | RendererConfiguration.ConfigureReflectionProbesProbePerRenderer;
                settings.sorting.sortOptions = SortOptions.SortByMaterialThenMesh;

                renderLoop.DrawRenderers(ref settings);
                renderLoop.Submit();
            }

            // Post effects
        }

        #if UNITY_EDITOR
        public override UnityEditor.SupportedRenderingFeatures GetSupportedRenderingFeatures()
        {
            var features = new UnityEditor.SupportedRenderingFeatures();

            features.reflectionProbe = UnityEditor.SupportedRenderingFeatures.ReflectionProbe.Rotation;

            return features;
        }

        #endif
    }
}
