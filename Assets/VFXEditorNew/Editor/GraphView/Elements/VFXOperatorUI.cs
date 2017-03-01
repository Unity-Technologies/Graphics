using System;
using System.Collections.Generic;
using System.Linq;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleEnums;
using UnityEngine.RMGUI.StyleSheets;

namespace UnityEditor.VFX.UI
{
    class VFXOperatorUI : Node
    {
        private Button m_Settings; //place holder for inner operator settings

        public void RandomizeSettings()
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

            presenter.Operator.position = presenter.position.position;
            presenter.Operator.collapsed = !presenter.expanded;
            presenter.Operator.settings = presenter.settings;

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

            presenter.Reset(); /*hacky : how the presenter is notified from its own changes ?*/
        }
    }
}