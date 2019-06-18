using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteAlways]
#if UNITY_EDITOR
    [CanEditMultipleObjects]
#endif
    public partial class DecalProjectorComponent : MonoBehaviour
    {
        internal static readonly Quaternion k_MinusYtoZRotation = Quaternion.Euler(-90, 0, 0);

        [SerializeField]
        private Material m_Material = null;
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
        private Vector2 m_UVScale = new Vector2(1, 1);
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
        private Vector3 m_Offset = new Vector3(0, -0.5f, 0);
        public Vector3 offset
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

        public DecalSystem.DecalHandle Handle
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
        
        public void OnEnable()
        {
            if (m_Material == null)
            {
#if UNITY_EDITOR
                var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
                m_Material = hdrp != null ? hdrp.GetDefaultDecalMaterial() : null;
#else
                m_Material = null;
#endif
            }

            if (m_Handle != null)
            {
                DecalSystem.instance.RemoveDecal(m_Handle);
                m_Handle = null;
            }
            
            Matrix4x4 sizeOffset = Matrix4x4.Translate(decalOffset) * Matrix4x4.Scale(decalSize);
            m_Handle = DecalSystem.instance.AddDecal(position, rotation, Vector3.one, sizeOffset, m_DrawDistance, m_FadeScale, uvScaleBias, m_AffectsTransparency, m_Material, gameObject.layer, m_FadeFactor);
        }

        public void OnDisable()
        {
            if (m_Handle != null)
            {
                DecalSystem.instance.RemoveDecal(m_Handle);
                m_Handle = null;
            }
        }

        // Declare the method signature of the delegate to call.
        public delegate void OnMaterialChangeDelegate();

        // Declare the event to which editor code will hook itself.
        public event OnMaterialChangeDelegate OnMaterialChange;

        public void OnValidate()
        {
            if (m_Handle != null) // don't do anything if OnEnable hasn't been called yet when scene is loading.
            {
                Matrix4x4 sizeOffset = Matrix4x4.Translate(decalOffset) * Matrix4x4.Scale(decalSize);
                // handle material changes, because decals are stored as sets sorted by material, if material changes decal needs to be removed and re-added to that it goes into correct set
                if (m_OldMaterial != m_Material)
                {
                    DecalSystem.instance.RemoveDecal(m_Handle);
                    m_Handle = DecalSystem.instance.AddDecal(position, rotation, Vector3.one, sizeOffset, m_DrawDistance, m_FadeScale, uvScaleBias, m_AffectsTransparency, m_Material, gameObject.layer, m_FadeFactor);
                    m_OldMaterial = m_Material;

                    if (!DecalSystem.IsHDRenderPipelineDecal(m_Material.shader.name)) // non HDRP/decal shaders such as shader graph decal do not affect transparency
                    {
                        m_AffectsTransparency = false;
                    }

                    // notify the editor that material has changed so it can update the shader foldout
                    if (OnMaterialChange != null)
                    {
                        OnMaterialChange();
                    }
                }
                else // no material change, just update whatever else changed
                {
                    DecalSystem.instance.UpdateCachedData(position, rotation, sizeOffset, m_DrawDistance, m_FadeScale, uvScaleBias, m_AffectsTransparency, m_Handle, gameObject.layer, m_FadeFactor);
                }
            }
        }

        public void LateUpdate()
        {
            if (m_Handle != null)
            {
                if (transform.hasChanged == true)
                {
                    Matrix4x4 sizeOffset = Matrix4x4.Translate(decalOffset) * Matrix4x4.Scale(decalSize);
                    DecalSystem.instance.UpdateCachedData(position, rotation, sizeOffset, m_DrawDistance, m_FadeScale, uvScaleBias, m_AffectsTransparency, m_Handle, gameObject.layer, m_FadeFactor);
                    transform.hasChanged = false;
                }
            }
        }

        public bool IsValid()
        {
            // don't draw if no material or if material is the default decal material (empty)
            if (m_Material == null)
                return false;

#if UNITY_EDITOR
            var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            if ((hdrp != null) && (m_Material == hdrp.GetDefaultDecalMaterial()))
                return false;
#endif

            return true;
        }
    }
}
