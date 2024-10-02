using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Common code for all Data-Driven Lens Flare used
    /// </summary>
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
        /// Max lens-flares-with-occlusion supported
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

        private static readonly bool s_SupportsLensFlare16bitsFormat = SystemInfo.IsFormatSupported(GraphicsFormat.R16_SFloat, GraphicsFormatUsage.Render);
        private static readonly bool s_SupportsLensFlare32bitsFormat = SystemInfo.IsFormatSupported(GraphicsFormat.R32_SFloat, GraphicsFormatUsage.Render);

        /// <summary>
        /// Check if we can use an OcclusionRT
        /// </summary>
        /// <returns>return true if we can have the OcclusionRT</returns>
        static public bool IsOcclusionRTCompatible()
        {
#if UNITY_SERVER
            return false;
#else
            return SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES3 &&
                    SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLCore &&
                    SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null &&
                    SystemInfo.graphicsDeviceType != GraphicsDeviceType.WebGPU &&
                    (s_SupportsLensFlare16bitsFormat || s_SupportsLensFlare32bitsFormat); //Caching this, because SupportsRenderTextureFormat allocates memory. Go figure.
#endif
        }

        static GraphicsFormat GetOcclusionRTFormat()
        {
            // SystemInfo.graphicsDeviceType == {GraphicsDeviceType.Direct3D12, GraphicsDeviceType.GameCoreXboxSeries, GraphicsDeviceType.XboxOneD3D12, GraphicsDeviceType.PlayStation5, ...}
            if (s_SupportsLensFlare16bitsFormat)
                return GraphicsFormat.R16_SFloat;
            else
                // Needed a R32_SFloat for Metal or/and DirectX < 11.3
                return GraphicsFormat.R32_SFloat;
        }

        /// <summary>
        /// Initialization function which must be called by the SRP.
        /// </summary>
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
                        enableRandomWrite: true,
                        dimension: TextureDimension.Tex2DArray);
                }
            }
        }

        /// <summary>
        /// Disposal function, must be called by the SRP to release all internal textures.
        /// </summary>
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
        /// Current unique instance
        /// </summary>
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
        /// Check if we have at least one Lens Flare added on the pool
        /// </summary>
        /// <returns>true if no Lens Flare were added</returns>
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
        /// Add a new lens flare component on the pool.
        /// </summary>
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
        /// Remove a lens flare data which exist in the pool.
        /// </summary>
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
        /// Attenuation by Light Shape for Point Light
        /// </summary>
        /// <returns>Attenuation Factor</returns>
        static public float ShapeAttenuationPointLight()
        {
            return 1.0f;
        }

        /// <summary>
        /// Attenuation by Light Shape for Directional Light
        /// </summary>
        /// <param name="forward">Forward Vector of Directional Light</param>
        /// <param name="wo">Vector pointing to the eye</param>
        /// <returns>Attenuation Factor</returns>
        static public float ShapeAttenuationDirLight(Vector3 forward, Vector3 wo)
        {
            return Mathf.Max(Vector3.Dot(-forward, wo), 0.0f);
        }

        /// <summary>
        /// Attenuation by Light Shape for Spot Light with Cone Shape
        /// </summary>
        /// <param name="forward">Forward Vector of Directional Light</param>
        /// <param name="wo">Vector pointing to the eye</param>
        /// <param name="spotAngle">The angle of the light's spotlight cone in degrees.</param>
        /// <param name="innerSpotPercent01">Get the inner spot radius between 0 and 1.</param>
        /// <returns>Attenuation Factor</returns>
        static public float ShapeAttenuationSpotConeLight(Vector3 forward, Vector3 wo, float spotAngle, float innerSpotPercent01)
        {
            float outerDot = Mathf.Max(Mathf.Cos(0.5f * spotAngle * Mathf.Deg2Rad), 0.0f);
            float innerDot = Mathf.Max(Mathf.Cos(0.5f * spotAngle * Mathf.Deg2Rad * innerSpotPercent01), 0.0f);
            float dot = Mathf.Max(Vector3.Dot(forward, wo), 0.0f);
            return Mathf.Clamp01((dot - outerDot) / (innerDot - outerDot));
        }

        /// <summary>
        /// Attenuation by Light Shape for Spot Light with Box Shape
        /// </summary>
        /// <param name="forward">Forward Vector of Directional Light</param>
        /// <param name="wo">Vector pointing to the eye</param>
        /// <returns>Attenuation Factor</returns>
        static public float ShapeAttenuationSpotBoxLight(Vector3 forward, Vector3 wo)
        {
            return Mathf.Max(Mathf.Sign(Vector3.Dot(forward, wo)), 0.0f);
        }

        /// <summary>
        /// Attenuation by Light Shape for Spot Light with Pyramid Shape
        /// </summary>
        /// <param name="forward">Forward Vector of Directional Light</param>
        /// <param name="wo">Vector pointing to the eye</param>
        /// <returns>Attenuation Factor</returns>
        static public float ShapeAttenuationSpotPyramidLight(Vector3 forward, Vector3 wo)
        {
            return ShapeAttenuationSpotBoxLight(forward, wo);
        }

        /// <summary>
        /// Attenuation by Light Shape for Area Light with Tube Shape
        /// </summary>
        /// <param name="lightPositionWS">World Space position of the Light</param>
        /// <param name="lightSide">Vector pointing to the side (right or left) or the light</param>
        /// <param name="lightWidth">Width (half extent) of the tube light</param>
        /// <param name="cam">Camera rendering the Tube Light</param>
        /// <returns>Attenuation Factor</returns>
        static public float ShapeAttenuationAreaTubeLight(Vector3 lightPositionWS, Vector3 lightSide, float lightWidth, Camera cam)
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

            Vector3 p1Global = lightPositionWS + lightSide * lightWidth * 0.5f;
            Vector3 p2Global = lightPositionWS - lightSide * lightWidth * 0.5f;
            Vector3 p1Front = lightPositionWS + cam.transform.right * lightWidth * 0.5f;
            Vector3 p2Front = lightPositionWS - cam.transform.right * lightWidth * 0.5f;

            Vector3 p1World = cam.transform.InverseTransformPoint(p1Global);
            Vector3 p2World = cam.transform.InverseTransformPoint(p2Global);
            Vector3 p1WorldFront = cam.transform.InverseTransformPoint(p1Front);
            Vector3 p2WorldFront = cam.transform.InverseTransformPoint(p2Front);

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
        /// Attenuation by Light Shape for Area Light with Rectangular Shape
        /// </summary>
        /// <param name="forward">Forward Vector of Directional Light</param>
        /// <param name="wo">Vector pointing to the eye</param>
        /// <returns>Attenuation Factor</returns>
        static public float ShapeAttenuationAreaRectangleLight(Vector3 forward, Vector3 wo)
        {
            return ShapeAttenuateForwardLight(forward, wo);
        }

        /// <summary>
        /// Attenuation by Light Shape for Area Light with Disc Shape
        /// </summary>
        /// <param name="forward">Forward Vector of Directional Light</param>
        /// <param name="wo">Vector pointing to the eye</param>
        /// <returns>Attenuation Factor</returns>
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

        /// <summary>
        /// Compute internal parameters needed to render single flare
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
        static public Vector4 GetFlareData0(Vector2 screenPos, Vector2 translationScale, Vector2 rayOff0, Vector2 vLocalScreenRatio, float angleDeg, float position, float angularOffset, Vector2 positionOffset, bool autoRotate)
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

        static Vector2 GetLensFlareRayOffset(Vector2 screenPos, float position, float globalCos0, float globalSin0, Vector2 vAspectRatio)
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
        /// Check if at least one LensFlareComponentSRP request occlusion from background clouds
        /// </summary>
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

        static void SetOcclusionPermutation(CommandBuffer cmd, bool useFogOpacityOcclusion, int _FlareSunOcclusionTex, Texture sunOcclusionTexture)
        {
            uint occlusionPermutation = (uint)(LensFlareOcclusionPermutation.Depth);

            if (useFogOpacityOcclusion && sunOcclusionTexture != null)
            {
                occlusionPermutation |= (uint)(LensFlareOcclusionPermutation.FogOpacity);
                cmd.SetGlobalTexture(_FlareSunOcclusionTex, sunOcclusionTexture);
            }

            int convInt = unchecked((int)occlusionPermutation);
            cmd.SetGlobalInt(_FlareOcclusionPermutation, convInt);
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
        /// Effective Job of drawing the set of Lens Flare registered
        /// </summary>
        /// <param name="lensFlareShader">Lens Flare material (HDRP or URP shader)</param>
        /// <param name="cam">Camera</param>
        /// <param name="xr">XR Infos</param>
        /// <param name="xrIndex">Index of the SinglePass XR</param>
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
        /// <param name="_FlareOcclusionTex">ShaderID for the FlareOcclusionTex</param>
        /// <param name="_FlareCloudOpacity">ShaderID for the FlareCloudOpacity</param>
        /// <param name="_FlareOcclusionIndex">ShaderID for the FlareOcclusionIndex</param>
        /// <param name="_FlareTex">ShaderID for the FlareTex</param>
        /// <param name="_FlareColorValue">ShaderID for the FlareColor</param>
        /// <param name="_FlareSunOcclusionTex">ShaderID for the _FlareSunOcclusionTex</param>
        /// <param name="_FlareData0">ShaderID for the FlareData0</param>
        /// <param name="_FlareData1">ShaderID for the FlareData1</param>
        /// <param name="_FlareData2">ShaderID for the FlareData2</param>
        /// <param name="_FlareData3">ShaderID for the FlareData3</param>
        /// <param name="_FlareData4">ShaderID for the FlareData4</param>
        [Obsolete("Use ComputeOcclusion without _FlareOcclusionTex.._FlareData4 parameters.")]
        static public void ComputeOcclusion(Material lensFlareShader, Camera cam, XRPass xr, int xrIndex,
            float actualWidth, float actualHeight,
            bool usePanini, float paniniDistance, float paniniCropToFit, bool isCameraRelative,
            Vector3 cameraPositionWS,
            Matrix4x4 viewProjMatrix,
            UnsafeCommandBuffer cmd,
            bool taaEnabled, bool hasCloudLayer, Texture cloudOpacityTexture, Texture sunOcclusionTexture,
            int _FlareOcclusionTex, int _FlareCloudOpacity, int _FlareOcclusionIndex, int _FlareTex, int _FlareColorValue, int _FlareSunOcclusionTex, int _FlareData0, int _FlareData1, int _FlareData2, int _FlareData3, int _FlareData4)
        {
            ComputeOcclusion(
                lensFlareShader, cam, xr, xrIndex,
                actualWidth, actualHeight,
                usePanini, paniniDistance, paniniCropToFit, isCameraRelative,
                cameraPositionWS,
                viewProjMatrix,
                cmd.m_WrappedCommandBuffer,
                taaEnabled, hasCloudLayer, cloudOpacityTexture, sunOcclusionTexture,
                _FlareOcclusionTex, _FlareCloudOpacity, _FlareOcclusionIndex, _FlareTex, _FlareColorValue, _FlareSunOcclusionTex, _FlareData0, _FlareData1, _FlareData2, _FlareData3, _FlareData4);
        }

        /// <summary>
        /// Effective Job of drawing the set of Lens Flare registered
        /// </summary>
        /// <param name="lensFlareShader">Lens Flare material (HDRP or URP shader)</param>
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

        /// <summary>
        /// Effective Job of drawing the set of Lens Flare registered
        /// </summary>
        /// <param name="lensFlareShader">Lens Flare material (HDRP or URP shader)</param>
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
        /// <param name="_FlareOcclusionTex">ShaderID for the FlareOcclusionTex</param>
        /// <param name="_FlareCloudOpacity">ShaderID for the FlareCloudOpacity</param>
        /// <param name="_FlareOcclusionIndex">ShaderID for the FlareOcclusionIndex</param>
        /// <param name="_FlareTex">ShaderID for the FlareTex</param>
        /// <param name="_FlareColorValue">ShaderID for the FlareColor</param>
        /// <param name="_FlareSunOcclusionTex">ShaderID for the _FlareSunOcclusionTex</param>
        /// <param name="_FlareData0">ShaderID for the FlareData0</param>
        /// <param name="_FlareData1">ShaderID for the FlareData1</param>
        /// <param name="_FlareData2">ShaderID for the FlareData2</param>
        /// <param name="_FlareData3">ShaderID for the FlareData3</param>
        /// <param name="_FlareData4">ShaderID for the FlareData4</param>
        [Obsolete("Use ComputeOcclusion without _FlareOcclusionTex.._FlareData4 parameters.")]
        static public void ComputeOcclusion(Material lensFlareShader, Camera cam, XRPass xr, int xrIndex,
            float actualWidth, float actualHeight,
            bool usePanini, float paniniDistance, float paniniCropToFit, bool isCameraRelative,
            Vector3 cameraPositionWS,
            Matrix4x4 viewProjMatrix,
            Rendering.CommandBuffer cmd,
            bool taaEnabled, bool hasCloudLayer, Texture cloudOpacityTexture, Texture sunOcclusionTexture,
            int _FlareOcclusionTex, int _FlareCloudOpacity, int _FlareOcclusionIndex, int _FlareTex, int _FlareColorValue, int _FlareSunOcclusionTex, int _FlareData0, int _FlareData1, int _FlareData2, int _FlareData3, int _FlareData4)
        {
            ComputeOcclusion(lensFlareShader, cam, xr, xrIndex,
                actualWidth, actualHeight,
                usePanini, paniniDistance, paniniCropToFit, isCameraRelative,
                cameraPositionWS,
                viewProjMatrix,
                cmd,
                taaEnabled, hasCloudLayer, cloudOpacityTexture, sunOcclusionTexture);
        }

        static bool ForceSingleElement(LensFlareDataElementSRP element)
        {
            return !element.allowMultipleElement
                || element.count == 1
                || element.flareType == SRPLensFlareType.Ring;
        }

        /// <summary>
        /// Effective Job of drawing the set of Lens Flare registered
        /// </summary>
        /// <param name="lensFlareShader">Lens Flare material (HDRP or URP shader)</param>
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

            xr.StopSinglePass(cmd);

#if UNITY_EDITOR
            bool inPrefabStage = IsPrefabStageEnabled();
            UnityEditor.SceneManagement.PrefabStage prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            GameObject prefabGameObject = null;
            LensFlareComponentSRP[] prefabStageLensFlares = null;
            if (prefabStage != null)
            {
                prefabGameObject = prefabStage.prefabContentsRoot;
                if (prefabGameObject == null)
                    return;
                prefabStageLensFlares = GetLensFlareComponents(prefabGameObject);
                if (prefabStageLensFlares.Length == 0)
                {
                    return;
                }
            }
#endif

            if (Instance.IsEmpty())
                return;

#if UNITY_EDITOR
            if (cam.cameraType == CameraType.SceneView)
            {
                // Determine whether the "Animated Materials" checkbox is checked for the current view.
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

            Vector2 screenSize = new Vector2(actualWidth, actualHeight);
            float screenRatio = screenSize.x / screenSize.y;
            Vector2 vScreenRatio = new Vector2(screenRatio, 1.0f);

#if ENABLE_VR && ENABLE_XR_MODULE
            if (xr.enabled && xr.singlePassEnabled)
            {
                CoreUtils.SetRenderTarget(cmd, occlusionRT, depthSlice: xrIndex);
                cmd.SetGlobalInt(_ViewId, xrIndex);
            }
            else
#endif
            {
                CoreUtils.SetRenderTarget(cmd, occlusionRT);
                if (xr.enabled) // multipass
                    cmd.SetGlobalInt(_ViewId, xr.multipassId);
                else
                    cmd.SetGlobalInt(_ViewId, -1);
            }

            if (!taaEnabled)
            {
                cmd.ClearRenderTarget(false, true, Color.black);
            }

            float dx = 1.0f / ((float)maxLensFlareWithOcclusion);
            float dy = 1.0f / ((float)(maxLensFlareWithOcclusionTemporalSample + mergeNeeded));
            float halfx = 0.5f / ((float)maxLensFlareWithOcclusion);
            float halfy = 0.5f / ((float)(maxLensFlareWithOcclusionTemporalSample + mergeNeeded));

            foreach (LensFlareCompInfo info in m_Data)
            {
                if (info == null || info.comp == null)
                    continue;

                LensFlareComponentSRP comp = info.comp;
                LensFlareDataSRP data = comp.lensFlareData;

                if (IsLensFlareSRPHidden(cam, comp, data) ||
                    !comp.useOcclusion ||
                    (comp.useOcclusion && comp.sampleCount == 0))
                    continue;

#if UNITY_EDITOR
                if (inPrefabStage && !IsCurrentPrefabLensFlareComponent(prefabGameObject, prefabStageLensFlares, comp))
                {
                    continue;
                }
#endif

                Light light = null;
                if (!comp.TryGetComponent<Light>(out light))
                    light = null;

                Vector3 positionWS;
                Vector3 viewportPos;

                bool isDirLight = false;
                if (light != null && light.type == LightType.Directional)
                {
                    positionWS = -light.transform.forward * cam.farClipPlane;
                    isDirLight = true;
                }
                else
                {
                    positionWS = comp.transform.position;
                }

                viewportPos = WorldToViewport(cam, !isDirLight, isCameraRelative, viewProjMatrix, positionWS);

                if (usePanini && cam == Camera.main)
                {
                    viewportPos = DoPaniniProjection(viewportPos, actualWidth, actualHeight, cam.fieldOfView, paniniCropToFit, paniniDistance);
                }

                if (viewportPos.z < 0.0f)
                    continue;

                if (!comp.allowOffScreen)
                {
                    if (viewportPos.x < 0.0f || viewportPos.x > 1.0f ||
                        viewportPos.y < 0.0f || viewportPos.y > 1.0f)
                        continue;
                }

                Vector3 diffToObject = positionWS - cameraPositionWS;
                // Check if the light is forward, can be an issue with,
                // the math associated to Panini projection
                if (Vector3.Dot(cam.transform.forward, diffToObject) < 0.0f)
                {
                    continue;
                }
                float distToObject = diffToObject.magnitude;
                float coefDistSample = distToObject / comp.maxAttenuationDistance;
                float coefScaleSample = distToObject / comp.maxAttenuationScale;
                float distanceAttenuation = !isDirLight && comp.distanceAttenuationCurve.length > 0 ? comp.distanceAttenuationCurve.Evaluate(coefDistSample) : 1.0f;
                float scaleByDistance = !isDirLight && comp.scaleByDistanceCurve.length >= 1 ? comp.scaleByDistanceCurve.Evaluate(coefScaleSample) : 1.0f;

                Vector3 dir;
                if (isDirLight)
                    dir = comp.transform.forward;
                else
                    dir = (cam.transform.position - comp.transform.position).normalized;
                Vector3 screenPosZ = WorldToViewport(cam, !isDirLight, isCameraRelative, viewProjMatrix, positionWS + dir * comp.occlusionOffset);

                float adjustedOcclusionRadius = isDirLight ? comp.celestialProjectedOcclusionRadius(cam) : comp.occlusionRadius;
                Vector2 occlusionRadiusEdgeScreenPos0 = (Vector2)viewportPos;
                Vector2 occlusionRadiusEdgeScreenPos1 = (Vector2)WorldToViewport(cam, !isDirLight, isCameraRelative, viewProjMatrix, positionWS + cam.transform.up * adjustedOcclusionRadius);
                float occlusionRadius = (occlusionRadiusEdgeScreenPos1 - occlusionRadiusEdgeScreenPos0).magnitude;

                cmd.SetGlobalVector(_FlareData1, new Vector4(occlusionRadius, comp.sampleCount, screenPosZ.z, actualHeight / actualWidth));

                SetOcclusionPermutation(cmd, comp.environmentOcclusion, _FlareSunOcclusionTex, sunOcclusionTexture);
                cmd.EnableShaderKeyword("FLARE_COMPUTE_OCCLUSION");

                Vector2 screenPos = new Vector2(2.0f * viewportPos.x - 1.0f, -(2.0f * viewportPos.y - 1.0f));
                if (!SystemInfo.graphicsUVStartsAtTop && isDirLight)
                    screenPos.y = -screenPos.y;

                Vector2 radPos = new Vector2(Mathf.Abs(screenPos.x), Mathf.Abs(screenPos.y));
                float radius = Mathf.Max(radPos.x, radPos.y); // l1 norm (instead of l2 norm)
                float radialsScaleRadius = comp.radialScreenAttenuationCurve.length > 0 ? comp.radialScreenAttenuationCurve.Evaluate(radius) : 1.0f;

                float compIntensity = comp.intensity * radialsScaleRadius * distanceAttenuation;

                if (compIntensity <= 0.0f)
                    continue;

                float globalCos0 = Mathf.Cos(0.0f);
                float globalSin0 = Mathf.Sin(0.0f);

                float position = 0.0f;

                float usedGradientPosition = Mathf.Clamp01(1.0f - 1e-6f);

                cmd.SetGlobalVector(_FlareData3, new Vector4(comp.allowOffScreen ? 1.0f : -1.0f, usedGradientPosition, Mathf.Exp(Mathf.Lerp(0.0f, 4.0f, 1.0f)), 1.0f / 3.0f));

                Vector2 rayOff = GetLensFlareRayOffset(screenPos, position, globalCos0, globalSin0, vScreenRatio);
                Vector4 flareData0 = GetFlareData0(screenPos, Vector2.one, rayOff, vScreenRatio, 0.0f, position, 0.0f, Vector2.zero, false);

                cmd.SetGlobalVector(_FlareData0, flareData0);
                cmd.SetGlobalVector(_FlareData2, new Vector4(screenPos.x, screenPos.y, 0.0f, 0.0f));

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
        /// Function that process a single element of a LensFlareDataSRP, this function is used on scene/game view and on the inspector for the thumbnail.
        /// </summary>
        /// <param name="element">Single LensFlare asset we need to process.</param>
        /// <param name="cmd">Command Buffer.</param>
        /// <param name="globalColorModulation">Color Modulation from Component?</param>
        /// <param name="light">Light used for the modulation of this singe element.</param>
        /// <param name="compIntensity">Intensity from Component.</param>
        /// <param name="scale">Scale from component</param>
        /// <param name="lensFlareShader">Shader used on URP or HDRP.</param>
        /// <param name="screenPos">Screen Position</param>
        /// <param name="compAllowOffScreen">Allow Lens Flare offscreen</param>
        /// <param name="vScreenRatio">Screen Ratio</param>
        /// <param name="flareData1">_FlareData1 used internally by the shader.</param>
        /// <param name="preview">true if we are on preview on the inspector</param>
        /// <param name="depth">Depth counter for recursive call of 'ProcessLensFlareSRPElementsSingle'.</param>
        public static void ProcessLensFlareSRPElementsSingle(LensFlareDataElementSRP element, Rendering.CommandBuffer cmd, Color globalColorModulation, Light light,
            float compIntensity, float scale, Material lensFlareShader, Vector2 screenPos, bool compAllowOffScreen, Vector2 vScreenRatio, Vector4 flareData1, bool preview, int depth)
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
                ProcessLensFlareSRPElements(ref element.lensFlareDataSRP.elements, cmd, globalColorModulation, light, compIntensity, scale, lensFlareShader, screenPos, compAllowOffScreen, vScreenRatio, flareData1, preview, depth + 1);
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

            flareData1.x = (float)element.flareType;
            if (ForceSingleElement(element))
                cmd.SetGlobalVector(_FlareData1, flareData1);

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
                Vector2 rayOffZ = GetLensFlareRayOffset(screenPos, position, globalCos0, globalSin0, vScreenRatio);
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
                Vector2 rayOff = GetLensFlareRayOffset(screenPos, position, globalCos0, globalSin0, vScreenRatio);
                if (element.enableRadialDistortion)
                {
                    Vector2 rayOff0 = GetLensFlareRayOffset(screenPos, 0.0f, globalCos0, globalSin0, vScreenRatio);
                    localSize = ComputeLocalSize(rayOff, rayOff0, localSize, element.distortionCurve);
                }
                Vector4 flareData0 = GetFlareData0(screenPos, element.translationScale, rayOff, vScreenRatio, rotation, position, angularOffset, element.positionOffset, element.autoRotate);

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
                        Vector2 rayOff = GetLensFlareRayOffset(screenPos, position, globalCos0, globalSin0, vScreenRatio);
                        if (element.enableRadialDistortion)
                        {
                            Vector2 rayOff0 = GetLensFlareRayOffset(screenPos, 0.0f, globalCos0, globalSin0, vScreenRatio);
                            localSize = ComputeLocalSize(rayOff, rayOff0, localSize, element.distortionCurve);
                        }

                        float timeScale = element.count >= 2 ? ((float)elemIdx) / ((float)(element.count - 1)) : 0.5f;

                        Color col = element.colorGradient.Evaluate(timeScale);

                        Vector4 flareData0 = GetFlareData0(screenPos, element.translationScale, rayOff, vScreenRatio, rotation + uniformAngle, position, angularOffset, element.positionOffset, element.autoRotate);
                        cmd.SetGlobalVector(_FlareData0, flareData0);

                        flareData1.y = (float)elemIdx;
                        cmd.SetGlobalVector(_FlareData1, flareData1);
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

                        Vector2 rayOff = GetLensFlareRayOffset(screenPos, position, globalCos0, globalSin0, vScreenRatio);
                        Vector2 localSize = size;
                        if (element.enableRadialDistortion)
                        {
                            Vector2 rayOff0 = GetLensFlareRayOffset(screenPos, 0.0f, globalCos0, globalSin0, vScreenRatio);
                            localSize = ComputeLocalSize(rayOff, rayOff0, localSize, element.distortionCurve);
                        }

                        localSize += localSize * (element.scaleVariation * RandomRange(-1.0f, 1.0f));

                        Color randCol = element.colorGradient.Evaluate(RandomRange(0.0f, 1.0f));

                        Vector2 localPositionOffset = element.positionOffset + RandomRange(-1.0f, 1.0f) * side;

                        float localRotation = rotation + RandomRange(-Mathf.PI, Mathf.PI) * element.rotationVariation;

                        if (localIntensity > 0.0f)
                        {
                            Vector4 flareData0 = GetFlareData0(screenPos, element.translationScale, rayOff, vScreenRatio, localRotation, position, angularOffset, localPositionOffset, element.autoRotate);
                            cmd.SetGlobalVector(_FlareData0, flareData0);
                            flareData1.y = (float)elemIdx;
                            cmd.SetGlobalVector(_FlareData1, flareData1);
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
                        Vector2 rayOff = GetLensFlareRayOffset(screenPos, localPos, globalCos0, globalSin0, vScreenRatio);
                        Vector2 localSize = size;
                        if (element.enableRadialDistortion)
                        {
                            Vector2 rayOff0 = GetLensFlareRayOffset(screenPos, 0.0f, globalCos0, globalSin0, vScreenRatio);
                            localSize = ComputeLocalSize(rayOff, rayOff0, localSize, element.distortionCurve);
                        }
                        float sizeCurveValue = element.scaleCurve.length > 0 ? element.scaleCurve.Evaluate(timeScale) : 1.0f;
                        localSize *= sizeCurveValue;

                        float angleFromCurve = element.uniformAngleCurve.Evaluate(timeScale) * (180.0f - (180.0f / (float)element.count));

                        Vector4 flareData0 = GetFlareData0(screenPos, element.translationScale, rayOff, vScreenRatio, rotation + angleFromCurve, localPos, angularOffset, element.positionOffset, element.autoRotate);
                        cmd.SetGlobalVector(_FlareData0, flareData0);
                        flareData1.y = (float)elemIdx;
                        cmd.SetGlobalVector(_FlareData1, flareData1);
                        cmd.SetGlobalVector(_FlareData2, new Vector4(screenPos.x, screenPos.y, localSize.x, localSize.y));
                        cmd.SetGlobalVector(_FlareColorValue, curColor * col);

                        UnityEngine.Rendering.Blitter.DrawQuad(cmd, lensFlareShader, materialPass);
                    }
                }
            }
        }

        static void ProcessLensFlareSRPElements(ref LensFlareDataElementSRP[] elements, Rendering.CommandBuffer cmd, Color globalColorModulation, Light light,
            float compIntensity, float scale, Material lensFlareShader, Vector2 screenPos, bool compAllowOffScreen, Vector2 vScreenRatio, Vector4 flareData1, bool preview, int depth)
        {
            if (depth > 16)
            {
                Debug.LogWarning("LensFlareSRPAsset contains too deep recursive asset (> 16). Be careful to not have recursive aggregation, A contains B, B contains A, ... which will produce an infinite loop.");
                return;
            }

            foreach (LensFlareDataElementSRP element in elements)
            {
                ProcessLensFlareSRPElementsSingle(element, cmd, globalColorModulation, light, compIntensity, scale, lensFlareShader, screenPos, compAllowOffScreen, vScreenRatio, flareData1, preview, depth);
            }
        }

        /// <summary>
        /// Effective Job of drawing the set of Lens Flare registered
        /// </summary>
        /// <param name="lensFlareShader">Lens Flare material (HDRP or URP shader)</param>
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
        /// <param name="_FlareOcclusionTex">ShaderID for the FlareOcclusionTex</param>
        /// <param name="_FlareOcclusionIndex">ShaderID for the FlareOcclusionIndex</param>
        /// <param name="_FlareOcclusionRemapTex">ShaderID for the OcclusionRemap</param>
        /// <param name="_FlareCloudOpacity">ShaderID for the FlareCloudOpacity</param>
        /// <param name="_FlareSunOcclusionTex">ShaderID for the _FlareSunOcclusionTex</param>
        /// <param name="_FlareTex">ShaderID for the FlareTex</param>
        /// <param name="_FlareColorValue">ShaderID for the FlareColor</param>
        /// <param name="_FlareData0">ShaderID for the FlareData0</param>
        /// <param name="_FlareData1">ShaderID for the FlareData1</param>
        /// <param name="_FlareData2">ShaderID for the FlareData2</param>
        /// <param name="_FlareData3">ShaderID for the FlareData3</param>
        /// <param name="_FlareData4">ShaderID for the FlareData4</param>
        /// <param name="debugView">Debug View which setup black background to see only Lens Flare</param>
        [Obsolete("Use DoLensFlareDataDrivenCommon without _FlareOcclusionRemapTex.._FlareData4 parameters.")]
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
            int _FlareOcclusionRemapTex, int _FlareOcclusionTex, int _FlareOcclusionIndex,
            int _FlareCloudOpacity, int _FlareSunOcclusionTex,
            int _FlareTex, int _FlareColorValue, int _FlareData0, int _FlareData1, int _FlareData2, int _FlareData3, int _FlareData4,
            bool debugView)
        {
            DoLensFlareDataDrivenCommon(lensFlareShader, cam, viewport, xr, xrIndex,
                actualWidth, actualHeight,
                usePanini, paniniDistance, paniniCropToFit,
                isCameraRelative,
                cameraPositionWS,
                viewProjMatrix,
                cmd,
                taaEnabled, hasCloudLayer, cloudOpacityTexture, sunOcclusionTexture,
                colorBuffer,
                GetLensFlareLightAttenuation,
                debugView);
        }

        /// <summary>
        /// Effective Job of drawing the set of Lens Flare registered
        /// </summary>
        /// <param name="lensFlareShader">Lens Flare material (HDRP or URP shader)</param>
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
        /// <param name="debugView">Debug View which setup black background to see only Lens Flare</param>
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
        /// Effective Job of drawing the set of Lens Flare registered
        /// </summary>
        /// <param name="lensFlareShader">Lens Flare material (HDRP or URP shader)</param>
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
        /// <param name="_FlareOcclusionTex">ShaderID for the FlareOcclusionTex</param>
        /// <param name="_FlareOcclusionIndex">ShaderID for the FlareOcclusionIndex</param>
        /// <param name="_FlareOcclusionRemapTex">ShaderID for the OcclusionRemap</param>
        /// <param name="_FlareCloudOpacity">ShaderID for the FlareCloudOpacity</param>
        /// <param name="_FlareSunOcclusionTex">ShaderID for the _FlareSunOcclusionTex</param>
        /// <param name="_FlareTex">ShaderID for the FlareTex</param>
        /// <param name="_FlareColorValue">ShaderID for the FlareColor</param>
        /// <param name="_FlareData0">ShaderID for the FlareData0</param>
        /// <param name="_FlareData1">ShaderID for the FlareData1</param>
        /// <param name="_FlareData2">ShaderID for the FlareData2</param>
        /// <param name="_FlareData3">ShaderID for the FlareData3</param>
        /// <param name="_FlareData4">ShaderID for the FlareData4</param>
        /// <param name="debugView">Debug View which setup black background to see only Lens Flare</param>
        [Obsolete("Use DoLensFlareDataDrivenCommon without _FlareOcclusionRemapTex.._FlareData4 parameters.")]
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
            int _FlareOcclusionRemapTex, int _FlareOcclusionTex, int _FlareOcclusionIndex,
            int _FlareCloudOpacity, int _FlareSunOcclusionTex,
            int _FlareTex, int _FlareColorValue, int _FlareData0, int _FlareData1, int _FlareData2, int _FlareData3, int _FlareData4,
            bool debugView)
        {
            DoLensFlareDataDrivenCommon(lensFlareShader, cam, viewport, xr, xrIndex,
                actualWidth, actualHeight,
                usePanini, paniniDistance, paniniCropToFit,
                isCameraRelative,
                cameraPositionWS,
                viewProjMatrix,
                cmd,
                taaEnabled, hasCloudLayer, cloudOpacityTexture, sunOcclusionTexture,
                colorBuffer,
                GetLensFlareLightAttenuation,
                debugView);
        }

        /// <summary>
        /// Effective Job of drawing the set of Lens Flare registered
        /// </summary>
        /// <param name="lensFlareShader">Lens Flare material (HDRP or URP shader)</param>
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
        /// <param name="debugView">Debug View which setup black background to see only Lens Flare</param>
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
#if UNITY_EDITOR
            bool inPrefabStage = IsPrefabStageEnabled();
            UnityEditor.SceneManagement.PrefabStage prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            GameObject prefabGameObject = null;
            LensFlareComponentSRP[] prefabStageLensFlares = null;
            if (prefabStage != null)
            {
                prefabGameObject = prefabStage.prefabContentsRoot;
                if (prefabGameObject == null)
                    return;
                prefabStageLensFlares = GetLensFlareComponents(prefabGameObject);
                if (prefabStageLensFlares.Length == 0)
                {
                    return;
                }
            }
