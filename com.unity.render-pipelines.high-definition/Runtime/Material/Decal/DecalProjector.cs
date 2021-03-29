using System;
using UnityEditor;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine.Rendering;

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
    public partial class DecalProjector : DecalBase
    {

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

        internal void OnValidate()
        {
            base.OnValidate();
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
                    RaiseOnMaterialChange();

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
