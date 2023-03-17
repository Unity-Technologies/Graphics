using UnityEditor.Overlays;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace UnityEditor.Rendering.HighDefinition
{
    [Overlay(typeof(SceneView), "HDRP Asset Settings Helper", true)]
    public class SettingsOverlay : Overlay
    {
        public static SettingsOverlay instance;

        private VisualElement rootElement;
        private List<SettingMessagePair> settingMessagePairs;

        public override VisualElement CreatePanelContent()
        {
            rootElement = new VisualElement() { name = "My Toolbar Root" };
            rootElement.Add(GetHeaderMessage());
            return rootElement;
        }

        public override void OnCreated()
        {
            base.OnCreated();
            instance = this;

            EditorApplication.update += EditorUpdate;
        }

        private void EditorUpdate()
        {
            if (settingHelperSO == null || settingMessagePairs == null)
                return;

            var needDisplay = false;
            
            foreach( var settingMessagePair in settingMessagePairs)
            {
                var need = settingMessagePair.requiredSetting.needsToBeEnabled;
                needDisplay |= need;

                if (need && !rootElement.Contains(settingMessagePair.visualElement))
                    rootElement.Add(settingMessagePair.visualElement);

                if (!need && rootElement.Contains(settingMessagePair.visualElement))
                    rootElement.Remove(settingMessagePair.visualElement);
            }

            displayed = needDisplay;
        }

        VisualElement GetHeaderMessage()
        {
            var label = new Label() { text = headerMessage };
            label.style.maxWidth = new StyleLength(250);
            label.style.paddingBottom = new StyleLength(5);
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }
           
        private string headerMessage = "";
        private SettingHelperSO m_settingHelperSO;
        public SettingHelperSO settingHelperSO
        {
            get { return m_settingHelperSO; }
            set
            {
                if (m_settingHelperSO != value)
                {
                    headerMessage = value.header;
                    
                    if (rootElement != null)
                        rootElement.Clear();
                    if (settingMessagePairs != null)
                        settingMessagePairs.Clear();
                    else
                        settingMessagePairs = new List<SettingMessagePair>();

                        
                    if (value != null)
                    {
                        if (rootElement == null)
                            CreatePanelContent();
                        rootElement.Add(GetHeaderMessage());

                        foreach( var requiredSetting in value.requiredSettings)
                        {
                            var newPair = new SettingMessagePair(requiredSetting);

                            settingMessagePairs.Add(newPair);
                            rootElement.Add(newPair.visualElement);
                        }
                    }
                }

                m_settingHelperSO = value;
            }
        }

        public SettingMessagePair AddSettingMessagePair(RequiredSetting _requiredSetting)
        {
            if (settingMessagePairs == null)
                settingMessagePairs = new List<SettingMessagePair>();

            var newSettingsMessagePair = new SettingMessagePair(_requiredSetting);
            settingMessagePairs.Add(newSettingsMessagePair);

            rootElement.Add(newSettingsMessagePair.visualElement);

            return newSettingsMessagePair;
        }

        public class SettingMessagePair
        {
            public RequiredSetting requiredSetting;
            public VisualElement visualElement;


            public SettingMessagePair(RequiredSetting _requiredSetting)
            {
                requiredSetting = _requiredSetting;

                visualElement = new VisualElement();
                // visualElement.Add(new Label() { text = requiredSetting.message });
                visualElement.Add(new Button(() =>
                {
                    requiredSetting.ShowSetting();
                }
                )
                { text = requiredSetting.message });
            }
        }
    }
}
