using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public enum SkyResolution
    {
        SkyResolution128 = 128,
        SkyResolution256 = 256,
        SkyResolution512 = 512,
        SkyResolution1024 = 1024,
        SkyResolution2048 = 2048,
        SkyResolution4096 = 4096
    }

    public enum EnvironementUpdateMode
    {
        OnChanged = 0,
        OnDemand,
        Realtime
    }

    public class BuiltinSkyParameters
    {
        public Matrix4x4                pixelCoordToViewDirMatrix;
        public Matrix4x4                invViewProjMatrix;
        public Vector3                  cameraPosWS;
        public Vector4                  screenSize;
        public CommandBuffer            commandBuffer;
        public Light                    sunLight;
        public RTHandleSystem.RTHandle  colorBuffer;
        public RTHandleSystem.RTHandle  depthBuffer;
        public HDCamera                 hdCamera;

        public DebugDisplaySettings debugSettings;

        public static RenderTargetIdentifier nullRT = -1;
    }

    public class SkyManager
    {
        Material                m_StandardSkyboxMaterial; // This is the Unity standard skybox material. Used to pass the correct cubemap to Enlighten.
        Material                m_BlitCubemapMaterial;
        Material                m_OpaqueAtmScatteringMaterial;

        SphericalHarmonicsL2    m_BlackAmbientProbe = new SphericalHarmonicsL2();

        bool                    m_UpdateRequired = false;

        // This is the sky used for rendering in the main view.
        // It will also be used for lighting if no lighting override sky is setup.
        //      Ambient Probe: Only used if Ambient Mode is set to dynamic in the Visual Environment component. Updated according to the Update Mode parameter.
        //      Sky Reflection Probe : Always used and updated according to the Update Mode parameter.
        SkyUpdateContext m_VisualSky = new SkyUpdateContext();
        // This is optional and is used only to compute ambient probe and sky reflection
        // Ambient Probe and Sky Reflection follow the same rule as visual sky
        SkyUpdateContext m_LightingOverrideSky = new SkyUpdateContext();
        // The sky rendering contexts holds the render textures used by the sky system.
        SkyRenderingContext m_SkyRenderingContext;

        // Sky used for static lighting. It will be used for ambient lighting if Ambient Mode is set to Static (even when realtime GI is enabled)
        // It will also be used for lightmap and light probe baking
        SkyUpdateContext m_StaticLightingSky = new SkyUpdateContext();
        // We need to have a separate rendering context for the static lighting sky because we have to keep it alive regardless of the state of visual/override sky
        SkyRenderingContext m_StaticLightingSkyRenderingContext;

        // This interpolation volume stack is used to interpolate the lighting override separately from the visual sky.
        // If a sky setting is present in this volume then it will be used for lighting override.
        VolumeStack m_LightingOverrideVolumeStack;
        LayerMask   m_LightingOverrideLayerMask = -1;

        static Dictionary<int, Type> m_SkyTypesDict = null;
        public static Dictionary<int, Type> skyTypesDict { get { if (m_SkyTypesDict == null) UpdateSkyTypes(); return m_SkyTypesDict; } }

        public Texture skyReflection { get { return m_SkyRenderingContext.reflectionTexture; } }

        // This list will hold the static lighting sky that should be used for baking ambient probe.
        // In practice we will always use the last one registered but we use a list to be able to roll back to the previous one once the user deletes the superfluous instances.
        private static List<StaticLightingSky> m_StaticLightingSkies = new List<StaticLightingSky>();

#if UNITY_EDITOR
        // For Preview windows we want to have a 'fixed' sky, so we can display chrome metal and have always the same look
        ProceduralSky m_DefaultPreviewSky;
#endif

        public SkyManager()
        {
#if UNITY_EDITOR
    #if UNITY_2019_2_OR_NEWER
            UnityEditor.Lightmapping.bakeStarted += OnBakeStarted;
    #else
            UnityEditor.Lightmapping.started += OnBakeStarted;
    #endif
#endif
        }

        ~SkyManager()
        {
#if UNITY_EDITOR
    #if UNITY_2019_2_OR_NEWER
            UnityEditor.Lightmapping.bakeStarted -= OnBakeStarted;
    #else
            UnityEditor.Lightmapping.started -= OnBakeStarted;
    #endif
#endif
        }

        SkySettings GetSkySetting(VolumeStack stack)
        {
            var visualEnv = stack.GetComponent<VisualEnvironment>();
            int skyID = visualEnv.skyType;
            Type skyType;
            if (skyTypesDict.TryGetValue(skyID, out skyType))
            {
                return (SkySettings)stack.GetComponent(skyType);
            }
            else
            {
                return null;
            }
        }

        static void UpdateSkyTypes()
        {
            if (m_SkyTypesDict == null)
            {
                m_SkyTypesDict = new Dictionary<int, Type>();

                var skyTypes = CoreUtils.GetAllAssemblyTypes().Where(t => t.IsSubclassOf(typeof(SkySettings)) && !t.IsAbstract);
                foreach (Type skyType in skyTypes)
                {
                    var uniqueIDs = skyType.GetCustomAttributes(typeof(SkyUniqueID), false);
                    if (uniqueIDs.Length == 0)
                    {
                        Debug.LogWarningFormat("Missing attribute SkyUniqueID on class {0}. Class won't be registered as an available sky.", skyType);
                    }
                    else
                    {
                        int uniqueID = ((SkyUniqueID)uniqueIDs[0]).uniqueID;
                        if (uniqueID == 0)
                        {
                            Debug.LogWarningFormat("0 is a reserved SkyUniqueID and is used in class {0}. Class won't be registered as an available sky.", skyType);
                            continue;
                        }

                        Type value;
                        if (m_SkyTypesDict.TryGetValue(uniqueID, out value))
                        {
                            Debug.LogWarningFormat("SkyUniqueID {0} used in class {1} is already used in class {2}. Class won't be registered as an available sky.", uniqueID, skyType, value);
                            continue;
                        }

                        m_SkyTypesDict.Add(uniqueID, skyType);
                    }
                }
            }
        }

        public void UpdateCurrentSkySettings(HDCamera hdCamera)
        {
            m_VisualSky.skySettings = GetSkySetting(VolumeManager.instance.stack);

#if UNITY_EDITOR
            if (HDUtils.IsRegularPreviewCamera(hdCamera.camera))
            {
                m_VisualSky.skySettings = GetDefaultPreviewSkyInstance();
            }
#endif

            // Update needs to happen before testing if the component is active other internal data structure are not properly updated yet.
            VolumeManager.instance.Update(m_LightingOverrideVolumeStack, hdCamera.volumeAnchor, m_LightingOverrideLayerMask);
            if (VolumeManager.instance.IsComponentActiveInMask<VisualEnvironment>(m_LightingOverrideLayerMask))
            {
                SkySettings newSkyOverride = GetSkySetting(m_LightingOverrideVolumeStack);
                if (m_LightingOverrideSky.skySettings != null && newSkyOverride == null)
                {
                    // When we switch from override to no override, we need to make sure that the visual sky will actually be properly re-rendered.
                    // Resetting the visual sky hash will ensure that.
                    m_VisualSky.skyParametersHash = -1;
                }
                m_LightingOverrideSky.skySettings = newSkyOverride;
            }
            else
            {
                m_LightingOverrideSky.skySettings = null;
            }
        }

        // Sets the global MIP-mapped cubemap '_SkyTexture' in the shader.
        // The texture being set is the sky (environment) map pre-convolved with GGX.
        public void SetGlobalSkyTexture(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._SkyTexture, skyReflection);
            float mipCount = Mathf.Clamp(Mathf.Log((float)skyReflection.width, 2.0f) + 1, 0.0f, 6.0f);
            cmd.SetGlobalFloat(HDShaderIDs._SkyTextureMipCount, mipCount);
        }

