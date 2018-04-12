using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Profiling;

namespace UnityEditor.VFX.UI
{
    class VFXNodeUI : Node, IControlledElement, ISettableControlledElement<VFXNodeController>, IVFXMovable
    {
        VFXNodeController m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public void OnMoved()
        {
            controller.position = GetPosition().position;
        }

        public VFXNodeController controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != null)
                {
                    m_Controller.UnregisterHandler(this);
                }
                m_Controller = value;
                OnNewController();
                if (m_Controller != null)
                {
                    m_Controller.RegisterHandler(this);
                }
            }
        }


        protected virtual void OnNewController()
        {
            if (controller != null)
                persistenceKey = string.Format("NodeID-{0}", controller.model.GetInstanceID());
        }

        public VisualElement settingsContainer {get; private set; }
        private List<PropertyRM> m_Settings = new List<PropertyRM>();


        public VFXNodeUI(string template) : base(template)
        {
            AddStyleSheetPath("VFXNodeUI");
            Initialize();
        }

        public VFXNodeUI() : base(UXMLHelper.GetUXMLPath("uxml/VFXNode.uxml"))
        {
            AddStyleSheetPath("StyleSheets/GraphView/Node.uss");
            Initialize();
        }

        void Initialize()
        {
            AddStyleSheetPath("VFXNode");
            AddToClassList("VFXNodeUI");
            RegisterCallback<ControllerChangedEvent>(OnChange);
            clippingOptions = ClippingOptions.ClipContents;
        }

        virtual protected void OnChange(ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                Profiler.BeginSample(GetType().Name + "::SelfChange()");
                SelfChange();
                Profiler.EndSample();
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

        protected VisualElement m_SettingsDivider;
        VisualElement           m_Content;


        public virtual bool hasSettingDivider
        {
            get { return true; }
        }

        protected virtual void SyncSettings()
        {
            Profiler.BeginSample("VFXNodeUI.SyncSettings");
            if (settingsContainer == null && controller.settings != null)
            {
                object settings = controller.settings;

                settingsContainer = this.Q("settings");

                m_SettingsDivider = this.Q("settings-divider");

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

                if (m_SettingsDivider != null)
                {
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
            Profiler.EndSample();
        }

        protected virtual bool syncInput
        {
            get { return true; }
        }

        void SyncAnchors()
        {
            Profiler.BeginSample("VFXNodeUI.SyncAnchors");
            if (syncInput)
                SyncAnchors(controller.inputPorts, inputContainer);
            SyncAnchors(controller.outputPorts, outputContainer);
            Profiler.EndSample();
        }

        void SyncAnchors(ReadOnlyCollection<VFXDataAnchorController> ports, VisualElement container)
        {
            var existingAnchors = container.Children().Cast<VFXDataAnchor>().ToDictionary(t => t.controller, t => t);


            Profiler.BeginSample("VFXNodeUI.SyncAnchors Delete");
            var deletedControllers = existingAnchors.Keys.Except(ports).ToArray();

            foreach (var deletedController in deletedControllers)
            {
                container.Remove(existingAnchors[deletedController]);
                existingAnchors.Remove(deletedController);
            }
            Profiler.EndSample();

            Profiler.BeginSample("VFXNodeUI.SyncAnchors New");
            var order = ports.Select((t, i) => new KeyValuePair<VFXDataAnchorController, int>(t, i)).ToDictionary(t => t.Key, t => t.Value);

            var newAnchors = ports.Except(existingAnchors.Keys).ToArray();

            foreach (var newController in newAnchors)
            {
                Profiler.BeginSample("VFXNodeUI.InstantiateDataAnchor");
                var newElement = InstantiateDataAnchor(newController, this);
                Profiler.EndSample();

                (newElement as VFXDataAnchor).controller = newController;

                container.Add(newElement);
                existingAnchors[newController] = newElement;
            }
            Profiler.EndSample();

            Profiler.BeginSample("VFXNodeUI.SyncAnchors Reorder");
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
                    if(container.ElementAt(i) != correctOrder[i])
                        correctOrder[i].PlaceInFront(correctOrder[i - 1]);
                }
            }
            Profiler.EndSample();
        }

        public void ForceUpdate()
        {
            SelfChange();
        }

        protected virtual void SelfChange()
        {
            Profiler.BeginSample("VFXNodeUI.SelfChange");
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
            Profiler.BeginSample("VFXNodeUI.SelfChange The Rest");
            RefreshExpandedState();
            RefreshLayout();
            Profiler.EndSample();
            Profiler.EndSample();
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

                return anchor;
            }
            else
            {
                return VFXOutputDataAnchor.Create(controller, node);
            }
        }

        public IEnumerable<VFXDataAnchor> GetPorts(bool input, bool output)
        {
            if (input)
            {
                foreach (var child in inputContainer)
                {
                    if (child is VFXDataAnchor)
                        yield return child as VFXDataAnchor;
                }
            }
            if (output)
            {
                foreach (var child in outputContainer)
                {
                    if (child is VFXDataAnchor)
                        yield return child as VFXDataAnchor;
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
