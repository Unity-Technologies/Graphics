using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class STP_Movement : MonoBehaviour
{
    public float DegreesPerFrame = 1.0f;
    public float XWeight = 1.0f;
    public float YWeight = 1.0f;
    public float ZWeight = 1.0f;
    public float Radius = 1.0f;
    public bool Rotate = false;

    private Vector3 m_originalPos;
    private int m_startFrame;

    // Start is called before the first frame update
    void Start()
    {
        m_originalPos = transform.position;
        m_startFrame = Time.frameCount;
    }

    // Update is called once per frame
    void Update()
    {
        int frameIndex = (Time.frameCount - m_startFrame) + 400;

        float angle = Mathf.Deg2Rad * (frameIndex * DegreesPerFrame);

        float x = Mathf.Sin(angle * XWeight);
        float y = Mathf.Cos(angle * YWeight);
        float z = Mathf.Cos(angle * ZWeight);

        transform.position = m_originalPos + new Vector3(x, y, z) * Radius;

        if (Rotate)
            transform.Rotate(2.0f, 2.0f, 0.0f);
    }
}
