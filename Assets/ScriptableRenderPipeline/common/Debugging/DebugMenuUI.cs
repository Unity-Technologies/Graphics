using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering
{
    public class DebugMenuUI
    {

        public static GameObject CreateTextDebugElement(string elementName, string text, int size = 14, TextAnchor alignment = TextAnchor.MiddleLeft, GameObject parent = null)
        {
            GameObject goText = new GameObject(elementName);
            goText.transform.SetParent(parent.transform, false);
            goText.transform.transform.localPosition = Vector3.zero;
            goText.transform.transform.localScale = Vector3.one;
            UI.Text titleText = goText.AddComponent<UI.Text>();
            titleText.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
            titleText.text = text;
            titleText.alignment = alignment;
            titleText.fontSize = size;

            RectTransform rectTransform = goText.GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0.0f, 0.0f);
            rectTransform.localPosition = new Vector3(0.0f, 0.0f);
            rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            rectTransform.anchorMax = new Vector2(1.0f, 1.0f);

            return goText;
        }
    }
}
