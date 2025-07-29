using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Provides methods for implementing lens flares in a render pipeline.
    /// </summary>
    /// <remarks>
    /// The High Definition Render Pipeline (HDRP) and Universal Render Pipeline (URP) use this class for their lens flare implementation. The class supports both screen space lens flares and quad-based lens flares.
    /// You must call the methods of `LensFlareCommonSRP` at several places inside the Scriptable Render Pipeline (SRP). At minimum, you must call the <see cref="Initialize"/> method.
    /// You can use any of these methods in a <see cref="LensFlareComponentSRP"/> `Monobehaviour` script.
    /// <see cref="Dispose"/>
    /// <see cref="DoLensFlareDataDrivenCommon(UnityEngine.Material,UnityEngine.Camera,UnityEngine.Rect,UnityEngine.Experimental.Rendering.XRPass,int,float,float,bool,float,float,bool,UnityEngine.Vector3,UnityEngine.Matrix4x4,UnityEngine.Rendering.UnsafeCommandBuffer,bool,bool,UnityEngine.Texture,UnityEngine.Texture,UnityEngine.Rendering.RenderTargetIdentifier,System.Func{UnityEngine.Light,UnityEngine.Camera,UnityEngine.Vector3,float},bool)"/>.
    ///
    /// Note that only one `LensFlareCommonSRP` can be alive at any point. To call members of this class, use the <see cref="Instance"/> property.
    /// </remarks>
    public sealed class LensFlareCommonSRP
    {
        private static LensFlareCommonSRP m_Instance = null;
        private static readonly object m_Padlock = new object();
        /// <summary>
        /// Class describing internal information stored to describe a shown LensFlare
        /// </summary>
        internal class LensFlareCompInfo
        {
            /// <summary>
            /// Index used to compute Occlusion in a fixed order
            /// </summary>
            internal int index;

            /// <summary>
            /// Component used
            /// </summary>
            internal LensFlareComponentSRP comp;

            internal LensFlareCompInfo(int idx, LensFlareComponentSRP cmp)
            {
                index = idx;
                comp = cmp;
            }
        }

        private static List<LensFlareCompInfo> m_Data = new List<LensFlareCompInfo>();
        private static List<int> m_AvailableIndicies = new List<int>();

        /// <summary>
        /// Defines how many lens flare with occlusion are supported in the view at any time.
        /// </summary>
        public static int maxLensFlareWithOcclusion = 128;


        /// <summary>
        /// With TAA Occlusion jitter depth, thought frame on HDRP.
        /// So we do a "unanimity vote" for occlusion thought 'maxLensFlareWithOcclusionTemporalSample' frame
        /// Important to keep this value maximum of 8
        /// If this value change that could implies an implementation modification on:
        /// com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/LensFlareMergeOcclusionDataDriven.compute
        /// </summary>
        public static int maxLensFlareWithOcclusionTemporalSample = 8;

        /// <summary>
        /// Set to 1 to enable temporal sample merge.
        /// Set to 0 to disable temporal sample merge (must support 16 bit textures, and the occlusion merge must be written in the last texel (vertical) of the lens flare texture.
        /// </summary>
        public static int mergeNeeded = 1;

        /// <summary>
        /// occlusion texture either provided or created automatically by the SRP for lens flare.
        /// Texture width is the max number of lens flares that have occlusion (x axis the lens flare index).
        /// y axis is the number of samples (maxLensFlareWithOcclusionTemporalSample) plus the number of merge results.
        /// Merge results must be done by the SRP and stored in the [(lens flareIndex), (maxLensFlareWithOcclusionTemporalSample + 1)] coordinate.
        /// Note: It's not supported on OpenGL3 and OpenGLCore
        /// </summary>
        public static RTHandle occlusionRT = null;

        private static int frameIdx = 0;

        internal static readonly int _FlareOcclusionPermutation = Shader.PropertyToID("_FlareOcclusionPermutation");
        internal static readonly int _FlareOcclusionRemapTex = Shader.PropertyToID("_FlareOcclusionRemapTex");
        internal static readonly int _FlareOcclusionTex = Shader.PropertyToID("_FlareOcclusionTex");
        internal static readonly int _FlareOcclusionIndex = Shader.PropertyToID("_FlareOcclusionIndex");
        internal static readonly int _FlareCloudOpacity = Shader.PropertyToID("_FlareCloudOpacity");
        internal static readonly int _FlareSunOcclusionTex = Shader.PropertyToID("_FlareSunOcclusionTex");
        internal static readonly int _FlareTex = Shader.PropertyToID("_FlareTex");
        internal static readonly int _FlareColorValue = Shader.PropertyToID("_FlareColorValue");
        internal static readonly int _FlareData0 = Shader.PropertyToID("_FlareData0");
        internal static readonly int _FlareData1 = Shader.PropertyToID("_FlareData1");
        internal static readonly int _FlareData2 = Shader.PropertyToID("_FlareData2");
        internal static readonly int _FlareData3 = Shader.PropertyToID("_FlareData3");
        internal static readonly int _FlareData4 = Shader.PropertyToID("_FlareData4");
        internal static readonly int _FlareData5 = Shader.PropertyToID("_FlareData5");
        internal static readonly int _FlareData6 = Shader.PropertyToID("_FlareData6");
        internal static readonly int _FlareRadialTint = Shader.PropertyToID("_FlareRadialTint");

        internal static readonly int _ViewId = Shader.PropertyToID("_ViewId");

        internal static readonly int _LensFlareScreenSpaceBloomMipTexture = Shader.PropertyToID("_LensFlareScreenSpaceBloomMipTexture");
        internal static readonly int _LensFlareScreenSpaceResultTexture = Shader.PropertyToID("_LensFlareScreenSpaceResultTexture");
        internal static readonly int _LensFlareScreenSpaceSpectralLut = Shader.PropertyToID("_LensFlareScreenSpaceSpectralLut");
        internal static readonly int _LensFlareScreenSpaceStreakTex = Shader.PropertyToID("_LensFlareScreenSpaceStreakTex");
        internal static readonly int _LensFlareScreenSpaceMipLevel = Shader.PropertyToID("_LensFlareScreenSpaceMipLevel");
        internal static readonly int _LensFlareScreenSpaceTintColor = Shader.PropertyToID("_LensFlareScreenSpaceTintColor");
        internal static readonly int _LensFlareScreenSpaceParams1 = Shader.PropertyToID("_LensFlareScreenSpaceParams1");
        internal static readonly int _LensFlareScreenSpaceParams2 = Shader.PropertyToID("_LensFlareScreenSpaceParams2");
        internal static readonly int _LensFlareScreenSpaceParams3 = Shader.PropertyToID("_LensFlareScreenSpaceParams3");
        internal static readonly int _LensFlareScreenSpaceParams4 = Shader.PropertyToID("_LensFlareScreenSpaceParams4");
        internal static readonly int _LensFlareScreenSpaceParams5 = Shader.PropertyToID("_LensFlareScreenSpaceParams5");

        private LensFlareCommonSRP()
        {
        }

        static readonly bool s_SupportsLensFlare16bitsFormat = SystemInfo.IsFormatSupported(GraphicsFormat.R16_SFloat, GraphicsFormatUsage.Render);
        static readonly bool s_SupportsLensFlare32bitsFormat = SystemInfo.IsFormatSupported(GraphicsFormat.R32_SFloat, GraphicsFormatUsage.Render);
        static readonly bool s_SupportsLensFlare16bitsFormatWithLoadStore = SystemInfo.IsFormatSupported(GraphicsFormat.R16_SFloat, GraphicsFormatUsage.LoadStore);
        static readonly bool s_SupportsLensFlare32bitsFormatWithLoadStore = SystemInfo.IsFormatSupported(GraphicsFormat.R32_SFloat, GraphicsFormatUsage.LoadStore);

        // UUM-91313: Some Android Vulkan devices (Adreno 540) don't support R16_SFloat with GraphicsFormatUsage.LoadStore,
        // which is required when creating a render texture with enableRandomWrite flag. Random writes are only needed by
        // the merge step (compute shader using the texture as UAV), so we enable the flag only if merging is enabled.
        static bool requireOcclusionRTRandomWrite => mergeNeeded > 0;

        static bool CheckOcclusionBasedOnDeviceType()
        {
#if UNITY_SERVER
            return false;
#else
            return SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null &&
                   SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES3 &&
                   SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLCore &&
                   SystemInfo.graphicsDeviceType != GraphicsDeviceType.WebGPU;
#endif
        }

        /// <summary>
        /// Check if we can create OcclusionRT texture to be used as render target
        /// </summary>
        /// <returns>Returns true if a supported format is found</returns>
        public static bool IsOcclusionRTCompatible()
        {
            if (requireOcclusionRTRandomWrite)
            {
                return CheckOcclusionBasedOnDeviceType() &&
                       (s_SupportsLensFlare16bitsFormatWithLoadStore || s_SupportsLensFlare32bitsFormatWithLoadStore);
            }
            return CheckOcclusionBasedOnDeviceType() &&
                    (s_SupportsLensFlare16bitsFormat || s_SupportsLensFlare32bitsFormat);
        }

        static GraphicsFormat GetOcclusionRTFormat()
        {
            // SystemInfo.graphicsDeviceType == {GraphicsDeviceType.Direct3D12, GraphicsDeviceType.GameCoreXboxSeries, GraphicsDeviceType.XboxOneD3D12, GraphicsDeviceType.PlayStation5, ...}
            if (requireOcclusionRTRandomWrite ? s_SupportsLensFlare16bitsFormatWithLoadStore : s_SupportsLensFlare16bitsFormat)
                return GraphicsFormat.R16_SFloat;
            else
                // Needed a R32_SFloat for Metal or/and DirectX < 11.3
                return GraphicsFormat.R32_SFloat;
        }

        /// <summary>
        /// Initializes the lens flares. You must call this method.
        /// </summary>
        /// <remarks>
        /// You usually call `Initialize` in the <see cref="RenderPipeline"/> constructor.
        /// </remarks>
        static public void Initialize()
        {
            frameIdx = 0;
            if (IsOcclusionRTCompatible())
            {
                // The height of occlusion texture is:
                //      - '1': when no temporal accumulation
                //      - 'maxLensFlareWithOcclusionTemporalSample + 1': for temporal accumulation, useful when TAA enabled
                if (occlusionRT == null)
                {
                    occlusionRT = RTHandles.Alloc(
                        width: maxLensFlareWithOcclusion,
                        height: Mathf.Max(mergeNeeded * (maxLensFlareWithOcclusionTemporalSample + 1), 1),
                        format: GetOcclusionRTFormat(),
                        slices: TextureXR.slices,
                        enableRandomWrite: requireOcclusionRTRandomWrite,
                        dimension: TextureDimension.Tex2DArray);
                }
            }
        }

        /// <summary>
        /// Releases all internal textures.
        /// </summary>
        /// <remarks>
        /// Usually, `Dispose` is called in the <see cref="RenderPipeline.Dispose(bool)"/> function.
        /// </remarks>
        static public void Dispose()
        {
            if (IsOcclusionRTCompatible())
            {
                if (occlusionRT != null)
                {
                    RTHandles.Release(occlusionRT);
                    occlusionRT = null;
                }
            }
        }

        /// <summary>
        /// Current unique instance.
        /// </summary>
        /// <remarks>
        /// Use this property to call other members of this class and make sure that only one lens flare system is running at any time.
        /// </remarks>
        public static LensFlareCommonSRP Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    lock (m_Padlock)
                    {
                        if (m_Instance == null)
                        {
                            m_Instance = new LensFlareCommonSRP();
                        }
                    }
                }
                return m_Instance;
            }
        }

        private System.Collections.Generic.List<LensFlareCompInfo> Data { get { return LensFlareCommonSRP.m_Data; } }

        /// <summary>
        /// Checks if at least one lens flare has been added to the pool.
        /// </summary>
        /// <remarks>
        /// You can use this method to check if there are any lens flares to render before rendering the lens flares.
        /// </remarks>
        /// <example>
        /// if (!LensFlareCommonSRP.Instance.IsEmpty())
        /// {
        ///     LensFlareCommonSRP.DoLensFlareDataDrivenCommon(...);
        /// }
        /// </example>
        /// <returns>`true` if no lens flare were added</returns>
        public bool IsEmpty()
        {
            return Data.Count == 0;
        }

        int GetNextAvailableIndex()
        {
            if (m_AvailableIndicies.Count == 0)
                return m_Data.Count;
            else
            {
                int nextIndex = m_AvailableIndicies[m_AvailableIndicies.Count - 1];
                m_AvailableIndicies.RemoveAt(m_AvailableIndicies.Count - 1);
                return nextIndex;
            }
        }

        /// <summary>
        /// Adds a new lens flare component for rendering.
        /// </summary>
        /// <remarks>
        /// When <see cref="LensFlareComponentSRP"/> is used, this method is called automatically when the lens flare is enabled.
        /// You don't need to call this function unless you're manually removing lens flare data using <see cref="RemoveData"/>.
        /// </remarks>
        /// <param name="newData">The new data added</param>
        public void AddData(LensFlareComponentSRP newData)
        {
            Debug.Assert(Instance == this, "LensFlareCommonSRP can have only one instance");

            if (!m_Data.Exists(x => x.comp == newData))
            {
                m_Data.Add(new LensFlareCompInfo(GetNextAvailableIndex(), newData));
            }
        }

        /// <summary>
        /// Removes a lens flare data from rendering.
        /// </summary>
        /// <remarks>
        /// When <see cref="LensFlareComponentSRP"/> is used, this method is called automatically when the lens flare is disabled.
        /// </remarks>
        /// <param name="data">The data which exist in the pool</param>
        public void RemoveData(LensFlareComponentSRP data)
        {
            Debug.Assert(Instance == this, "LensFlareCommonSRP can have only one instance");

            LensFlareCompInfo info = m_Data.Find(x => x.comp == data);
            if (info != null)
            {
                int newIndex = info.index;
                m_Data.Remove(info);
                m_AvailableIndicies.Add(newIndex);
                if (m_Data.Count == 0)
                    m_AvailableIndicies.Clear();
            }
        }


        /// <summary>
        /// Obtains the attenuation for a point light.
        /// </summary>
        /// <remarks>
        /// This method can be used to help compute the light attenuation to pass to <see cref="DoLensFlareDataDrivenCommon(UnityEngine.Material,UnityEngine.Camera,UnityEngine.Rect,UnityEngine.Experimental.Rendering.XRPass,int,float,float,bool,float,float,bool,UnityEngine.Vector3,UnityEngine.Matrix4x4,UnityEngine.Rendering.UnsafeCommandBuffer,bool,bool,UnityEngine.Texture,UnityEngine.Texture,UnityEngine.Rendering.RenderTargetIdentifier,System.Func{UnityEngine.Light,UnityEngine.Camera,UnityEngine.Vector3,float},bool)"/>
        /// </remarks>
        /// <example>
        /// <para>
        /// To handle more than one light type, write a dedicated function to compute the attenuation using these helpers
        /// </para>
        /// <code>
        /// static float GetLensFlareLightAttenuation(Light light, Camera cam, Vector3 wo)
        /// {
        ///     switch (light.type)
        ///     {
        ///         case LightType.Directional:
        ///             return LensFlareCommonSRP.ShapeAttenuationDirLight(light.transform.forward, cam.transform.forward);
        ///         case LightType.Point:
        ///             // Do nothing point are omnidirectional for the lens flare
        ///             return LensFlareCommonSRP.ShapeAttenuationPointLight();
        ///         case LightType.Spot:
        ///             float innerSpotPercent01 = 1;
        ///             return LensFlareCommonSRP.ShapeAttenuationSpotConeLight(light.transform.forward, wo, light.spotAngle, light.innerSpotAngle / 180.0f);
        ///         case LightType.Pyramid:
        ///             return LensFlareCommonSRP.ShapeAttenuationSpotPyramidLight(light.transform.forward, wo);
        ///         case LightType.Box:
        ///             return LensFlareCommonSRP.ShapeAttenuationSpotBoxLight(light.transform.forward, wo);
        ///         case LightType.Rectangle:
        ///             return LensFlareCommonSRP.ShapeAttenuationAreaRectangleLight(light.transform.forward, wo);
        ///         case LightType.Tube:
        ///             float shapeWidth = 1; // Get this data from an external source if our render pipeline supports tube lights.
        ///             return LensFlareCommonSRP.ShapeAttenuationAreaTubeLight(light.transform.position, light.transform.right, shapeWidth, cam);
        ///         case LightType.Disc:
        ///             return LensFlareCommonSRP.ShapeAttenuationAreaDiscLight(light.transform.forward, wo);
        ///         default: throw new Exception($"GetLensFlareLightAttenuation HDLightType Unknown {typeof(LightType)}: {light.type}");
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <returns>Attenuation Factor</returns>
        static public float ShapeAttenuationPointLight()
        {
            return 1.0f;
        }

        /// <summary>
        /// Obtains the attenuation for a directional light.
        /// </summary>
        /// <param name="forward">Forward Vector of Directional Light</param>
        /// <param name="wo">Vector pointing to the eye</param>
        /// <returns>Attenuation Factor</returns>
        /// <remarks>
        /// This method can be used to help compute the light attenuation to pass to <see cref="DoLensFlareDataDrivenCommon(UnityEngine.Material,UnityEngine.Camera,UnityEngine.Rect,UnityEngine.Experimental.Rendering.XRPass,int,float,float,bool,float,float,bool,UnityEngine.Vector3,UnityEngine.Matrix4x4,UnityEngine.Rendering.UnsafeCommandBuffer,bool,bool,UnityEngine.Texture,UnityEngine.Texture,UnityEngine.Rendering.RenderTargetIdentifier,System.Func{UnityEngine.Light,UnityEngine.Camera,UnityEngine.Vector3,float},bool)"/>
        /// </remarks>
        /// <example>
        /// <para>
        /// To handle more than one light type, write a dedicated function to compute the attenuation using these helpers
        /// </para>
        /// <code>
        /// static float GetLensFlareLightAttenuation(Light light, Camera cam, Vector3 wo)
        /// {
        ///     switch (light.type)
        ///     {
        ///         case LightType.Directional:
        ///             return LensFlareCommonSRP.ShapeAttenuationDirLight(light.transform.forward, cam.transform.forward);
        ///         case LightType.Point:
        ///             // Do nothing point are omnidirectional for the lens flare
        ///             return LensFlareCommonSRP.ShapeAttenuationPointLight();
        ///         case LightType.Spot:
        ///             float innerSpotPercent01 = 1;
        ///             return LensFlareCommonSRP.ShapeAttenuationSpotConeLight(light.transform.forward, wo, light.spotAngle, light.innerSpotAngle / 180.0f);
        ///         case LightType.Pyramid:
        ///             return LensFlareCommonSRP.ShapeAttenuationSpotPyramidLight(light.transform.forward, wo);
        ///         case LightType.Box:
        ///             return LensFlareCommonSRP.ShapeAttenuationSpotBoxLight(light.transform.forward, wo);
        ///         case LightType.Rectangle:
        ///             return LensFlareCommonSRP.ShapeAttenuationAreaRectangleLight(light.transform.forward, wo);
        ///         case LightType.Tube:
        ///             float shapeWidth = 1; // Get this data from an external source if our render pipeline supports tube lights.
        ///             return LensFlareCommonSRP.ShapeAttenuationAreaTubeLight(light.transform.position, light.transform.right, shapeWidth, cam);
        ///         case LightType.Disc:
        ///             return LensFlareCommonSRP.ShapeAttenuationAreaDiscLight(light.transform.forward, wo);
        ///         default: throw new Exception($"GetLensFlareLightAttenuation HDLightType Unknown {typeof(LightType)}: {light.type}");
        ///     }
        /// }
        /// </code>
        /// </example>
        static public float ShapeAttenuationDirLight(Vector3 forward, Vector3 wo)
        {
            return Mathf.Max(Vector3.Dot(-forward, wo), 0.0f);
        }

        /// <summary>
        /// Obtains the attenuation for a cone spot light.
        /// </summary>
        /// <param name="forward">Forward Vector of Directional Light</param>
        /// <param name="wo">Vector pointing to the eye</param>
        /// <param name="spotAngle">The angle of the light's spotlight cone in degrees.</param>
        /// <param name="innerSpotPercent01">Get the inner spot radius between 0 and 1.</param>
        /// <returns>Attenuation Factor</returns>
        /// <remarks>
        /// This method can be used to help compute the light attenuation to pass to <see cref="DoLensFlareDataDrivenCommon(UnityEngine.Material,UnityEngine.Camera,UnityEngine.Rect,UnityEngine.Experimental.Rendering.XRPass,int,float,float,bool,float,float,bool,UnityEngine.Vector3,UnityEngine.Matrix4x4,UnityEngine.Rendering.UnsafeCommandBuffer,bool,bool,UnityEngine.Texture,UnityEngine.Texture,UnityEngine.Rendering.RenderTargetIdentifier,System.Func{UnityEngine.Light,UnityEngine.Camera,UnityEngine.Vector3,float},bool)"/>
        /// </remarks>
        /// <example>
        /// <para>
        /// To handle more than one light type, write a dedicated function to compute the attenuation using these helpers
        /// </para>
        /// <code>
        /// static float GetLensFlareLightAttenuation(Light light, Camera cam, Vector3 wo)
        /// {
        ///     switch (light.type)
        ///     {
        ///         case LightType.Directional:
        ///             return LensFlareCommonSRP.ShapeAttenuationDirLight(light.transform.forward, cam.transform.forward);
        ///         case LightType.Point:
        ///             // Do nothing point are omnidirectional for the lens flare
        ///             return LensFlareCommonSRP.ShapeAttenuationPointLight();
        ///         case LightType.Spot:
        ///             float innerSpotPercent01 = 1;
        ///             return LensFlareCommonSRP.ShapeAttenuationSpotConeLight(light.transform.forward, wo, light.spotAngle, light.innerSpotAngle / 180.0f);
        ///         case LightType.Pyramid:
        ///             return LensFlareCommonSRP.ShapeAttenuationSpotPyramidLight(light.transform.forward, wo);
        ///         case LightType.Box:
        ///             return LensFlareCommonSRP.ShapeAttenuationSpotBoxLight(light.transform.forward, wo);
        ///         case LightType.Rectangle:
        ///             return LensFlareCommonSRP.ShapeAttenuationAreaRectangleLight(light.transform.forward, wo);
        ///         case LightType.Tube:
        ///             float shapeWidth = 1; // Get this data from an external source if our render pipeline supports tube lights.
        ///             return LensFlareCommonSRP.ShapeAttenuationAreaTubeLight(light.transform.position, light.transform.right, shapeWidth, cam);
        ///         case LightType.Disc:
        ///             return LensFlareCommonSRP.ShapeAttenuationAreaDiscLight(light.transform.forward, wo);
        ///         default: throw new Exception($"GetLensFlareLightAttenuation HDLightType Unknown {typeof(LightType)}: {light.type}");
        ///     }
        /// }
        /// </code>
        /// </example>
        static public float ShapeAttenuationSpotConeLight(Vector3 forward, Vector3 wo, float spotAngle, float innerSpotPercent01)
        {
            float outerDot = Mathf.Max(Mathf.Cos(0.5f * spotAngle * Mathf.Deg2Rad), 0.0f);
            float innerDot = Mathf.Max(Mathf.Cos(0.5f * spotAngle * Mathf.Deg2Rad * innerSpotPercent01), 0.0f);
            float dot = Mathf.Max(Vector3.Dot(forward, wo), 0.0f);
            return Mathf.Clamp01((dot - outerDot) / (innerDot - outerDot));
        }

        /// <summary>
        /// Obtains the attenuation for a box spot light.
        /// </summary>
        /// <param name="forward">Forward Vector of Directional Light</param>
        /// <param name="wo">Vector pointing to the eye</param>
        /// <returns>Attenuation Factor</returns>
        /// <remarks>
        /// This method can be used to help compute the light attenuation to pass to <see cref="DoLensFlareDataDrivenCommon(UnityEngine.Material,UnityEngine.Camera,UnityEngine.Rect,UnityEngine.Experimental.Rendering.XRPass,int,float,float,bool,float,float,bool,UnityEngine.Vector3,UnityEngine.Matrix4x4,UnityEngine.Rendering.UnsafeCommandBuffer,bool,bool,UnityEngine.Texture,UnityEngine.Texture,UnityEngine.Rendering.RenderTargetIdentifier,System.Func{UnityEngine.Light,UnityEngine.Camera,UnityEngine.Vector3,float},bool)"/>
        /// </remarks>
        /// <example>
        /// <para>
        /// To handle more than one light type, write a dedicated function to compute the attenuation using these helpers
        /// </para>
        /// <code>
        /// static float GetLensFlareLightAttenuation(Light light, Camera cam, Vector3 wo)
        /// {
        ///     switch (light.type)
        ///     {
        ///         case LightType.Directional:
        ///             return LensFlareCommonSRP.ShapeAttenuationDirLight(light.transform.forward, cam.transform.forward);
        ///         case LightType.Point:
        ///             // Do nothing point are omnidirectional for the lens flare
        ///             return LensFlareCommonSRP.ShapeAttenuationPointLight();
        ///         case LightType.Spot:
        ///             float innerSpotPercent01 = 1;
        ///             return LensFlareCommonSRP.ShapeAttenuationSpotConeLight(light.transform.forward, wo, light.spotAngle, light.innerSpotAngle / 180.0f);
        ///         case LightType.Pyramid:
        ///             return LensFlareCommonSRP.ShapeAttenuationSpotPyramidLight(light.transform.forward, wo);
        ///         case LightType.Box:
        ///             return LensFlareCommonSRP.ShapeAttenuationSpotBoxLight(light.transform.forward, wo);
        ///         case LightType.Rectangle:
        ///             return LensFlareCommonSRP.ShapeAttenuationAreaRectangleLight(light.transform.forward, wo);
        ///         case LightType.Tube:
        ///             float shapeWidth = 1; // Get this data from an external source if our render pipeline supports tube lights.
        ///             return LensFlareCommonSRP.ShapeAttenuationAreaTubeLight(light.transform.position, light.transform.right, shapeWidth, cam);
        ///         case LightType.Disc:
        ///             return LensFlareCommonSRP.ShapeAttenuationAreaDiscLight(light.transform.forward, wo);
        ///         default: throw new Exception($"GetLensFlareLightAttenuation HDLightType Unknown {typeof(LightType)}: {light.type}");
        ///     }
        /// }
        /// </code>
        /// </example>
        static public float ShapeAttenuationSpotBoxLight(Vector3 forward, Vector3 wo)
        {
            return Mathf.Max(Mathf.Sign(Vector3.Dot(forward, wo)), 0.0f);
        }

        /// <summary>
        /// Obtains the attenuation for a pyramid spot light.
        /// </summary>
        /// <param name="forward">Forward Vector of Directional Light</param>
        /// <param name="wo">Vector pointing to the eye</param>
        /// <returns>Attenuation Factor</returns>
        /// <remarks>
        /// This method can be used to help compute the light attenuation to pass to <see cref="DoLensFlareDataDrivenCommon(UnityEngine.Material,UnityEngine.Camera,UnityEngine.Rect,UnityEngine.Experimental.Rendering.XRPass,int,float,float,bool,float,float,bool,UnityEngine.Vector3,UnityEngine.Matrix4x4,UnityEngine.Rendering.UnsafeCommandBuffer,bool,bool,UnityEngine.Texture,UnityEngine.Texture,UnityEngine.Rendering.RenderTargetIdentifier,System.Func{UnityEngine.Light,UnityEngine.Camera,UnityEngine.Vector3,float},bool)"/>
        /// </remarks>
        /// <example>
        /// <para>
        /// To handle more than one light type, write a dedicated function to compute the attenuation using these helpers
        /// </para>
        /// <code>
        /// static float GetLensFlareLightAttenuation(Light light, Camera cam, Vector3 wo)
        /// {
        ///     switch (light.type)
        ///     {
        ///         case LightType.Directional:
        ///             return LensFlareCommonSRP.ShapeAttenuationDirLight(light.transform.forward, cam.transform.forward);
        ///         case LightType.Point:
        ///             // Do nothing point are omnidirectional for the lens flare
        ///             return LensFlareCommonSRP.ShapeAttenuationPointLight();
        ///         case LightType.Spot:
        ///             float innerSpotPercent01 = 1;
        ///             return LensFlareCommonSRP.ShapeAttenuationSpotConeLight(light.transform.forward, wo, light.spotAngle, light.innerSpotAngle / 180.0f);
        ///         case LightType.Pyramid:
        ///             return LensFlareCommonSRP.ShapeAttenuationSpotPyramidLight(light.transform.forward, wo);
        ///         case LightType.Box:
        ///             return LensFlareCommonSRP.ShapeAttenuationSpotBoxLight(light.transform.forward, wo);
        ///         case LightType.Rectangle:
        ///             return LensFlareCommonSRP.ShapeAttenuationAreaRectangleLight(light.transform.forward, wo);
        ///         case LightType.Tube:
        ///             float shapeWidth = 1; // Get this data from an external source if our render pipeline supports tube lights.
        ///             return LensFlareCommonSRP.ShapeAttenuationAreaTubeLight(light.transform.position, light.transform.right, shapeWidth, cam);
        ///         case LightType.Disc:
        ///             return LensFlareCommonSRP.ShapeAttenuationAreaDiscLight(light.transform.forward, wo);
        ///         default: throw new Exception($"GetLensFlareLightAttenuation HDLightType Unknown {typeof(LightType)}: {light.type}");
        ///     }
        /// }
        /// </code>
        /// </example>
        static public float ShapeAttenuationSpotPyramidLight(Vector3 forward, Vector3 wo)
        {
            return ShapeAttenuationSpotBoxLight(forward, wo);
        }

        /// <summary>
        /// Obtains the attenuation for a tube light.
        /// </summary>
        /// <param name="lightPositionWS">World Space position of the Light</param>
        /// <param name="lightSide">Vector pointing to the side (right or left) or the light</param>
        /// <param name="lightWidth">Width (half extent) of the tube light</param>
        /// <param name="cam">Camera rendering the Tube Light</param>
        /// <returns>Attenuation Factor</returns>
        /// <remarks>
        /// This method can be used to help compute the light attenuation to pass to <see cref="DoLensFlareDataDrivenCommon(UnityEngine.Material,UnityEngine.Camera,UnityEngine.Rect,UnityEngine.Experimental.Rendering.XRPass,int,float,float,bool,float,float,bool,UnityEngine.Vector3,UnityEngine.Matrix4x4,UnityEngine.Rendering.UnsafeCommandBuffer,bool,bool,UnityEngine.Texture,UnityEngine.Texture,UnityEngine.Rendering.RenderTargetIdentifier,System.Func{UnityEngine.Light,UnityEngine.Camera,UnityEngine.Vector3,float},bool)"/>
        /// </remarks>
        /// <example>
        /// <para>
        /// To handle more than one light type, write a dedicated function to compute the attenuation using these helpers
        /// </para>
        /// <code>
        /// static float GetLensFlareLightAttenuation(Light light, Camera cam, Vector3 wo)
        /// {
        ///     switch (light.type)
        ///     {
        ///         case LightType.Directional:
        ///             return LensFlareCommonSRP.ShapeAttenuationDirLight(light.transform.forward, cam.transform.forward);
        ///         case LightType.Point:
        ///             // Do nothing point are omnidirectional for the lens flare
        ///             return LensFlareCommonSRP.ShapeAttenuationPointLight();
        ///         case LightType.Spot:
        ///             float innerSpotPercent01 = 1;
        ///             return LensFlareCommonSRP.ShapeAttenuationSpotConeLight(light.transform.forward, wo, light.spotAngle, light.innerSpotAngle / 180.0f);
        ///         case LightType.Pyramid:
        ///             return LensFlareCommonSRP.ShapeAttenuationSpotPyramidLight(light.transform.forward, wo);
        ///         case LightType.Box:
        ///             return LensFlareCommonSRP.ShapeAttenuationSpotBoxLight(light.transform.forward, wo);
        ///         case LightType.Rectangle:
        ///             return LensFlareCommonSRP.ShapeAttenuationAreaRectangleLight(light.transform.forward, wo);
        ///         case LightType.Tube:
        ///             float shapeWidth = 1; // Get this data from an external source if our render pipeline supports tube lights.
        ///             return LensFlareCommonSRP.ShapeAttenuationAreaTubeLight(light.transform.position, light.transform.right, shapeWidth, cam);
        ///         case LightType.Disc:
        ///             return LensFlareCommonSRP.ShapeAttenuationAreaDiscLight(light.transform.forward, wo);
        ///         default: throw new Exception($"GetLensFlareLightAttenuation HDLightType Unknown {typeof(LightType)}: {light.type}");
        ///     }
        /// }
        /// </code>
        /// </example>
        public static float ShapeAttenuationAreaTubeLight(Vector3 lightPositionWS, Vector3 lightSide, float lightWidth, Camera cam)
        {
            // Ref: https://hal.archives-ouvertes.fr/hal-02155101/document
            // Listing 1.6. Analytic line-diffuse integration.
            float Fpo(float d, float l)
            {
                return l / (d * (d * d + l * l)) + Mathf.Atan(l / d) / (d * d);
            }

            float Fwt(float d, float l)
            {
                return l * l / (d * (d * d + l * l));
            }

            var cameraTransform = cam.transform;
            Vector3 p1Global = lightPositionWS + 0.5f * lightWidth * lightSide;
            Vector3 p2Global = lightPositionWS - 0.5f * lightWidth * lightSide;
            Vector3 p1Front = lightPositionWS + 0.5f * lightWidth * cameraTransform.right;
            Vector3 p2Front = lightPositionWS - 0.5f * lightWidth * cameraTransform.right;

            Vector3 p1World = cameraTransform.InverseTransformPoint(p1Global);
            Vector3 p2World = cameraTransform.InverseTransformPoint(p2Global);
            Vector3 p1WorldFront = cameraTransform.InverseTransformPoint(p1Front);
            Vector3 p2WorldFront = cameraTransform.InverseTransformPoint(p2Front);

            float DiffLineIntegral(Vector3 p1, Vector3 p2)
            {
                float diffIntegral;
                // tangent
                Vector3 wt = (p2 - p1).normalized;
                // clamping
                if (p1.z <= 0.0 && p2.z <= 0.0)
                {
                    diffIntegral = 0.0f;
                }
                else
                {
                    if (p1.z < 0.0)
                        p1 = (p1 * p2.z - p2 * p1.z) / (+p2.z - p1.z);
                    if (p2.z < 0.0)
                        p2 = (-p1 * p2.z + p2 * p1.z) / (-p2.z + p1.z);
                    // parameterization
                    float l1 = Vector3.Dot(p1, wt);
                    float l2 = Vector3.Dot(p2, wt);
                    // shading point orthonormal projection on the line
                    Vector3 po = p1 - l1 * wt;
                    // distance to line
                    float d = po.magnitude;
                    // integral
                    float integral = (Fpo(d, l2) - Fpo(d, l1)) * po.z + (Fwt(d, l2) - Fwt(d, l1)) * wt.z;
                    diffIntegral = integral / Mathf.PI;
                }

                return diffIntegral;
            }

            float frontModulation = DiffLineIntegral(p1WorldFront, p2WorldFront);
            float worldModulation = DiffLineIntegral(p1World, p2World);

            return frontModulation > 0.0f ? worldModulation / frontModulation : 1.0f;
        }

        static float ShapeAttenuateForwardLight(Vector3 forward, Vector3 wo)
        {
            return Mathf.Max(Vector3.Dot(forward, wo), 0.0f);
        }

        /// <summary>
        /// Obtains the attenuation for a rectangle light.
        /// </summary>
        /// <param name="forward">Forward Vector of Directional Light</param>
        /// <param name="wo">Vector pointing to the eye</param>
        /// <returns>Attenuation Factor</returns>
        /// <remarks>
        /// This method can be used to help compute the light attenuation to pass to <see cref="DoLensFlareDataDrivenCommon(UnityEngine.Material,UnityEngine.Camera,UnityEngine.Rect,UnityEngine.Experimental.Rendering.XRPass,int,float,float,bool,float,float,bool,UnityEngine.Vector3,UnityEngine.Matrix4x4,UnityEngine.Rendering.UnsafeCommandBuffer,bool,bool,UnityEngine.Texture,UnityEngine.Texture,UnityEngine.Rendering.RenderTargetIdentifier,System.Func{UnityEngine.Light,UnityEngine.Camera,UnityEngine.Vector3,float},bool)"/>
        /// </remarks>
        /// <example>
        /// <para>
        /// To handle more than one light type, write a dedicated function to compute the attenuation using these helpers
        /// </para>
        /// <code>
        /// static float GetLensFlareLightAttenuation(Light light, Camera cam, Vector3 wo)
        /// {
        ///     switch (light.type)
        ///     {
        ///         case LightType.Directional:
        ///             return LensFlareCommonSRP.ShapeAttenuationDirLight(light.transform.forward, cam.transform.forward);
        ///         case LightType.Point:
        ///             // Do nothing point are omnidirectional for the lens flare
        ///             return LensFlareCommonSRP.ShapeAttenuationPointLight();
        ///         case LightType.Spot:
        ///             float innerSpotPercent01 = 1;
        ///             return LensFlareCommonSRP.ShapeAttenuationSpotConeLight(light.transform.forward, wo, light.spotAngle, light.innerSpotAngle / 180.0f);
        ///         case LightType.Pyramid:
        ///             return LensFlareCommonSRP.ShapeAttenuationSpotPyramidLight(light.transform.forward, wo);
        ///         case LightType.Box:
        ///             return LensFlareCommonSRP.ShapeAttenuationSpotBoxLight(light.transform.forward, wo);
        ///         case LightType.Rectangle:
        ///             return LensFlareCommonSRP.ShapeAttenuationAreaRectangleLight(light.transform.forward, wo);
        ///         case LightType.Tube:
        ///             float shapeWidth = 1; // Get this data from an external source if our render pipeline supports tube lights.
        ///             return LensFlareCommonSRP.ShapeAttenuationAreaTubeLight(light.transform.position, light.transform.right, shapeWidth, cam);
        ///         case LightType.Disc:
        ///             return LensFlareCommonSRP.ShapeAttenuationAreaDiscLight(light.transform.forward, wo);
        ///         default: throw new Exception($"GetLensFlareLightAttenuation HDLightType Unknown {typeof(LightType)}: {light.type}");
        ///     }
        /// }
        /// </code>
        /// </example>
        static public float ShapeAttenuationAreaRectangleLight(Vector3 forward, Vector3 wo)
        {
            return ShapeAttenuateForwardLight(forward, wo);
        }

        /// <summary>
        /// Obtains the attenuation for a disc light.
        /// </summary>
        /// <param name="forward">Forward Vector of Directional Light</param>
        /// <param name="wo">Vector pointing to the eye</param>
        /// <returns>Attenuation Factor</returns>
        /// <remarks>
        /// This method can be used to help compute the light attenuation to pass to <see cref="DoLensFlareDataDrivenCommon(UnityEngine.Material,UnityEngine.Camera,UnityEngine.Rect,UnityEngine.Experimental.Rendering.XRPass,int,float,float,bool,float,float,bool,UnityEngine.Vector3,UnityEngine.Matrix4x4,UnityEngine.Rendering.UnsafeCommandBuffer,bool,bool,UnityEngine.Texture,UnityEngine.Texture,UnityEngine.Rendering.RenderTargetIdentifier,System.Func{UnityEngine.Light,UnityEngine.Camera,UnityEngine.Vector3,float},bool)"/>
        /// </remarks>
        /// <example>
        /// <para>
        /// To handle more than one light type, write a dedicated function to compute the attenuation using these helpers
        /// </para>
        /// <code>
        /// static float GetLensFlareLightAttenuation(Light light, Camera cam, Vector3 wo)
        /// {
        ///     switch (light.type)
        ///     {
        ///         case LightType.Directional:
        ///             return LensFlareCommonSRP.ShapeAttenuationDirLight(light.transform.forward, cam.transform.forward);
        ///         case LightType.Point:
        ///             // Do nothing point are omnidirectional for the lens flare
        ///             return LensFlareCommonSRP.ShapeAttenuationPointLight();
        ///         case LightType.Spot:
        ///             float innerSpotPercent01 = 1;
        ///             return LensFlareCommonSRP.ShapeAttenuationSpotConeLight(light.transform.forward, wo, light.spotAngle, light.innerSpotAngle / 180.0f);
        ///         case LightType.Pyramid:
        ///             return LensFlareCommonSRP.ShapeAttenuationSpotPyramidLight(light.transform.forward, wo);
        ///         case LightType.Box:
        ///             return LensFlareCommonSRP.ShapeAttenuationSpotBoxLight(light.transform.forward, wo);
        ///         case LightType.Rectangle:
        ///             return LensFlareCommonSRP.ShapeAttenuationAreaRectangleLight(light.transform.forward, wo);
        ///         case LightType.Tube:
        ///             float shapeWidth = 1; // Get this data from an external source if our render pipeline supports tube lights.
        ///             return LensFlareCommonSRP.ShapeAttenuationAreaTubeLight(light.transform.position, light.transform.right, shapeWidth, cam);
        ///         case LightType.Disc:
        ///             return LensFlareCommonSRP.ShapeAttenuationAreaDiscLight(light.transform.forward, wo);
        ///         default: throw new Exception($"GetLensFlareLightAttenuation HDLightType Unknown {typeof(LightType)}: {light.type}");
        ///     }
        /// }
        /// </code>
        /// </example>
        static public float ShapeAttenuationAreaDiscLight(Vector3 forward, Vector3 wo)
        {
            return ShapeAttenuateForwardLight(forward, wo);
        }

        static bool IsLensFlareSRPHidden(Camera cam, LensFlareComponentSRP comp, LensFlareDataSRP data)
        {
            if (!comp.enabled ||
                !comp.gameObject.activeSelf ||
                !comp.gameObject.activeInHierarchy ||
                data == null ||
                data.elements == null ||
                data.elements.Length == 0 ||
                comp.intensity <= 0.0f ||
                ((cam.cullingMask & (1 << comp.gameObject.layer)) == 0))
                return true;

            return false;
        }

        static Vector4 InternalGetFlareData0(
            Vector2 screenPos,
            Vector2 translationScale,
            Vector2 rayOff0,
            Vector2 vLocalScreenRatio,
            float angleDeg,
            float position,
            float angularOffset,
            Vector2 positionOffset,
            bool autoRotate
        )
        {
            if (!SystemInfo.graphicsUVStartsAtTop)
            {
                angleDeg *= -1;
                positionOffset.y *= -1;
            }

            float globalCos0 = Mathf.Cos(-angularOffset * Mathf.Deg2Rad);
            float globalSin0 = Mathf.Sin(-angularOffset * Mathf.Deg2Rad);

            Vector2 rayOff = -translationScale * (screenPos + screenPos * (position - 1.0f));
            rayOff = new Vector2(globalCos0 * rayOff.x - globalSin0 * rayOff.y,
                globalSin0 * rayOff.x + globalCos0 * rayOff.y);

            float rotation = angleDeg;

            rotation += 180.0f;
            if (autoRotate)
            {
                Vector2 pos = (rayOff.normalized * vLocalScreenRatio) * translationScale;

                rotation += -Mathf.Rad2Deg * Mathf.Atan2(pos.y, pos.x);
            }
            rotation *= Mathf.Deg2Rad;
            float localCos0 = Mathf.Cos(-rotation);
            float localSin0 = Mathf.Sin(-rotation);

            return new Vector4(localCos0, localSin0, positionOffset.x + rayOff0.x * translationScale.x, -positionOffset.y + rayOff0.y * translationScale.y);
        }

        /// <summary>
        /// Computes the internal parameters needed to render a single flare.
        /// </summary>
        /// <param name="screenPos">The screen position of the flare.</param>
        /// <param name="translationScale">The scale of translation applied to the flare.</param>
        /// <param name="rayOff0">The base offset for the flare ray.</param>
        /// <param name="vLocalScreenRatio">The ratio of the flare's local screen size.</param>
        /// <param name="angleDeg">The base angle of rotation for the flare.</param>
        /// <param name="position">The position along the flare's radial line, relative to the source, where 1.0 represents the edge of the screen.</param>
        /// <param name="angularOffset">Angular offset applied to the flare's position.</param>
        /// <param name="positionOffset">The offset from the flare's calculated position.</param>
        /// <param name="autoRotate">Flag to enable automatic rotation based on flare's position.</param>
        /// <returns>A Vector4 object representing the shader parameters _FlareData0.</returns>
        [Obsolete("This is now deprecated as a public API. Call ComputeOcclusion() or DoLensFlareDataDrivenCommon() instead. #from(6000.3)")]
        static public Vector4 GetFlareData0(Vector2 screenPos, Vector2 translationScale, Vector2 rayOff0, Vector2 vLocalScreenRatio, float angleDeg, float position, float angularOffset, Vector2 positionOffset, bool autoRotate)
        {
            return InternalGetFlareData0(screenPos, translationScale, rayOff0, vLocalScreenRatio, angleDeg, position, angularOffset, positionOffset, autoRotate);
        }

        static Vector2 GetLensFlareRayOffset(Vector2 screenPos, float position, float globalCos0, float globalSin0)
        {
            Vector2 rayOff = -(screenPos + screenPos * (position - 1.0f));
            return new Vector2(globalCos0 * rayOff.x - globalSin0 * rayOff.y,
                               globalSin0 * rayOff.x + globalCos0 * rayOff.y);
        }

        static Vector3 WorldToViewport(Camera camera, bool isLocalLight, bool isCameraRelative, Matrix4x4 viewProjMatrix, Vector3 positionWS)
        {
            if (isLocalLight)
            {
                return WorldToViewportLocal(isCameraRelative, viewProjMatrix, camera.transform.position, positionWS);
            }
            else
            {
                return WorldToViewportDistance(camera, positionWS);
            }
        }

        static Vector3 WorldToViewportLocal(bool isCameraRelative, Matrix4x4 viewProjMatrix, Vector3 cameraPosWS, Vector3 positionWS)
        {
            Vector3 localPositionWS = positionWS;
            if (isCameraRelative)
            {
                localPositionWS -= cameraPosWS;
            }
            Vector4 viewportPos4 = viewProjMatrix * localPositionWS;
            Vector3 viewportPos = new Vector3(viewportPos4.x, viewportPos4.y, 0f);
            viewportPos /= viewportPos4.w;
            viewportPos.x = viewportPos.x * 0.5f + 0.5f;
            viewportPos.y = viewportPos.y * 0.5f + 0.5f;
            viewportPos.y = 1.0f - viewportPos.y;
            viewportPos.z = viewportPos4.w;
            return viewportPos;
        }

        static Vector3 WorldToViewportDistance(Camera cam, Vector3 positionWS)
        {
            Vector4 camPos = cam.worldToCameraMatrix * positionWS;
            Vector4 viewportPos4 = cam.projectionMatrix * camPos;
            Vector3 viewportPos = new Vector3(viewportPos4.x, viewportPos4.y, 0f);
            viewportPos /= viewportPos4.w;
            viewportPos.x = viewportPos.x * 0.5f + 0.5f;
            viewportPos.y = viewportPos.y * 0.5f + 0.5f;
            viewportPos.z = viewportPos4.w;
            return viewportPos;
        }

        /// <summary>
        /// Checks if at least one `LensFlareComponentSRP` requests occlusion from environment effects.
        /// </summary>
        /// <remarks>
        /// Environment occlusion can be enabled by setting <see cref="LensFlareComponentSRP.environmentOcclusion"/> to true.
        /// </remarks>
        /// <param name="cam">Camera</param>
        /// <returns>true if cloud occlusion is requested</returns>
        static public bool IsCloudLayerOpacityNeeded(Camera cam)
        {
            if (Instance.IsEmpty())
                return false;

#if UNITY_EDITOR
            if (cam.cameraType == CameraType.SceneView)
            {
                // Determine whether the "Animated Materials" checkbox is checked for the current view.
                for (int i = 0; i < UnityEditor.SceneView.sceneViews.Count; i++) // Using a foreach on an ArrayList generates garbage ...
                {
                    var sv = UnityEditor.SceneView.sceneViews[i] as UnityEditor.SceneView;
                    if (sv.camera == cam && !sv.sceneViewState.flaresEnabled)
                    {
                        return false;
                    }
                }
            }
#endif

            foreach (LensFlareCompInfo info in Instance.Data)
            {
                if (info == null || info.comp == null)
                    continue;

                LensFlareComponentSRP comp = info.comp;
                LensFlareDataSRP data = comp.lensFlareData;

                if (IsLensFlareSRPHidden(cam, comp, data) ||
                    !comp.useOcclusion ||
                    (comp.useOcclusion && comp.sampleCount == 0))
                    continue;

                if (comp.environmentOcclusion)
                    return true;
            }

            return false;
        }

