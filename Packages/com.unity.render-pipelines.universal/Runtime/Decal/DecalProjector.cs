using System;
using UnityEditor;

namespace UnityEngine.Rendering.Universal
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
    [ExecuteAlways]
#if UNITY_EDITOR
    [CanEditMultipleObjects]
#endif
    [AddComponentMenu("Rendering/URP Decal Projector")]
    public class DecalProjector : MonoBehaviour
    {
        internal delegate void DecalProjectorAction(DecalProjector decalProjector);
        internal static event DecalProjectorAction onDecalAdd;
        internal static event DecalProjectorAction onDecalRemove;
        internal static event DecalProjectorAction onDecalPropertyChange;
        internal static event Action onAllDecalPropertyChange;
        internal static event DecalProjectorAction onDecalMaterialChange;
        internal static Material defaultMaterial { get; set; }
        internal static bool isSupported => onDecalAdd != null;

        internal DecalEntity decalEntity { get; set; }

        [SerializeField]
        private Material m_Material = null;
        /// <summary>
        /// The material used by the decal.
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
        uint m_DecalLayerMask = 1;
        /// <summary>
        /// The layer of the decal.
        /// </summary>
        public uint renderingLayerMask
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
        internal Vector3 m_Offset = new Vector3(0, 0, 0.5f);
        /// <summary>
        /// Change the offset position.
        /// Do not expose: Could be changed by the inspector when manipulating the gizmo.
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
        /// </summary>
        public Vector3 size
        {
            get
            {
                return m_Size;
            }
            set
            {
                m_Size = value;
                OnValidate();
            }
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

        /// <summary>A scale that should be used for rendering and handles.</summary>
        internal Vector3 effectiveScale => m_ScaleMode == DecalScaleMode.InheritFromHierarchy ? transform.lossyScale : Vector3.one;
        /// <summary>current size in a way the DecalSystem will be able to use it</summary>
        internal Vector3 decalSize => new Vector3(m_Size.x, m_Size.z, m_Size.y);
        /// <summary>current size in a way the DecalSystem will be able to use it</summary>
        internal Vector3 decalOffset => new Vector3(m_Offset.x, -m_Offset.z, m_Offset.y);
        /// <summary>current uv parameters in a way the DecalSystem will be able to use it</summary>
        internal Vector4 uvScaleBias => new Vector4(m_UVScale.x, m_UVScale.y, m_UVBias.x, m_UVBias.y);

        void InitMaterial()
        {
            if (m_Material == null)
            {
#if UNITY_EDITOR
                m_Material = defaultMaterial;
#endif
            }
        }

        void OnEnable()
        {
            InitMaterial();

            m_OldMaterial = m_Material;

            onDecalAdd?.Invoke(this);

#if UNITY_EDITOR
            // Handle scene visibility
            UnityEditor.SceneVisibilityManager.visibilityChanged += UpdateDecalVisibility;
#endif
        }

#if UNITY_EDITOR
        void UpdateDecalVisibility()
        {
            // Fade out the decal when it is hidden by the scene visibility
            if (UnityEditor.SceneVisibilityManager.instance.IsHidden(gameObject))
            {
                onDecalRemove?.Invoke(this);
            }
            else
            {
                onDecalAdd?.Invoke(this);
                onDecalPropertyChange?.Invoke(this); // Scene culling mask may have changed.
            }
        }

#endif

        void OnDisable()
        {
            onDecalRemove?.Invoke(this);

#if UNITY_EDITOR
            UnityEditor.SceneVisibilityManager.visibilityChanged -= UpdateDecalVisibility;
#endif
        }

        internal void OnValidate()
        {
            if (!isActiveAndEnabled)
                return;

            if (m_Material != m_OldMaterial)
            {
                onDecalMaterialChange?.Invoke(this);
                m_OldMaterial = m_Material;
            }
            else
                onDecalPropertyChange?.Invoke(this);
        }

        /// <summary>
        /// Checks if material is valid for rendering decals.
        /// </summary>
        /// <returns>True if material is valid.</returns>
        public bool IsValid()
        {
            if (material == null)
                return false;

            if (material.FindPass(DecalShaderPassNames.DBufferProjector) != -1)
                return true;

            if (material.FindPass(DecalShaderPassNames.DecalProjectorForwardEmissive) != -1)
                return true;

            if (material.FindPass(DecalShaderPassNames.DecalScreenSpaceProjector) != -1)
                return true;

            if (material.FindPass(DecalShaderPassNames.DecalGBufferProjector) != -1)
                return true;

            return false;
        }

        internal static void UpdateAllDecalProperties()
        {
            onAllDecalPropertyChange?.Invoke();
        }
    }
}
