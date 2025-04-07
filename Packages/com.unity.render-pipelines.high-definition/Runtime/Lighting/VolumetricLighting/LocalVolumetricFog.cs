using System;
using UnityEngine.Serialization;
using static Unity.Mathematics.math;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>The scaling mode to apply to Local Volumetric Fog.</summary>
    public enum LocalVolumetricFogScaleMode
    {
        /// <summary>Ignores the transformation hierarchy and uses the scale values in the Local Volumetric Fog component directly.</summary>
        [InspectorName("Scale Invariant")]
        ScaleInvariant,
        /// <summary>Multiplies the lossy scale of the Transform with the Local Volumetric Fog's size then applies this to the Local Volumetric Fog component.</summary>
        [InspectorName("Inherit from Hierarchy")]
        InheritFromHierarchy,
    }

    /// <summary>Artist-friendly Local Volumetric Fog parametrization.</summary>
    [Serializable]
    public partial struct LocalVolumetricFogArtistParameters
    {
        /// <summary>Single scattering albedo: [0, 1]. Alpha is ignored.</summary>
        [ColorUsage(false)]
        public Color albedo;
        /// <summary>Mean free path, in meters: [1, inf].</summary>
        public float meanFreePath; // Should be chromatic - this is an optimization!

        /// <summary>
        /// Specifies how the fog in the volume will interact with the fog.
        /// </summary>
        public LocalVolumetricFogBlendingMode blendingMode;

        /// <summary>
        /// Rendering priority of the volume, higher priority will be rendered first.
        /// </summary>
        public int priority;

        /// <summary>Anisotropy of the phase function: [-1, 1]. Positive values result in forward scattering, and negative values - in backward scattering.</summary>
        [FormerlySerializedAs("asymmetry")]
        public float anisotropy;   // . Not currently available for Local Volumetric Fog

        /// <summary>Texture containing density values.</summary>
        public Texture volumeMask;
        /// <summary>Scrolling speed of the density texture.</summary>
        public Vector3 textureScrollingSpeed;
        /// <summary>Tiling rate of the density texture.</summary>
        public Vector3 textureTiling;

        /// <summary>Edge fade factor along the positive X, Y and Z axes.</summary>
        [FormerlySerializedAs("m_PositiveFade")]
        public Vector3 positiveFade;
        /// <summary>Edge fade factor along the negative X, Y and Z axes.</summary>
        [FormerlySerializedAs("m_NegativeFade")]
        public Vector3 negativeFade;

        [SerializeField, FormerlySerializedAs("m_UniformFade")]
        internal float m_EditorUniformFade;
        [SerializeField]
        internal Vector3 m_EditorPositiveFade;
        [SerializeField]
        internal Vector3 m_EditorNegativeFade;
        [SerializeField, FormerlySerializedAs("advancedFade"), FormerlySerializedAs("m_AdvancedFade")]
        internal bool m_EditorAdvancedFade;

        /// <summary>The scaling mode to apply to Local Volumetric Fog.</summary>
        public LocalVolumetricFogScaleMode scaleMode;

        /// <summary>Dimensions of the volume.</summary>
        public Vector3 size;
        /// <summary>Inverts the fade gradient.</summary>
        public bool invertFade;

        /// <summary>Distance at which density fading starts.</summary>
        public float distanceFadeStart;
        /// <summary>Distance at which density fading ends.</summary>
        public float distanceFadeEnd;
        /// <summary>Allows translation of the tiling density texture.</summary>
        [SerializeField, FormerlySerializedAs("volumeScrollingAmount")]
        public Vector3 textureOffset;

        /// <summary>When Blend Distance is above 0, controls which kind of falloff is applied to the transition area.</summary>
        public LocalVolumetricFogFalloffMode falloffMode;

        /// <summary>The mask mode to use when writing this volume in the volumetric fog.</summary>
        public LocalVolumetricFogMaskMode maskMode;

        /// <summary>The material used to mask the local volumetric fog when the mask mode is set to Material. The material needs to use the "Fog Volume" material type in Shader Graph.</summary>
        public Material materialMask;

        /// <summary>Minimum fog distance you can set in the meanFreePath parameter</summary>
        internal const float kMinFogDistance = 0.05f;

        /// <summary>Constructor.</summary>
        /// <param name="color">Single scattering albedo.</param>
        /// <param name="_meanFreePath">Mean free path.</param>
        /// <param name="_anisotropy">Anisotropy.</param>
        public LocalVolumetricFogArtistParameters(Color color, float _meanFreePath, float _anisotropy)
        {
            albedo = color;
            meanFreePath = _meanFreePath;
            blendingMode = LocalVolumetricFogBlendingMode.Additive;
            priority = 0;
            anisotropy = _anisotropy;

            volumeMask = null;
            materialMask = null;
            textureScrollingSpeed = Vector3.zero;
            textureTiling = Vector3.one;
            textureOffset = textureScrollingSpeed;

            scaleMode = LocalVolumetricFogScaleMode.ScaleInvariant;
            size = Vector3.one;

            positiveFade = Vector3.one * 0.1f;
            negativeFade = Vector3.one * 0.1f;
            invertFade = false;

            distanceFadeStart = 10000;
            distanceFadeEnd = 10000;

            falloffMode = LocalVolumetricFogFalloffMode.Linear;
            maskMode = LocalVolumetricFogMaskMode.Texture;

            m_EditorPositiveFade = positiveFade;
            m_EditorNegativeFade = negativeFade;
            m_EditorUniformFade = 0.1f;
            m_EditorAdvancedFade = false;
        }

        internal void Update(float time)
        {
            //Update scrolling based on deltaTime
            if (volumeMask != null)
            {
                // Switch from right-handed to left-handed coordinate system.
                textureOffset = -(textureScrollingSpeed * time);
            }
        }

        internal void Constrain()
        {
            albedo.r = Mathf.Clamp01(albedo.r);
            albedo.g = Mathf.Clamp01(albedo.g);
            albedo.b = Mathf.Clamp01(albedo.b);
            albedo.a = 1.0f;

            meanFreePath = Mathf.Clamp(meanFreePath, kMinFogDistance, float.MaxValue);

            anisotropy = Mathf.Clamp(anisotropy, -1.0f, 1.0f);

            textureOffset = Vector3.zero;

            distanceFadeStart = Mathf.Max(0, distanceFadeStart);
            distanceFadeEnd = Mathf.Max(distanceFadeStart, distanceFadeEnd);
        }

        internal LocalVolumetricFogEngineData ConvertToEngineData()
        {
            LocalVolumetricFogEngineData data = new LocalVolumetricFogEngineData();

            data.scattering = VolumeRenderingUtils.ScatteringFromExtinctionAndAlbedo(VolumeRenderingUtils.ExtinctionFromMeanFreePath(meanFreePath), (Vector4)albedo);

            data.blendingMode = blendingMode;

            data.textureScroll = textureOffset;
            data.textureTiling = textureTiling;

            // Clamp to avoid NaNs.
            Vector3 positiveFade = this.positiveFade;
            Vector3 negativeFade = this.negativeFade;

            data.rcpPosFaceFade.x = Mathf.Min(1.0f / positiveFade.x, float.MaxValue);
            data.rcpPosFaceFade.y = Mathf.Min(1.0f / positiveFade.y, float.MaxValue);
            data.rcpPosFaceFade.z = Mathf.Min(1.0f / positiveFade.z, float.MaxValue);

            data.rcpNegFaceFade.y = Mathf.Min(1.0f / negativeFade.y, float.MaxValue);
            data.rcpNegFaceFade.x = Mathf.Min(1.0f / negativeFade.x, float.MaxValue);
            data.rcpNegFaceFade.z = Mathf.Min(1.0f / negativeFade.z, float.MaxValue);

            data.invertFade = invertFade ? 1 : 0;
            data.falloffMode = falloffMode;

            float distFadeLen = Mathf.Max(distanceFadeEnd - distanceFadeStart, 0.00001526f);

            data.rcpDistFadeLen = 1.0f / distFadeLen;
            data.endTimesRcpDistFadeLen = distanceFadeEnd * data.rcpDistFadeLen;

            return data;
        }
    } // class LocalVolumetricFogParameters

    /// <summary>Local Volumetric Fog class.</summary>
    [HDRPHelpURLAttribute("create-a-local-fog-effect")]
    [ExecuteAlways]
    [AddComponentMenu("Rendering/Local Volumetric Fog")]
    public partial class LocalVolumetricFog : MonoBehaviour
    {
        /// <summary>Local Volumetric Fog parameters.</summary>
        public LocalVolumetricFogArtistParameters parameters = new LocalVolumetricFogArtistParameters(Color.white, 10.0f, 0.0f);

        /// <summary>Action shich should be performed after updating the texture.</summary>
        public Action OnTextureUpdated;

        [NonSerialized]
        MaterialPropertyBlock m_RenderingProperties;
        [NonSerialized]
        int m_GlobalIndex;

        [NonSerialized]
        internal Material textureMaterial;

        /// <summary>stores the current effective scale, Vector3.one if the component is Scale Invariant, or lossy scale if the component Inherit From Hiearchy.</summary>
        internal Vector3 effectiveScale;

        /// <summary>stores the final scale of the local volumetric fog component.</summary>
        internal Vector3 scaledSize;

        /// <summary>Gather and Update any parameters that may have changed.</summary>
        internal void PrepareParameters(float time)
        {
            parameters.Update(time);
        }

        private void OnEnable()
        {
            LocalVolumetricFogManager.manager.RegisterVolume(this);

#if UNITY_EDITOR
            // Handle scene visibility
            SceneVisibilityManager.visibilityChanged -= UpdateLocalVolumetricFogVisibility;
            SceneVisibilityManager.visibilityChanged += UpdateLocalVolumetricFogVisibility;
            SceneView.duringSceneGui -= UpdateLocalVolumetricFogVisibilityPrefabStage;
            SceneView.duringSceneGui += UpdateLocalVolumetricFogVisibilityPrefabStage;
#endif
        }

        internal int GetGlobalIndex() => m_GlobalIndex;

        internal void PrepareDrawCall(int globalIndex)
        {
            m_GlobalIndex = globalIndex;
            if (!LocalVolumetricFogManager.manager.IsInitialized())
                return;

            if (m_RenderingProperties == null)
                m_RenderingProperties = new MaterialPropertyBlock();
            m_RenderingProperties.Clear();

            m_RenderingProperties.SetInteger(HDShaderIDs._VolumetricFogGlobalIndex, m_GlobalIndex);

            Material material = parameters.materialMask;

            // Setup parameters for the default volumetric fog ShaderGraph
            if (parameters.maskMode == LocalVolumetricFogMaskMode.Texture)
            {
                bool alphaTexture = false;
                if (textureMaterial == null && HDRenderPipelineGlobalSettings.instance != null)
                {
                    var runtimeShaders = GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineRuntimeShaders>();
                    textureMaterial = CoreUtils.CreateEngineMaterial(runtimeShaders.defaultFogVolumeShader);
                }

                // Setup properties for material:
                FogVolumeAPI.SetupFogVolumeBlendMode(textureMaterial, parameters.blendingMode);

                material = textureMaterial;
                if (parameters.volumeMask != null)
                {

                    m_RenderingProperties.SetTexture(HDShaderIDs._VolumetricMask, parameters.volumeMask);
                    textureMaterial.EnableKeyword("_ENABLE_VOLUMETRIC_FOG_MASK");
                    if (parameters.volumeMask is Texture3D t3d)
                        alphaTexture = t3d.format == TextureFormat.Alpha8;
                }
                else
                {
                    textureMaterial.DisableKeyword("_ENABLE_VOLUMETRIC_FOG_MASK");
                }

                m_RenderingProperties.SetVector(HDShaderIDs._VolumetricScrollSpeed, parameters.textureScrollingSpeed);
                m_RenderingProperties.SetVector(HDShaderIDs._VolumetricTiling, parameters.textureTiling);
                m_RenderingProperties.SetFloat(HDShaderIDs._AlphaOnlyTexture, alphaTexture ? 1 : 0);

                m_RenderingProperties.SetFloat(FogVolumeAPI.k_FogDistanceProperty, parameters.meanFreePath);
                m_RenderingProperties.SetColor(FogVolumeAPI.k_SingleScatteringAlbedoProperty, parameters.albedo.gamma);
            }

            if (material == null)
                return;

            // We can put this in global
            m_RenderingProperties.SetBuffer(HDShaderIDs._VolumetricMaterialData, LocalVolumetricFogManager.manager.volumetricMaterialDataBuffer);

            effectiveScale = GetEffectiveScale(this.transform);
            scaledSize = GetScaledSize(this.transform);

            // Send local properties inside constants instead of structured buffer to optimize GPU reads
            var engineData = parameters.ConvertToEngineData();
            var tr = transform;
            var position = tr.position;
            var bounds = new OrientedBBox(Matrix4x4.TRS(position, tr.rotation, scaledSize));
            m_RenderingProperties.SetVector(HDShaderIDs._VolumetricMaterialObbRight, bounds.right);
            m_RenderingProperties.SetVector(HDShaderIDs._VolumetricMaterialObbUp, bounds.up);
            m_RenderingProperties.SetVector(HDShaderIDs._VolumetricMaterialObbExtents, new Vector3(bounds.extentX, bounds.extentY, bounds.extentZ));
            m_RenderingProperties.SetVector(HDShaderIDs._VolumetricMaterialObbCenter, bounds.center);

            m_RenderingProperties.SetVector(HDShaderIDs._VolumetricMaterialRcpPosFaceFade, engineData.rcpPosFaceFade);
            m_RenderingProperties.SetVector(HDShaderIDs._VolumetricMaterialRcpNegFaceFade, engineData.rcpNegFaceFade);
            m_RenderingProperties.SetInteger(HDShaderIDs._VolumetricMaterialInvertFade, engineData.invertFade);

            m_RenderingProperties.SetFloat(HDShaderIDs._VolumetricMaterialRcpDistFadeLen, engineData.rcpDistFadeLen);
            m_RenderingProperties.SetFloat(HDShaderIDs._VolumetricMaterialEndTimesRcpDistFadeLen, engineData.endTimesRcpDistFadeLen);
            m_RenderingProperties.SetInteger(HDShaderIDs._VolumetricMaterialFalloffMode, (int)engineData.falloffMode);

            var AABBExtents = abs(bounds.right * bounds.extentX) +
                     abs(bounds.up * bounds.extentY) +
                     abs(bounds.forward * bounds.extentZ);

            var AABB = new Bounds(bounds.center, AABBExtents * 2f);
            var renderParams = new RenderParams
            {
                layer = gameObject.layer,
                rendererPriority = parameters.priority,
                worldBounds = AABB,
                motionVectorMode = MotionVectorGenerationMode.ForceNoMotion,
                reflectionProbeUsage = ReflectionProbeUsage.Off,
                renderingLayerMask = 0xFFFFFFFF,
                material = material,
                matProps = m_RenderingProperties,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false,
                lightProbeUsage = LightProbeUsage.Off,
#if UNITY_EDITOR
                overrideSceneCullingMask = true,
                sceneCullingMask = gameObject.sceneCullingMask,
#endif
            };

            Graphics.RenderPrimitivesIndexedIndirect(renderParams, MeshTopology.Triangles, LocalVolumetricFogManager.manager.volumetricMaterialIndexBuffer, LocalVolumetricFogManager.manager.globalIndirectBuffer, 1, m_GlobalIndex);
        }

        internal Vector3 GetEffectiveScale(Transform tr)
        {
            return (parameters.scaleMode == LocalVolumetricFogScaleMode.InheritFromHierarchy) ? tr.lossyScale : Vector3.one;
        }

        internal Vector3 GetScaledSize(Transform tr)
        {
            return Vector3.Max(Vector3.one * 0.001f, Vector3.Scale(parameters.size, GetEffectiveScale(tr)));
        }

#if UNITY_EDITOR
        void UpdateLocalVolumetricFogVisibility()
        {
            bool isVisible = !SceneVisibilityManager.instance.IsHidden(gameObject);
            UpdateLocalVolumetricFogVisibility(isVisible);
        }


        void UpdateLocalVolumetricFogVisibilityPrefabStage(SceneView sv)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                bool isVisible = true;
                bool isInPrefabStage = gameObject.scene == stage.scene;

                if (!isInPrefabStage && stage.mode == PrefabStage.Mode.InIsolation)
                    isVisible = false;
                if (!isInPrefabStage && CoreUtils.IsSceneViewPrefabStageContextHidden())
                    isVisible = false;

                UpdateLocalVolumetricFogVisibility(isVisible);
            }
        }

        void UpdateLocalVolumetricFogVisibility(bool isVisible)
        {
            if (isVisible)
            {
                if (!LocalVolumetricFogManager.manager.ContainsVolume(this))
                    LocalVolumetricFogManager.manager.RegisterVolume(this);
            }
            else
            {
                if (LocalVolumetricFogManager.manager.ContainsVolume(this))
                    LocalVolumetricFogManager.manager.DeRegisterVolume(this);
            }
        }
#endif

        private void OnDisable()
        {
            LocalVolumetricFogManager.manager.DeRegisterVolume(this);

#if UNITY_EDITOR
            SceneVisibilityManager.visibilityChanged -= UpdateLocalVolumetricFogVisibility;
            SceneView.duringSceneGui -= UpdateLocalVolumetricFogVisibilityPrefabStage;
#endif
        }

        private void OnValidate()
        {
            parameters.Constrain();
        }
    }
}
