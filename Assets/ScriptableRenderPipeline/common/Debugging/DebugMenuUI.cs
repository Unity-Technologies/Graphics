using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering
{
    public class DebugMenuUI
    {
        public static Color kColorSelected = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        public static Color kColorUnSelected = new Color(0.25f, 0.25f, 0.25f, 1.0f);
        public static Color kBackgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);
        public static float kDebugItemNameWidth = 150.0f;

        GameObject          m_Root = null;
        GameObject[]        m_MenuRoots = null;
        GameObject          m_MainMenuRoot = null;
        GameObject          m_PersistentMenuRoot = null;
        bool                m_Enabled = false;
        DebugMenuManager    m_DebugMenuManager = null;

        public DebugMenuUI(DebugMenuManager manager)
        {
            m_DebugMenuManager = manager;
        }

        public void Toggle()
        {
            m_Enabled = !m_Enabled;
            m_MainMenuRoot.SetActive(m_Enabled);
        }

        public void EnablePersistentView(bool value)
        {
            m_PersistentMenuRoot.SetActive(value);
        }

        void CleanUpGUI()
        {
            Object.Destroy(m_Root);
        }

        public void BuildGUI()
        {
            if (m_Root != null)
                return;

            float kBorderSize = 5.0f;
            m_Root = new GameObject("DebugMenu Root");
            Canvas canvas = m_Root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            UI.CanvasScaler canvasScaler = m_Root.AddComponent<UI.CanvasScaler>();
            canvasScaler.uiScaleMode = UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(800.0f, 600.0f);

            // TODO: Move background an layout to the menu itself.
            m_MainMenuRoot = new GameObject("Background");
            m_MainMenuRoot.AddComponent<CanvasRenderer>();
            var image = m_MainMenuRoot.AddComponent<UI.Image>();
            m_MainMenuRoot.transform.SetParent(m_Root.transform, false);
            image.rectTransform.pivot = new Vector2(0.0f, 0.0f);
            image.rectTransform.localPosition = Vector3.zero;
            image.rectTransform.localScale = Vector3.one;
            image.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            image.rectTransform.anchorMax = new Vector2(0.5f, 1.0f);
            image.rectTransform.anchoredPosition = new Vector2(kBorderSize, kBorderSize);
            image.rectTransform.sizeDelta = new Vector2(-(kBorderSize * 2.0f), -(kBorderSize * 2.0f));
            image.color = kBackgroundColor;

            GameObject goVL = DebugMenuUI.CreateVerticalLayoutGroup("DebugMenu VLayout", true, true, true, false, 5.0f, m_MainMenuRoot);
            RectTransform menuVLRectTransform = goVL.GetComponent<RectTransform>();
            menuVLRectTransform.pivot = new Vector2(0.0f, 0.0f);
            menuVLRectTransform.localPosition = Vector3.zero;
            menuVLRectTransform.localScale = Vector3.one;
            menuVLRectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            menuVLRectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            menuVLRectTransform.anchoredPosition = new Vector2(kBorderSize, kBorderSize);
            menuVLRectTransform.sizeDelta = new Vector2(-(kBorderSize * 2.0f), -(kBorderSize * 2.0f));

            // TODO: Move background an layout to the menu itself.
            m_PersistentMenuRoot = new GameObject("Background_Persistent");
            m_PersistentMenuRoot.AddComponent<CanvasRenderer>();
            image = m_PersistentMenuRoot.AddComponent<UI.Image>();
            m_PersistentMenuRoot.transform.SetParent(m_Root.transform, false);
            image.rectTransform.pivot = new Vector2(0.0f, 0.0f);
            image.rectTransform.localPosition = Vector3.zero;
            image.rectTransform.localScale = Vector3.one;
            image.rectTransform.anchorMin = new Vector2(0.7f, 0.8f);
            image.rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            image.rectTransform.anchoredPosition = new Vector2(kBorderSize, kBorderSize);
            image.rectTransform.sizeDelta = new Vector2(-(kBorderSize * 2.0f), -(kBorderSize * 2.0f));
            image.color = kBackgroundColor;

            GameObject goVL2 = DebugMenuUI.CreateVerticalLayoutGroup("DebugMenu VLayout", true, true, true, false, 5.0f, m_PersistentMenuRoot);
            menuVLRectTransform = goVL2.GetComponent<RectTransform>();
            menuVLRectTransform.pivot = new Vector2(0.0f, 0.0f);
            menuVLRectTransform.localPosition = Vector3.zero;
            menuVLRectTransform.localScale = Vector3.one;
            menuVLRectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            menuVLRectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            menuVLRectTransform.anchoredPosition = new Vector2(kBorderSize, kBorderSize);
            menuVLRectTransform.sizeDelta = new Vector2(-(kBorderSize * 2.0f), -(kBorderSize * 2.0f));

            m_PersistentMenuRoot.SetActive(false);

            DebugMenuUI.CreateTextElement("DebugMenuTitle", "Debug Menu", 14, TextAnchor.MiddleCenter, goVL);

            int menuCount = m_DebugMenuManager.menuCount;
            m_MenuRoots = new GameObject[menuCount];
            for (int i = 0; i < menuCount; ++i)
            {
                m_MenuRoots[i] = m_DebugMenuManager.GetDebugMenu(i).BuildGUI(goVL);
            }

            m_DebugMenuManager.GetPersistentDebugMenu().BuildGUI(goVL2);
        }

        public static GameObject CreateVerticalLayoutGroup(string name, bool controlWidth, bool controlHeight, bool forceExpandWidth, bool forceExpandHeight, GameObject parent = null )
        {
            return CreateVerticalLayoutGroup(name, controlWidth, controlHeight, forceExpandWidth, forceExpandHeight, 0.0f, parent);
        }

        public static GameObject CreateVerticalLayoutGroup(string name, bool controlWidth, bool controlHeight, bool forceExpandWidth, bool forceExpandHeight, float spacing, GameObject parent = null )
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            UI.VerticalLayoutGroup verticalLayout = go.AddComponent<UI.VerticalLayoutGroup>();
            verticalLayout.childControlHeight = controlHeight;
            verticalLayout.childControlWidth = controlWidth;
            verticalLayout.childForceExpandHeight = forceExpandHeight;
            verticalLayout.childForceExpandWidth = forceExpandWidth;
            verticalLayout.spacing = spacing;

            return go;
        }

        public static GameObject CreateHorizontalLayoutGroup(string name, bool controlWidth, bool controlHeight, bool forceExpandWidth, bool forceExpandHeight, GameObject parent = null)
        {
            return CreateHorizontalLayoutGroup(name, controlWidth, controlHeight, forceExpandWidth, forceExpandHeight, 0.0f, parent);
        }

        public static GameObject CreateHorizontalLayoutGroup(string name, bool controlWidth, bool controlHeight, bool forceExpandWidth, bool forceExpandHeight, float spacing = 1.0f, GameObject parent = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            UI.HorizontalLayoutGroup horizontalLayout = go.AddComponent<UI.HorizontalLayoutGroup>();
            horizontalLayout.childControlHeight = controlHeight;
            horizontalLayout.childControlWidth = controlWidth;
            horizontalLayout.childForceExpandHeight = forceExpandHeight;
            horizontalLayout.childForceExpandWidth = forceExpandWidth;
            horizontalLayout.spacing = spacing;

            return go;
        }

        public static GameObject CreateTextElement(string elementName, string text, int size = 14, TextAnchor alignment = TextAnchor.MiddleLeft, GameObject parent = null)
        {
            GameObject goText = new GameObject(elementName);
            goText.transform.SetParent(parent.transform, false);
            goText.transform.transform.localPosition = Vector3.zero;
            goText.transform.transform.localScale = Vector3.one;
            UI.Text textComponent = goText.AddComponent<UI.Text>();
            textComponent.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
            textComponent.text = text;
            textComponent.alignment = alignment;
            textComponent.fontSize = size;
            textComponent.verticalOverflow = VerticalWrapMode.Overflow;
            textComponent.color = DebugMenuUI.kColorUnSelected;

            RectTransform rectTransform = goText.GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0.0f, 0.0f);
            rectTransform.localPosition = new Vector3(0.0f, 0.0f);
            rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            rectTransform.anchorMax = new Vector2(1.0f, 1.0f);

            return goText;
        }
    }
}