#if UNITY_EDITOR
        ProceduralSky GetDefaultPreviewSkyInstance()
        {
            if (m_DefaultPreviewSky == null)
            {
                m_DefaultPreviewSky = ScriptableObject.CreateInstance<ProceduralSky>();
            }

            return m_DefaultPreviewSky;
        }

#endif

        public void Build(HDRenderPipelineAsset hdAsset, IBLFilterBSDF[] iblFilterBSDFArray)
        {
            m_SkyRenderingContext = new SkyRenderingContext(iblFilterBSDFArray, (int)hdAsset.currentPlatformRenderPipelineSettings.lightLoopSettings.skyReflectionSize, true);

            m_StandardSkyboxMaterial = CoreUtils.CreateEngineMaterial(hdAsset.renderPipelineResources.shaders.skyboxCubemapPS);
            m_BlitCubemapMaterial = CoreUtils.CreateEngineMaterial(hdAsset.renderPipelineResources.shaders.blitCubemapPS);
            m_OpaqueAtmScatteringMaterial = CoreUtils.CreateEngineMaterial(hdAsset.renderPipelineResources.shaders.opaqueAtmosphericScatteringPS);

            m_LightingOverrideVolumeStack = VolumeManager.instance.CreateStack();
            m_LightingOverrideLayerMask = hdAsset.currentPlatformRenderPipelineSettings.lightLoopSettings.skyLightingOverrideLayerMask;
            m_StaticLightingSkyRenderingContext = new SkyRenderingContext(iblFilterBSDFArray, (int)hdAsset.currentPlatformRenderPipelineSettings.lightLoopSettings.skyReflectionSize, false);
        }

        public void Cleanup()
        {
            CoreUtils.Destroy(m_StandardSkyboxMaterial);
            CoreUtils.Destroy(m_BlitCubemapMaterial);
            CoreUtils.Destroy(m_OpaqueAtmScatteringMaterial);

            m_VisualSky.Cleanup();
            m_LightingOverrideSky.Cleanup();

            m_SkyRenderingContext.Cleanup();

#if UNITY_EDITOR
            CoreUtils.Destroy(m_DefaultPreviewSky);

            m_StaticLightingSky.Cleanup();
            m_StaticLightingSkyRenderingContext.Cleanup();
#endif
        }

        public bool IsLightingSkyValid()
        {
            return m_VisualSky.IsValid() || m_LightingOverrideSky.IsValid();
        }

        public bool IsVisualSkyValid()
        {
            return m_VisualSky.IsValid();
        }

        Texture GetStaticLightingTexture()
        {
            StaticLightingSky staticLightingSky = GetStaticLightingSky();
            if (staticLightingSky != null)
            {
                return m_StaticLightingSky.IsValid() ? (Texture)m_StaticLightingSkyRenderingContext.cubemapRT : CoreUtils.blackCubeTexture;
            }
            else
            {
                return CoreUtils.blackCubeTexture;
            }
        }

        SphericalHarmonicsL2 GetStaticLightingAmbientProbe()
        {
            StaticLightingSky staticLightingSky = GetStaticLightingSky();
            if (staticLightingSky != null)
            {
                return m_StaticLightingSky.IsValid() ? m_StaticLightingSkyRenderingContext.ambientProbe : m_BlackAmbientProbe;
            }
            else
            {
                return m_BlackAmbientProbe;
            }
        }

        void BlitCubemap(CommandBuffer cmd, Cubemap source, RenderTexture dest)
        {
            var propertyBlock = new MaterialPropertyBlock();

            for (int i = 0; i < 6; ++i)
            {
                CoreUtils.SetRenderTarget(cmd, dest, ClearFlag.None, 0, (CubemapFace)i);
                propertyBlock.SetTexture("_MainTex", source);
                propertyBlock.SetFloat("_faceIndex", (float)i);
                cmd.DrawProcedural(Matrix4x4.identity, m_BlitCubemapMaterial, 0, MeshTopology.Triangles, 3, 1, propertyBlock);
            }

            // Generate mipmap for our cubemap
            Debug.Assert(dest.autoGenerateMips == false);
            cmd.GenerateMips(dest);
        }

        public void RequestEnvironmentUpdate()
        {
            m_UpdateRequired = true;
        }

        public void UpdateEnvironment(HDCamera hdCamera, Light sunLight, CommandBuffer cmd)
        {
            // WORKAROUND for building the player.
            // When building the player, for some reason we end up in a state where frameCount is not updated but all currently setup shader texture are reset to null
            // resulting in a rendering error (compute shader property not bound) that makes the player building fails...
            // So we just check if the texture is bound here so that we can setup a pink one to avoid the error without breaking half the world.
            if (Shader.GetGlobalTexture(HDShaderIDs._SkyTexture) == null)
                cmd.SetGlobalTexture(HDShaderIDs._SkyTexture, CoreUtils.magentaCubeTexture);

            bool isRegularPreview = HDUtils.IsRegularPreviewCamera(hdCamera.camera);

            SkyAmbientMode ambientMode = VolumeManager.instance.stack.GetComponent<VisualEnvironment>().skyAmbientMode;
            SkyUpdateContext currentSky = m_LightingOverrideSky.IsValid() ? m_LightingOverrideSky : m_VisualSky;

            // Preview should never use dynamic ambient or they will conflict with main view (async readback of sky texture will update ambient probe for main view one frame later)
            if (isRegularPreview)
                ambientMode = SkyAmbientMode.Static;

            m_SkyRenderingContext.UpdateEnvironment(currentSky, hdCamera, sunLight, m_UpdateRequired, ambientMode == SkyAmbientMode.Dynamic, cmd);
            StaticLightingSky staticLightingSky = GetStaticLightingSky();
            // We don't want to update the static sky during preview because it contains custom lights that may change the result.
            // The consequence is that previews will use main scene static lighting but we consider this to be acceptable.
            if (staticLightingSky != null && !isRegularPreview)
            {
                m_StaticLightingSky.skySettings = staticLightingSky.skySettings;
                m_StaticLightingSkyRenderingContext.UpdateEnvironment(m_StaticLightingSky, hdCamera, sunLight, false, true, cmd);
            }

            bool useRealtimeGI = true;
#if UNITY_EDITOR
            useRealtimeGI = UnityEditor.Lightmapping.realtimeGI;
#endif
            // Working around GI current system
            // When using baked lighting, setting up the ambient probe should be sufficient => We only need to update RenderSettings.ambientProbe with either the static or visual sky ambient probe (computed from GPU)
            // When using real time GI. Enlighten will pull sky information from Skybox material. So in order for dynamic GI to work, we update the skybox material texture and then set the ambient mode to SkyBox
            // Problem: We can't check at runtime if realtime GI is enabled so we need to take extra care (see useRealtimeGI usage below)
            RenderSettings.ambientMode = AmbientMode.Custom; // Needed to specify ourselves the ambient probe (this will update internal ambient probe data passed to shaders)
            if (ambientMode == SkyAmbientMode.Static)
            {
                RenderSettings.ambientProbe = GetStaticLightingAmbientProbe();
                m_StandardSkyboxMaterial.SetTexture("_Tex", GetStaticLightingTexture());
            }
            else
            {
                RenderSettings.ambientProbe = m_SkyRenderingContext.ambientProbe;
                // Workaround in the editor:
                // When in the editor, if we use baked lighting, we need to setup the skybox material with the static lighting texture otherwise when baking, the dynamic texture will be used
                if (useRealtimeGI)
                {
                    m_StandardSkyboxMaterial.SetTexture("_Tex", currentSky.IsValid() ? (Texture)m_SkyRenderingContext.cubemapRT : CoreUtils.blackCubeTexture);
                }
                else
                {
                    m_StandardSkyboxMaterial.SetTexture("_Tex", GetStaticLightingTexture());
                }
            }

            // This is only needed if we use realtime GI otherwise enlighten won't get the right sky information
            RenderSettings.skybox = m_StandardSkyboxMaterial; // Setup this material as the default to be use in RenderSettings
            RenderSettings.ambientIntensity = 1.0f;
            RenderSettings.ambientMode = AmbientMode.Skybox; // Force skybox for our HDRI
            RenderSettings.reflectionIntensity = 1.0f;
            RenderSettings.customReflection = null;

            m_UpdateRequired = false;

            SetGlobalSkyTexture(cmd);
            if (IsLightingSkyValid())
            {
                cmd.SetGlobalInt(HDShaderIDs._EnvLightSkyEnabled, 1);
            }
            else
            {
                cmd.SetGlobalInt(HDShaderIDs._EnvLightSkyEnabled, 0);
            }
        }

        public void RenderSky(HDCamera camera, Light sunLight, RTHandleSystem.RTHandle colorBuffer, RTHandleSystem.RTHandle depthBuffer, DebugDisplaySettings debugSettings, CommandBuffer cmd)
        {
            m_SkyRenderingContext.RenderSky(m_VisualSky, camera, sunLight, colorBuffer, depthBuffer, debugSettings, cmd);
        }

        public void RenderOpaqueAtmosphericScattering(CommandBuffer cmd, HDCamera hdCamera, RTHandleSystem.RTHandle colorBuffer, RTHandleSystem.RTHandle depthBuffer,
                                                      Matrix4x4 pixelCoordToViewDirWS, bool isMSAA)
        {
            using (new ProfilingSample(cmd, "Opaque Atmospheric Scattering"))
            {
                var propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, pixelCoordToViewDirWS);
                HDUtils.DrawFullScreen(cmd, hdCamera, m_OpaqueAtmScatteringMaterial, colorBuffer, depthBuffer, propertyBlock, isMSAA? 1 : 0);
            }
        }

        static public StaticLightingSky GetStaticLightingSky()
        {
            if (m_StaticLightingSkies.Count == 0)
                return null;
            else
                return m_StaticLightingSkies[m_StaticLightingSkies.Count - 1];
        }

        static public void RegisterStaticLightingSky(StaticLightingSky staticLightingSky)
        {
            if (!m_StaticLightingSkies.Contains(staticLightingSky))
            {
                if (m_StaticLightingSkies.Count != 0)
                {
                    Debug.LogWarning("One Static Lighting Sky component was already set for baking, only the latest one will be used.");
                }
                m_StaticLightingSkies.Add(staticLightingSky);
            }
        }

        static public void UnRegisterStaticLightingSky(StaticLightingSky staticLightingSky)
        {
            m_StaticLightingSkies.Remove(staticLightingSky);
        }

        public Texture2D ExportSkyToTexture()
        {
            if (!m_VisualSky.IsValid())
            {
                Debug.LogError("Cannot export sky to a texture, no Sky is setup.");
                return null;
            }

            RenderTexture skyCubemap = m_SkyRenderingContext.cubemapRT;

            int resolution = skyCubemap.width;

            var tempRT = new RenderTexture(resolution * 6, resolution, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear)
            {
                dimension = TextureDimension.Tex2D,
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = FilterMode.Trilinear
            };
            tempRT.Create();

            var temp = new Texture2D(resolution * 6, resolution, TextureFormat.RGBAFloat, false);
            var result = new Texture2D(resolution * 6, resolution, TextureFormat.RGBAFloat, false);

            // Note: We need to invert in Y the cubemap faces because the current sky cubemap is inverted (because it's a RT)
            // So to invert it again so that it's a proper cubemap image we need to do it in several steps because ReadPixels does not have scale parameters:
            // - Convert the cubemap into a 2D texture
            // - Blit and invert it to a temporary target.
            // - Read this target again into the result texture.
            int offset = 0;
            for (int i = 0; i < 6; ++i)
            {
                UnityEngine.Graphics.SetRenderTarget(skyCubemap, 0, (CubemapFace)i);
                temp.ReadPixels(new Rect(0, 0, resolution, resolution), offset, 0);
                temp.Apply();
                offset += resolution;
            }

            // Flip texture.
            UnityEngine.Graphics.Blit(temp, tempRT, new Vector2(1.0f, -1.0f), new Vector2(0.0f, 0.0f));

            result.ReadPixels(new Rect(0, 0, resolution * 6, resolution), 0, 0);
            result.Apply();

            UnityEngine.Graphics.SetRenderTarget(null);
            CoreUtils.Destroy(temp);
            CoreUtils.Destroy(tempRT);

            return result;
        }

