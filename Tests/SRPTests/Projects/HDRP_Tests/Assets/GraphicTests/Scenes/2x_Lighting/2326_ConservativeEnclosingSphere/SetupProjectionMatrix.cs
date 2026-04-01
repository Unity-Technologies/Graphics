using UnityEngine;

public class SetupProjectionMatrix : MonoBehaviour
{
    [SerializeField] private Camera m_camera;
    [SerializeField] private Matrix4x4 m_projectionMatrix;

    private void Start()
    {
        m_camera.projectionMatrix = m_projectionMatrix;
    }
}
