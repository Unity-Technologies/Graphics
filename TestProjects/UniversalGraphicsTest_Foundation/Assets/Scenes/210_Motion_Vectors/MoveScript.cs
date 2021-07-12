using UnityEngine;

public class MoveScript : MonoBehaviour
{
    int m_FrameCounter;
    float m_Speed = -1f;

    void Update()
    {
        transform.position += transform.right * m_Speed;
        ++m_FrameCounter;

        if (m_FrameCounter > 4)
        {
            m_FrameCounter = 0;
            m_Speed *= -1;
        }
    }
}