#if UNITY_EDITOR
        static bool IsPrefabStageEnabled()
        {
            return UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null;
        }

        static LensFlareComponentSRP[] GetLensFlareComponents(GameObject go)
        {
            return go.GetComponentsInChildren<LensFlareComponentSRP>(false);
        }

        static bool IsCurrentPrefabLensFlareComponent(GameObject go, LensFlareComponentSRP[] components, LensFlareComponentSRP comp)
        {
            foreach (LensFlareComponentSRP x in components)
            {
                if (x == comp)
                    return true;
            }

            return false;
        }
#endif

        /// <summary>
        /// Renders the set of lens flare registered.
        /// </summary>
        /// <param name="lensFlareShader">lens flare material (HDRP or URP shader)</param>
        /// <param name="cam">Camera</param>
        /// <param name="xr">XRPass data.</param>
        /// <param name="xrIndex">XR multipass ID.</param>
        /// <param name="actualWidth">Width actually used for rendering after dynamic resolution and XR is applied.</param>
        /// <param name="actualHeight">Height actually used for rendering after dynamic resolution and XR is applied.</param>
        /// <param name="usePanini">Set if use Panani Projection</param>
        /// <param name="paniniDistance">Distance used for Panini projection</param>
        /// <param name="paniniCropToFit">CropToFit parameter used for Panini projection</param>
        /// <param name="isCameraRelative">Set if camera is relative</param>
        /// <param name="cameraPositionWS">Camera World Space position</param>
        /// <param name="viewProjMatrix">View Projection Matrix of the current camera</param>
        /// <param name="cmd">Command Buffer</param>
        /// <param name="taaEnabled">Set if TAA is enabled</param>
        /// <param name="hasCloudLayer">Unused</param>
        /// <param name="cloudOpacityTexture">Unused</param>
        /// <param name="sunOcclusionTexture">Sun Occlusion Texture from VolumetricCloud on HDRP or null</param>
        static public void ComputeOcclusion(Material lensFlareShader, Camera cam, XRPass xr, int xrIndex,
            float actualWidth, float actualHeight,
            bool usePanini, float paniniDistance, float paniniCropToFit, bool isCameraRelative,
            Vector3 cameraPositionWS,
            Matrix4x4 viewProjMatrix,
            UnsafeCommandBuffer cmd,
            bool taaEnabled, bool hasCloudLayer, Texture cloudOpacityTexture, Texture sunOcclusionTexture)
        {
            ComputeOcclusion(
                lensFlareShader, cam, xr, xrIndex,
                actualWidth, actualHeight,
                usePanini, paniniDistance, paniniCropToFit, isCameraRelative,
                cameraPositionWS,
                viewProjMatrix,
                cmd.m_WrappedCommandBuffer,
                taaEnabled, hasCloudLayer, cloudOpacityTexture, sunOcclusionTexture);
        }

        static bool ForceSingleElement(LensFlareDataElementSRP element)
        {
            return !element.allowMultipleElement
                || element.count == 1
                || element.flareType == SRPLensFlareType.Ring;
        }

        // Do the once-per-frame setup before we loop over the lens flare
        // components to render them. This involves querying whether we're
        // editing a prefab, checking if lens flares are disabled in scene view,
        // setting the render target, and potentially clearing it.
        // If this returns false, we can early out before rendering anything.
        static bool PreDrawSetup(
            bool occlusionOnly,
            bool clearRenderTarget,
            RenderTargetIdentifier rt,
            Camera cam,
            XRPass xr,
            int xrIndex,
            Rendering.CommandBuffer cmd
#if UNITY_EDITOR
            , out bool inPrefabStage,
            out GameObject prefabGameObject,
            out LensFlareComponentSRP[] prefabStageLensFlares
#endif
        )
        {
            // Get prefab information if we're in prefab staging mode
#if UNITY_EDITOR
            inPrefabStage = IsPrefabStageEnabled();
            UnityEditor.SceneManagement.PrefabStage prefabStage =UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            prefabGameObject = null;
            prefabStageLensFlares = null;
            if (prefabStage != null)
            {
                if (prefabStage.prefabContentsRoot == null)
                    return false;

                prefabGameObject = prefabStage.prefabContentsRoot;
                prefabStageLensFlares = GetLensFlareComponents(prefabGameObject);
                if (prefabStageLensFlares.Length == 0)
                {
                    return false;
                }
            }
#endif

            xr.StopSinglePass(cmd);

            if (Instance.IsEmpty())
                return false;

            // Early out if we're in the scene view and if flares are disabled
            // there.
#if UNITY_EDITOR
            if (cam.cameraType == CameraType.SceneView)
            {
                // Determine whether the "Animated Materials" checkbox is
                // checked for the current view.
                for (int i = 0; i < UnityEditor.SceneView.sceneViews.Count; i++)
                {
                    var sv = UnityEditor.SceneView.sceneViews[i] as UnityEditor.SceneView;
                    if (sv.camera == cam && !sv.sceneViewState.flaresEnabled)
                    {
                        return false;
                    }
                }
            }
#endif

            // Set the render target and the view ID
            {
                int viewId = occlusionOnly? -1 : 0;
#if ENABLE_VR && ENABLE_XR_MODULE
                if (xr.enabled && xr.singlePassEnabled)
                {
                    CoreUtils.SetRenderTarget(cmd, rt, depthSlice: xrIndex);
                    cmd.SetGlobalInt(_ViewId, xrIndex);
                }
                else
#endif
                {
                    CoreUtils.SetRenderTarget(cmd, rt);
                    if (xr.enabled) // multipass
                        cmd.SetGlobalInt(_ViewId, xr.multipassId);
                    else
                        cmd.SetGlobalInt(_ViewId, viewId);
                }
            }

            // TODO: Maybe we need to do this?
            /* if (!occlusionOnly) {
                cmd.SetViewport(viewport);
            } */

            if (clearRenderTarget) {
                cmd.ClearRenderTarget(false, true, Color.black);
            }

            return true;
        }

        static bool DoComponent(
            bool occlusionOnly,
            LensFlareCompInfo info,
            Camera cam,
            Vector3 cameraPositionWS,
            float actualWidth,
            float actualHeight,
            bool usePanini,
            float paniniDistance,
            float paniniCropToFit,
            bool isCameraRelative,
            Matrix4x4 viewProjMatrix,
            Rendering.CommandBuffer cmd,
#if UNITY_EDITOR
            bool inPrefabStage,
            GameObject prefabGameObject,
            LensFlareComponentSRP[] prefabStageLensFlares,
#endif
            out Vector3 flarePosWS,
            out Vector3 flarePosViewport,
            out Vector2 flarePosScreen,
            out Vector3 camToFlare,
            out Light light,
            out bool isDirLight,
            out float flareIntensity,
            out float distanceAttenuation
        )
        {
            // Init out values in case we early out
            flarePosWS = Vector3.zero;
            flarePosViewport = Vector3.zero;
            flarePosScreen = Vector2.zero;
            camToFlare = Vector3.zero;
            isDirLight = false;
            light = null;
            flareIntensity = 0f;
            distanceAttenuation = 1f;

            if (info == null || info.comp == null)
                return false;

            LensFlareComponentSRP comp = info.comp;
            LensFlareDataSRP data = comp.lensFlareData;

            if (IsLensFlareSRPHidden(cam, comp, data))
                return false;

            // If we're in occlusion-only mode, and occlusion isn't used, we can
            // early out. If we're in color mode, this component
            // will be drawn, so we do not early out.
            if (occlusionOnly && !comp.useOcclusion)
                return false;

#if UNITY_EDITOR
            // If we're editing a prefab, but we're not editing this component,
            // we don't want to render it. Early out.
            if (inPrefabStage && !IsCurrentPrefabLensFlareComponent(prefabGameObject, prefabStageLensFlares, comp))
            {
                return false;
            }
#endif

            // Get the light and some related variables
            {
                if (!comp.TryGetComponent<Light>(out light))
                    light = null;


                if (light != null && light.type == LightType.Directional)
                {
                    flarePosWS = -light.transform.forward * cam.farClipPlane;
                    isDirLight = true;
                }
                else
                {
                    flarePosWS = comp.transform.position;
                }
            }

            // Users can specify a light override. If set, it specifices the light
            // component where color and shape values are fetched from for this
            // component. This only matters in the color pass, and not in the
            // occlusion-only pass.
            if (!occlusionOnly && comp.lightOverride != null)
            {
                light = comp.lightOverride;
            }

            flarePosViewport = WorldToViewport(cam, !isDirLight, isCameraRelative, viewProjMatrix, flarePosWS);

            if (usePanini && cam == Camera.main)
            {
                flarePosViewport = DoPaniniProjection(flarePosViewport, actualWidth, actualHeight, cam.fieldOfView, paniniCropToFit, paniniDistance);
            }

            if (flarePosViewport.z < 0.0f)
                return false;

            if (!comp.allowOffScreen)
            {
                // Early out if the component is outside the screen
                if (flarePosViewport.x < 0.0f || flarePosViewport.x > 1.0f ||
                    flarePosViewport.y < 0.0f || flarePosViewport.y > 1.0f)
                    return false;
            }

            // Early out if the light is behind the camera. If we don't do this,
            // it can cause issues with Panini projection.
            camToFlare = flarePosWS - cameraPositionWS;
            if (Vector3.Dot(cam.transform.forward, camToFlare) < 0.0f)
                return false;

            float distToObject = camToFlare.magnitude; // TOD: return;
            float coefDistSample = distToObject / comp.maxAttenuationDistance;
            distanceAttenuation = !isDirLight && comp.distanceAttenuationCurve.length > 0 ? comp.distanceAttenuationCurve.Evaluate(coefDistSample) : 1.0f;

            // Calculate the screen-space position
            flarePosScreen = new Vector2(2.0f * flarePosViewport.x - 1.0f, -(2.0f * flarePosViewport.y - 1.0f));
            if (!SystemInfo.graphicsUVStartsAtTop && isDirLight)
                // Y-flip for OpenGL
                flarePosScreen.y = -flarePosScreen.y;

            Vector2 radPos = new Vector2(Mathf.Abs(flarePosScreen.x), Mathf.Abs(flarePosScreen.y));
            float radius = Mathf.Max(radPos.x, radPos.y); // L1 norm (instead of L2 norm)
            float radialsScaleRadius = comp.radialScreenAttenuationCurve.length > 0 ?
                comp.radialScreenAttenuationCurve.Evaluate(radius) : 1.0f;


            // Early out if the component's intensity is zero or negative
            flareIntensity = comp.intensity * radialsScaleRadius * distanceAttenuation;
            if (flareIntensity <= 0.0f)
                return false;

            // Set _FlareData1
            {
                float adjustedOcclusionRadius =
                    isDirLight ? comp.celestialProjectedOcclusionRadius(cam) : comp.occlusionRadius;
                Vector2 occlusionRadiusEdgeScreenPos0 = (Vector2)flarePosViewport;
                Vector2 occlusionRadiusEdgeScreenPos1 = (Vector2)WorldToViewport(cam, !isDirLight, isCameraRelative,
                    viewProjMatrix, flarePosWS + cam.transform.up * adjustedOcclusionRadius);
                float occlusionRadius = (occlusionRadiusEdgeScreenPos1 - occlusionRadiusEdgeScreenPos0).magnitude;

                Vector3 dir = (cam.transform.position - comp.transform.position).normalized;
                Vector3 screenPosZ = WorldToViewport(cam, !isDirLight, isCameraRelative, viewProjMatrix,
                    flarePosWS + dir * comp.occlusionOffset);

                cmd.SetGlobalVector(_FlareData1,
                    new Vector4(occlusionRadius, comp.sampleCount, screenPosZ.z, actualHeight / actualWidth));
            }

            return true;
        }

        /// <summary>
        /// Computes the occlusion of lens flare using the depth buffer and additional occlusion textures if not null.
        /// </summary>
        /// <param name="lensFlareShader">Lens flare material (HDRP or URP shader)</param>
        /// <param name="cam">Camera</param>
        /// <param name="xr">XRPass data.</param>
        /// <param name="xrIndex">XR multipass ID.</param>
        /// <param name="actualWidth">Width actually used for rendering after dynamic resolution and XR is applied.</param>
        /// <param name="actualHeight">Height actually used for rendering after dynamic resolution and XR is applied.</param>
        /// <param name="usePanini">Set if use Panani Projection</param>
        /// <param name="paniniDistance">Distance used for Panini projection</param>
        /// <param name="paniniCropToFit">CropToFit parameter used for Panini projection</param>
        /// <param name="isCameraRelative">Set if camera is relative</param>
        /// <param name="cameraPositionWS">Camera World Space position</param>
        /// <param name="viewProjMatrix">View Projection Matrix of the current camera</param>
        /// <param name="cmd">Command Buffer</param>
        /// <param name="taaEnabled">Set if TAA is enabled</param>
        /// <param name="hasCloudLayer">Unused</param>
        /// <param name="cloudOpacityTexture">Unused</param>
        /// <param name="sunOcclusionTexture">Sun Occlusion Texture from VolumetricCloud on HDRP or null</param>
        static public void ComputeOcclusion(Material lensFlareShader, Camera cam, XRPass xr, int xrIndex,
            float actualWidth, float actualHeight,
            bool usePanini, float paniniDistance, float paniniCropToFit, bool isCameraRelative,
            Vector3 cameraPositionWS,
            Matrix4x4 viewProjMatrix,
            Rendering.CommandBuffer cmd,
            bool taaEnabled, bool hasCloudLayer, Texture cloudOpacityTexture, Texture sunOcclusionTexture)
        {
            if (!IsOcclusionRTCompatible())
                return;

            // Once-per frame setup
            bool clearRenderTarget = !taaEnabled;
            bool ok = PreDrawSetup(
                true,
                clearRenderTarget,
                occlusionRT,
                cam,
                xr,
                xrIndex,
                cmd
#if UNITY_EDITOR
                , out bool inPrefabStage,
                out GameObject prefabGameObject,
                out LensFlareComponentSRP[] prefabStageLensFlares
#endif
            );

            if (!ok)
                return;

            float aspect = actualWidth / actualHeight;

            foreach (LensFlareCompInfo info in m_Data)
            {
                bool okComp = DoComponent(
                    true,
                    info,
                    cam,
                    cameraPositionWS,
                    actualWidth,
                    actualHeight,
                    usePanini,
                    paniniDistance,
                    paniniCropToFit,
                    isCameraRelative,
                    viewProjMatrix,
                    cmd,
#if UNITY_EDITOR
                    inPrefabStage,
                    prefabGameObject,
                    prefabStageLensFlares,
#endif
                    out Vector3 flarePosWS,
                    out Vector3 flarePosViewport,
                    out Vector2 flarePosScreen,
                    out Vector3 camToFlare,
                    out Light light,
                    out bool isDirLight,
                    out float flareIntensity,
                    out float distanceAttenuation
                );
                if (!okComp)
                    continue;

                LensFlareComponentSRP comp = info.comp;

                // Set the occlusion keyword and bind occlusion shader constants
                {
                    cmd.EnableShaderKeyword("FLARE_COMPUTE_OCCLUSION");
                    uint occlusionPermutation = (uint)(LensFlareOcclusionPermutation.Depth);

                    if (comp.environmentOcclusion && sunOcclusionTexture != null)
                    {
                        occlusionPermutation |= (uint)(LensFlareOcclusionPermutation.FogOpacity);
                        cmd.SetGlobalTexture(_FlareSunOcclusionTex, sunOcclusionTexture);
                    }

                    int convInt = unchecked((int)occlusionPermutation);
                    cmd.SetGlobalInt(_FlareOcclusionPermutation, convInt);
                }

                float globalCos0 = Mathf.Cos(0.0f);
                float globalSin0 = Mathf.Sin(0.0f);

                float position = 0.0f;

                float usedGradientPosition = Mathf.Clamp01(1.0f - 1e-6f);

                cmd.SetGlobalVector(_FlareData3, new Vector4(comp.allowOffScreen ? 1.0f : -1.0f, usedGradientPosition, Mathf.Exp(Mathf.Lerp(0.0f, 4.0f, 1.0f)), 1.0f / 3.0f));

                Vector2 rayOff = GetLensFlareRayOffset(flarePosScreen, position, globalCos0, globalSin0);
                Vector2 vScreenRatio = new Vector2(aspect, 1f);
                Vector4 flareData0 = InternalGetFlareData0(flarePosScreen, Vector2.one, rayOff, vScreenRatio, 0.0f, position, 0.0f, Vector2.zero, false);

                cmd.SetGlobalVector(_FlareData0, flareData0);
                cmd.SetGlobalVector(_FlareData2, new Vector4(flarePosScreen.x, flarePosScreen.y, 0.0f, 0.0f));

                Rect rect;
                if (taaEnabled)
                    rect = new Rect() { x = info.index, y = frameIdx + mergeNeeded, width = 1, height = 1 };
                else
                    rect = new Rect() { x = info.index, y = 0, width = 1, height = 1 };
                cmd.SetViewport(rect);

                Blitter.DrawQuad(cmd, lensFlareShader, lensFlareShader.FindPass("LensFlareOcclusion"));
            }

            // Clear the remaining buffer if not TAA the whole OcclusionRT is already cleared
            if (taaEnabled)
            {
                CoreUtils.SetRenderTarget(cmd, occlusionRT, depthSlice: xrIndex);
                cmd.SetViewport(new Rect() { x = m_Data.Count, y = 0, width = (maxLensFlareWithOcclusion - m_Data.Count), height = (maxLensFlareWithOcclusionTemporalSample + mergeNeeded) });
                cmd.ClearRenderTarget(false, true, Color.black);
            }

            ++frameIdx;
            frameIdx %= maxLensFlareWithOcclusionTemporalSample;

            xr.StartSinglePass(cmd);
        }

        /// <summary>
        /// Renders a single element of a LensFlareDataSRP, this function is used on scene/game view and on the inspector for the thumbnail.
        /// </summary>
        /// <remarks>
        /// Can be used to draw aa single lens flare for editor or preview purpose.
        /// </remarks>
        /// <param name="element">Single LensFlare asset we need to process.</param>
        /// <param name="cmd">Command Buffer.</param>
        /// <param name="globalColorModulation">Color Modulation from Component?</param>
        /// <param name="light">Light used for the modulation of this singe element.</param>
        /// <param name="compIntensity">Intensity from Component.</param>
        /// <param name="scale">Scale from component</param>
        /// <param name="lensFlareShader">Shader used on URP or HDRP.</param>
        /// <param name="screenPos">Screen Position</param>
        /// <param name="compAllowOffScreen">Allow lens flare offscreen</param>
        /// <param name="vScreenRatio">Screen Ratio</param>
        /// <param name="flareData1">_FlareData1 used internally by the shader.</param>
        /// <param name="preview">true if we are on preview on the inspector</param>
        /// <param name="depth">Depth counter for recursive call of 'ProcessLensFlareSRPElementsSingle'.</param>
        public static void ProcessLensFlareSRPElementsSingle(LensFlareDataElementSRP element, Rendering.CommandBuffer cmd, Color globalColorModulation, Light light,
            float compIntensity, float scale, Material lensFlareShader, Vector2 screenPos, bool compAllowOffScreen, Vector2 vScreenRatio, Vector3 flareData1, bool preview, int depth)
        {
            if (element == null ||
                element.visible == false ||
                (element.lensFlareTexture == null && element.flareType == SRPLensFlareType.Image) ||
                element.localIntensity <= 0.0f ||
                element.count <= 0 ||
                (element.flareType == SRPLensFlareType.LensFlareDataSRP && element.lensFlareDataSRP == null))
                return;

            if (element.flareType == SRPLensFlareType.LensFlareDataSRP && element.lensFlareDataSRP != null)
            {
                Vector3 unused = new();
                ProcessLensFlareSRPElements(ref element.lensFlareDataSRP.elements, cmd, globalColorModulation, light, compIntensity, scale, lensFlareShader, screenPos, compAllowOffScreen, vScreenRatio.x, unused, preview, depth + 1);
                return;
            }

            Color colorModulation = globalColorModulation;
            if (light != null && element.modulateByLightColor)
            {
                if (light.useColorTemperature)
                    colorModulation *= light.color * Mathf.CorrelatedColorTemperatureToRGB(light.colorTemperature);
                else
                    colorModulation *= light.color;
            }

            Color curColor = colorModulation;

            float currentIntensity = element.localIntensity * compIntensity;

            if (currentIntensity <= 0.0f)
                return;

            Texture texture = element.lensFlareTexture;
            float usedAspectRatio;
            if (element.flareType == SRPLensFlareType.Image)
                usedAspectRatio = element.preserveAspectRatio ? ((((float)texture.height) / (float)texture.width)) : 1.0f;
            else
                usedAspectRatio = 1.0f;

            float rotation = element.rotation;

            Vector2 elemSizeXY;
            if (element.preserveAspectRatio)
            {
                if (usedAspectRatio >= 1.0f)
                {
                    elemSizeXY = new Vector2(element.sizeXY.x / usedAspectRatio, element.sizeXY.y);
                }
                else
                {
                    elemSizeXY = new Vector2(element.sizeXY.x, element.sizeXY.y * usedAspectRatio);
                }
            }
            else
            {
                elemSizeXY = new Vector2(element.sizeXY.x, element.sizeXY.y);
            }
            float scaleSize = 0.1f; // Arbitrary value
            Vector2 size = new Vector2(elemSizeXY.x, elemSizeXY.y);
            float combinedScale = scaleSize * element.uniformScale * scale;
            size *= combinedScale;

            curColor *= element.tint;

            float angularOffset = SystemInfo.graphicsUVStartsAtTop ? element.angularOffset : -element.angularOffset;
            float globalCos0 = Mathf.Cos(-angularOffset * Mathf.Deg2Rad);
            float globalSin0 = Mathf.Sin(-angularOffset * Mathf.Deg2Rad);

            float position = 2.0f * element.position;

            SRPLensFlareBlendMode blendMode = element.blendMode;
            int materialPass;
#if UNITY_EDITOR
            if (!preview)
#endif
            {
                if (blendMode == SRPLensFlareBlendMode.Additive)
                    materialPass = lensFlareShader.FindPass("LensFlareAdditive");
                else if (blendMode == SRPLensFlareBlendMode.Screen)
                    materialPass = lensFlareShader.FindPass("LensFlareScreen");
                else if (blendMode == SRPLensFlareBlendMode.Premultiply)
                    materialPass = lensFlareShader.FindPass("LensFlarePremultiply");
                else if (blendMode == SRPLensFlareBlendMode.Lerp)
                    materialPass = lensFlareShader.FindPass("LensFlareLerp");
                else
                    materialPass = lensFlareShader.FindPass("LensFlareOcclusion");
            }
#if UNITY_EDITOR
            else
            {
                if (element.inverseSDF)
                    materialPass = lensFlareShader.FindPass("FlarePreviewInverted");
                else
                    materialPass = lensFlareShader.FindPass("FlarePreviewNotInverted");
            }
#endif

            var flareData6 = new Vector4((float)element.flareType, 0, 0, 0);
            if (ForceSingleElement(element))
                cmd.SetGlobalVector(_FlareData6, flareData6);

            if (element.flareType == SRPLensFlareType.Circle ||
                element.flareType == SRPLensFlareType.Polygon ||
                element.flareType == SRPLensFlareType.Ring)
            {
                if (element.inverseSDF)
                {
                    cmd.EnableShaderKeyword("FLARE_INVERSE_SDF");
                }
                else
                {
                    cmd.DisableShaderKeyword("FLARE_INVERSE_SDF");
                }
            }
            else
            {
                cmd.DisableShaderKeyword("FLARE_INVERSE_SDF");
            }

            if (element.lensFlareTexture != null)
                cmd.SetGlobalTexture(_FlareTex, element.lensFlareTexture);

            if (element.tintColorType != SRPLensFlareColorType.Constant)
                cmd.SetGlobalTexture(_FlareRadialTint, element.tintGradient.GetTexture());

            float usedGradientPosition = Mathf.Clamp01((1.0f - element.edgeOffset) - 1e-6f);
            if (element.flareType == SRPLensFlareType.Polygon)
                usedGradientPosition = Mathf.Pow(usedGradientPosition + 1.0f, 5);

            Vector2 ComputeLocalSize(Vector2 rayOff, Vector2 rayOff0, Vector2 curSize, AnimationCurve distortionCurve)
            {
                Vector2 rayOffZ = GetLensFlareRayOffset(screenPos, position, globalCos0, globalSin0);
                Vector2 localRadPos;
                float localRadius;
                if (!element.distortionRelativeToCenter)
                {
                    localRadPos = (rayOff - rayOff0) * 0.5f;
                    localRadius = Mathf.Clamp01(Mathf.Max(Mathf.Abs(localRadPos.x), Mathf.Abs(localRadPos.y))); // l1 norm (instead of l2 norm)
                }
                else
                {
                    localRadPos = screenPos + (rayOff + new Vector2(element.positionOffset.x, -element.positionOffset.y)) * element.translationScale;
                    localRadius = Mathf.Clamp01(localRadPos.magnitude); // l2 norm (instead of l1 norm)
                }

                float localLerpValue = Mathf.Clamp01(distortionCurve.Evaluate(localRadius));
                return new Vector2(Mathf.Lerp(curSize.x, element.targetSizeDistortion.x * combinedScale / usedAspectRatio, localLerpValue),
                    Mathf.Lerp(curSize.y, element.targetSizeDistortion.y * combinedScale, localLerpValue));
            }

            float usedSDFRoundness = element.sdfRoundness;

            Vector4 data3 =
                new Vector4(compAllowOffScreen ? 1.0f : -1.0f,
                            usedGradientPosition,
                            Mathf.Exp(Mathf.Lerp(0.0f, 4.0f, Mathf.Clamp01(1.0f - element.fallOff))),
                            element.flareType == SRPLensFlareType.Ring ? element.ringThickness : 1.0f / (float)element.sideCount);
            cmd.SetGlobalVector(_FlareData3, data3);
            if (element.flareType == SRPLensFlareType.Polygon)
            {
                float invSide = 1.0f / (float)element.sideCount;
                float rCos = Mathf.Cos(Mathf.PI * invSide);
                float roundValue = rCos * usedSDFRoundness;
                float r = rCos - roundValue;
                float an = 2.0f * Mathf.PI * invSide;
                float he = r * Mathf.Tan(0.5f * an);
                cmd.SetGlobalVector(_FlareData4, new Vector4(usedSDFRoundness, r, an, he));
            }
            else if (element.flareType == SRPLensFlareType.Ring)
            {
                cmd.SetGlobalVector(_FlareData4, new Vector4(element.noiseAmplitude, element.noiseFrequency, element.noiseSpeed, 0.0f));
            }
            else
            {
                cmd.SetGlobalVector(_FlareData4, new Vector4(usedSDFRoundness, 0.0f, 0.0f, 0.0f));
            }

            cmd.SetGlobalVector(_FlareData5, new Vector4((float)(element.tintColorType), currentIntensity, element.shapeCutOffSpeed, element.shapeCutOffRadius));
            if (ForceSingleElement(element))
            {
                Vector2 localSize = size;
                Vector2 rayOff = GetLensFlareRayOffset(screenPos, position, globalCos0, globalSin0);
                if (element.enableRadialDistortion)
                {
                    Vector2 rayOff0 = GetLensFlareRayOffset(screenPos, 0.0f, globalCos0, globalSin0);
                    localSize = ComputeLocalSize(rayOff, rayOff0, localSize, element.distortionCurve);
                }
                Vector4 flareData0 = InternalGetFlareData0(screenPos, element.translationScale, rayOff, vScreenRatio, rotation, position, angularOffset, element.positionOffset, element.autoRotate);

                cmd.SetGlobalVector(_FlareData0, flareData0);
                cmd.SetGlobalVector(_FlareData2, new Vector4(screenPos.x, screenPos.y, localSize.x, localSize.y));
                cmd.SetGlobalVector(_FlareColorValue, curColor);

                UnityEngine.Rendering.Blitter.DrawQuad(cmd, lensFlareShader, materialPass);
            }
            else
            {
                float dLength = 2.0f * element.lengthSpread / ((float)(element.count - 1));

                if (element.distribution == SRPLensFlareDistribution.Uniform)
                {
                    float uniformAngle = 0.0f;
                    for (int elemIdx = 0; elemIdx < element.count; ++elemIdx)
                    {
                        Vector2 localSize = size;
                        Vector2 rayOff = GetLensFlareRayOffset(screenPos, position, globalCos0, globalSin0);
                        if (element.enableRadialDistortion)
                        {
                            Vector2 rayOff0 = GetLensFlareRayOffset(screenPos, 0.0f, globalCos0, globalSin0);
                            localSize = ComputeLocalSize(rayOff, rayOff0, localSize, element.distortionCurve);
                        }

                        float timeScale = element.count >= 2 ? ((float)elemIdx) / ((float)(element.count - 1)) : 0.5f;

                        Color col = element.colorGradient.Evaluate(timeScale);

                        Vector4 flareData0 = InternalGetFlareData0(screenPos, element.translationScale, rayOff, vScreenRatio, rotation + uniformAngle, position, angularOffset, element.positionOffset, element.autoRotate);
                        cmd.SetGlobalVector(_FlareData0, flareData0);

                        flareData6.y = (float)elemIdx;
                        cmd.SetGlobalVector(_FlareData6, flareData6);
                        cmd.SetGlobalVector(_FlareData2, new Vector4(screenPos.x, screenPos.y, localSize.x, localSize.y));
                        cmd.SetGlobalVector(_FlareColorValue, curColor * col);

                        UnityEngine.Rendering.Blitter.DrawQuad(cmd, lensFlareShader, materialPass);
                        position += dLength;
                        uniformAngle += element.uniformAngle;
                    }
                }
                else if (element.distribution == SRPLensFlareDistribution.Random)
                {
                    Random.State backupRandState = UnityEngine.Random.state;
                    Random.InitState(element.seed);
                    Vector2 side = new Vector2(globalSin0, globalCos0);
                    side *= element.positionVariation.y;
                    float RandomRange(float min, float max)
                    {
                        return Random.Range(min, max);
                    }

                    for (int elemIdx = 0; elemIdx < element.count; ++elemIdx)
                    {
                        float localIntensity = RandomRange(-1.0f, 1.0f) * element.intensityVariation + 1.0f;

                        Vector2 rayOff = GetLensFlareRayOffset(screenPos, position, globalCos0, globalSin0);
                        Vector2 localSize = size;
                        if (element.enableRadialDistortion)
                        {
                            Vector2 rayOff0 = GetLensFlareRayOffset(screenPos, 0.0f, globalCos0, globalSin0);
                            localSize = ComputeLocalSize(rayOff, rayOff0, localSize, element.distortionCurve);
                        }

                        localSize += localSize * (element.scaleVariation * RandomRange(-1.0f, 1.0f));

                        Color randCol = element.colorGradient.Evaluate(RandomRange(0.0f, 1.0f));

                        Vector2 localPositionOffset = element.positionOffset + RandomRange(-1.0f, 1.0f) * side;

                        float localRotation = rotation + RandomRange(-Mathf.PI, Mathf.PI) * element.rotationVariation;

                        if (localIntensity > 0.0f)
                        {
                            Vector4 flareData0 = InternalGetFlareData0(screenPos, element.translationScale, rayOff, vScreenRatio, localRotation, position, angularOffset, localPositionOffset, element.autoRotate);
                            cmd.SetGlobalVector(_FlareData0, flareData0);
                            flareData6.y = (float)elemIdx;
                            cmd.SetGlobalVector(_FlareData6, flareData6);
                            cmd.SetGlobalVector(_FlareData2, new Vector4(screenPos.x, screenPos.y, localSize.x, localSize.y));
                            cmd.SetGlobalVector(_FlareColorValue, curColor * randCol * localIntensity);

                            UnityEngine.Rendering.Blitter.DrawQuad(cmd, lensFlareShader, materialPass);
                        }

                        position += dLength;
                        position += 0.5f * dLength * RandomRange(-1.0f, 1.0f) * element.positionVariation.x;
                    }
                    Random.state = backupRandState;
                }
                else if (element.distribution == SRPLensFlareDistribution.Curve)
                {
                    for (int elemIdx = 0; elemIdx < element.count; ++elemIdx)
                    {
                        float timeScale = element.count >= 2 ? ((float)elemIdx) / ((float)(element.count - 1)) : 0.5f;

                        Color col = element.colorGradient.Evaluate(timeScale);

                        float positionSpacing = element.positionCurve.length > 0 ? element.positionCurve.Evaluate(timeScale) : 1.0f;

                        float localPos = position + 2.0f * element.lengthSpread * positionSpacing;
                        Vector2 rayOff = GetLensFlareRayOffset(screenPos, localPos, globalCos0, globalSin0);
                        Vector2 localSize = size;
                        if (element.enableRadialDistortion)
                        {
                            Vector2 rayOff0 = GetLensFlareRayOffset(screenPos, 0.0f, globalCos0, globalSin0);
                            localSize = ComputeLocalSize(rayOff, rayOff0, localSize, element.distortionCurve);
                        }
                        float sizeCurveValue = element.scaleCurve.length > 0 ? element.scaleCurve.Evaluate(timeScale) : 1.0f;
                        localSize *= sizeCurveValue;

                        float angleFromCurve = element.uniformAngleCurve.Evaluate(timeScale) * (180.0f - (180.0f / (float)element.count));

                        Vector4 flareData0 = InternalGetFlareData0(screenPos, element.translationScale, rayOff, vScreenRatio, rotation + angleFromCurve, localPos, angularOffset, element.positionOffset, element.autoRotate);
                        cmd.SetGlobalVector(_FlareData0, flareData0);
                        flareData6.y = (float)elemIdx;
                        cmd.SetGlobalVector(_FlareData6, flareData6);
                        cmd.SetGlobalVector(_FlareData2, new Vector4(screenPos.x, screenPos.y, localSize.x, localSize.y));
                        cmd.SetGlobalVector(_FlareColorValue, curColor * col);

                        UnityEngine.Rendering.Blitter.DrawQuad(cmd, lensFlareShader, materialPass);
                    }
                }
            }
        }

        static void ProcessLensFlareSRPElements(ref LensFlareDataElementSRP[] elements, Rendering.CommandBuffer cmd, Color globalColorModulation, Light light,
            float compIntensity, float scale, Material lensFlareShader, Vector2 screenPos, bool compAllowOffScreen, float aspect, Vector4 flareData6, bool preview, int depth)
        {
            if (depth > 16)
            {
                Debug.LogWarning("LensFlareSRPAsset contains too deep recursive asset (> 16). Be careful to not have recursive aggregation, A contains B, B contains A, ... which will produce an infinite loop.");
                return;
            }

            foreach (LensFlareDataElementSRP element in elements)
            {
                Vector3 unused = new();
                ProcessLensFlareSRPElementsSingle(element, cmd, globalColorModulation, light, compIntensity, scale, lensFlareShader, screenPos, compAllowOffScreen, new Vector2(aspect, 1), unused, preview, depth);
            }
        }

        /// <summary>
        /// Renders all visible lens flares.
        /// </summary>
        /// <remarks>
        /// Call this function during the post processing phase of the Render Pipeline.
        /// </remarks>
        /// <param name="lensFlareShader">Lens flare material (HDRP or URP shader)</param>
        /// <param name="cam">Camera</param>
        /// <param name="viewport">Viewport used for rendering and XR applied.</param>
        /// <param name="xr">XRPass data.</param>
        /// <param name="xrIndex">XR multipass ID.</param>
        /// <param name="actualWidth">Width actually used for rendering after dynamic resolution and XR is applied.</param>
        /// <param name="actualHeight">Height actually used for rendering after dynamic resolution and XR is applied.</param>
        /// <param name="usePanini">Set if use Panani Projection</param>
        /// <param name="paniniDistance">Distance used for Panini projection</param>
        /// <param name="paniniCropToFit">CropToFit parameter used for Panini projection</param>
        /// <param name="isCameraRelative">Set if camera is relative</param>
        /// <param name="cameraPositionWS">Camera World Space position</param>
        /// <param name="viewProjMatrix">View Projection Matrix of the current camera</param>
        /// <param name="cmd">Command Buffer</param>
        /// <param name="taaEnabled">Set if TAA is enabled</param>
        /// <param name="hasCloudLayer">Unused</param>
        /// <param name="cloudOpacityTexture">Unused</param>
        /// <param name="sunOcclusionTexture">Sun Occlusion Texture from VolumetricCloud on HDRP or null</param>
        /// <param name="colorBuffer">Source Render Target which contains the Color Buffer</param>
        /// <param name="GetLensFlareLightAttenuation">Delegate to which return return the Attenuation of the light based on their shape which uses the functions ShapeAttenuation...(...), must reimplemented per SRP</param>
        /// <param name="debugView">Debug View which setup black background to see only lens flare</param>
        static public void DoLensFlareDataDrivenCommon(Material lensFlareShader, Camera cam, Rect viewport, XRPass xr, int xrIndex,
            float actualWidth, float actualHeight,
            bool usePanini, float paniniDistance, float paniniCropToFit,
            bool isCameraRelative,
            Vector3 cameraPositionWS,
            Matrix4x4 viewProjMatrix,
            UnsafeCommandBuffer cmd,
            bool taaEnabled, bool hasCloudLayer, Texture cloudOpacityTexture, Texture sunOcclusionTexture,
            Rendering.RenderTargetIdentifier colorBuffer,
            System.Func<Light, Camera, Vector3, float> GetLensFlareLightAttenuation,
            bool debugView)
        {
            DoLensFlareDataDrivenCommon(lensFlareShader, cam, viewport, xr, xrIndex,
                actualWidth, actualHeight,
                usePanini, paniniDistance, paniniCropToFit,
                isCameraRelative,
                cameraPositionWS,
                viewProjMatrix,
                cmd.m_WrappedCommandBuffer,
                taaEnabled, hasCloudLayer, cloudOpacityTexture, sunOcclusionTexture,
                colorBuffer,
                GetLensFlareLightAttenuation,
                debugView);
        }

        /// <summary>
        /// Renders all visible lens flares.
        /// </summary>
        /// <remarks>
        /// Call this function during the post processing phase of the Render Pipeline.
        /// </remarks>
        /// <param name="lensFlareShader">Lens flare material (HDRP or URP shader)</param>
        /// <param name="cam">Camera</param>
        /// <param name="viewport">Viewport used for rendering and XR applied.</param>
        /// <param name="xr">XRPass data.</param>
        /// <param name="xrIndex">XR multipass ID.</param>
        /// <param name="actualWidth">Width actually used for rendering after dynamic resolution and XR is applied.</param>
        /// <param name="actualHeight">Height actually used for rendering after dynamic resolution and XR is applied.</param>
        /// <param name="usePanini">Set if use Panani Projection</param>
        /// <param name="paniniDistance">Distance used for Panini projection</param>
        /// <param name="paniniCropToFit">CropToFit parameter used for Panini projection</param>
        /// <param name="isCameraRelative">Set if camera is relative</param>
        /// <param name="cameraPositionWS">Camera World Space position</param>
        /// <param name="viewProjMatrix">View Projection Matrix of the current camera</param>
        /// <param name="cmd">Command Buffer</param>
        /// <param name="taaEnabled">Set if TAA is enabled</param>
        /// <param name="hasCloudLayer">Unused</param>
        /// <param name="cloudOpacityTexture">Unused</param>
        /// <param name="sunOcclusionTexture">Sun Occlusion Texture from VolumetricCloud on HDRP or null</param>
        /// <param name="colorBuffer">Source Render Target which contains the Color Buffer</param>
        /// <param name="GetLensFlareLightAttenuation">Delegate to which return return the Attenuation of the light based on their shape which uses the functions ShapeAttenuation...(...), must reimplemented per SRP</param>
        /// <param name="debugView">Debug View which setup black background to see only lens flare</param>
        static public void DoLensFlareDataDrivenCommon(Material lensFlareShader, Camera cam, Rect viewport, XRPass xr, int xrIndex,
            float actualWidth, float actualHeight,
            bool usePanini, float paniniDistance, float paniniCropToFit,
            bool isCameraRelative,
            Vector3 cameraPositionWS,
            Matrix4x4 viewProjMatrix,
            Rendering.CommandBuffer cmd,
            bool taaEnabled, bool hasCloudLayer, Texture cloudOpacityTexture, Texture sunOcclusionTexture,
            Rendering.RenderTargetIdentifier colorBuffer,
            System.Func<Light, Camera, Vector3, float> GetLensFlareLightAttenuation,
            bool debugView)
        {
            // Once-per frame setup
            bool clearRenderTarget = debugView;
            bool ok = PreDrawSetup(
                false,
                clearRenderTarget,
                colorBuffer,
                cam,
                xr,
                xrIndex,
                cmd
#if UNITY_EDITOR
                , out bool inPrefabStage,
                out GameObject prefabGameObject,
                out LensFlareComponentSRP[] prefabStageLensFlares
#endif
            );

            if (!ok)
                return;

            cmd.SetViewport(viewport);
            float aspect = actualWidth / actualHeight;

            foreach (LensFlareCompInfo info in m_Data)
            {
                bool okComp = DoComponent(
                    false,
                    info,
                    cam,
                    cameraPositionWS,
                    actualWidth,
                    actualHeight,
                    usePanini,
                    paniniDistance,
                    paniniCropToFit,
                    isCameraRelative,
                    viewProjMatrix,
                    cmd,
#if UNITY_EDITOR
                    inPrefabStage,
                    prefabGameObject,
                    prefabStageLensFlares,
#endif
                    out Vector3 flarePosWS,
                    out Vector3 flarePosViewport,
                    out Vector2 flarePosScreen,
                    out Vector3 camToFlare,
                    out Light light,
                    out bool isDirLight,
                    out float flareIntensity,
                    out float distanceAttenuation
                );
                if (!okComp)
                    continue;

                LensFlareComponentSRP comp = info.comp;


                // Set the occlusion-related keywords
                if (comp.useOcclusion && IsOcclusionRTCompatible())
                {
                    // We want occlusion for our lens flare and our API supports an occlusion render target.
                    Debug.Assert(occlusionRT != null);
                    cmd.SetGlobalTexture(_FlareOcclusionTex, occlusionRT);
                    cmd.EnableShaderKeyword("FLARE_HAS_OCCLUSION");
                }
                else if (comp.useOcclusion && !IsOcclusionRTCompatible())
                {
                    // We want occlusion for our lens flare, but our API doesn't support an occlusion render target.
                    // In this case, we won't have an _FlareOcclusionTex, but we can get the depth info from
                    // _CameraDepthTexture instead.
                    cmd.EnableShaderKeyword("FLARE_HAS_OCCLUSION");
                }
                else
                {
                    cmd.DisableShaderKeyword("FLARE_HAS_OCCLUSION");
                }

                if (IsOcclusionRTCompatible())
                    cmd.DisableShaderKeyword("FLARE_OPENGL3_OR_OPENGLCORE");
                else
                    cmd.EnableShaderKeyword("FLARE_OPENGL3_OR_OPENGLCORE");

                cmd.SetGlobalVector(_FlareOcclusionIndex, new Vector4((float)info.index, 0.0f, 0.0f, 0.0f));
                cmd.SetGlobalTexture(_FlareOcclusionRemapTex, comp.occlusionRemapCurve.GetTexture());

                Vector4 flareData6 = new Vector4();
                float coefScaleSample = camToFlare.magnitude / comp.maxAttenuationScale;
                float scaleByDistance = !isDirLight && comp.scaleByDistanceCurve.length >= 1 ? comp.scaleByDistanceCurve.Evaluate(coefScaleSample) : 1.0f;

                Color globalColorModulation = Color.white;
                if (light != null)
                {
                    if (comp.attenuationByLightShape)
                        globalColorModulation *= GetLensFlareLightAttenuation(light, cam, -camToFlare.normalized);
                }

                globalColorModulation *= distanceAttenuation;
                ProcessLensFlareSRPElements(ref comp.lensFlareData.elements, cmd, globalColorModulation, light,
                    flareIntensity, scaleByDistance * comp.scale, lensFlareShader,
                    flarePosScreen, comp.allowOffScreen, aspect, flareData6, false, 0);
            }

            xr.StartSinglePass(cmd);
        }

        /// <summary>
        /// Renders the screen space lens flare effect.
        /// </summary>
        /// <remarks>
        /// Call this function during the post processing of the render pipeline after the bloom.
        /// </remarks>
        /// <param name="lensFlareShader">Lens flare material (HDRP or URP shader)</param>
        /// <param name="cam">Camera</param>
        /// <param name="actualWidth">Width actually used for rendering after dynamic resolution and XR is applied.</param>
        /// <param name="actualHeight">Height actually used for rendering after dynamic resolution and XR is applied.</param>
        /// <param name="tintColor">tintColor to multiply all the flare by</param>
        /// <param name="originalBloomTexture">original Bloom texture used to write on at the end of compositing</param>
        /// <param name="bloomMipTexture">Bloom mip texture used as data for the effect</param>
        /// <param name="spectralLut">spectralLut used for chromatic aberration effect</param>
        /// <param name="streakTextureTmp">Texture used for the multiple pass streaks effect</param>
        /// <param name="streakTextureTmp2">Texture used for the multiple pass streaks effect</param>
        /// <param name="parameters1">globalIntensity, regularIntensity, reverseIntensity, warpedIntensity</param>
        /// <param name="parameters2">vignetteEffect, startingPosition, scale, freeSlot</param>
        /// <param name="parameters3">samples, sampleDimmer, chromaticAbberationIntensity, chromaticAbberationSamples</param>
        /// <param name="parameters4">streaksIntensity, streaksLength, streaksOrientation, streaksThreshold</param>
        /// <param name="parameters5">downsampleStreak, warpedFlareScaleX, warpedFlareScaleY, freeSlot</param>
        /// <param name="cmd">UnsafeCommandBuffer</param>
        /// <param name="result">Result RT for the lens flare Screen Space</param>
        /// <param name="debugView">Information if we are in debug mode or not</param>
        static public void DoLensFlareScreenSpaceCommon(
            Material lensFlareShader,
            Camera cam,
            float actualWidth,
            float actualHeight,
            Color tintColor,
            Texture originalBloomTexture,
            Texture bloomMipTexture,
            Texture spectralLut,
            Texture streakTextureTmp,
            Texture streakTextureTmp2,
            Vector4 parameters1,
            Vector4 parameters2,
            Vector4 parameters3,
            Vector4 parameters4,
            Vector4 parameters5,
            UnsafeCommandBuffer cmd,
            RTHandle result,
            bool debugView)
        {
            DoLensFlareScreenSpaceCommon(
            lensFlareShader,
            cam,
            actualWidth,
            actualHeight,
            tintColor,
            originalBloomTexture,
            bloomMipTexture,
            spectralLut,
            streakTextureTmp,
            streakTextureTmp2,
            parameters1,
            parameters2,
            parameters3,
            parameters4,
            parameters5,
            cmd.m_WrappedCommandBuffer,
            result,
            debugView);
        }

        /// <summary>
        /// Renders the screen space lens flare effect.
        /// </summary>
        /// <remarks>
        /// Call this function during the post processing of the render pipeline after the bloom.
        /// </remarks>
        /// <param name="lensFlareShader">Lens flare material (HDRP or URP shader)</param>
        /// <param name="cam">Camera</param>
        /// <param name="actualWidth">Width actually used for rendering after dynamic resolution and XR is applied.</param>
        /// <param name="actualHeight">Height actually used for rendering after dynamic resolution and XR is applied.</param>
        /// <param name="tintColor">tintColor to multiply all the flare by</param>
        /// <param name="originalBloomTexture">original Bloom texture used to write on at the end of compositing</param>
        /// <param name="bloomMipTexture">Bloom mip texture used as data for the effect</param>
        /// <param name="spectralLut">spectralLut used for chromatic aberration effect</param>
        /// <param name="streakTextureTmp">Texture used for the multiple pass streaks effect</param>
        /// <param name="streakTextureTmp2">Texture used for the multiple pass streaks effect</param>
        /// <param name="parameters1">globalIntensity, regularIntensity, reverseIntensity, warpedIntensity</param>
        /// <param name="parameters2">vignetteEffect, startingPosition, scale, freeSlot</param>
        /// <param name="parameters3">samples, sampleDimmer, chromaticAbberationIntensity, chromaticAbberationSamples</param>
        /// <param name="parameters4">streaksIntensity, streaksLength, streaksOrientation, streaksThreshold</param>
        /// <param name="parameters5">downsampleStreak, warpedFlareScaleX, warpedFlareScaleY, freeSlot</param>
        /// <param name="cmd">Command Buffer</param>
        /// <param name="result">Result RT for the lens flare Screen Space</param>
        /// <param name="debugView">Information if we are in debug mode or not</param>
        static public void DoLensFlareScreenSpaceCommon(
            Material lensFlareShader,
            Camera cam,
            float actualWidth,
            float actualHeight,
            Color tintColor,
            Texture originalBloomTexture,
            Texture bloomMipTexture,
            Texture spectralLut,
            Texture streakTextureTmp,
            Texture streakTextureTmp2,
            Vector4 parameters1,
            Vector4 parameters2,
            Vector4 parameters3,
            Vector4 parameters4,
            Vector4 parameters5,
            Rendering.CommandBuffer cmd,
            RTHandle result,
            bool debugView)
        {

            //Multiplying parameters value here for easier maintenance since they are the same numbers between SRPs
            parameters2.x = Mathf.Pow(parameters2.x, 0.25f);        // Vignette effect
            parameters3.z = parameters3.z / 20f;                    // chromaticAbberationIntensity
            parameters4.y = parameters4.y * 10f;                    // Streak Length
            parameters4.z = parameters4.z / 90f;                    // Streak Orientation
            parameters5.y = 1.0f / parameters5.y;                   // WarpedFlareScale X
            parameters5.z = 1.0f / parameters5.z;                   // WarpedFlareScale Y

            cmd.SetViewport(new Rect() { width = actualWidth, height = actualHeight });
            if (debugView)
            {
                // Background pitch black to see only the flares
                cmd.ClearRenderTarget(false, true, Color.black);
            }

#if UNITY_EDITOR
            if (cam.cameraType == CameraType.SceneView)
            {
                // Determine whether the "Flare" checkbox is checked for the current view.
                for (int i = 0; i < UnityEditor.SceneView.sceneViews.Count; i++) // Using a foreach on an ArrayList generates garbage ...
                {
                    var sv = UnityEditor.SceneView.sceneViews[i] as UnityEditor.SceneView;
                    if (sv.camera == cam && !sv.sceneViewState.flaresEnabled)
                    {
                        return;
                    }
                }
            }
#endif

            // Multiple scaleX by aspect ratio so that default 1:1 scale for warped flare stays circular (as in data driven lens flare)
            float warpedScaleX = parameters5.y;
            warpedScaleX *= actualWidth / actualHeight;
            parameters5.y = warpedScaleX;

            // This is to make sure the streak length is the same in all resolutions
            float streaksLength = parameters4.y;
            streaksLength *= actualWidth * 0.0005f;
            parameters4.y = streaksLength;

            // List of the passes in LensFlareScreenSpace.shader
            int prefilterPass = lensFlareShader.FindPass("LensFlareScreenSpac Prefilter");
            int downSamplePass = lensFlareShader.FindPass("LensFlareScreenSpace Downsample");
            int upSamplePass = lensFlareShader.FindPass("LensFlareScreenSpace Upsample");
            int compositionPass = lensFlareShader.FindPass("LensFlareScreenSpace Composition");
            int writeToBloomPass = lensFlareShader.FindPass("LensFlareScreenSpace Write to BloomTexture");

            // Setting the input textures
            cmd.SetGlobalTexture(_LensFlareScreenSpaceBloomMipTexture, bloomMipTexture);
            cmd.SetGlobalTexture(_LensFlareScreenSpaceSpectralLut, spectralLut);

            // Setting parameters of the effects
            cmd.SetGlobalVector(_LensFlareScreenSpaceParams1, parameters1);
            cmd.SetGlobalVector(_LensFlareScreenSpaceParams2, parameters2);
            cmd.SetGlobalVector(_LensFlareScreenSpaceParams3, parameters3);
            cmd.SetGlobalVector(_LensFlareScreenSpaceParams4, parameters4);
            cmd.SetGlobalVector(_LensFlareScreenSpaceParams5, parameters5);
            cmd.SetGlobalColor(_LensFlareScreenSpaceTintColor, tintColor);

            // We only do the first 3 pass if StreakIntensity (parameters4.x) is set to something above 0 to save costs
            if (parameters4.x > 0)
            {
                // Prefilter
                Rendering.CoreUtils.SetRenderTarget(cmd, streakTextureTmp);
                UnityEngine.Rendering.Blitter.DrawQuad(cmd, lensFlareShader, prefilterPass);

                int maxLevel = Mathf.FloorToInt(Mathf.Log(Mathf.Max(actualHeight, actualWidth), 2.0f));
                int maxLevelDownsample = Mathf.Max(1, maxLevel);
                int maxLevelUpsample = 2;
                int startIndex = 0;
                bool even = false;

                // Downsample
                for (int i = 0; i < maxLevelDownsample; i++)
                {
                    even = (i % 2 == 0);
                    cmd.SetGlobalInt(_LensFlareScreenSpaceMipLevel, i);
                    cmd.SetGlobalTexture(_LensFlareScreenSpaceStreakTex, even ? streakTextureTmp : streakTextureTmp2);
                    Rendering.CoreUtils.SetRenderTarget(cmd, even ? streakTextureTmp2 : streakTextureTmp);

                    UnityEngine.Rendering.Blitter.DrawQuad(cmd, lensFlareShader, downSamplePass);
                }

                //Since we do a ping pong between streakTextureTmp & streakTextureTmp2, we need to know which texture is the last;
                if (even)
                    startIndex = 1;

                //Upsample
                for (int i = startIndex; i < (startIndex + maxLevelUpsample); i++)
                {
                    even = (i % 2 == 0);
                    cmd.SetGlobalInt(_LensFlareScreenSpaceMipLevel, (i - startIndex));
                    cmd.SetGlobalTexture(_LensFlareScreenSpaceStreakTex, even ? streakTextureTmp : streakTextureTmp2);
                    Rendering.CoreUtils.SetRenderTarget(cmd, even ? streakTextureTmp2 : streakTextureTmp);

                    UnityEngine.Rendering.Blitter.DrawQuad(cmd, lensFlareShader, upSamplePass);
                }

                cmd.SetGlobalTexture(_LensFlareScreenSpaceStreakTex, even ? streakTextureTmp2 : streakTextureTmp);
            }

            // Composition (Flares + Streaks)
            Rendering.CoreUtils.SetRenderTarget(cmd, result);
            UnityEngine.Rendering.Blitter.DrawQuad(cmd, lensFlareShader, compositionPass);

            // Final pass, we add the result of the previous pass to the Original Bloom Texture.
            cmd.SetGlobalTexture(_LensFlareScreenSpaceResultTexture, result);
            Rendering.CoreUtils.SetRenderTarget(cmd, originalBloomTexture);
            UnityEngine.Rendering.Blitter.DrawQuad(cmd, lensFlareShader, writeToBloomPass);
        }

