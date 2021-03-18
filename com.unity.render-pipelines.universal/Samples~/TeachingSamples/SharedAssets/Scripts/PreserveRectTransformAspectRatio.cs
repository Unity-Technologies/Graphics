using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform)), ExecuteInEditMode]
public class PreserveRectTransformAspectRatio : MonoBehaviour
{
    private RectTransform rect;
    // Start is called before the first frame update
    void Start()
    {
        rect = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        float width = (rect.anchorMax.x - rect.anchorMin.x) * Screen.width;
        rect.sizeDelta = new Vector2(0, width);
    }
}
