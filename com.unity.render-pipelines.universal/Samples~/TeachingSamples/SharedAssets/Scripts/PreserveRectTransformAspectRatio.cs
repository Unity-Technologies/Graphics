using UnityEngine;

[RequireComponent(typeof(RectTransform)), ExecuteInEditMode]
public class PreserveRectTransformAspectRatio : MonoBehaviour
{
    private RectTransform m_RectTransform;

    void Start()
    {
        m_RectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        float width = (m_RectTransform.anchorMax.x - m_RectTransform.anchorMin.x) * Screen.width;
        m_RectTransform.sizeDelta = new Vector2(0, width);
    }
}