#if UNITY_EDITOR
        void OnBakeStarted()
        {
            var hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdrp == null)
                return;

            // Happens sometime in the tests.
            if (m_StandardSkyboxMaterial == null)
                m_StandardSkyboxMaterial = CoreUtils.CreateEngineMaterial(hdrp.asset.renderPipelineResources.shaders.skyboxCubemapPS);

            // At the start of baking we need to update the GI system with the static lighting sky in order for lightmaps and probes to be baked with it.
            var staticLightingSky = GetStaticLightingSky();
            if (staticLightingSky != null)
            {
                m_StandardSkyboxMaterial.SetTexture("_Tex", m_StaticLightingSky.IsValid() ? (Texture)m_StaticLightingSkyRenderingContext.cubemapRT : CoreUtils.blackCubeTexture);

            }
            else
            {
                m_StandardSkyboxMaterial.SetTexture("_Tex", CoreUtils.blackCubeTexture);
            }

            RenderSettings.skybox = m_StandardSkyboxMaterial; // Setup this material as the default to be use in RenderSettings
            RenderSettings.ambientIntensity = 1.0f;
            RenderSettings.ambientMode = AmbientMode.Skybox; // Force skybox for our HDRI
            RenderSettings.reflectionIntensity = 1.0f;
            RenderSettings.customReflection = null;

            DynamicGI.UpdateEnvironment();
        }
#endif
    }
}
