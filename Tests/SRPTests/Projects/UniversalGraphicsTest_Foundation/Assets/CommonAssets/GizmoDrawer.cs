using UnityEngine;

public class GizmoDrawer : MonoBehaviour
{
    public Material GizmoMaterial;
    public Mesh m_GizmoMesh;

    void OnDrawGizmos()
    {
        if (m_GizmoMesh == null)
        {
            return;
        }


        for (int i = -2; i <= 2; i++)
        {
            for (int j = -2; j <= 2; j++)
            {
                Color c = new Color((i + 2.0f) / 5.0f, 0, (j + 2.0f) / 5.0f);
                GizmoMaterial.SetColor("_Color", c);
                GizmoMaterial.SetColor("_BaseColor", c);
                GizmoMaterial.SetPass(0);
                Graphics.DrawMeshNow(m_GizmoMesh, transform.position + new Vector3(i,j,0), Quaternion.identity);
            }
        }
    }
}
