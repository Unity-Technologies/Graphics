using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering
{
    [ExecuteInEditMode]
    public class DebugMenuManager : MonoBehaviour
    {
        bool            m_Enabled = false;
        int             m_ActiveMenuIndex = 0;
        List<DebugMenu> m_DebugMenus = new List<DebugMenu>();

        // UI
        GameObject      m_Root = null;
        GameObject[]    m_MenuRoots = null;

        public bool isEnabled       { get { return m_Enabled; } }
        public int activeMenuIndex  { get { return m_ActiveMenuIndex; } set { m_ActiveMenuIndex = value; } }
        public int  menuCount       { get { return m_DebugMenus.Count; } }

        public DebugMenu GetDebugMenu(int index)
        {
            if (index < m_DebugMenus.Count)
                return m_DebugMenus[index];
            else
                return null;
        }

        void LookUpDebugMenuClasses()
        {
            var types = Assembly.GetAssembly(typeof(DebugMenu)).GetTypes()
                .Where(t => t.IsSubclassOf(typeof(DebugMenu)));

            m_DebugMenus.Clear();

            foreach (var type in types)
            {
                m_DebugMenus.Add((DebugMenu)Activator.CreateInstance(type));
            }
        }

        void OnEnable()
        {
            LookUpDebugMenuClasses();
        }

        void Update()
        {
            DebugActionManager.instance.Update();

            if(DebugActionManager.instance.GetAction(DebugActionManager.DebugAction.EnableDebugMenu))
            {
                ToggleMenu();
            }

            if(DebugActionManager.instance.GetAction(DebugActionManager.DebugAction.PreviousDebugMenu))
            {
                PreviousDebugMenu();
            }
            if (DebugActionManager.instance.GetAction(DebugActionManager.DebugAction.NextDebugMenu))
            {
                NextDebugMenu();
            }
        }

        void PreviousDebugMenu()
        {
            m_MenuRoots[m_ActiveMenuIndex].SetActive(false);
            m_ActiveMenuIndex = m_ActiveMenuIndex - 1;
            if (m_ActiveMenuIndex == -1)
                m_ActiveMenuIndex = m_DebugMenus.Count - 1;
            m_MenuRoots[m_ActiveMenuIndex].SetActive(true);
        }

        void NextDebugMenu()
        {
            m_MenuRoots[m_ActiveMenuIndex].SetActive(false);
            m_ActiveMenuIndex = (m_ActiveMenuIndex + 1) % m_DebugMenus.Count;
            m_MenuRoots[m_ActiveMenuIndex].SetActive(true);
        }

        void ToggleMenu()
        {
            m_Enabled = !m_Enabled;
            if(!m_Enabled)
            {
                CleanUpGUI();
            }
            else
            {
                BuildGUI();
            }
        }

        void CleanUpGUI()
        {
            Object.Destroy(m_Root);
        }

        void BuildGUI()
        {
            float kBorderSize = 5.0f;
            m_Root = new GameObject("DebugMenu Root");
            Canvas canvas = m_Root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            UI.CanvasScaler canvasScaler = m_Root.AddComponent<UI.CanvasScaler>();
            canvasScaler.uiScaleMode = UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(800.0f, 600.0f);

            RectTransform canvasRT = canvas.GetComponent<RectTransform>();
            //canvasRT.localScale = Vector3.one;
            float width = canvasRT.rect.width;
            float height = canvasRT.rect.height;

            GameObject go = new GameObject("Background");
            go.AddComponent<CanvasRenderer>();
            var image = go.AddComponent<UI.Image>();
            go.transform.SetParent(m_Root.transform, false);
            image.rectTransform.pivot = new Vector2(0.0f, 0.0f);
            image.rectTransform.localPosition = Vector3.zero;
            image.rectTransform.localScale = Vector3.one;
            image.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            image.rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            image.rectTransform.anchoredPosition = new Vector2(kBorderSize, kBorderSize);
            image.rectTransform.sizeDelta = new Vector2(-(kBorderSize * 2.0f), -(kBorderSize * 2.0f));
            image.color = new Color(0.5f, 0.5f, 0.5f, 0.4f);

            GameObject  goVL = new GameObject("DebugMenu VLayout");
            goVL.transform.SetParent(go.transform, false);
            UI.VerticalLayoutGroup menuVL = goVL.AddComponent<UI.VerticalLayoutGroup>();
            RectTransform menuVLRectTransform = goVL.GetComponent<RectTransform>();
            menuVLRectTransform.pivot = new Vector2(0.0f, 0.0f);
            menuVLRectTransform.localPosition = Vector3.zero;
            menuVLRectTransform.localScale = Vector3.one;
            menuVLRectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            menuVLRectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            menuVLRectTransform.anchoredPosition = new Vector2(kBorderSize, kBorderSize);
            menuVLRectTransform.sizeDelta = new Vector2(-(kBorderSize * 2.0f), -(kBorderSize * 2.0f));
            menuVL.spacing = 5.0f;
            menuVL.childControlWidth = true;
            menuVL.childControlHeight = true;
            menuVL.childForceExpandWidth = true;
            menuVL.childForceExpandHeight = false;

            DebugMenuUI.CreateTextDebugElement("DebugMenuTitle", "Debug Menu", 14, TextAnchor.MiddleCenter, goVL);

            int menuCount = m_DebugMenus.Count;
            m_MenuRoots = new GameObject[menuCount];
            for (int i = 0; i < menuCount; ++i)
            {
                m_MenuRoots[i] = m_DebugMenus[i].BuildGUI(goVL);

                //m_MenuRoots[i] = new GameObject(string.Format("{0}", m_DebugMenus[i].name));
                //m_MenuRoots[i].transform.parent = m_Root.transform;
                //m_MenuRoots[i].transform.localPosition = Vector3.zero;

                //GameObject title = new GameObject(string.Format("{0} Title", m_DebugMenus[i].name));
                //title.transform.parent = m_MenuRoots[i].transform;
                //title.transform.transform.localPosition = Vector3.zero;
                //UI.Text titleText = title.AddComponent<UI.Text>();
                //titleText.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
                //titleText.text = m_DebugMenus[i].name;
            }

            m_MenuRoots[activeMenuIndex].SetActive(true);
        }
    }

}
