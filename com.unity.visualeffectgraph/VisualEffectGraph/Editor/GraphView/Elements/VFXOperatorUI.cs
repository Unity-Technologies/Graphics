using System;
using System.Collections.Generic;
using System.Linq;
using UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.VFX.UI
{
    class VFXSlotContainerUI : VFXNodeUI
    {
        public VisualContainer m_SettingsContainer;

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            var presenter = GetPresenter<VFXContextSlotContainerPresenter>();

            if (presenter == null)
                return;

            if (m_SettingsContainer == null && presenter.settings != null)
            {
                object settings = presenter.settings;

                m_SettingsContainer = new VisualContainer { name = "settings" };

                leftContainer.InsertChild(1, m_SettingsContainer); //between title and input

                foreach (var setting in presenter.settings)
                {
                    AddSetting(setting);
                }
            }
            if (m_SettingsContainer != null)
            {
                for (int i = 0; i < m_SettingsContainer.childrenCount; ++i)
                {
                    PropertyRM prop = m_SettingsContainer.GetChildAt(i) as PropertyRM;
                    if (prop != null)
                        prop.Update();
                }
            }
        }

        protected void AddSetting(VFXSettingPresenter setting)
        {
            m_SettingsContainer.AddChild(PropertyRM.Create(setting, 100));
        }
    }

    class VFXOperatorUI : VFXSlotContainerUI, IKeyFocusBlocker
    {
        private Button m_Settings; //place holder for inner operator settings

        private void RandomizeSettings()
        {
            var presenter = GetPresenter<VFXOperatorPresenter>();
            if (presenter.settings != null)
            {
                var type = presenter.settings.GetType();
                var newSettings = System.Activator.CreateInstance(type);
                foreach (var field in type.GetFields())
                {
                    if (field.FieldType == typeof(string))
                    {
                        string rand = "";
                        var size = UnityEngine.Random.Range(1, 5);
                        for (int i = 0; i < size; ++i)
                        {
                            var channel = new[] { 'x', 'y', 'z', 'w' };
                            rand += channel[UnityEngine.Random.Range(0, 4)];
                        }
                        field.SetValue(newSettings, rand);
                    }
                }
                presenter.settings = newSettings;
            }
        }

        public VFXOperatorUI()
        {
            m_Settings = new Button(RandomizeSettings);
            inputContainer.AddChild(m_Settings);
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            var presenter = GetPresenter<VFXOperatorPresenter>();
            if (presenter == null || presenter.Operator == null)
                return;

            if (presenter.settings == null)
            {
                m_Settings.visible = false;
                m_Settings.height = 0.0f;
            }
            else
            {
                m_Settings.visible = true;
                m_Settings.height = 24.0f;
                var field = presenter.settings.GetType().GetFields().FirstOrDefault(o => o.FieldType == typeof(string));
                m_Settings.text = field == null ? "" : (string)field.GetValue(presenter.settings);
            }
        }
    }
}
