using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveScript : MonoBehaviour
{
    Vector3 m_originPos;
    int m_frameCounter;
    float m_speed = -1f;
    
    void Start()
    {
        m_originPos = transform.position;
    }
    
    void Update()
    {
        transform.position += transform.right * m_speed;
        ++m_frameCounter;

        if (m_frameCounter > 4)
        {
            m_frameCounter = 0;
            m_speed *= -1;
        }
    }
}
