using UnityEngine;

public class Rotator : MonoBehaviour
{
    [SerializeField]
    private Space m_Space;

    [SerializeField]
    private Vector3 m_Axis;

    [SerializeField]
    private float m_AngularVelocity;

    void Update()
    {
        transform.Rotate(m_Axis, m_AngularVelocity * Time.deltaTime, m_Space);
    }
}