#region Panini Projection
        static Vector2 DoPaniniProjection(Vector2 screenPos, float actualWidth, float actualHeight, float fieldOfView, float paniniProjectionCropToFit, float paniniProjectionDistance)
        {
            Vector2 viewExtents = CalcViewExtents(actualWidth, actualHeight, fieldOfView);
            Vector2 cropExtents = CalcCropExtents(actualWidth, actualHeight, fieldOfView, paniniProjectionDistance);

            float scaleX = cropExtents.x / viewExtents.x;
            float scaleY = cropExtents.y / viewExtents.y;
            float scaleF = Mathf.Min(scaleX, scaleY);

            float paniniD = paniniProjectionDistance;
            float paniniS = Mathf.Lerp(1.0f, Mathf.Clamp01(scaleF), paniniProjectionCropToFit);

            Vector2 pos = new Vector2(2.0f * screenPos.x - 1.0f, 2.0f * screenPos.y - 1.0f);

            Vector2 projPos = Panini_Generic_Inv(pos * viewExtents, paniniD) / (viewExtents * paniniS);

            return new Vector2(0.5f * projPos.x + 0.5f, 0.5f * projPos.y + 0.5f);
        }

        static Vector2 CalcViewExtents(float actualWidth, float actualHeight, float fieldOfView)
        {
            float fovY = fieldOfView * Mathf.Deg2Rad;
            float aspect = actualWidth / actualHeight;

            float viewExtY = Mathf.Tan(0.5f * fovY);
            float viewExtX = aspect * viewExtY;

            return new Vector2(viewExtX, viewExtY);
        }

        static Vector2 CalcCropExtents(float actualWidth, float actualHeight, float fieldOfView, float d)
        {
            // given
            //    S----------- E--X-------
            //    |    `  ~.  /,´
            //    |-- ---    Q
            //    |        ,/    `
            //  1 |      ,´/       `
            //    |    ,´ /         ´
            //    |  ,´  /           ´
            //    |,`   /             ,
            //    O    /
            //    |   /               ,
            //  d |  /
            //    | /                ,
            //    |/                .
            //    P
            //    |              ´
            //    |         , ´
            //    +-    ´
            //
            // have X
            // want to find E

            float viewDist = 1.0f + d;

            Vector2 projPos = CalcViewExtents(actualWidth, actualHeight, fieldOfView);
            float projHyp = Mathf.Sqrt(projPos.x * projPos.x + 1.0f);

            float cylDistMinusD = 1.0f / projHyp;
            float cylDist = cylDistMinusD + d;
            Vector2 cylPos = projPos * cylDistMinusD;

            return cylPos * (viewDist / cylDist);
        }

        static Vector2 Panini_Generic_Inv(Vector2 projPos, float d)
        {
            // given
            //    S----------- E--X-------
            //    |    `  ~.  /,´
            //    |-- ---    Q
            //    |        ,/    `
            //  1 |      ,´/       `
            //    |    ,´ /         ´
            //    |  ,´  /           ´
            //    |,`   /             ,
            //    O    /
            //    |   /               ,
            //  d |  /
            //    | /                ,
            //    |/                .
            //    P
            //    |              ´
            //    |         , ´
            //    +-    ´
            //
            // have X
            // want to find E

            float viewDist = 1.0f + d;
            float projHyp = Mathf.Sqrt(projPos.x * projPos.x + 1.0f);

            float cylDistMinusD = 1.0f / projHyp;
            float cylDist = cylDistMinusD + d;
            Vector2 cylPos = projPos * cylDistMinusD;

            return cylPos * (viewDist / cylDist);
        }

#endregion
    }
}
