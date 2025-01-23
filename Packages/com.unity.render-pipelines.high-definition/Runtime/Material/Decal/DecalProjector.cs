using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering.HighDefinition;
using UnityEditor.SceneManagement;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>The scaling mode to apply to decals that use the Decal Projector.</summary>
    public enum DecalScaleMode
    {
        /// <summary>Ignores the transformation hierarchy and uses the scale values in the Decal Projector component directly.</summary>
        ScaleInvariant,
        /// <summary>Multiplies the lossy scale of the Transform with the Decal Projector's own scale then applies this to the decal.</summary>
        [InspectorName("Inherit from Hierarchy")]
        InheritFromHierarchy,
    }

    /// <summary>
    /// Decal Projector component.
    /// </summary>
    [HDRPHelpURLAttribute("understand-decals")]
    [ExecuteAlways]
#if UNITY_EDITOR
    [CanEditMultipleObjects]
#endif
    [AddComponentMenu("Rendering/HDRP Decal Projector")]
    public partial class DecalProjector : MonoBehaviour
    {
        [SerializeField]
        private Material m_Material = null;
        /// <summary>
        /// The material used by the decal. It should be of type HDRP/Decal if you want to have transparency.
        /// </summary>
        public Material material
        {
            get
            {
                return m_Material;
            }
            set
            {
                m_Material = value;
                OnValidate();
            }
        }

#if UNITY_EDITOR
        internal int cachedEditorLayer = 0;
#endif

        [SerializeField]
        private float m_DrawDistance = 1000.0f;
        /// <summary>
        /// Distance from camera at which the Decal is not rendered anymore.
        /// </summary>
        public float drawDistance
        {
            get
            {
                return m_DrawDistance;
            }
            set
            {
                m_DrawDistance = Mathf.Max(0f, value);
                OnValidate();
            }
        }

        [SerializeField]
        [Range(0, 1)]
        private float m_FadeScale = 0.9f;
        /// <summary>
        /// Percent of the distance from the camera at which this Decal start to fade off.
        /// </summary>
        public float fadeScale
        {
            get
            {
                return m_FadeScale;
            }
            set
            {
                m_FadeScale = Mathf.Clamp01(value);
                OnValidate();
            }
        }

        [SerializeField]
        [Range(0, 180)]
        private float m_StartAngleFade = 180.0f;
        /// <summary>
        /// Angle between decal backward orientation and vertex normal of receiving surface at which the Decal start to fade off.
        /// </summary>
        public float startAngleFade
        {
            get
            {
                return m_StartAngleFade;
            }
            set
            {
                m_StartAngleFade = Mathf.Clamp(value, 0.0f, 180.0f);
                OnValidate();
            }
        }

        [SerializeField]
        [Range(0, 180)]
        private float m_EndAngleFade = 180.0f;
        /// <summary>
        /// Angle between decal backward orientation and vertex normal of receiving surface at which the Decal end to fade off.
        /// </summary>
        public float endAngleFade
        {
            get
            {
                return m_EndAngleFade;
            }
            set
            {
                m_EndAngleFade = Mathf.Clamp(value, m_StartAngleFade, 180.0f);
                OnValidate();
            }
        }

        [SerializeField]
        private Vector2 m_UVScale = new Vector2(1, 1);
        /// <summary>
        /// Tilling of the UV of the projected texture.
        /// </summary>
        public Vector2 uvScale
        {
            get
            {
                return m_UVScale;
            }
            set
            {
                m_UVScale = value;
                OnValidate();
            }
        }

        [SerializeField]
        private Vector2 m_UVBias = new Vector2(0, 0);
        /// <summary>
        /// Offset of the UV of the projected texture.
        /// </summary>
        public Vector2 uvBias
        {
            get
            {
                return m_UVBias;
            }
            set
            {
                m_UVBias = value;
                OnValidate();
            }
        }

        [SerializeField]
        private bool m_AffectsTransparency = false;
        /// <summary>
        /// Change the transparency. It is only compatible when using HDRP/Decal shader.
        /// </summary>
        public bool affectsTransparency
        {
            get
            {
                return m_AffectsTransparency;
            }
            set
            {
                m_AffectsTransparency = value;
                OnValidate();
            }
        }

        [SerializeField]
        private IntScalableSettingValue m_TransparentTextureResolution = new IntScalableSettingValue
        {
            @override = 256,
            useOverride = true,
        };

        /// <summary>
        /// Resolution that is used when the projector affects transparency and a shader graph is being used
        /// </summary>
        public IntScalableSettingValue TransparentTextureResolution
        {
            get => m_TransparentTextureResolution;
            set
            {
                m_TransparentTextureResolution = value;
                OnValidate();
            }
        }

        [SerializeField]
        RenderingLayerMask m_DecalLayerMask = (RenderingLayerMask) (uint) UnityEngine.RenderingLayerMask.defaultRenderingLayerMask;
        /// <summary>
        /// The layer of the decal.
        /// </summary>
        public RenderingLayerMask decalLayerMask
        {
            get => m_DecalLayerMask;
            set => m_DecalLayerMask = value;
        }

        [SerializeField]
        private DecalScaleMode m_ScaleMode = DecalScaleMode.ScaleInvariant;
        /// <summary>
        /// The scaling mode to apply to decals that use this Decal Projector.
        /// </summary>
        public DecalScaleMode scaleMode
        {
            get => m_ScaleMode;
            set
            {
                m_ScaleMode = value;
                OnValidate();
            }
        }

        [SerializeField]
        internal Vector3 m_Offset = new Vector3(0, 0, 0);
        /// <summary>
        /// Change the pivot position.
        /// It is an offset between the center of the projection and the transform position.
        /// </summary>
        public Vector3 pivot
        {
            get
            {
                return m_Offset;
            }
            set
            {
                m_Offset = value;
                OnValidate();
            }
        }

        [SerializeField]
        internal Vector3 m_Size = new Vector3(1, 1, 1);
        /// <summary>
        /// The size of the projection volume.
        /// See also <seealso cref="ResizeAroundPivot"/> to rescale relatively to the pivot position.
        /// </summary>
        public Vector3 size
        {
            get => m_Size;
            set
            {
                m_Size = value;
                OnValidate();
            }
        }

        /// <summary>
        /// Update the pivot to resize centered on the pivot position.
        /// </summary>
        /// <param name="newSize">The new size.</param>
        public void ResizeAroundPivot(Vector3 newSize)
        {
            for (int axis = 0; axis < 3; ++axis)
                if (m_Size[axis] > Mathf.Epsilon)
                    m_Offset[axis] *= newSize[axis] / m_Size[axis];
            size = newSize;
        }

        [SerializeField]
        [Range(0, 1)]
        private float m_FadeFactor = 1.0f;
        /// <summary>
        /// Controls the transparency of the decal.
        /// </summary>
        public float fadeFactor
        {
            get
            {
                return m_FadeFactor;
            }
            set
            {
                m_FadeFactor = Mathf.Clamp01(value);
                OnValidate();
            }
        }

        private Material m_OldMaterial = null;
        private DecalSystem.DecalHandle m_Handle = null;

        /// <summary>A scale that should be used for rendering and handles.</summary>
        internal Vector3 effectiveScale => m_ScaleMode == DecalScaleMode.InheritFromHierarchy ? transform.lossyScale : Vector3.one;

        /// <summary>current position in a way the DecalSystem will be able to use it</summary>
        internal Vector3 position => transform.position;
        /// <summary>current uv parameters in a way the DecalSystem will be able to use it</summary>
        internal Vector4 uvScaleBias => new Vector4(m_UVScale.x, m_UVScale.y, m_UVBias.x, m_UVBias.y);

        internal DecalSystem.DecalHandle Handle
        {
            get
            {
                return this.m_Handle;
            }
            set
            {
                this.m_Handle = value;
            }
        }

        // Struct used to gather all decal property required to be cached to be sent to shader code
        internal struct CachedDecalData
        {
            public float drawDistance;
            public float fadeScale;
            public float startAngleFade;
            public float endAngleFade;
            public Vector4 uvScaleBias;
            public bool affectsTransparency;
            public int layerMask;
            public ulong sceneLayerMask;
            public float fadeFactor;
            public RenderingLayerMask decalLayerMask;
        }

        internal CachedDecalData GetCachedDecalData()
        {
            CachedDecalData data = new CachedDecalData();

            data.drawDistance = m_DrawDistance;
            data.fadeScale = m_FadeScale;
            data.startAngleFade = m_StartAngleFade;
            data.endAngleFade = m_EndAngleFade;
            data.uvScaleBias = uvScaleBias;
            data.affectsTransparency = m_AffectsTransparency;
            data.layerMask = gameObject.layer;
            data.sceneLayerMask = gameObject.sceneCullingMask;
            data.fadeFactor = m_FadeFactor;
            data.decalLayerMask = decalLayerMask;

            return data;
        }

        void InitMaterial()
        {
#if UNITY_EDITOR
            if (m_Material == null && GraphicsSettings.TryGetRenderPipelineSettings<HDRenderPipelineEditorMaterials>(out var defaultMaterials))
                m_Material = defaultMaterials.defaultDecalMaterial;
#endif

            if (m_Material == null)
            {
                Debug.LogWarning($"{name} has a null {typeof(Material)} on the {nameof(material)} property.");
            }
        }

        void Reset() => InitMaterial();

#if UNITY_EDITOR
        static List<DecalProjector> m_DecalProjectorInstances;  // List of decal projectors that have had OnEnable() called on them
        [NonSerialized] int m_EditorInstanceIndex = -1;
#endif
        void OnEnable()
        {
            InitMaterial();

            if (m_Handle != null)
            {
                DecalSystem.instance.RemoveDecal(m_Handle);
                m_Handle = null;
            }

            m_Handle = DecalSystem.instance.AddDecal(this);
            m_OldMaterial = m_Material;

#if UNITY_EDITOR
            cachedEditorLayer = gameObject.layer;
            // Handle scene visibility

            // Create a list to keep track of all active decal projectors and register our event handlers that dispatch events to each one.
            // We do this instead of registering event handlers individually for each instance as that generates far more garbage on the heap
            if (m_DecalProjectorInstances == null)
            {
                m_DecalProjectorInstances = new List<DecalProjector>();
                SceneVisibilityManager.visibilityChanged += UpdateDecalVisibilityDispatcher;
                PrefabStage.prefabStageOpened += RegisterDecalVisibilityUpdatePrefabStage;

                if (PrefabStageUtility.GetCurrentPrefabStage() != null) // In case the prefab stage is already opened when enabling the decal
                    RegisterDecalVisibilityUpdatePrefabStage();
            }
            m_EditorInstanceIndex = m_DecalProjectorInstances.Count;
            m_DecalProjectorInstances.Add(this);
#endif
        }

#if UNITY_EDITOR
        // Handler for the SceneVisibilityManager.visibilityChanged event, calls UpdateDecalVisibility() on each active decal projector
        static void UpdateDecalVisibilityDispatcher()
        {
            foreach (DecalProjector decalProjector in m_DecalProjectorInstances)
                decalProjector.UpdateDecalVisibility();
        }

        // Handle the SceneVisibilityManager.visibilityChanged event for this decal projector
        void UpdateDecalVisibility()
        {
            // This callback is called before HDRP is initialized after a domain reload
            if (HDRenderPipeline.currentPipeline == null)
                return;

            // Fade out the decal when it is hidden by the scene visibility
            if (SceneVisibilityManager.instance.IsHidden(gameObject) && m_Handle != null)
            {
                DecalSystem.instance.RemoveDecal(m_Handle);
                m_Handle = null;
            }
            else if (m_Handle == null)
            {
                m_Handle = DecalSystem.instance.AddDecal(this);
            }
            else
            {
                // Scene culling mask may have changed.
                DecalSystem.instance.UpdateCachedData(m_Handle, this);
            }
        }

        // Handler for the PrefabStage.prefabStageOpened event
        static void RegisterDecalVisibilityUpdatePrefabStage(PrefabStage stage = null)
        {
            SceneView.duringSceneGui -= UpdateDecalVisibilityPrefabStageDispatcher;
            SceneView.duringSceneGui += UpdateDecalVisibilityPrefabStageDispatcher;
        }

        void UnregisterDecalVisibilityUpdatePrefabStage()
        {
            SceneView.duringSceneGui -= UpdateDecalVisibilityPrefabStageDispatcher;
        }

        // Handler for the SceneView.duringSceneGui event, calls UpdateDecalVisibilityPrefabStage() on each active decal projector
        static void UpdateDecalVisibilityPrefabStageDispatcher(SceneView sv)
        {
            foreach (DecalProjector decalProjector in m_DecalProjectorInstances)
                decalProjector.UpdateDecalVisibilityPrefabStage(sv);

        }

        // Handle the SceneView.duringSceneGui event for this decal projector
        bool m_LastPrefabStageVisibility = true;
        void UpdateDecalVisibilityPrefabStage(SceneView sv)
        {
            bool showDecal = true;

            // If prefab context is not hidden, then we should render the decal
            if (!CoreUtils.IsSceneViewPrefabStageContextHidden())
                showDecal = true;

            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                bool isDecalInPrefabStage = gameObject.scene == stage.scene;

                if (!isDecalInPrefabStage && stage.mode == PrefabStage.Mode.InIsolation)
                    showDecal = false;
                if (!isDecalInPrefabStage && CoreUtils.IsSceneViewPrefabStageContextHidden())
                    showDecal = false;
            }

            // Update decal visibility based on showDecal
            if (!m_LastPrefabStageVisibility && showDecal)
                m_Handle = DecalSystem.instance.AddDecal(this);
            else if (m_LastPrefabStageVisibility && !showDecal)
                DecalSystem.instance.RemoveDecal(m_Handle);
            m_LastPrefabStageVisibility = showDecal;
        }
