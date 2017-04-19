using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering
{
    public class DebugMenu
    {
        public string name { get { return m_Name; } }

        protected string m_Name = "Unknown Debug Menu";

        private GameObject m_Root = null;

        public GameObject BuildGUI(GameObject parent)
        {
            m_Root = new GameObject(string.Format("{0}", m_Name));
            m_Root.transform.SetParent(parent.transform);
            m_Root.transform.localPosition = Vector3.zero;
            m_Root.transform.localScale = Vector3.one;

            //UI.LayoutElement layoutElement = m_Root.AddComponent<UI.LayoutElement>();
            //layoutElement.ignoreLayout = true;

            UI.VerticalLayoutGroup menuVL = m_Root.AddComponent<UI.VerticalLayoutGroup>();
            menuVL.spacing = 5.0f;
            menuVL.childControlWidth = true;
            menuVL.childControlHeight = true;
            menuVL.childForceExpandWidth = true;
            menuVL.childForceExpandHeight = false;

            RectTransform menuVLRectTransform = m_Root.GetComponent<RectTransform>();
            menuVLRectTransform.pivot = new Vector2(0.0f, 0.0f);
            menuVLRectTransform.localPosition = new Vector3(0.0f, 0.0f);
            menuVLRectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            menuVLRectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            //menuVLRectTransform.anchoredPosition = new Vector2(kBorderSize, kBorderSize);
            //menuVLRectTransform.sizeDelta = new Vector2(-(kBorderSize * 2.0f), -(kBorderSize * 2.0f));


            //RectTransform rectTransform = m_Root.GetComponent<RectTransform>();
            //rectTransform.pivot = new Vector2(0.0f, 0.0f);
            //rectTransform.localPosition = new Vector3(0.0f, 0.0f);
            //rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            //rectTransform.anchorMax = new Vector2(1.0f, 1.0f);

            DebugMenuUI.CreateTextDebugElement(string.Format("{0} Title", m_Name), m_Name, 14, TextAnchor.MiddleLeft, m_Root);
            for (int i = 0; i < 12; ++i )
            {
                DebugMenuUI.CreateTextDebugElement(string.Format("{0} Blabla", i), string.Format("{0} Blabla", i), 10, TextAnchor.MiddleLeft, m_Root);
            }

            m_Root.SetActive(false);
            return m_Root;
        }
    }

    public class LightingDebugMenu
        : DebugMenu
    {
        public LightingDebugMenu()
        {
            m_Name = "Lighting";
        }
    }

    public class RenderingDebugMenu
        : DebugMenu
    {
        public RenderingDebugMenu()
        {
            m_Name = "Rendering";
        }
    }

    public class PwetteDebugMenu
        : DebugMenu
    {
        public PwetteDebugMenu()
        {
            m_Name = "Pwette";
        }
    }
}