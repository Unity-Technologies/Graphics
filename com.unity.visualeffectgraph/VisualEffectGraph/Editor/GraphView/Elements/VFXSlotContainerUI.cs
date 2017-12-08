using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements.StyleEnums;
using System.Reflection;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    abstract class VFXSlotContainerUI : VFXNodeUI
    {
        public VisualElement m_SettingsContainer;
        private List<PropertyRM> m_Settings = new List<PropertyRM>();

        public bool collapse
        {
            get { return controller.model.collapsed; }

            set
            {
                if (controller.model.collapsed != value)
                {
                    controller.model.collapsed = value;
                }
            }
        }

        public VFXSlotContainerUI()
        {
        }

        protected override void SelfChange()
        {
            base.SelfChange();

            if (controller == null)
                return;

            if (m_SettingsContainer == null && controller.settings != null)
            {
                object settings = controller.settings;

                m_SettingsContainer = new VisualElement { name = "settings" };

                inputContainer.Insert(0, m_SettingsContainer); //between title and input

                foreach (var setting in controller.settings)
                {
                    AddSetting(setting);
                }
            }
            if (m_SettingsContainer != null)
            {
                var activeSettings = controller.model.GetSettings(false, VFXSettingAttribute.VisibleFlags.InGraph);
                for (int i = 0; i < m_Settings.Count; ++i)
                    m_Settings[i].RemoveFromHierarchy();

                for (int i = 0; i < m_Settings.Count; ++i)
                {
                    PropertyRM prop = m_Settings[i];
                    if (prop != null && activeSettings.Any(s => s.Name == controller.settings[i].name))
                    {
                        m_SettingsContainer.Add(prop);
                        prop.Update();
                    }
                }
            }

            if (controller.model.collapsed)
            {
                AddToClassList("collapsed");
            }
            else
            {
                RemoveFromClassList("collapsed");
            }
        }

        public virtual void GetPreferedWidths(ref float labelWidth, ref float controlWidth)
        {
            foreach (var setting in m_Settings)
            {
                float portLabelWidth = setting.GetPreferredLabelWidth();
                float portControlWidth = setting.GetPreferredControlWidth();

                if (labelWidth < portLabelWidth)
                {
                    labelWidth = portLabelWidth;
                }
                if (controlWidth < portControlWidth)
                {
                    controlWidth = portControlWidth;
                }
            }
        }

        public virtual void ApplyWidths(float labelWidth, float controlWidth)
        {
            foreach (var setting in m_Settings)
            {
                setting.SetLabelWidth(labelWidth);
            }
        }

        protected void AddSetting(VFXSettingPresenter setting)
        {
            var rm = PropertyRM.Create(setting, 100);
            if (rm != null)
            {
                m_Settings.Add(rm);
            }
            else
            {
                Debug.LogErrorFormat("Cannot create presenter for {0}", setting.name);
            }
        }
    }
}
