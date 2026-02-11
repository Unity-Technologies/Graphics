using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;

public class SplitGroupStressTest : MonoBehaviour
{

    public List<Texture> Textures;
    public List<Mesh> Meshes;
    public int MaxInstanceCount = 100;
    public int InstancePerFrame = 30;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private List<VisualEffect> m_OriginalVisualEffects;
    private List<VisualEffect> m_VisualEffects;
    private int m_Index;

    int kTextureId = Shader.PropertyToID("Texture");
    int kMeshId = Shader.PropertyToID("Mesh");

    private bool m_DeletionMode = false;

    void Start()
    {
        m_OriginalVisualEffects = new List<VisualEffect>(FindObjectsByType<VisualEffect>());
        m_VisualEffects = new List<VisualEffect>(MaxInstanceCount);

        // To test global texture support
        Shader.SetGlobalTexture("texture_a", Textures[0]);
    }

    // Update is called once per frame
    void Update()
    {
        if (!m_DeletionMode)
        {
            if (m_VisualEffects.Count < MaxInstanceCount)
            {
                for (int i = 0; i < InstancePerFrame; i++)
                {
                    var refVfx = m_OriginalVisualEffects[m_Index % m_OriginalVisualEffects.Count];
                    var newPos = refVfx.gameObject.transform.localPosition + Vector3.left;
                    var vfx = Instantiate(refVfx, newPos, Quaternion.identity);
                    vfx.SetTexture(kTextureId, Textures[m_Index % Textures.Count]);
                    vfx.SetMesh(kMeshId, Meshes[m_Index % Meshes.Count]);
                    m_VisualEffects.Add(vfx);
                    m_Index++;
                }
            }
            else
            {
                m_DeletionMode = true;
            }
        }
        else
        {
            for (int i = 0; i < InstancePerFrame; i++)
            {
                if (m_VisualEffects.Count > 0)
                {
                    Destroy(m_VisualEffects[m_VisualEffects.Count - 1].gameObject);
                    m_VisualEffects.RemoveAt(m_VisualEffects.Count - 1);
                }
            }
        }

    }
}
