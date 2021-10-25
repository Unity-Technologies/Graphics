using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ScreenSpacePlacement : MonoBehaviour
{
    [SerializeField]
    private Camera m_Cam;
    [SerializeField]
    private Transform m_FlareObject;

    void OnGUI()
    {
        Event currentEvent = Event.current;
        Vector2 mousePos = new Vector2();

        mousePos.x = currentEvent.mousePosition.x;
        mousePos.y = m_Cam.pixelHeight - currentEvent.mousePosition.y;

        if (m_FlareObject != null && mousePos.x > 0 && mousePos.y > 0 && mousePos.x < m_Cam.pixelWidth && mousePos.y < m_Cam.pixelHeight)
        {
            Vector3 point = m_Cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, m_Cam.nearClipPlane));

            m_FlareObject.position = point;
        }
    }
}
