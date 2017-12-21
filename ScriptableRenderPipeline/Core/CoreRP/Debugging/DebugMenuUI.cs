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

        bool                m_Enabled = false;
        int                 m_ActivePanelIndex = 0;
        GameObject          m_Root = null;
        GameObject          m_MainPanelLayout = null;
        GameObject          m_PersistentPanelLayout = null;
        List<DebugPanelUI>  m_DebugPanelUIs = new List<DebugPanelUI>();
        DebugPanelUI        m_PersistentDebugPanelUI = null;
        GameObject          m_MainMenuRoot = null;
        GameObject          m_PersistentPanelRoot = null;
        DebugMenuManager    m_DebugMenuManager = null;

        public int  panelCount          { get { return m_DebugPanelUIs.Count; } }
        public bool isEnabled           { get { return m_Enabled; } }
        public int  activePanelIndex    { get { return m_ActivePanelIndex; } set { m_ActivePanelIndex = value; } }

        static bool         s_UIChanged = false;
        static public bool  changed     { get { return s_UIChanged; } set { s_UIChanged = value; } }

        public DebugMenuUI(DebugMenuManager manager)
        {
            m_DebugMenuManager = manager;
        }

        // HACK: for some reason, the layout of the selected menu may fail if the previous menu in the list is disabled
        // Disabling and re-enabling everything seems to fix the issue...
        private void HackSelectPanel()
        {
            m_MainMenuRoot.SetActive(false);
            m_MainMenuRoot.SetActive(true);
        }

        public void EnablePersistentView(bool value)
        {
            m_PersistentPanelRoot.SetActive(value);
        }

        public void PreviousDebugPanel()
        {
            m_DebugPanelUIs[m_ActivePanelIndex].SetSelected(false);
            m_ActivePanelIndex = m_ActivePanelIndex - 1;
            if (m_ActivePanelIndex == -1)
                m_ActivePanelIndex = m_DebugPanelUIs.Count - 1;

            m_DebugPanelUIs[m_ActivePanelIndex].SetSelected(true);

            HackSelectPanel();
        }

        public void NextDebugPanel()
        {
            m_DebugPanelUIs[m_ActivePanelIndex].SetSelected(false);
            m_ActivePanelIndex = (m_ActivePanelIndex + 1) % m_DebugPanelUIs.Count;
            m_DebugPanelUIs[m_ActivePanelIndex].SetSelected(true);

            HackSelectPanel();
        }

        public void ToggleMenu()
        {
            m_Enabled = !m_Enabled;
            if(m_Enabled)
            {
                BuildGUI();
                m_MainMenuRoot.SetActive(true);
                m_DebugPanelUIs[m_ActivePanelIndex].SetSelected(m_Enabled);
            }
            else
            {
                m_MainMenuRoot.SetActive(false);
            }
        }

        public void OnValidate()
        {
            m_DebugPanelUIs[m_ActivePanelIndex].OnValidate();
        }

        public void OnMoveHorizontal(float value)
        {
            m_DebugPanelUIs[m_ActivePanelIndex].OnMoveHorizontal(value);
        }

        public void OnMoveVertical(float value)
        {
            m_DebugPanelUIs[m_ActivePanelIndex].OnMoveVertical(value);
        }

        public void Update()
        {
            if(m_PersistentDebugPanelUI != null)
                m_PersistentDebugPanelUI.Update();

            if (!m_Enabled)
                return;

            if (m_ActivePanelIndex != -1)
                m_DebugPanelUIs[m_ActivePanelIndex].Update();
        }

        public void OnMakePersistent()
        {
            DebugPanel persistentPanel = DebugMenuManager.instance.GetPersistentDebugPanel();

            DebugItem selectedItem = m_DebugPanelUIs[m_ActivePanelIndex].GetSelectedDebugItem();
            if (selectedItem != null && selectedItem.readOnly)
            {
                if (persistentPanel.HasDebugItem(selectedItem))
                {
                    persistentPanel.RemoveDebugItem(selectedItem);
                }
                else
                {
                    persistentPanel.AddDebugItem(selectedItem);
                }
            }

            if (m_PersistentDebugPanelUI.itemCount == 0)
            {
                m_PersistentDebugPanelUI.SetSelected(false);
                EnablePersistentView(false); // Temp, should just need the above. Wait for background UI to be moved to menu itself
            }
            else
            {
                m_PersistentDebugPanelUI.SetSelected(true);
                m_PersistentDebugPanelUI.ResetSelectedItem();
                EnablePersistentView(true);
                HackSelectPanel();
            }
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

            m_MainPanelLayout = DebugMenuUI.CreateVerticalLayoutGroup("DebugMenu VLayout", true, true, true, false, 5.0f, m_MainMenuRoot);
            RectTransform menuVLRectTransform = m_MainPanelLayout.GetComponent<RectTransform>();
            menuVLRectTransform.pivot = new Vector2(0.0f, 0.0f);
            menuVLRectTransform.localPosition = Vector3.zero;
            menuVLRectTransform.localScale = Vector3.one;
            menuVLRectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            menuVLRectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            menuVLRectTransform.anchoredPosition = new Vector2(kBorderSize, kBorderSize);
            menuVLRectTransform.sizeDelta = new Vector2(-(kBorderSize * 2.0f), -(kBorderSize * 2.0f));

            // TODO: Move background an layout to the menu itself.
            m_PersistentPanelRoot = new GameObject("Background_Persistent");
            m_PersistentPanelRoot.AddComponent<CanvasRenderer>();
            image = m_PersistentPanelRoot.AddComponent<UI.Image>();
            m_PersistentPanelRoot.transform.SetParent(m_Root.transform, false);
            image.rectTransform.pivot = new Vector2(0.0f, 0.0f);
            image.rectTransform.localPosition = Vector3.zero;
            image.rectTransform.localScale = Vector3.one;
            image.rectTransform.anchorMin = new Vector2(0.7f, 0.8f);
            image.rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            image.rectTransform.anchoredPosition = new Vector2(kBorderSize, kBorderSize);
            image.rectTransform.sizeDelta = new Vector2(-(kBorderSize * 2.0f), -(kBorderSize * 2.0f));
            image.color = kBackgroundColor;

            m_PersistentPanelLayout = DebugMenuUI.CreateVerticalLayoutGroup("DebugMenu VLayout", true, true, true, false, 5.0f, m_PersistentPanelRoot);
            menuVLRectTransform = m_PersistentPanelLayout.GetComponent<RectTransform>();
            menuVLRectTransform.pivot = new Vector2(0.0f, 0.0f);
            menuVLRectTransform.localPosition = Vector3.zero;
            menuVLRectTransform.localScale = Vector3.one;
            menuVLRectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            menuVLRectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            menuVLRectTransform.anchoredPosition = new Vector2(kBorderSize, kBorderSize);
            menuVLRectTransform.sizeDelta = new Vector2(-(kBorderSize * 2.0f), -(kBorderSize * 2.0f));

            m_PersistentPanelRoot.SetActive(false);

            DebugMenuUI.CreateTextElement("DebugMenuTitle", "Debug Window", 14, TextAnchor.MiddleCenter, m_MainPanelLayout);

            m_DebugMenuManager.GetPersistentDebugPanel().panelUI.BuildGUI(m_PersistentPanelLayout);
            m_PersistentDebugPanelUI = m_DebugMenuManager.GetPersistentDebugPanel().panelUI;

            for (int i = 0; i < m_DebugMenuManager.panelCount; ++i)
            {
                m_DebugMenuManager.GetDebugPanel(i).panelUI.BuildGUI(m_MainPanelLayout);
                m_DebugPanelUIs[i].SetSelected(false);
            }
        }

        public void AddDebugPanel(DebugPanel panel)
        {
            m_DebugPanelUIs.Add(panel.panelUI);
        }

#if UNITY_EDITOR
        public void OnEditorGUI()
        {
            s_UIChanged = false;
            if(!m_DebugPanelUIs[m_ActivePanelIndex].empty)
                m_DebugPanelUIs[m_ActivePanelIndex].OnEditorGUI();
            if(s_UIChanged)
            {
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }
#endif

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
