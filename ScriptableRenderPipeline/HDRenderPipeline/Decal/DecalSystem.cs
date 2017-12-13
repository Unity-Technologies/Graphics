using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class DecalSystem
    {
        static DecalSystem m_Instance;
        static public DecalSystem instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new DecalSystem();
                return m_Instance;
            }
        }

        internal HashSet<DecalProjectorComponent> m_Decals = new HashSet<DecalProjectorComponent>();
        Mesh m_DecalMesh;

        public DecalSystem()
        {
            m_DecalMesh = CoreUtils.CreateDecalMesh();
        }

       
        public void AddDecal(DecalProjectorComponent d)
        {
            // If no decal material assign, don't add it
            if (d.m_Material == null)
                return;

            if (d.m_Material.GetTexture("_BaseColorMap") || d.m_Material.GetTexture("_NormalMap"))
            {
                RemoveDecal(d);
                m_Decals.Add(d);
            }
        }

        public void RemoveDecal(DecalProjectorComponent d)
        {
            m_Decals.Remove(d);
        }

        public void Render(ScriptableRenderContext renderContext, Vector3 cameraPos, CommandBuffer cmd)
        {
            if (m_DecalMesh == null)
                m_DecalMesh = CoreUtils.CreateDecalMesh();
            foreach (var decal in m_Decals)
            {
				decal.UpdatePropertyBlock(cameraPos);
                cmd.DrawMesh(m_DecalMesh, decal.transform.localToWorldMatrix, decal.m_Material, 0, 0, decal.GetPropertyBlock());
            }
        }
    }
}
