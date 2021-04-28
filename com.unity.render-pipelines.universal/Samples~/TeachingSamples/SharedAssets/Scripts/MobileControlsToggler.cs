using UnityEngine;

public class MobileControlsToggler : MonoBehaviour
{
    [SerializeField]
    private GameObject m_MovementControls;
    [SerializeField]
    private GameObject m_AimControls;

    void Start()
    {
        if (SystemInfo.deviceType == DeviceType.Handheld)
        {
            m_MovementControls.SetActive(true);
            m_AimControls.SetActive(true);
        }
    }
}
