using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.SDF;


public class VFXUpdateSkinnedSDF : MonoBehaviour
{
    MeshToSDFBaker m_Baker;
    SkinnedMeshRenderer m_SkinnedMeshRenderer;
    Mesh m_Mesh;
    VisualEffect m_Vfx;
    public int maxResolution = 64;
    public Vector3 center;
    public Vector3 sizeBox;
    public int signPassCount = 1;
    public float threshold = 0.5f;


    void Start()
    {
        m_Mesh = new Mesh();
        m_Vfx = GetComponent<VisualEffect>();
        m_SkinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        m_SkinnedMeshRenderer.BakeMesh(m_Mesh);
        m_Baker = new MeshToSDFBaker(sizeBox, center, maxResolution, m_Mesh, signPassCount, threshold);
        m_Baker.BakeSDF();
        m_Vfx.SetTexture("WalkingSDF", m_Baker.SdfTexture);
        m_Vfx.SetVector3("BoxSize", m_Baker.GetActualBoxSize());
    }
    void Update()
    {
        m_SkinnedMeshRenderer.BakeMesh(m_Mesh);
        m_Baker.BakeSDF();
        m_Vfx.SetTexture("WalkingSDF", m_Baker.SdfTexture);
    }

    void OnDestroy()
    {
        if (m_Baker != null)
        {
            m_Baker.Dispose();
        }
    }
}
