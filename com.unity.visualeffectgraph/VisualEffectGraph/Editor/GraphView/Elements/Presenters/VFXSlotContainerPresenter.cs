using System;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class VFXSlotContainerPresenter : VFXNodePresenter
    {
        private static bool IsTypeSupported(Type type)
        {
            return type.IsEnum || type == typeof(bool) || type == typeof(string);
        }

        public override void Init(VFXModel model, VFXViewPresenter viewPresenter)
        {
            base.Init(model, viewPresenter);

            var settings = model.GetSettings(true).Where(o => IsTypeSupported(o.FieldType));
            m_Settings = new VFXSettingPresenter[settings.Count()];
            int cpt = 0;
            foreach (var setting in settings)
            {
                VFXSettingPresenter settingPresenter = VFXSettingPresenter.CreateInstance<VFXSettingPresenter>();

                settingPresenter.Init(this.slotContainer, setting.Name, setting.FieldType);
                m_Settings[cpt++] = settingPresenter;
            }
            OnInvalidate(model, VFXModel.InvalidationCause.kStructureChanged);
            viewPresenter.AddInvalidateDelegate(model, OnInvalidate);
        }

        protected void OnInvalidate(VFXModel model, VFXModel.InvalidationCause cause)
        {
            if (model as IVFXSlotContainer == slotContainer && (cause == VFXModel.InvalidationCause.kStructureChanged || cause == VFXModel.InvalidationCause.kSettingChanged))
            {
                var inputs = inputAnchors;
                List<NodeAnchorPresenter> newAnchors = new List<NodeAnchorPresenter>();

                UpdateSlots(newAnchors, slotContainer.inputSlots, true, true);

                foreach (var anchor in inputAnchors.Except(newAnchors).Cast<VFXDataAnchorPresenter>())
                {
                    viewPresenter.UnregisterDataAnchorPresenter(anchor);
                }
                m_InputAnchors = newAnchors;
                newAnchors = new List<NodeAnchorPresenter>();
                UpdateSlots(newAnchors, slotContainer.outputSlots, true, false);

                foreach (var anchor in outputAnchors.Except(newAnchors).Cast<VFXDataAnchorPresenter>())
                {
                    viewPresenter.UnregisterDataAnchorPresenter(anchor);
                }
                m_OutputAnchors = newAnchors;

                // separate UpdateInfos for the recreation of the list to make the code more reantrant, as UpdateInfos can trigger a compilation, that itself calls OnInvalidate.
                foreach (var anchor in m_InputAnchors)
                {
                    (anchor as VFXDataAnchorPresenter).UpdateInfos();
                }
                foreach (var anchor in m_OutputAnchors)
                {
                    (anchor as VFXDataAnchorPresenter).UpdateInfos();
                }
            }
        }

        void UpdateSlots(List<NodeAnchorPresenter> newAnchors, IEnumerable<VFXSlot> slotList, bool expanded, bool input)
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
                    viewPresenter.RegisterDataAnchorPresenter(propPresenter);

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
                result = inputAnchors.Cast<VFXDataAnchorPresenter>().Where(t => t.model == slot).FirstOrDefault();
            else
                result = outputAnchors.Cast<VFXDataAnchorPresenter>().Where(t => t.model == slot).FirstOrDefault();

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

        public bool expanded
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
            foreach (VFXDataAnchorPresenter presenter in inputAnchors.Cast<VFXDataAnchorPresenter>())
            {
                presenter.DrawGizmo(component);
            }
        }

        [SerializeField]
        private VFXSettingPresenter[] m_Settings;
    }
}
