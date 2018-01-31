using System;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class VFXSlotContainerController : VFXNodeController
    {
        public VFXSlotContainerController(VFXModel model, VFXViewController viewController) : base(model, viewController)
        {
            var settings = model.GetSettings(true);
            m_Settings = new VFXSettingController[settings.Count()];
            int cpt = 0;
            foreach (var setting in settings)
            {
                var settingController = new VFXSettingController();
                settingController.Init(this.slotContainer, setting.Name, setting.FieldType);
                m_Settings[cpt++] = settingController;
            }
        }

        bool m_SyncingSlots;
        public void DataEdgesMightHaveChanged()
        {
            if (viewController != null && !m_SyncingSlots)
            {
                viewController.DataEdgesMightHaveChanged();
            }
        }

        protected override void ModelChanged(UnityEngine.Object obj)
        {
            var inputs = inputPorts;
            List<VFXDataAnchorController> newAnchors = new List<VFXDataAnchorController>();

            m_SyncingSlots = true;
            bool changed = UpdateSlots(newAnchors, slotContainer.inputSlots, true, true);

            foreach (var anchorController in m_InputPorts.Except(newAnchors))
            {
                anchorController.OnDisable();
            }
            m_InputPorts = newAnchors;
            newAnchors = new List<VFXDataAnchorController>();
            changed |= UpdateSlots(newAnchors, slotContainer.outputSlots, true, false);

            foreach (var anchorController in m_OutputPorts.Except(newAnchors))
            {
                anchorController.OnDisable();
            }
            m_OutputPorts = newAnchors;
            m_SyncingSlots = false;


            base.ModelChanged(obj);
            // Call after base.ModelChanged which ensure the ui has been refreshed before we try to create potiential new edges.
            if (changed)
                viewController.DataEdgesMightHaveChanged();
        }

        public override VFXSlotContainerController slotContainerController { get { return this; } }


        bool UpdateSlots(List<VFXDataAnchorController> newAnchors, IEnumerable<VFXSlot> slotList, bool expanded, bool input)
        {
            VFXSlot[] slots = slotList.ToArray();
            bool changed = false;
            foreach (VFXSlot slot in slots)
            {
                VFXDataAnchorController propController = GetPropertyController(slot, input);

                if (propController == null)
                {
                    propController = AddDataAnchor(slot, input, !expanded);
                    changed = true;
                }
                newAnchors.Add(propController);

                if (!VFXDataAnchorController.SlotShouldSkipFirstLevel(slot))
                {
                    changed |= UpdateSlots(newAnchors, slot.children, expanded && propController.expandedSelf, input);
                }
                else
                {
                    VFXSlot firstSlot = slot.children.First();
                    changed |= UpdateSlots(newAnchors, firstSlot.children, expanded && propController.expandedSelf, input);
                }
            }

            return changed;
        }

        public VFXDataAnchorController GetPropertyController(VFXSlot slot, bool input)
        {
            VFXDataAnchorController result = null;

            if (input)
                result = inputPorts.Cast<VFXDataAnchorController>().Where(t => t.model == slot).FirstOrDefault();
            else
                result = outputPorts.Cast<VFXDataAnchorController>().Where(t => t.model == slot).FirstOrDefault();

            return result;
        }

        protected virtual VFXDataAnchorController AddDataAnchor(VFXSlot slot, bool input, bool hidden)
        {
            return null;
        }

        public IVFXSlotContainer slotContainer { get { return model as IVFXSlotContainer; } }

        public VFXSettingController[] settings
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
            foreach (VFXDataAnchorController controller in inputPorts.Cast<VFXDataAnchorController>())
            {
                controller.DrawGizmo(component);
            }
        }

        public override IEnumerable<Controller> allChildren
        {
            get { return base.allChildren.Concat(m_Settings.Cast<Controller>()); }
        }

        private VFXSettingController[] m_Settings;
    }
}
