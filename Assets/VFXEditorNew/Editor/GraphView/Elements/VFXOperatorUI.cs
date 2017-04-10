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
    class VFXOperatorUI : Node
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
        public override NodeAnchor InstantiateNodeAnchor(NodeAnchorPresenter presenter)
        {
            return VFXDataAnchor.Create<VFXDataEdgePresenter>(presenter as VFXDataAnchorPresenter);
        }

        public VFXOperatorUI()
        {
            m_Settings = new Button(RandomizeSettings);
            inputContainer.AddChild(m_Settings);
            clipChildren = false;
            inputContainer.clipChildren = false;
            mainContainer.clipChildren = false;
            leftContainer.clipChildren =false;
            rightContainer.clipChildren = false;
            outputContainer.clipChildren = false;
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            var presenter = GetPresenter<VFXOperatorPresenter>();
            if (presenter == null || presenter.node == null)
                return;

            presenter.node.position = presenter.position.position;
            presenter.node.collapsed = !presenter.expanded;
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
        }
    }
}