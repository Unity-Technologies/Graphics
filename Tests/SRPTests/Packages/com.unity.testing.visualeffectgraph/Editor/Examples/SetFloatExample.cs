using UnityEngine;
using UnityEngine.VFX;

class SetFloatExample : MonoBehaviour
{
    [SerializeField] VisualEffect m_Vfx;
    [SerializeField] float m_Frequency = 1f;
    [SerializeField] float m_Phase = 0f;

    static readonly int k_MyValuePropertyNameID = Shader.PropertyToID("MyValueProperty");

    void Update()
    {
        m_Vfx.SetFloat(k_MyValuePropertyNameID, Mathf.Cos(Time.time * m_Frequency + m_Phase));
    }
}
