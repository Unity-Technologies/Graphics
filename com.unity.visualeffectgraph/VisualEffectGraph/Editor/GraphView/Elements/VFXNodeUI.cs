using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.VFX.UI
{
    class VFXNodeUI : Node, IControlledElement<VFXSlotContainerPresenter>, IControlledElement<VFXNodeController>
    {
        VFXSlotContainerPresenter m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        VFXNodeController IControlledElement<VFXNodeController>.controller
        {
            get { return m_Controller; }
            set
            {
                controller = value as VFXSlotContainerPresenter;
            }
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
            else if (e.controller is VFXDataAnchorPresenter)
            {
                RefreshExpandedState();
            }
        }

        protected virtual bool HasPosition()
        {
            return true;
        }

        protected void SyncSettings()
        {
            if (m_SettingsContainer == null && controller.settings != null)
            {
                object settings = controller.settings;

                m_SettingsContainer = new VisualElement { name = "settings" };


                VisualElement inputParent = new VisualElement() { name = "inputAndSettings" };

                mainContainer.Q("contents").Insert(0, m_SettingsContainer);

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
        }

        protected virtual bool syncInput
        {
            get { return true; }
        }

        void SyncAnchors()
        {
            if (syncInput)
                SyncAnchors(controller.inputPorts, inputContainer);
            SyncAnchors(controller.outputPorts, outputContainer);
        }

        void SyncAnchors(ReadOnlyCollection<VFXDataAnchorPresenter> ports, VisualElement container)
        {
            var existingAnchors = container.Children().Cast<VFXDataAnchor>().ToDictionary(t => t.controller, t => t);

            var deletedControllers = existingAnchors.Keys.Except(ports);

            foreach (var deletedController in deletedControllers)
            {
                container.Remove(existingAnchors[deletedController]);
            }

            foreach (var newController in ports.Except(existingAnchors.Keys))
            {
                var newElement = InstantiateDataAnchor(newController);
                (newElement as IControlledElement<VFXDataAnchorPresenter>).controller = newController;

                container.Add(newElement);
            }
        }

        protected virtual void SelfChange()
        {
            if (controller == null)
                return;

            title = controller.title;

            if (HasPosition())
            {
                style.positionType = PositionType.Absolute;
                style.positionLeft = controller.position.x;
                style.positionTop = controller.position.y;
            }

            if (controller.model.superCollapsed)
            {
                AddToClassList("superCollapsed");
            }
            else
            {
                RemoveFromClassList("superCollapsed");
            }

            SyncSettings();
            SyncAnchors();
            RefreshExpandedState();
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

        public virtual void GetPreferedSettingsWidths(ref float labelWidth, ref float controlWidth)
        {
            foreach (var setting in m_Settings)
            {
                float portLabelWidth = setting.GetPreferredLabelWidth() + 5;
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

        public virtual void GetPreferedWidths(ref float labelWidth, ref float controlWidth)
        {
        }

        public virtual void ApplyWidths(float labelWidth, float controlWidth)
        {
        }

        public virtual void ApplySettingsWidths(float labelWidth, float controlWidth)
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