#endif

            xr.StopSinglePass(cmd);

            Vector2 vScreenRatio;

            if (Instance.IsEmpty())
                return;

#if UNITY_EDITOR
            if (cam.cameraType == CameraType.SceneView)
            {
                // Determine whether the "Animated Materials" checkbox is checked for the current view.
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

            Vector2 screenSize = new Vector2(actualWidth, actualHeight);
            float screenRatio = screenSize.x / screenSize.y;
            vScreenRatio = new Vector2(screenRatio, 1.0f);

#if ENABLE_VR && ENABLE_XR_MODULE
            if (xr.enabled && xr.singlePassEnabled)
            {
                CoreUtils.SetRenderTarget(cmd, colorBuffer, depthSlice: xrIndex);
                cmd.SetGlobalInt(_ViewId, xrIndex);
            }
            else
#endif
            {
                CoreUtils.SetRenderTarget(cmd, colorBuffer);
                if (xr.enabled) // multipass
                    cmd.SetGlobalInt(_ViewId, xr.multipassId);
                else
                    cmd.SetGlobalInt(_ViewId, 0);
            }

            cmd.SetViewport(viewport);
            if (debugView)
            {
                // Background pitch black to see only the Flares
                cmd.ClearRenderTarget(false, true, Color.black);
            }

            foreach (LensFlareCompInfo info in m_Data)
            {
                if (info == null || info.comp == null)
                    continue;

                LensFlareComponentSRP comp = info.comp;
                LensFlareDataSRP data = comp.lensFlareData;

                if (IsLensFlareSRPHidden(cam, comp, data))
                    continue;

#if UNITY_EDITOR
                if (inPrefabStage && !IsCurrentPrefabLensFlareComponent(prefabGameObject, prefabStageLensFlares, comp))
                {
                    continue;
                }
#endif

                Light light = null;
                if (!comp.TryGetComponent<Light>(out light))
                    light = null;

                Vector3 positionWS;
                Vector3 viewportPos;

                bool isDirLight = false;
                if (light != null && light.type == LightType.Directional)
                {
                    positionWS = -light.transform.forward * cam.farClipPlane;
                    isDirLight = true;
                }
                else
                {
                    positionWS = comp.transform.position;
                }

                // After positionWS computation, lightOverride do not change the position
                if (comp.lightOverride != null)
                {
                    light = comp.lightOverride;
                }

                viewportPos = WorldToViewport(cam, !isDirLight, isCameraRelative, viewProjMatrix, positionWS);

                if (usePanini && cam == Camera.main)
                {
                    viewportPos = DoPaniniProjection(viewportPos, actualWidth, actualHeight, cam.fieldOfView, paniniCropToFit, paniniDistance);
                }

                if (viewportPos.z < 0.0f)
                    continue;

                if (!comp.allowOffScreen)
                {
                    if (viewportPos.x < 0.0f || viewportPos.x > 1.0f ||
                        viewportPos.y < 0.0f || viewportPos.y > 1.0f)
                        continue;
                }

                Vector3 diffToObject = positionWS - cameraPositionWS;
                // Check if the light is forward, can be an issue with,
                // the math associated to Panini projection
                if (Vector3.Dot(cam.transform.forward, diffToObject) < 0.0f)
                {
                    continue;
                }
                float distToObject = diffToObject.magnitude;
                float coefDistSample = distToObject / comp.maxAttenuationDistance;
                float coefScaleSample = distToObject / comp.maxAttenuationScale;
                float distanceAttenuation = !isDirLight && comp.distanceAttenuationCurve.length > 0 ? comp.distanceAttenuationCurve.Evaluate(coefDistSample) : 1.0f;
                float scaleByDistance = !isDirLight && comp.scaleByDistanceCurve.length >= 1 ? comp.scaleByDistanceCurve.Evaluate(coefScaleSample) : 1.0f;

                Color globalColorModulation = Color.white;

                if (light != null)
                {
                    if (comp.attenuationByLightShape)
                        globalColorModulation *= GetLensFlareLightAttenuation(light, cam, -diffToObject.normalized);
                }

                Vector2 screenPos = new Vector2(2.0f * viewportPos.x - 1.0f, -(2.0f * viewportPos.y - 1.0f));

                if(!SystemInfo.graphicsUVStartsAtTop && isDirLight) // Y-flip for OpenGL & directional light
                    screenPos.y = -screenPos.y;

                Vector2 radPos = new Vector2(Mathf.Abs(screenPos.x), Mathf.Abs(screenPos.y));
                float radius = Mathf.Max(radPos.x, radPos.y); // l1 norm (instead of l2 norm)
                float radialsScaleRadius = comp.radialScreenAttenuationCurve.length > 0 ? comp.radialScreenAttenuationCurve.Evaluate(radius) : 1.0f;

                float compIntensity = comp.intensity * radialsScaleRadius * distanceAttenuation;

                if (compIntensity <= 0.0f)
                    continue;

                globalColorModulation *= distanceAttenuation;

                Vector3 dir = (cam.transform.position - comp.transform.position).normalized;
                Vector3 screenPosZ = WorldToViewport(cam, !isDirLight, isCameraRelative, viewProjMatrix, positionWS + dir * comp.occlusionOffset);

                float adjustedOcclusionRadius = isDirLight ? comp.celestialProjectedOcclusionRadius(cam) : comp.occlusionRadius;
                Vector2 occlusionRadiusEdgeScreenPos0 = (Vector2)viewportPos;
                Vector2 occlusionRadiusEdgeScreenPos1 = (Vector2)WorldToViewport(cam, !isDirLight, isCameraRelative, viewProjMatrix, positionWS + cam.transform.up * adjustedOcclusionRadius);
                float occlusionRadius = (occlusionRadiusEdgeScreenPos1 - occlusionRadiusEdgeScreenPos0).magnitude;

                if (comp.useOcclusion)
                {
                    cmd.SetGlobalTexture(_FlareOcclusionTex, occlusionRT);
                    cmd.EnableShaderKeyword("FLARE_HAS_OCCLUSION");
                }
                else
                {
                    cmd.DisableShaderKeyword("FLARE_HAS_OCCLUSION");
                }

                if (IsOcclusionRTCompatible())
                {
                    cmd.DisableShaderKeyword("FLARE_OPENGL3_OR_OPENGLCORE");
                }
                else
                {
                    cmd.EnableShaderKeyword("FLARE_OPENGL3_OR_OPENGLCORE");
                }

                cmd.SetGlobalVector(_FlareOcclusionIndex, new Vector4((float)info.index, 0.0f, 0.0f, 0.0f));
                cmd.SetGlobalTexture(_FlareOcclusionRemapTex, comp.occlusionRemapCurve.GetTexture());

                Vector4 flareData1 = new Vector4(0.0f, comp.sampleCount, screenPosZ.z, actualHeight / actualWidth);
                ProcessLensFlareSRPElements(ref data.elements, cmd, globalColorModulation, light,
                    compIntensity, scaleByDistance * comp.scale, lensFlareShader,
                    screenPos, comp.allowOffScreen, vScreenRatio, flareData1, false, 0);
            }

            xr.StartSinglePass(cmd);
        }

        /// <summary>
        /// Effective Job of drawing Lens Flare Screen Space.
        /// </summary>
        /// <param name="lensFlareShader">Lens Flare material (HDRP or URP shader)</param>
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
        /// <param name="result">Result RT for the Lens Flare Screen Space</param>
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
        /// Effective Job of drawing Lens Flare Screen Space.
        /// </summary>
        /// <param name="lensFlareShader">Lens Flare material (HDRP or URP shader)</param>
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
        /// <param name="result">Result RT for the Lens Flare Screen Space</param>
        /// <param name="_LensFlareScreenSpaceBloomMipTexture">ShaderID for the original bloom texture</param>
        /// <param name="_LensFlareScreenSpaceResultTexture">ShaderID for the LensFlareScreenSpaceResultTexture texture</param>
        /// <param name="_LensFlareScreenSpaceSpectralLut">ShaderID for the LensFlareScreenSpaceSpectralLut texture</param>
        /// <param name="_LensFlareScreenSpaceStreakTex">ShaderID for the LensFlareScreenSpaceStreakTex streak temp texture</param>
        /// <param name="_LensFlareScreenSpaceMipLevel">ShaderID for the LensFlareScreenSpaceMipLevel parameter</param>
        /// <param name="_LensFlareScreenSpaceTintColor">ShaderID for the LensFlareScreenSpaceTintColor color</param>
        /// <param name="_LensFlareScreenSpaceParams1">ShaderID for the LensFlareScreenSpaceParams1</param>
        /// <param name="_LensFlareScreenSpaceParams2">ShaderID for the LensFlareScreenSpaceParams2</param>
        /// <param name="_LensFlareScreenSpaceParams3">ShaderID for the LensFlareScreenSpaceParams3</param>
        /// <param name="_LensFlareScreenSpaceParams4">ShaderID for the LensFlareScreenSpaceParams4</param>
        /// <param name="_LensFlareScreenSpaceParams5">ShaderID for the LensFlareScreenSpaceParams5</param>
        /// <param name="debugView">Information if we are in debug mode or not</param>
        [Obsolete("Use DoLensFlareScreenSpaceCommon without _Shader IDs parameters.")]
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
            int _LensFlareScreenSpaceBloomMipTexture,
            int _LensFlareScreenSpaceResultTexture,
            int _LensFlareScreenSpaceSpectralLut,
            int _LensFlareScreenSpaceStreakTex,
            int _LensFlareScreenSpaceMipLevel,
            int _LensFlareScreenSpaceTintColor,
            int _LensFlareScreenSpaceParams1,
            int _LensFlareScreenSpaceParams2,
            int _LensFlareScreenSpaceParams3,
            int _LensFlareScreenSpaceParams4,
            int _LensFlareScreenSpaceParams5,
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
            cmd,
            result,
            debugView);
        }

        /// <summary>
        /// Effective Job of drawing Lens Flare Screen Space.
        /// </summary>
        /// <param name="lensFlareShader">Lens Flare material (HDRP or URP shader)</param>
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
        /// <param name="result">Result RT for the Lens Flare Screen Space</param>
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
