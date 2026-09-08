using System;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.U2D;
using UnityEngine.Rendering.RenderGraphModule;
using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class <c>Light2D</c> is a 2D light which can be used with the 2D Renderer.
    /// </summary>
    ///
    [ExecuteAlways, DisallowMultipleComponent]
    [Icon("UnityEngine/Light Icon")]
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.Universal", "Unity.RenderPipelines.Universal.Runtime")]
    [AddComponentMenu("Rendering/2D/Light 2D")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html?subfolder=/manual/2DLightProperties.html")]
    public sealed partial class Light2D : Light2DBase, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Deprecated Light types that are no supported. Please migrate to either Freeform or Point lights.
        /// </summary>
        public enum DeprecatedLightType
        {
            /// <summary>
            /// N-gon shaped lights.
            /// </summary>
            Parametric = 0,
        }

        /// <summary>
        /// An enumeration of the types of light
        /// </summary>
        public enum LightType
        {
            /// <summary>
            /// N-gon shaped lights. Deprecated.
            /// </summary>
            Parametric = 0,
            /// <summary>
            /// The shape of the light is based on a user defined closed shape with multiple points.
            /// </summary>
            Freeform = 1,
            /// <summary>
            /// The shape of the light is based on a Sprite.
            /// </summary>
            Sprite = 2,
            /// <summary>
            /// The shape of light is circular and can also be configured into a pizza shape.
            /// </summary>
            Point = 3,
            /// <summary>
            /// Shapeless light that affects the entire screen.
            /// </summary>
            Global = 4,
            /// <summary>
            /// Light completely supplied by a provider
            /// </summary>
            Provider = 5,
        }

        /// <summary>
        /// The accuracy of how the normal map calculation.
        /// </summary>
        public enum NormalMapQuality
        {
            /// <summary>
            /// Normal map not used.
            /// </summary>
            Disabled = 2,
            /// <summary>
            /// Faster calculation with less accuracy suited for small shapes on screen.
            /// </summary>
            Fast = 0,
            /// <summary>
            /// Accurate calculation useful for better output on bigger shapes on screen.
            /// </summary>
            Accurate = 1
        }

        /// <summary>
        /// Determines how the final color is calculated when multiple lights overlap each other
        /// </summary>
        public enum OverlapOperation
        {
            /// <summary>
            /// Colors are added together
            /// </summary>
            Additive,
            /// <summary>
            /// Colors are blended using standard blending (alpha, 1-alpha)
            /// </summary>
            AlphaBlend
        }

        private enum ComponentVersions
        {
            Version_Unserialized = 0,
            Version_1 = 1,
            Version_2 = 2,
            Version_3 = 3,
            Version_4 = 4
        }

        const ComponentVersions k_CurrentComponentVersion = ComponentVersions.Version_4;
        [SerializeField] ComponentVersions m_ComponentVersion = ComponentVersions.Version_Unserialized;

        static Bounds kEmptyBounds = new Bounds(Vector3.zero, Vector3.zero);

#if USING_ANIMATION_MODULE
        [UnityEngine.Animations.NotKeyable]
#endif
        [SerializeField] LightType m_LightType = LightType.Point;
        [SerializeField, FormerlySerializedAs("m_LightOperationIndex")]
        int m_BlendStyleIndex = 0;

        [SerializeReference] Light2DProvider m_Light2DProvider;

#if UNITY_EDITOR
        [SerializeReference] Light2DProviderSources m_SelectionSources = new Light2DProviderSources();
#endif

        [SerializeField] float m_FalloffIntensity = 0.5f;

        [ColorUsage(true)]
        [SerializeField] Color m_Color = Color.white;
        [SerializeField] float m_Intensity = 1;

        [FormerlySerializedAs("m_LightVolumeOpacity")]
        [SerializeField] float m_LightVolumeIntensity = 1.0f;

        [FormerlySerializedAs("m_LightVolumeIntensityEnabled")]
        [SerializeField] bool m_LightVolumeEnabled = false;
        [SerializeField] int[] m_ApplyToSortingLayers;  // These are sorting layer IDs. If we need to update this at runtime make sure we add code to update global lights

        [Reload("Textures/2D/Sparkle.png")]
        [SerializeField] Sprite m_LightCookieSprite;

        // Offers only Materials whose shader declares the Light2D passes the renderer draws. As with
        // the shadow material, the filter is a discovery aid only -- drag-and-drop bypasses the
        // picker -- so Light2DEditor still reports incompatible assignments.
        [UnityEngine.Search.SearchContext("", k_LightMaterialSearchProviderId, UnityEngine.Search.SearchViewFlags.ObjectPickerAdvancedUI | UnityEngine.Search.SearchViewFlags.ListView | UnityEngine.Search.SearchViewFlags.IgnoreSavedSearches | UnityEngine.Search.SearchViewFlags.DisableSavedSearchQuery)]
        [SerializeField] Material m_Material;

        internal const string k_LightMaterialSearchProviderId = "light2dmaterial";

        [Obsolete("Use m_LightCookieSprite instead")]
        [SerializeField] Sprite m_DeprecatedPointLightCookieSprite;

        [SerializeField] int m_LightOrder = 0;

        [SerializeField] bool m_AlphaBlendOnOverlap = false; // This is now deprecated. Keep it here for backwards compatibility.

        [SerializeField] OverlapOperation m_OverlapOperation = OverlapOperation.Additive;

        [FormerlySerializedAs("m_PointLightDistance")]
        [SerializeField] float m_NormalMapDistance = 3.0f;

#if USING_ANIMATION_MODULE
        [UnityEngine.Animations.NotKeyable]
#endif
        [FormerlySerializedAs("m_PointLightQuality")]
        [SerializeField] NormalMapQuality m_NormalMapQuality = NormalMapQuality.Disabled;

        [SerializeField] bool m_UseNormalMap = false;   // This is now deprecated. Keep it here for backwards compatibility.

        [FormerlySerializedAs("m_ShadowIntensityEnabled")]
        [SerializeField] bool m_ShadowsEnabled = true;

        [Range(0, 1)]
        [SerializeField] float m_ShadowIntensity = 0.75f;

        [Range(0, 1)]
        [SerializeField] float m_ShadowSoftness = 0.3f;

        [Range(0, 1)]
        [SerializeField] float m_ShadowSoftnessFalloffIntensity = 0.5f;

        [SerializeField] bool m_ShadowVolumeIntensityEnabled = false;
        [Range(0, 1)]
        [SerializeField] float m_ShadowVolumeIntensity = 0.75f;

        // The picker is filtered to Materials whose shader declares all five shadow passes, so it
        // cannot offer one that would silently drop a phase. The provider is named by string id
        // rather than by Type because it lives in the editor assembly, which this one cannot see;
        // the id must stay in step with Shadow2DMaterialSearchProvider.k_ProviderId.
        //
        // The filter is a discovery aid only -- drag-and-drop bypasses the picker -- so Light2DEditor
        // still reports incompatible assignments.
        [UnityEngine.Search.SearchContext("", k_ShadowMaterialSearchProviderId, UnityEngine.Search.SearchViewFlags.ObjectPickerAdvancedUI | UnityEngine.Search.SearchViewFlags.ListView | UnityEngine.Search.SearchViewFlags.IgnoreSavedSearches | UnityEngine.Search.SearchViewFlags.DisableSavedSearchQuery)]
        [SerializeField] Material m_ShadowMaterial = null;

        internal const string k_ShadowMaterialSearchProviderId = "shadow2dmaterial";

        // Resolved shader-pass indices for m_ShadowMaterial, one per ShadowRendering.ShadowPassRole,
        // with -1 where the shader does not declare that role. Never serialized: pass indices are
        // name -> index lookups into one specific Shader object and do not survive a shader change.
        //
        // Cached here rather than resolved at draw time because Material.FindPass is a
        // case-insensitive string search that allocates an uppercase temporary per call, and the draw
        // path would otherwise call it once per phase, per light, per frame.
        [NonSerialized] int[] m_ShadowMaterialPassIndices;
        [NonSerialized] Material m_ResolvedShadowMaterial;
        [NonSerialized] Shader m_ResolvedShadowMaterialShader;

        [SerializeField] RenderingLayerMask m_RenderingLayersMask = RenderingLayerMask.defaultRenderingLayerMask;

        Mesh m_Mesh;

        // m_Mesh either points at a Mesh this component allocated through lightMesh, or at one handed over by
        // Light2DProvider.GetMesh(). That contract only returns a mesh to render and does not transfer ownership,
        // so provider meshes may be shared between lights and must never be destroyed from here.
        bool m_OwnsMesh;

        [NonSerialized]
        private LightUtility.LightMeshVertex[] m_Vertices = new LightUtility.LightMeshVertex[1];

        [NonSerialized]
        private ushort[] m_Triangles = new ushort[1];

        internal LightUtility.LightMeshVertex[] vertices { get { return m_Vertices; } set { m_Vertices = value; } }

        internal ushort[] indices { get { return m_Triangles; } set { m_Triangles = value; } }

        // Transients
        EntityId m_PreviousLightCookieSprite;
        internal Vector3 m_CachedPosition;

        // We use Blue Channel of LightMesh's vertex color to indicate Slot Index.
        int m_BatchSlotIndex = 0;
        internal int batchSlotIndex { get { return m_BatchSlotIndex; } set {  m_BatchSlotIndex = value; } }

        private EntityId lightCookieSpriteEntityId => lightCookieSprite?.GetEntityId() ?? EntityId.None;

        internal bool useCookieSprite => (lightType == LightType.Point || lightType == LightType.Sprite) && (lightCookieSprite != null && lightCookieSprite.texture != null);

        internal RTHandle m_CookieSpriteTexture = null;
        internal TextureHandle m_CookieSpriteTextureHandle;

        [SerializeField]
        Bounds m_LocalBounds;
        internal BoundingSphere boundingSphere { get; private set; }

        internal Mesh lightMesh
        {
            get
            {
                if (null == m_Mesh)
                {
                    m_Mesh = new Mesh();
                    m_OwnsMesh = true;
                }
                return m_Mesh;
            }
        }

        // Takes the mesh a Light2DProvider handed us, releasing any mesh we allocated ourselves first so replacing
        // it does not leak.
        void SetProviderMesh(Mesh providerMesh)
        {
            // A provider is free to hand back the mesh we already hold, in which case ownership is unchanged.
            if (m_Mesh == providerMesh)
                return;

            if (m_OwnsMesh)
                CoreUtils.Destroy(m_Mesh);

            m_Mesh = providerMesh;
            m_OwnsMesh = false;
        }

        internal bool hasCachedMesh => (vertices.Length > 1 && indices.Length > 1);

        internal bool forceUpdate = false;

        /// <summary>
        /// The light's current type
        /// </summary>
        public LightType lightType
        {
            get { return (LightType)m_LightType; }
            set
            {
                m_LightType = value;

#if UNITY_EDITOR
                if((int)m_LightType != (int)LightType.Provider)
                    m_SelectionSources.selectedHashCode = (int)m_LightType;
#endif
            }
        }

        /// <summary>
        /// Gets or sets the current light2DProvider
        /// </summary>
        public Light2DProvider light2DProvider
        {
            get { return m_Light2DProvider; }
            set { m_Light2DProvider = value; }
        }

        /// <summary>
        /// The lights current operation index
        /// </summary>
        public int blendStyleIndex { get => m_BlendStyleIndex; set => m_BlendStyleIndex = value; }

        /// <summary>
        /// Specifies the darkness of the shadow
        /// </summary>
        public float shadowIntensity { get => m_ShadowIntensity; set => m_ShadowIntensity = Mathf.Clamp01(value); }


        /// <summary>
        /// Specifies the softness of the soft shadow
        /// </summary>
        public float shadowSoftness { get => m_ShadowSoftness; set => m_ShadowSoftness = value; }


        /// <summary>
        /// Specifies that the shadows are enabled
        /// </summary>
        public bool shadowsEnabled { get => m_ShadowsEnabled; set => m_ShadowsEnabled = value; }

        /// <summary>
        /// Specifies the darkness of the shadow
        /// </summary>
        public float shadowVolumeIntensity { get => m_ShadowVolumeIntensity; set => m_ShadowVolumeIntensity = Mathf.Clamp01(value); }

        /// <summary>
        /// Specifies that the volumetric shadows are enabled
        /// </summary>
        public bool volumetricShadowsEnabled { get => m_ShadowVolumeIntensityEnabled; set => m_ShadowVolumeIntensityEnabled = value; }

        /// <summary>
        /// Optional custom <c>Material</c> to use when rendering this light's shadows. When <c>null</c>, URP's built-in shadow materials are used.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A custom material replaces the built-in materials completely for every shadow this light casts: URP never
        /// combines a custom pass with a built-in one, because the two shaders are free to use different vertex layouts.
        /// </para>
        /// <para>
        /// 2D shadows are drawn as a four-phase stencil handshake and URP selects a shader pass per phase by name,
        /// so the shader must declare all five: <c>Self</c>, <c>UnshadowMark</c>, <c>UnshadowUnmark</c>,
        /// <c>ProjectedSelf</c> and <c>ProjectedUnshadow</c>. A phase whose pass the material does not declare is
        /// skipped, so that part of the shadow does not render, and the Light2D inspector reports it.
        /// </para>
        /// <para>
        /// The material's shader must also be able to read the shadow geometry this project builds, which is a project
        /// setting rather than a per-caster one — so a compatible material is compatible with every caster the light
        /// touches. Enhanced shadow geometry is required for ShaderGraph-authored shadow shaders; in a project on
        /// the Legacy generation this property is ignored entirely and URP's built-in shader is used.
        /// </para>
        /// <para>
        /// Each pass must also reproduce the blend, <c>ColorMask</c> and <c>Stencil</c> state of the built-in pass it
        /// replaces (see <c>Shadow2D.shader</c>); URP does not set that state from script. The material is used as-is
        /// and is never cloned, so it is shared by every light it is assigned to.
        /// </para>
        /// </remarks>
        public Material shadowMaterial
        {
            get => m_ShadowMaterial;
            set => m_ShadowMaterial = value;
        }

        /// <summary>
        /// The material URP actually draws this light's shadows with, or <c>null</c> to use the
        /// built-in shadow materials.
        /// </summary>
        /// <remarks>
        /// Differs from <see cref="shadowMaterial"/> only in a project on the Legacy shadow geometry
        /// generation, where a custom material is ignored rather than cleared. Ignored and not
        /// cleared deliberately: a project that later moves to Enhanced gets its assignment back, and
        /// a value the inspector hides is not one the user can be expected to have removed.
        /// </remarks>
        internal Material effectiveShadowMaterial => Shadow2DGeometry.enhancedGeometryEnabled ? m_ShadowMaterial : null;

        /// <summary>
        /// The shader pass index on <see cref="effectiveShadowMaterial"/> for <paramref name="role"/>, or -1 when no
        /// material applies, it has no shader, or its shader does not declare that pass.
        /// </summary>
        /// <remarks>
        /// Safe to call per draw: the indices are resolved only when the material or its shader
        /// changes, so the steady-state cost is two reference comparisons. The shader is tracked
        /// separately from the material because it can be reassigned on the same Material asset in
        /// the editor, which invalidates the resolved indices.
        /// </remarks>
        internal int GetShadowMaterialPassIndex(ShadowRendering.ShadowPassRole role)
        {
            var mat = effectiveShadowMaterial;
            if (mat == null)
                return -1;

            var shader = mat.shader;
            if (shader == null)
                return -1;

            if (m_ShadowMaterialPassIndices == null || m_ResolvedShadowMaterial != mat || m_ResolvedShadowMaterialShader != shader)
                ResolveShadowMaterialPassIndices(mat, shader);

            return m_ShadowMaterialPassIndices[(int)role];
        }

        // Resolution happens only on a material or shader change, so the "declares nothing" warning
        // below is naturally one-shot and needs no separate bookkeeping. `expected: None` suppresses
        // BuildPassIndices' own per-role warnings: which roles are actually drawn depends on the
        // casters this light reaches, and the inspector reports the material's own gaps precisely
        // (ShadowRendering.GetMissingShadowPasses).
        void ResolveShadowMaterialPassIndices(Material mat, Shader shader)
        {
            m_ShadowMaterialPassIndices = ShadowRendering.BuildPassIndices(mat, ShadowRendering.ShadowPassRoles.None);
            m_ResolvedShadowMaterial = mat;
            m_ResolvedShadowMaterialShader = shader;

            for (int i = 0; i < m_ShadowMaterialPassIndices.Length; i++)
            {
                if (m_ShadowMaterialPassIndices[i] >= 0)
                    return;
            }

            // Covers materials assigned from script and player builds, where no inspector runs.
            Debug.LogWarning($"[Shadow2D] Material '{mat.name}' (shader '{shader.name}') is assigned to Light2D on '{name}' but declares none of the shadow passes " +
                $"'{ShadowRendering.k_SelfPassName}', '{ShadowRendering.k_UnshadowMarkPassName}', '{ShadowRendering.k_UnshadowUnmarkPassName}', '{ShadowRendering.k_ProjectedSelfPassName}' or '{ShadowRendering.k_ProjectedUnshadowPassName}'. " +
                "A custom material replaces URP's shadow shaders rather than supplementing them, so this light will cast no shadows. " +
                "Add a `Pass { Name \"...\" ... }` for each phase, or clear the material to use URP's built-in shadow shaders.", this);
        }

        /// <summary>
        /// The lights current color
        /// </summary>
        public Color color { get => m_Color; set => m_Color = value; }

        /// <summary>
        /// The lights current intensity
        /// </summary>
        public float intensity { get => m_Intensity; set => m_Intensity = value; }

        /// <summary>
        /// The lights current intensity
        /// </summary>
        ///
        [Obsolete("#from(2021.1)")]
        public float volumeOpacity => m_LightVolumeIntensity;

        /// <summary>
        /// Controls the visibility of the light's volume
        /// </summary>
        public float volumeIntensity { get => m_LightVolumeIntensity; set => m_LightVolumeIntensity = value; }

        /// <summary>
        /// Enables or disables the light's volume
        /// </summary>
        ///
        [Obsolete("#from(2023.1)")]
        public bool volumeIntensityEnabled { get => m_LightVolumeEnabled; set => m_LightVolumeEnabled = value; }

        /// <summary>
        /// Enables or disables the light's volume
        /// </summary>
        ///
        public bool volumetricEnabled { get => m_LightVolumeEnabled; set => m_LightVolumeEnabled = value; }

        /// <summary>
        /// The Sprite that's used by the Sprite Light type to control the shape light
        /// </summary>
        public Sprite lightCookieSprite { get { return m_LightCookieSprite; } set => m_LightCookieSprite = value; }

        /// <summary>
        /// Optional custom <c>Material</c> to use when rendering this <c>Light2D</c>. When <c>null</c>, URP's built-in light material is used.
        /// </summary>
        /// <remarks>
        /// Assigning a non-null <c>Material</c> bypasses URP's variant-bit material cache and the automatic keyword application (volumetric, additive blending, normal map, shadow map, point-light cookie, etc.).
        /// The custom material is used as-is, so the caller owns keyword state. The material must be single-pass: the batched draw path issues all passes per batch.
        /// </remarks>
        public Material material { get => m_Material; set => m_Material = value; }

        /// <summary>
        /// Controls the brightness and distance of the fall off (edge) of the light
        /// </summary>
        public float falloffIntensity { get => m_FalloffIntensity; set => m_FalloffIntensity = Mathf.Clamp(value, 0, 1); }

        /// <summary>
        /// Controls the falloff for soft shadows
        /// </summary>
        public float shadowSoftnessFalloffIntensity { get => m_ShadowSoftnessFalloffIntensity; set => m_ShadowSoftnessFalloffIntensity = Mathf.Clamp(value, 0, 1); }

        /// <summary>
        /// Checks if the alpha overlap operation is alpha blend.
        /// This is obsolete.
        /// </summary>
        [Obsolete("#from(2021.1)")]
        public bool alphaBlendOnOverlap { get { return m_OverlapOperation == OverlapOperation.AlphaBlend; } }

        /// <summary>
        /// Controls the overlap operation mode.
        /// </summary>
        public OverlapOperation overlapOperation { get => m_OverlapOperation; set => m_OverlapOperation = value; }

        /// <summary>
        /// Gets or sets the light order. The lightOrder determines the order in which the lights are rendered onto the light textures.
        /// </summary>
        public int lightOrder { get => m_LightOrder; set => m_LightOrder = value; }

        /// <summary>
        /// The simulated z distance of the light from the surface used in normal map calculation.
        /// </summary>
        public float normalMapDistance => m_NormalMapDistance;

        /// <summary>
        /// Returns the calculation quality for the normal map rendering. Please refer to NormalMapQuality.
        /// </summary>
        public NormalMapQuality normalMapQuality => m_NormalMapQuality;

        /// <summary>
        /// Returns if volumetric shadows should be rendered.
        /// </summary>
        public bool renderVolumetricShadows => volumetricShadowsEnabled && shadowVolumeIntensity > 0;

        /// <summary>
        /// Gets or sets the target sorting layers for the light. Contains an array of sorting layer IDs.
        /// </summary>
        public int[] targetSortingLayers
        {
            get => m_ApplyToSortingLayers;
            set
            {
                var layers = new List<int>();
                foreach (var layerID in value)
                {
                    if (SortingLayer.IsValid(layerID))
                        layers.Add(layerID);
                }
                m_ApplyToSortingLayers = layers.ToArray();
            }
        }

        /// <summary>
        /// Gets or sets the rendering layer mask for the light.
        /// </summary>
        public RenderingLayerMask renderingLayerMask
        {
            get { return m_RenderingLayersMask; }
            set
            {
                m_RenderingLayersMask = value;
            }
        }

        bool IsValidLayer(string name)
        {
            // Have this check as SortingLayer.NameToID returns 0 (default layer) if layer is not found
            foreach (var layer in Light2DManager.GetCachedSortingLayer())
            {
                if (layer.name == name)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Adds a target sorting layer to the light.
        /// </summary>
        /// <param name="layerName">The sorting layer name to be added.</param>
        /// <returns>Returns true if the sorting layer is added. Returns false if the layer name is invalid or has already been added.</returns>
        public bool AddTargetSortingLayer(string layerName)
        {
            var layers = new List<int>(m_ApplyToSortingLayers);
            var id = SortingLayer.NameToID(layerName);

            // Invalid or duplicate layerID
            if (!IsValidLayer(layerName) || layers.Contains(id))
                return false;

            layers.Add(id);
            m_ApplyToSortingLayers = layers.ToArray();

            return true;
        }

        /// <summary>
        /// Adds a target sorting layer to the light.
        /// </summary>
        /// <param name="layerID">The sorting layer ID to be added.</param>
        /// <returns>Returns true if the sorting layer is added. Returns false if the layer ID is invalid or has already been added.</returns>
        public bool AddTargetSortingLayer(int layerID)
        {
            return AddTargetSortingLayer(SortingLayer.IDToName(layerID));
        }

        /// <summary>
        /// Removes a target sorting layer from the light.
        /// </summary>
        /// <param name="layerName">The sorting layer name to be removed.</param>
        /// <returns>Returns true if the sorting layer is removed. Returns false if the layer name is invalid or doesn't exist.</returns>
        public bool RemoveTargetSortingLayer(string layerName)
        {
            var layers = new List<int>(m_ApplyToSortingLayers);
            var id = SortingLayer.NameToID(layerName);

            // Invalid or layerID does not exist
            if (!IsValidLayer(layerName) || !layers.Contains(id))
                return false;

            layers.Remove(id);
            m_ApplyToSortingLayers = layers.ToArray();

            return true;
        }

        /// <summary>
        /// Removes a target sorting layer from the light.
        /// </summary>
        /// <param name="layerID">The sorting layer ID to be removed.</param>
        /// <returns>Returns true if the sorting layer is removed. Returns false if the layer ID is invalid or doesn't exist.</returns>
        public bool RemoveTargetSortingLayer(int layerID)
        {
            return RemoveTargetSortingLayer(SortingLayer.IDToName(layerID));
        }

        internal void MarkForUpdate()
        {
            forceUpdate = true;
        }

        internal void CacheValues()
        {
            m_CachedPosition = transform.position;
        }

        internal int GetTopMostLitLayer()
        {
            var largestIndex = Int32.MinValue;
            var largestLayer = 0;

            var layers = Light2DManager.GetCachedSortingLayer();
            for (var i = 0; i < m_ApplyToSortingLayers.Length; ++i)
            {
                for (var layer = layers.Length - 1; layer >= largestLayer; --layer)
                {
                    if (layers[layer].id == m_ApplyToSortingLayers[i])
                    {
                        largestIndex = layers[layer].value;
                        largestLayer = layer;
                    }
                }
            }

            return largestIndex;
        }

        internal Bounds UpdateSpriteMesh()
        {
            if (m_LightCookieSprite == null && (m_Vertices.Length != 1 || m_Triangles.Length != 1))
            {
                m_Vertices = new LightUtility.LightMeshVertex[1];
                m_Triangles = new ushort[1];
            }
            return LightUtility.GenerateSpriteMesh(this, m_LightCookieSprite, LightBatch.GetBatchColor());
        }

        internal void UpdateBatchSlotIndex()
        {
            if (lightMesh && lightMesh.colors != null && lightMesh.colors.Length != 0)
                m_BatchSlotIndex = LightBatch.GetBatchSlotIndex(lightMesh.colors[0].b);
        }

        internal bool NeedsColorIndexBaking()
        {
            if (lightMesh && LightBatch.isBatchingSupported)
            {
                if (lightMesh.colors.Length != 0)
                    return lightMesh.colors[0].b == 0;
            }
            return false;
        }

        internal Bounds UpdateProviderMesh()
        {
            SetProviderMesh(light2DProvider.GetMesh());

            if (m_Mesh != null)
                return m_Mesh.bounds;
            else
                return kEmptyBounds;
        }

        internal void UpdateCookieSpriteTexture()
        {
            m_CookieSpriteTexture?.Release();

            if (useCookieSprite)
                m_CookieSpriteTexture = RTHandles.Alloc(lightCookieSprite.texture);

        }

        internal void UpdateMesh(bool forceUpdate = false)
        {
            var lightTypeChanged = LightUtility.CheckForChange(lightType, ref m_PreviousLightType);

            if (lightType != LightType.Provider)
            {

                var shapePathHash = LightUtility.GetShapePathHash(shapePath);
                var fallOffSizeChanged = LightUtility.CheckForChange(m_ShapeLightFalloffSize, ref m_PreviousShapeLightFalloffSize);
                var parametricRadiusChanged = LightUtility.CheckForChange(m_ShapeLightParametricRadius, ref m_PreviousShapeLightParametricRadius);
                var parametricSidesChanged = LightUtility.CheckForChange(m_ShapeLightParametricSides, ref m_PreviousShapeLightParametricSides);
                var parametricAngleOffsetChanged = LightUtility.CheckForChange(m_ShapeLightParametricAngleOffset, ref m_PreviousShapeLightParametricAngleOffset);
                var spriteInstanceChanged = LightUtility.CheckForChange(lightCookieSpriteEntityId, ref m_PreviousLightCookieSprite);
                var shapePathHashChanged = LightUtility.CheckForChange(shapePathHash, ref m_PreviousShapePathHash);
                var hashChanged = fallOffSizeChanged || parametricRadiusChanged || parametricSidesChanged ||
                    parametricAngleOffsetChanged || spriteInstanceChanged || shapePathHashChanged || lightTypeChanged || NeedsColorIndexBaking();

                // Mesh Rebuilding
                if (hashChanged || forceUpdate)
                {
                    var batchChannelColor = LightBatch.GetBatchColor();

                    switch (lightType)
                    {
                        case LightType.Freeform:
                            m_LocalBounds = LightUtility.GenerateShapeMesh(this, m_ShapePath, m_ShapeLightFalloffSize, batchChannelColor);
                            break;
                        case LightType.Parametric:
                            m_LocalBounds = LightUtility.GenerateParametricMesh(this, m_ShapeLightParametricRadius, m_ShapeLightFalloffSize, m_ShapeLightParametricAngleOffset, m_ShapeLightParametricSides, batchChannelColor);
                            break;
                        case LightType.Sprite:
                            m_LocalBounds = UpdateSpriteMesh();
                            break;
                        case LightType.Point:
                            m_LocalBounds = LightUtility.GenerateParametricMesh(this, 1.412135f, 0, 0, 4, batchChannelColor);
                            break;
                    }

                    UpdateCookieSpriteTexture();
                    UpdateBatchSlotIndex();
                }
            }
            else
            {
                if (light2DProvider != null)
                {
                    if (m_Mesh != null)
                    {
                        SetProviderMesh(light2DProvider.GetMesh());
                        m_LocalBounds = m_Mesh.bounds;
                    }
                }
                else
                {
                    // A provider light can legitimately have no provider -- the script backing it was
                    // deleted, for instance -- and then no mesh was ever allocated, so m_Mesh is null.
                    if (m_Mesh != null)
                        m_Mesh.Clear();

                    m_LocalBounds = kEmptyBounds;
                }
            }
        }

        internal void UpdateBoundingSphere()
        {
            if (isPointLight)
            {
                boundingSphere = new BoundingSphere(transform.position, m_PointLightOuterRadius);
                return;
            }

            var maxBound = transform.TransformPoint(Vector3.Max(m_LocalBounds.max, m_LocalBounds.max + (Vector3)m_ShapeLightFalloffOffset));
            var minBound = transform.TransformPoint(Vector3.Min(m_LocalBounds.min, m_LocalBounds.min + (Vector3)m_ShapeLightFalloffOffset));
            var center = 0.5f * (maxBound + minBound);
            var radius = Vector3.Magnitude(maxBound - center);

            boundingSphere = new BoundingSphere(center, radius);
        }

        internal bool IsLitLayer(int layer)
        {
            if (m_ApplyToSortingLayers == null)
                return false;

            for (var i = 0; i < m_ApplyToSortingLayers.Length; i++)
                if (m_ApplyToSortingLayers[i] == layer)
                    return true;

            return false;
        }

        internal Matrix4x4 GetMatrix()
        {
            var matrix = transform.localToWorldMatrix;
            if (lightType == Light2D.LightType.Point)
            {
                var scale = new Vector3(pointLightOuterRadius, pointLightOuterRadius, pointLightOuterRadius);
                matrix = Matrix4x4.TRS(transform.position, transform.rotation, scale);
            }
            return matrix;
        }

        private void Awake()
        {
            // Default target sorting layers to "All"
            if (m_ApplyToSortingLayers == null)
            {
                m_ApplyToSortingLayers = new int[SortingLayer.layers.Length];
                for (int i = 0; i < m_ApplyToSortingLayers.Length; ++i)
                    m_ApplyToSortingLayers[i] = SortingLayer.layers[i].id;
            }
        }

        void OnEnable()
        {
            m_PreviousLightCookieSprite = lightCookieSpriteEntityId;
            Light2DManager.RegisterLight(this);
            UpdateCookieSpriteTexture();

#if UNITY_EDITOR
            SortingLayer.onLayerAdded += OnSortingLayerAdded;
            SortingLayer.onLayerRemoved += OnSortingLayerRemoved;
#endif
        }

        private void OnDestroy()
        {
            // Only the mesh we allocated ourselves is ours to destroy; a provider owns the one it returned.
            if (m_OwnsMesh)
                CoreUtils.Destroy(m_Mesh);

            m_Mesh = null;
            m_OwnsMesh = false;
        }

        private void OnDisable()
        {
            Light2DManager.DeregisterLight(this);
            m_CookieSpriteTexture?.Release();

#if UNITY_EDITOR
            SortingLayer.onLayerAdded -= OnSortingLayerAdded;
            SortingLayer.onLayerRemoved -= OnSortingLayerRemoved;
#endif
        }

        private void LateUpdate()
        {
            if (lightType == LightType.Global)
                return;

            UpdateMesh(forceUpdate);
            UpdateBoundingSphere();

            forceUpdate = false;
        }

#if UNITY_EDITOR
        private void OnSortingLayerAdded(SortingLayer layer)
        {
            var newArray = new int[m_ApplyToSortingLayers.Length + 1];
            for (int i = 0; i < m_ApplyToSortingLayers.Length; i++)
            {
                newArray[i] = m_ApplyToSortingLayers[i];
            }
            newArray[m_ApplyToSortingLayers.Length] = layer.id;
            m_ApplyToSortingLayers = newArray;
        }

        private void OnSortingLayerRemoved(SortingLayer layer)
        {
            var tempList = new List<int>();
            foreach (var x in m_ApplyToSortingLayers)
            {
                if (x != layer.id && SortingLayer.IsValid(x))
                    tempList.Add(x);
            }
            m_ApplyToSortingLayers = tempList.ToArray();
        }
#endif

        /// <summary>
        /// OnBeforeSerialize implementation.
        /// </summary>
        public void OnBeforeSerialize()
        {
            m_ComponentVersion = k_CurrentComponentVersion;
        }

        /// <summary>
        /// OnAfterSerialize implementation.
        /// </summary>
        public void OnAfterDeserialize()
        {
            // Upgrade from no serialized version
            if (m_ComponentVersion == ComponentVersions.Version_Unserialized)
            {
                m_ShadowVolumeIntensityEnabled = m_ShadowVolumeIntensity > 0;
                m_ShadowsEnabled = m_ShadowIntensity > 0;
                m_LightVolumeEnabled = m_LightVolumeIntensity > 0;
                m_NormalMapQuality = !m_UseNormalMap ? NormalMapQuality.Disabled : m_NormalMapQuality;
                m_OverlapOperation = m_AlphaBlendOnOverlap ? OverlapOperation.AlphaBlend : m_OverlapOperation;
                m_ComponentVersion = ComponentVersions.Version_1;
            }

            if (m_ComponentVersion < ComponentVersions.Version_2)
            {
                m_ShadowSoftness = 0;
            }


            if (m_ComponentVersion < ComponentVersions.Version_3)
            {
#if UNITY_EDITOR
                m_SelectionSources.selectedHashCode = (int)m_LightType;
#endif
            }

            if (m_ComponentVersion < ComponentVersions.Version_4)
            {
#pragma warning disable CS0618
                if (m_LightType == LightType.Point && (object)m_DeprecatedPointLightCookieSprite != null)
                {
                    m_LightCookieSprite = m_DeprecatedPointLightCookieSprite;
                }
#pragma warning restore CS0618
            }
        }
    }
}
