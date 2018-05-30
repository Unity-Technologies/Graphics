using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateAround : MonoBehaviour
{
    public Transform m_RotationPivot;
    public float m_DegreePerSecond;

    public void Update()
    {
        float step = m_DegreePerSecond * Time.deltaTime;
        transform.RotateAround(m_RotationPivot.transform.position, Vector3.up, step);
    }
}
