using UnityEngine;

public class PortraitWarningToggler : MonoBehaviour
{
    [SerializeField]
    private GameObject m_WarningPanel;

    void Update()
    {
        if (SystemInfo.deviceType == DeviceType.Handheld && Screen.orientation == ScreenOrientation.Portrait)
        {
            m_WarningPanel.SetActive(true);
        }
        else
        {
            m_WarningPanel.SetActive(false);
        }
    }
}