#endif  // UNITY_EDITOR

        void OnDisable()
        {
            if (m_Handle != null)
            {
                DecalSystem.instance.RemoveDecal(m_Handle);
                m_Handle = null;
            }
#if UNITY_EDITOR
            // Remove this instance from the list tracking active decal projectors.
            // We do this by swapping the last element of the array into the slot we are deleting rather than shuffling the entire contents above the slot down as it's faster and the actual ordering is not significant
            var movedInstance = m_DecalProjectorInstances[m_EditorInstanceIndex] = m_DecalProjectorInstances[m_DecalProjectorInstances.Count - 1];
            movedInstance.m_EditorInstanceIndex = m_EditorInstanceIndex;
            m_EditorInstanceIndex = -1;
            m_DecalProjectorInstances.RemoveAt(m_DecalProjectorInstances.Count - 1);

            // If the list of active instances is now empty delete it and un-register our event handlers
            if (m_DecalProjectorInstances.Count == 0)
            {
                SceneVisibilityManager.visibilityChanged -= UpdateDecalVisibilityDispatcher;
                PrefabStage.prefabStageOpened -= RegisterDecalVisibilityUpdatePrefabStage;
                UnregisterDecalVisibilityUpdatePrefabStage();
                m_DecalProjectorInstances = null;
            }
#endif
        }

        /// <summary>
        /// Event called each time the used material change.
        /// </summary>
        public event Action OnMaterialChange;

        internal void OnValidate()
        {
            if (m_Handle != null) // don't do anything if OnEnable hasn't been called yet when scene is loading.
            {
                if (m_Material == null)
                {
                    DecalSystem.instance.RemoveDecal(m_Handle);
                }

                // handle material changes, because decals are stored as sets sorted by material, if material changes decal needs to be removed and re-added to that it goes into correct set
                if (m_OldMaterial != m_Material)
                {
                    DecalSystem.instance.RemoveDecal(m_Handle);

                    if (m_Material != null)
                    {
                        m_Handle = DecalSystem.instance.AddDecal(this);
                    }

                    // notify the editor that material has changed so it can update the shader foldout
                    if (OnMaterialChange != null)
                    {
                        OnMaterialChange();
                    }

                    m_OldMaterial = m_Material;
                }
                else // no material change, just update whatever else changed
                {
                    DecalSystem.instance.UpdateCachedData(m_Handle, this);
                }

#if UNITY_EDITOR
                cachedEditorLayer = gameObject.layer;
#endif
            }
        }

        /// <summary>
        /// Check if the material is set and if it is different than the default one
        /// </summary>
        /// <returns>True: the material is set and is not the default one</returns>
        public bool IsValid()
        {
            // don't draw if no material or if material is the default decal material (empty)
            if (m_Material == null)
                return false;

#if UNITY_EDITOR
            if (!HDRenderPipeline.isReady || m_Material == HDRenderPipeline.currentAsset.GetDefaultDecalMaterial())
                return false;
#endif

            return true;
        }

        /// <summary>
        /// If called it will force an update of the shader graph textures in the transparent decal atlas
        /// if the DecalProjector has Affects Transparent enabled
        /// </summary>
        public void UpdateTransparentShaderGraphTextures()
        {
            DecalSystem.instance.UpdateTransparentShaderGraphTextures(m_Handle, this);
        }
    }
}
