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
            var presenter = GetPresenter<VFXSlotContainerPresenter>();

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
            var rm = PropertyRM.Create(setting, 100);
            if (rm != null)
            {
                m_SettingsContainer.AddChild(rm);
            }
            else
            {
                Debug.LogErrorFormat("Cannot create presenter for {0}", setting.name);
            }
        }
    }

    class VFXOperatorUI : VFXSlotContainerUI, IKeyFocusBlocker
    {
        public VFXOperatorUI()
        {
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            var presenter = GetPresenter<VFXOperatorPresenter>();
            if (presenter == null || presenter.Operator == null)
                return;
        }
    }
}
