using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    public class DecalProjectorComponent : MonoBehaviour
    {
        public Material m_Material = null;
		public float m_DrawDistance = 1000.0f;
        public float m_FadeScale = 0.9f;
        public Vector2 m_UVScale = new Vector2(1,1);
        public Vector2 m_UVBias = new Vector2(0,0);
        private Material m_OldMaterial = null;
        private DecalSystem.DecalHandle m_Handle = null;

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

        public Material Mat
        {
            get { return this.m_Material; }
        }

        public void OnEnable()
        {
            if (m_Material == null)
            {
                var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
                m_Material = hdrp != null ? hdrp.GetDefaultDecalMaterial() : null;
            }

            if(m_Handle != null)
                DecalSystem.instance.RemoveDecal(m_Handle);
            Vector4 uvScaleBias = new Vector4(m_UVScale.x, m_UVScale.y, m_UVBias.x, m_UVBias.y);
            m_Handle = DecalSystem.instance.AddDecal(transform, m_DrawDistance, m_FadeScale, uvScaleBias, m_Material);
        }

        public void OnDisable()
        {
            DecalSystem.instance.RemoveDecal(m_Handle);
            m_Handle = null;
        }

		// Declare the method signature of the delegate to call.	
		public delegate void OnMaterialChangeDelegate();

		// Declare the event to which editor code will hook itself.
		public event OnMaterialChangeDelegate OnMaterialChange;

        public void OnValidate()
        {
            // handle material changes
            if (m_OldMaterial != m_Material)
            {
                if( m_Handle != null)
                    DecalSystem.instance.RemoveDecal(m_Handle);
                Vector4 uvScaleBias = new Vector4(m_UVScale.x, m_UVScale.y, m_UVBias.x, m_UVBias.y);
                m_Handle = DecalSystem.instance.AddDecal(transform, m_DrawDistance, m_FadeScale, uvScaleBias, m_Material);
                m_OldMaterial = m_Material;

                // notify the editor that material has changed so it can update the shader foldout
				if (OnMaterialChange != null)
				{
					OnMaterialChange();
				}
            }

            if (m_Material != null)
            {
                Shader shader = m_Material.shader;
                if((shader != null) &&  (shader.name != "HDRenderPipeline/Decal"))
                {
                    Debug.LogWarning("Decal projector component material is not using HDRenderPipeline/Decal shader.", this);
                }
            }
        }

        public void Update()
        {
            if (m_Handle != null)
            {
                if (transform.hasChanged == true)
                {
                    Vector4 uvScaleBias = new Vector4(m_UVScale.x, m_UVScale.y, m_UVBias.x, m_UVBias.y);
                    DecalSystem.instance.UpdateCachedData(transform, m_DrawDistance, m_FadeScale, uvScaleBias, m_Handle);
                    transform.hasChanged = false;
                }
            }
        }

        private void DrawGizmo(bool selected)
        {
            var col = new Color(0.0f, 0.7f, 1f, 1.0f);
            Matrix4x4 offset = Matrix4x4.Translate(new Vector3(0.0f, -0.5f, 0.0f));
            Gizmos.matrix = transform.localToWorldMatrix * offset;
            col.a = selected ? 0.5f : 0.2f;
            Gizmos.color = col;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }

        public void OnDrawGizmosSelected()
        {
            DrawGizmo(true);
            // if this object is selected there is a chance the transform was changed so update culling info
            Vector4 uvScaleBias = new Vector4(m_UVScale.x, m_UVScale.y, m_UVBias.x, m_UVBias.y);
            DecalSystem.instance.UpdateCachedData(transform, m_DrawDistance, m_FadeScale, uvScaleBias, m_Handle);
        }

        public void OnDrawGizmos()
        {
            DrawGizmo(false);
        }

        public bool IsValid()
        {
            // don't draw if no material or if material is the default decal material (empty)
            if (m_Material == null)
                return false;

            var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            if ((hdrp != null) && (m_Material == hdrp.GetDefaultDecalMaterial()))
                return false;

            return true;
        }
    }
}
