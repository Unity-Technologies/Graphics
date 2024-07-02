using UnityEngine;
using UnityEngine.VFX.SDF;

public class BakeAndAssignSDF : MonoBehaviour
{
    MeshToSDFBaker m_Baker;

    public Mesh m_Mesh;
    public Material sdfSlicerMaterial;
    public int maxResolution = 64;
    public Vector3 center;
    public Vector3 sizeBox;
    public int signPassCount = 1;
    public float threshold = 0.5f;

    void Start()
    {
        m_Baker = new MeshToSDFBaker(sizeBox, center, maxResolution, m_Mesh, signPassCount, threshold);
        m_Baker.BakeSDF();
        sdfSlicerMaterial.SetTexture("_SDF", m_Baker.SdfTexture);
        sdfSlicerMaterial.SetFloat("_Layout", GetLayoutSize());
        sdfSlicerMaterial.SetFloat("_DimensionZ", m_Baker.GetGridSize().z);
    }

    // Update is called once per frame
    void Update()
    {
        m_Baker.Reinit(sizeBox, center, maxResolution, m_Mesh, signPassCount, threshold);
        m_Baker.BakeSDF();
        sdfSlicerMaterial.SetTexture("_SDF", m_Baker.SdfTexture);
        sdfSlicerMaterial.SetFloat("_Layout", GetLayoutSize());
        sdfSlicerMaterial.SetFloat("_DimensionZ", m_Baker.GetGridSize().z);
    }

    int GetLayoutSize()
    {
        int z = m_Baker.GetGridSize().z;
        return Mathf.CeilToInt(Mathf.Sqrt(z));
    }

    void OnDestroy()
    {
        if (m_Baker != null)
        {
            m_Baker.Dispose();
        }
    }
}
