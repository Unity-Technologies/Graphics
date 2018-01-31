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
    class VFXNodeUI : Node, IControlledElement<VFXSlotContainerController>, ISettableControlledElement<VFXNodeController>
    {
        VFXSlotContainerController m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        VFXNodeController ISettableControlledElement<VFXNodeController>.controller
        {
            get { return m_Controller; }
            set { controller = value as VFXSlotContainerController; }
        }
        public override void UpdatePresenterPosition()
        {
            controller.position = GetPosition().position;
        }

        public VFXSlotContainerController controller
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

        public VisualElement settingsContainer {get; private set; }
        private List<PropertyRM> m_Settings = new List<PropertyRM>();
        public VFXNodeUI()
        {
            AddToClassList("VFXNodeUI");
            RegisterCallback<ControllerChangedEvent>(OnChange);
            clippingOptions = ClippingOptions.ClipContents;
        }

        virtual protected void OnChange(ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                SelfChange();
            }
            else if (e.controller is VFXDataAnchorController)
            {
                RefreshExpandedState();
            }
        }

        protected virtual bool HasPosition()
        {
            return true;
        }

        VisualElement m_SettingsDivider;
        VisualElement m_Content;

        protected void SyncSettings()
        {
            if (settingsContainer == null && controller.settings != null)
            {
                object settings = controller.settings;

                settingsContainer = new VisualElement { name = "settings" };
                m_SettingsDivider = new VisualElement() {name = "divider"};
                m_SettingsDivider.AddToClassList("horizontal");

                m_Content = mainContainer.Q("contents");
                m_Content.Insert(0, m_SettingsDivider);

                m_Content.Insert(1, settingsContainer);

                foreach (var setting in controller.settings)
                {
                    AddSetting(setting);
                }
            }
            if (settingsContainer != null)
            {
                var activeSettings = controller.model.GetSettings(false, VFXSettingAttribute.VisibleFlags.InGraph);
                for (int i = 0; i < m_Settings.Count; ++i)
                    m_Settings[i].RemoveFromHierarchy();

                bool hasSettings = false;
                for (int i = 0; i < m_Settings.Count; ++i)
                {
                    PropertyRM prop = m_Settings[i];
                    if (prop != null && activeSettings.Any(s => s.Name == controller.settings[i].name))
                    {
                        hasSettings = true;
                        settingsContainer.Add(prop);
                        prop.Update();
                    }
                }

                if (hasSettings)
                {
                    if (m_SettingsDivider.parent == null)
                    {
                        m_Content.Insert(0, m_SettingsDivider);
                    }
                }
                else
                {
                    if (m_SettingsDivider.parent != null)
                    {
                        m_SettingsDivider.RemoveFromHierarchy();
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

        void SyncAnchors(ReadOnlyCollection<VFXDataAnchorController> ports, VisualElement container)
        {
            var existingAnchors = container.Children().Cast<VFXDataAnchor>().ToDictionary(t => t.controller, t => t);

            var deletedControllers = existingAnchors.Keys.Except(ports).ToArray();

            foreach (var deletedController in deletedControllers)
            {
                container.Remove(existingAnchors[deletedController]);
                existingAnchors.Remove(deletedController);
            }

            var order = ports.Select((t, i) => new KeyValuePair<VFXDataAnchorController, int>(t, i)).ToDictionary(t => t.Key, t => t.Value);

            var newAnchors = ports.Except(existingAnchors.Keys).ToArray();

            foreach (var newController in newAnchors)
            {
                var newElement = InstantiateDataAnchor(newController, this);
                (newElement as VFXDataAnchor).controller = newController;

                container.Add(newElement);
                existingAnchors[newController] = newElement;
            }

            //Reorder anchors.
            if (ports.Count > 0)
            {
                var correctOrder = new VFXDataAnchor[ports.Count];
                foreach (var kv in existingAnchors)
                {
                    correctOrder[order[kv.Key]] = kv.Value;
                }

                correctOrder[0].SendToBack();
                for (int i = 1; i < correctOrder.Length; ++i)
                {
                    correctOrder[i].PlaceInFront(correctOrder[i - 1]);
                }
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

            base.expanded = controller.expanded;

            SyncSettings();
            SyncAnchors();
            RefreshExpandedState();
            RefreshLayout();
        }

        public override bool expanded
        {
            get { return base.expanded; }
            set
            {
                if (base.expanded == value)
                    return;

                base.expanded = value;
                controller.expanded = value;
            }
        }


        public virtual VFXDataAnchor InstantiateDataAnchor(VFXDataAnchorController controller, VFXNodeUI node)
        {
            if (controller.direction == Direction.Input)
            {
                VFXEditableDataAnchor anchor = VFXEditableDataAnchor.Create(controller, node);
                controller.sourceNode.viewController.onRecompileEvent += anchor.OnRecompile;

                return anchor;
            }
            else
            {
                return VFXOutputDataAnchor.Create(controller, node);
            }
        }

        protected override void OnPortRemoved(Port anchor)
        {
            if (anchor is VFXEditableDataAnchor)
            {
                controller.viewController.onRecompileEvent -= (anchor as VFXEditableDataAnchor).OnRecompile;
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
                if (setting.parent == null)
                    continue;
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

        protected void AddSetting(VFXSettingController setting)
        {
            var rm = PropertyRM.Create(setting, 100);
            if (rm != null)
            {
                m_Settings.Add(rm);
            }
            else
            {
                Debug.LogErrorFormat("Cannot create controller for {0}", setting.name);
            }
        }

        public virtual void RefreshLayout()
        {
        }
    }
}
