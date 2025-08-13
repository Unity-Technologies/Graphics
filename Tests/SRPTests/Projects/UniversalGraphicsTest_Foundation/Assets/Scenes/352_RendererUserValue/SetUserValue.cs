using UnityEngine;

public class SetUserValue : MonoBehaviour
{
    public Color32 userValue;

    private Renderer[] m_Renderers;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        uint cc = ((uint)userValue.a << 24) | ((uint)userValue.b << 16) | ((uint)userValue.g << 8) | ((uint)userValue.r << 0);

        m_Renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in m_Renderers)
        {
            MeshRenderer meshRenderer = renderer as MeshRenderer;
            SkinnedMeshRenderer skinnedMeshRenderer = renderer as SkinnedMeshRenderer;
            if (meshRenderer)
            {
                meshRenderer.SetShaderUserValue(cc);
            }
            else if (skinnedMeshRenderer)
            {
                skinnedMeshRenderer.SetShaderUserValue(cc);
            }
        }
    }

}
