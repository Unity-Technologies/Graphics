using UnityEngine;

public class SineMover : MonoBehaviour
{
    [SerializeField]
    private Vector3 m_Amplitude;
    [SerializeField]
    private float m_Period;

    private Vector3 m_StartPosition;

    void Start()
    {
        m_StartPosition = transform.position;
    }

    void Update()
    {
        transform.position = m_StartPosition + m_Amplitude * Mathf.Sin(Time.time * Mathf.PI * (2 / m_Period));
    }
}
