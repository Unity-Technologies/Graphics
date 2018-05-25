using System.Collections;
using UnityEngine;

public class Boomerang : MonoBehaviour
{
    public Transform m_Target1;
    public Transform m_Target2;
    public float m_Speed = 2f;

    private Vector3 m_Direction;
    private Vector3[] m_Targets = new Vector3[2];
    private int m_TargetIndex = 0;

    void OnEnable()
    {
        m_Targets[0] = m_Target1.position;
        m_Targets[1] = m_Target2.position;
        transform.position = m_Targets[0];
        m_Direction = Vector3.Normalize(m_Targets[1] - m_Targets[0]);

        m_TargetIndex = 1;
    }

    void Update()
    {
        transform.position += m_Direction * m_Speed * Time.deltaTime;
        Vector3 targetPosVec = m_Targets[m_TargetIndex] - transform.position;
        if (Vector3.Dot(targetPosVec, m_Direction) < 0)
        {
            m_TargetIndex = (m_TargetIndex + 1) % 2;
            m_Direction = -m_Direction;
        }
    }
}
