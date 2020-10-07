using System;
using UnityEditor;
using UnityEditor.Rendering.HighDefinition;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Decal Projector component.
    /// </summary>
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "Decal-Projector" + Documentation.endURL)]
    [ExecuteAlways]
#if UNITY_EDITOR
    [CanEditMultipleObjects]
#endif
    [AddComponentMenu("Rendering/Decal Projector")]
    public partial class DecalProjector : MonoBehaviour
    {
        internal static readonly Quaternion k_MinusYtoZRotation = Quaternion.Euler(-90, 0, 0);

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
        private int m_Layer;
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
        [Range(0,1)]
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
        DecalLayerEnum m_DecalLayerMask = DecalLayerEnum.LightLayerDefault;
        /// <summary>
        /// The layer of the decal.
        /// </summary>
        public DecalLayerEnum decalLayerMask
        {
            get => m_DecalLayerMask;
            set => m_DecalLayerMask = value;
        }

        [SerializeField]
        private Vector3 m_Offset = new Vector3(0, 0, 0.5f);
        /// <summary>
        /// Change the offset position.
        /// Do not expose: Could be changed by the inspector when manipulating the gizmo.
        /// </summary>
        internal Vector3 offset
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
        Vector3 m_Size = new Vector3(1, 1, 1);
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
        private DecalSystem.DecalHandle m_Handle = null;


        /// <summary>current rotation in a way the DecalSystem will be able to use it</summary>
        internal Quaternion rotation => transform.rotation * k_MinusYtoZRotation;
        /// <summary>current position in a way the DecalSystem will be able to use it</summary>
        internal Vector3 position => transform.position;
        /// <summary>current size in a way the DecalSystem will be able to use it</summary>
        internal Vector3 decalSize => new Vector3(m_Size.x, m_Size.z, m_Size.y);
        /// <summary>current size in a way the DecalSystem will be able to use it</summary>
        internal Vector3 decalOffset => new Vector3(m_Offset.x, -m_Offset.z, m_Offset.y);
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
            public Matrix4x4 localToWorld;
            public Quaternion rotation;
            public Matrix4x4 sizeOffset;
            public float drawDistance;
            public float fadeScale;
            public float startAngleFade;
            public float endAngleFade;
            public Vector4 uvScaleBias;
            public bool affectsTransparency;
            public int layerMask;
            public ulong sceneLayerMask;
            public float fadeFactor;
            public DecalLayerEnum decalLayerMask;
        }

        internal CachedDecalData GetCachedDecalData()
        {
            CachedDecalData data = new CachedDecalData();

            data.localToWorld = Matrix4x4.TRS(position, rotation, Vector3.one);
            data.rotation = rotation;
            data.sizeOffset = Matrix4x4.Translate(decalOffset) * Matrix4x4.Scale(decalSize);
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
            if (m_Material == null)
            {
#if UNITY_EDITOR
                var hdrp = HDRenderPipeline.defaultAsset;
                m_Material = hdrp != null ? hdrp.GetDefaultDecalMaterial() : null;
#else
                m_Material = null;
#endif
            }
        }

        void Reset() => InitMaterial();

        void OnEnable()
        {
            InitMaterial();

            if (m_Handle != null)
            {
                DecalSystem.instance.RemoveDecal(m_Handle);
                m_Handle = null;
            }

            m_Handle = DecalSystem.instance.AddDecal(m_Material, GetCachedDecalData());
            m_OldMaterial = m_Material;

#if UNITY_EDITOR
            m_Layer = gameObject.layer;
            // Handle scene visibility
            UnityEditor.SceneVisibilityManager.visibilityChanged += UpdateDecalVisibility;
#endif
        }

#if UNITY_EDITOR
        void UpdateDecalVisibility()
        {
            // Fade out the decal when it is hidden by the scene visibility
            if (UnityEditor.SceneVisibilityManager.instance.IsHidden(gameObject) && m_Handle != null)
            {
                DecalSystem.instance.RemoveDecal(m_Handle);
                m_Handle = null;
            }
            else if (m_Handle == null)
            {
                m_Handle = DecalSystem.instance.AddDecal(m_Material, GetCachedDecalData());
            }
            else
            {
                // Scene culling mask may have changed.
                DecalSystem.instance.UpdateCachedData(m_Handle, GetCachedDecalData());
            }
        }
#endif

        void OnDisable()
        {
            if (m_Handle != null)
            {
                DecalSystem.instance.RemoveDecal(m_Handle);
                m_Handle = null;
            }
#if UNITY_EDITOR
            UnityEditor.SceneVisibilityManager.visibilityChanged -= UpdateDecalVisibility;
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
                        m_Handle = DecalSystem.instance.AddDecal(m_Material, GetCachedDecalData());

                        if (!DecalSystem.IsHDRenderPipelineDecal(m_Material.shader)) // non HDRP/decal shaders such as shader graph decal do not affect transparency
                        {
                            m_AffectsTransparency = false;
                        }
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
                    DecalSystem.instance.UpdateCachedData(m_Handle, GetCachedDecalData());
                }
            }
        }

#if UNITY_EDITOR
        void Update() // only run in editor
        {
            if (m_Layer != gameObject.layer)
            {
                m_Layer = gameObject.layer;
                DecalSystem.instance.UpdateCachedData(m_Handle, GetCachedDecalData());
            }
        }
#endif

        void LateUpdate()
        {
            if (m_Handle != null)
            {
                if (transform.hasChanged == true)
                {
                    DecalSystem.instance.UpdateCachedData(m_Handle, GetCachedDecalData());
                    transform.hasChanged = false;
                }
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
            var hdrp = HDRenderPipeline.defaultAsset;
            if ((hdrp != null) && (m_Material == hdrp.GetDefaultDecalMaterial()))
                return false;
#endif

            return true;
        }
    }
}
