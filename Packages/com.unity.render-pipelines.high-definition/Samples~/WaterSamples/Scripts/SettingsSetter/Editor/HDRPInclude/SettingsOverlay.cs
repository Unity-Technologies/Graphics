using UnityEditor.Overlays;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace UnityEditor.Rendering.HighDefinition
{
    [Overlay(typeof(SceneView), "HDRP Asset Settings Helper", true)]
    public class SettingsOverlay : Overlay, ITransientOverlay
    {
        public static SettingHelperSO settingsSO;

        private VisualElement rootElement;
        private List<SettingMessagePair> settingMessagePairs;

        public override VisualElement CreatePanelContent()
        {
            rootElement = new VisualElement() { name = "Settings Helper" };
            return rootElement;
        }

        public bool visible => IsVisible();

        private bool IsVisible()
        {
            if (settingsSO == null)
            {
                settingMessagePairs = null;
                return false;
            }
            if (settingMessagePairs == null)
                InitializeMessages();

            var needDisplay = false;
            
            foreach (var settingMessagePair in settingMessagePairs)
            {
                var need = settingMessagePair.requiredSetting.needsToBeEnabled;
                needDisplay |= need;

                if (need && !rootElement.Contains(settingMessagePair.visualElement))
                    rootElement.Add(settingMessagePair.visualElement);

                if (!need && rootElement.Contains(settingMessagePair.visualElement))
                    rootElement.Remove(settingMessagePair.visualElement);
            }

            return needDisplay;
        }

        VisualElement GetHeaderMessage()
        {
            var label = new Label() { text = settingsSO.header };
            label.style.maxWidth = new StyleLength(250);
            label.style.paddingBottom = new StyleLength(5);
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        void InitializeMessages()
        {
            if (rootElement == null)
                CreatePanelContent();
            rootElement.Clear();
            rootElement.Add(GetHeaderMessage());

            settingMessagePairs = new List<SettingMessagePair>();

            foreach (var requiredSetting in settingsSO.requiredSettings)
            {
                var newPair = new SettingMessagePair(requiredSetting);

                settingMessagePairs.Add(newPair);
                rootElement.Add(newPair.visualElement);
            }
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
