using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.VFX.UI
{
    class VFXNodeUI : Node, IControlledElement<VFXSlotContainerPresenter>
    {
        VFXSlotContainerPresenter m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public override void UpdatePresenterPosition()
        {
            controller.position = GetPosition().position;
        }

        public VFXSlotContainerPresenter controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != null)
                {
                    m_Controller.UnregisterHandler(this);
                }
                m_Controller = value;
                if (m_Controller != null)
                {
                    m_Controller.RegisterHandler(this);
                }
            }
        }

        protected VisualElement m_SettingsContainer;
        private List<PropertyRM> m_Settings = new List<PropertyRM>();
        public VFXNodeUI()
        {
            AddToClassList("VFXNodeUI");
            RegisterCallback<ControllerChangedEvent>(OnChange);
        }

        virtual protected void OnChange(ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                SelfChange();
            }
        }

        protected virtual bool HasPosition()
        {
            return true;
        }

        protected virtual void SelfChange()
        {
            var presenter = controller;

            if (presenter == null)
                return;

            if (HasPosition())
            {
                style.positionType = PositionType.Absolute;
                style.positionLeft = presenter.position.x;
                style.positionTop = presenter.position.y;
            }


            if (m_SettingsContainer == null && presenter.settings != null)
            {
                object settings = presenter.settings;

                m_SettingsContainer = new VisualElement { name = "settings" };

                inputContainer.Insert(0, m_SettingsContainer); //between title and input

                foreach (var setting in presenter.settings)
                {
                    AddSetting(setting);
                }
            }
            if (m_SettingsContainer != null)
            {
                var activeSettings = presenter.model.GetSettings(false, VFXSettingAttribute.VisibleFlags.InGraph);
                for (int i = 0; i < m_Settings.Count; ++i)
                    m_Settings[i].RemoveFromHierarchy();

                for (int i = 0; i < m_Settings.Count; ++i)
                {
                    PropertyRM prop = m_Settings[i];
                    if (prop != null && activeSettings.Any(s => s.Name == presenter.settings[i].name))
                    {
                        m_SettingsContainer.Add(prop);
                        prop.Update();
                    }
                }
            }

            GraphView graphView = this.GetFirstAncestorOfType<GraphView>();
            if (graphView != null)
            {
                var allEdges = graphView.Query<Edge>().ToList();

                foreach (Port anchor in this.Query<Port>().Where(t => true).ToList())
                {
                    foreach (var edge in allEdges.Where(t =>
                        {
                            var pres = t.GetPresenter<EdgePresenter>();
                            return pres != null && (pres.output == anchor.presenter || pres.input == anchor.presenter);
                        }))
                    {
                        edge.OnDataChanged();
                    }
                }
            }
        }

        public virtual VFXDataAnchor InstantiateDataAnchor(VFXDataAnchorPresenter presenter)
        {
            if (presenter.direction == Direction.Input)
            {
                VFXEditableDataAnchor anchor = VFXEditableDataAnchor.Create(presenter);
                presenter.sourceNode.viewPresenter.onRecompileEvent += anchor.OnRecompile;

                return anchor;
            }
            else
            {
                return VFXOutputDataAnchor.Create(presenter);
            }
        }

        protected override void OnPortRemoved(Port anchor)
        {
            if (anchor is VFXEditableDataAnchor)
            {
                controller.viewPresenter.onRecompileEvent -= (anchor as VFXEditableDataAnchor).OnRecompile;
            }
        }

        public IEnumerable<Port> GetPorts(bool input, bool output)
        {
            if (input)
            {
                foreach (var child in inputContainer)
                {
                    if (child is Port)
                        yield return child as Port;
                }
            }
            if (output)
            {
                foreach (var child in outputContainer)
                {
                    if (child is Port)
                        yield return child as Port;
                }
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
