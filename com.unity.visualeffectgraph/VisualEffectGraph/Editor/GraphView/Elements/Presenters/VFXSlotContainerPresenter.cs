using System;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class VFXSlotContainerPresenter : VFXNodeController
    {
        public override void Init(VFXModel model, VFXViewPresenter viewPresenter)
        {
            base.Init(model, viewPresenter);

            var settings = model.GetSettings(true);
            m_Settings = new VFXSettingPresenter[settings.Count()];
            int cpt = 0;
            foreach (var setting in settings)
            {
                var settingPresenter = VFXSettingPresenter.CreateInstance<VFXSettingPresenter>();
                settingPresenter.Init(this.slotContainer, setting.Name, setting.FieldType);
                m_Settings[cpt++] = settingPresenter;
            }
            OnInvalidate(model, VFXModel.InvalidationCause.kStructureChanged);
            viewPresenter.AddInvalidateDelegate(model, OnInvalidate);
        }

        public override VFXSlotContainerPresenter slotContainerPresenter { get { return this; } }

        protected void OnInvalidate(VFXModel model, VFXModel.InvalidationCause cause)
        {
            if (model as IVFXSlotContainer == slotContainer && (cause == VFXModel.InvalidationCause.kStructureChanged || cause == VFXModel.InvalidationCause.kSettingChanged))
            {
                var inputs = inputPorts;
                List<VFXDataAnchorPresenter> newAnchors = new List<VFXDataAnchorPresenter>();

                UpdateSlots(newAnchors, slotContainer.inputSlots, true, true);

                m_InputPorts = newAnchors;
                newAnchors = new List<VFXDataAnchorPresenter>();
                UpdateSlots(newAnchors, slotContainer.outputSlots, true, false);
                m_OutputPorts = newAnchors;

                // separate UpdateInfos for the recreation of the list to make the code more reantrant, as UpdateInfos can trigger a compilation, that itself calls OnInvalidate.
                foreach (var anchor in m_InputPorts)
                {
                    (anchor as VFXDataAnchorPresenter).UpdateInfos();
                }
                foreach (var anchor in m_OutputPorts)
                {
                    (anchor as VFXDataAnchorPresenter).UpdateInfos();
                }
            }
        }

        void UpdateSlots(List<VFXDataAnchorPresenter> newAnchors, IEnumerable<VFXSlot> slotList, bool expanded, bool input)
        {
            VFXSlot[] slots = slotList.ToArray();
            {
                foreach (VFXSlot slot in slots)
                {
                    VFXDataAnchorPresenter propPresenter = GetPropertyPresenter(slot, input);

                    if (propPresenter == null)
                    {
                        propPresenter = AddDataAnchor(slot, input);
                    }
                    newAnchors.Add(propPresenter);

                    if (!typeof(ISpaceable).IsAssignableFrom(slot.property.type) || slot.children.Count() != 1)
                    {
                        UpdateSlots(newAnchors, slot.children, expanded && !slot.collapsed, input);
                    }
                    else
                    {
                        VFXSlot firstSlot = slot.children.First();
                        UpdateSlots(newAnchors, firstSlot.children, expanded && !slot.collapsed, input);
                    }
                }
            }
        }

        public VFXDataAnchorPresenter GetPropertyPresenter(VFXSlot slot, bool input)
        {
            VFXDataAnchorPresenter result = null;

            if (input)
                result = inputPorts.Cast<VFXDataAnchorPresenter>().Where(t => t.model == slot).FirstOrDefault();
            else
                result = outputPorts.Cast<VFXDataAnchorPresenter>().Where(t => t.model == slot).FirstOrDefault();

            return result;
        }

        protected virtual VFXDataAnchorPresenter AddDataAnchor(VFXSlot slot, bool input)
        {
            return null;
        }

        public IVFXSlotContainer slotContainer { get { return model as IVFXSlotContainer; } }

        public VFXSettingPresenter[] settings
        {
            get { return m_Settings; }
        }

        public virtual bool enabled
        {
            get { return true; }
        }

        public new bool expanded
        {
            get
            {
                return !slotContainer.collapsed;
            }

            set
            {
                if (value != !slotContainer.collapsed)
                {
                    slotContainer.collapsed = !value;
                }
            }
        }

        public virtual void DrawGizmos(VFXComponent component)
        {
            foreach (VFXDataAnchorPresenter presenter in inputPorts.Cast<VFXDataAnchorPresenter>())
            {
                presenter.DrawGizmo(component);
            }
        }

        [SerializeField]
        private VFXSettingPresenter[] m_Settings;
    }
}
