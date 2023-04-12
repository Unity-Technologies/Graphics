using UnityEngine;

public class MotionVectorMoveAndStill : MonoBehaviour
{
    void OnEnable()
    {
    }

    public float m_Wait = 1.0f;
    void Update()
    {
        m_Wait -= Time.deltaTime;
        if (m_Wait < 0)
        {
            m_Wait = float.MaxValue;
            var currentTransform = GetComponent<Transform>();
            var position = currentTransform.position;
            position.x -= 1.0f;
            currentTransform.position = position;
        }
    }

    void OnDisable()
    {
    }
}
