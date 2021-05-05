using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveScript : MonoBehaviour
{
    Vector3 m_originPos;
    void Start()
    {
        m_originPos = transform.position;
    }
    
    void Update()
    {
        transform.position = m_originPos + transform.right * Mathf.Sin(Time.time * 10f);
    }
}
